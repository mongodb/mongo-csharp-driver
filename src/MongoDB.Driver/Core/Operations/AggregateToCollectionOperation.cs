/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class AggregateToCollectionOperation : IWriteOperation<BsonDocument>
    {
        private bool? _allowDiskUse;
        private bool? _bypassDocumentValidation;
        private Collation _collation;
        private readonly CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private readonly DatabaseNamespace _databaseNamespace;
        private BsonValue _hint;
        private BsonDocument _let;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IReadOnlyList<BsonDocument> _pipeline;
        private ReadConcern _readConcern;
        private ReadPreference _readPreference;
        private WriteConcern _writeConcern;

        public AggregateToCollectionOperation(DatabaseNamespace databaseNamespace, IEnumerable<BsonDocument> pipeline, MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _pipeline = Ensure.IsNotNull(pipeline, nameof(pipeline)).ToList();
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));

            EnsureIsOutputToCollectionPipeline();
            _pipeline = SimplifyOutStageIfOutputDatabaseIsSameAsInputDatabase(_pipeline);
        }

        public AggregateToCollectionOperation(CollectionNamespace collectionNamespace, IEnumerable<BsonDocument> pipeline, MessageEncoderSettings messageEncoderSettings)
            : this(Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace)).DatabaseNamespace, pipeline, messageEncoderSettings)
        {
            _collectionNamespace = collectionNamespace;
        }

        public bool? AllowDiskUse
        {
            get { return _allowDiskUse; }
            set { _allowDiskUse = value; }
        }

        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public BsonValue Hint
        {
            get { return _hint; }
            set { _hint = value; }
        }

        public BsonDocument Let
        {
            get { return _let; }
            set { _let = value; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public string OperationName => "aggregate";

        public IReadOnlyList<BsonDocument> Pipeline
        {
            get { return _pipeline; }
        }

        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set
            {
                _readConcern = value;
            }
        }

        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
            set
            {
                _readPreference = value;
            }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        public BsonDocument Execute(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            var mayUseSecondary = new MayUseSecondary(_readPreference);
            using (BeginOperation())
            using (var channelSource = binding.GetWriteChannelSource(operationContext, mayUseSecondary))
            using (var channel = channelSource.GetChannel(operationContext))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session, channel.ConnectionDescription, mayUseSecondary.EffectiveReadPreference);
                return operation.Execute(operationContext, channelBinding);
            }
        }

        public async Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            var mayUseSecondary = new MayUseSecondary(_readPreference);
            using (BeginOperation())
            using (var channelSource = await binding.GetWriteChannelSourceAsync(operationContext, mayUseSecondary).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session, channel.ConnectionDescription, mayUseSecondary.EffectiveReadPreference);
                return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
            }
        }

        public BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription)
        {
            var readConcern = _readConcern != null
                ? ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, _readConcern)
                : null;
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(operationContext, session, _writeConcern);
            return new BsonDocument
            {
                { "aggregate", _collectionNamespace == null ? (BsonValue)1 : _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(_pipeline) },
                { "allowDiskUse", () => _allowDiskUse.Value, _allowDiskUse.HasValue },
                { "bypassDocumentValidation", () => _bypassDocumentValidation.Value, _bypassDocumentValidation.HasValue },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue && !operationContext.IsRootContextTimeoutConfigured() },
                { "collation", () => _collation.ToBsonDocument(), _collation != null },
                { "readConcern", readConcern, readConcern != null },
                { "writeConcern", writeConcern, writeConcern != null },
                { "cursor", new BsonDocument() },
                { "hint", _hint, _hint != null },
                { "let", _let, _let != null },
                { "comment", _comment, _comment != null }
            };
        }

        private EventContext.OperationNameDisposer BeginOperation() => EventContext.BeginOperation(OperationName);

        private WriteCommandOperation<BsonDocument> CreateOperation(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription, ReadPreference effectiveReadPreference)
        {
            var command = CreateCommand(operationContext, session, connectionDescription);
            var operation = new WriteCommandOperation<BsonDocument>(_databaseNamespace, command, BsonDocumentSerializer.Instance, MessageEncoderSettings, OperationName);
            if (effectiveReadPreference != null)
            {
                operation.ReadPreference = effectiveReadPreference;
            }
            return operation;
        }

        private void EnsureIsOutputToCollectionPipeline()
        {
            var lastStage = _pipeline.LastOrDefault();
            var lastStageName = lastStage?.GetElement(0).Name;
            if (lastStage == null || (lastStageName != "$out" && lastStageName != "$merge"))
            {
                throw new ArgumentException("The last stage of the pipeline for an AggregateToCollectionOperation must have a $out or $merge operator.", "pipeline");
            }
        }

        private IReadOnlyList<BsonDocument> SimplifyOutStageIfOutputDatabaseIsSameAsInputDatabase(IReadOnlyList<BsonDocument> pipeline)
        {
            var lastStage = pipeline.Last();
            var lastStageName = lastStage.GetElement(0).Name;
            if (lastStageName == "$out" && lastStage["$out"] is BsonDocument outDocument && !outDocument.Contains("timeseries"))
            {
                if (outDocument.TryGetValue("db", out var db) && db.IsString &&
                    outDocument.TryGetValue("coll", out var coll) && coll.IsString)
                {
                    var outputDatabaseName = db.AsString;
                    if (outputDatabaseName == _databaseNamespace.DatabaseName)
                    {
                        var outputCollectionName = coll.AsString;
                        var simplifiedOutStage = lastStage.Clone().AsBsonDocument;
                        simplifiedOutStage["$out"] = outputCollectionName;

                        var modifiedPipeline = new List<BsonDocument>(pipeline);
                        modifiedPipeline[modifiedPipeline.Count - 1] = simplifiedOutStage;

                        return modifiedPipeline;
                    }
                }
            }

            return pipeline; // unchanged
        }

        internal class MayUseSecondary : IMayUseSecondaryCriteria
        {
            public MayUseSecondary(ReadPreference readPreference)
            {
                ReadPreference = EffectiveReadPreference = readPreference;
            }

            public ReadPreference EffectiveReadPreference { get; set; }
            public ReadPreference ReadPreference { get; }

            public bool CanUseSecondary(ServerDescription server)
            {
                return Feature.AggregateOutOnSecondary.IsSupported(server.MaxWireVersion);
            }
        }
    }
}

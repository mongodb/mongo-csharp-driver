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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class AggregateExplainOperation : IReadOperation<BsonDocument>
    {
        // fields
        private bool? _allowDiskUse;
        private Collation _collation;
        private CollectionNamespace _collectionNamespace;
        private string _comment;
        private BsonValue _hint;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private IReadOnlyList<BsonDocument> _pipeline;

        public AggregateExplainOperation(CollectionNamespace collectionNamespace, IEnumerable<BsonDocument> pipeline, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _pipeline = Ensure.IsNotNull(pipeline, nameof(pipeline)).ToList();
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public bool? AllowDiskUse
        {
            get { return _allowDiskUse; }
            set { _allowDiskUse = value; }
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

        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public BsonValue Hint
        {
            get { return _hint; }
            set { _hint = value; }
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

        public IReadOnlyList<BsonDocument> Pipeline
        {
            get { return _pipeline; }
        }

        internal BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "explain", true },
                { "pipeline", new BsonArray(_pipeline) },
                { "allowDiskUse", () => _allowDiskUse.Value, _allowDiskUse.HasValue },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue },
                { "collation", () => _collation.ToBsonDocument(), _collation != null },
                { "hint", () => _hint, _hint != null },
                { "comment", _comment, _comment != null }
            };
        }

        public BsonDocument Execute(IReadBinding binding, CancellationToken cancellationToken)
        {
            using (var channelSource = binding.GetReadChannelSource(cancellationToken))
            using (var channel = channelSource.GetChannel(cancellationToken))
            using (var channelBinding = new ChannelReadBinding(channelSource.Server, channel, binding.ReadPreference, binding.Session.Fork()))
            {
                var operation = CreateOperation();
                return operation.Execute(channelBinding, cancellationToken);
            }
        }

        public async Task<BsonDocument> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            using (var channelSource = await binding.GetReadChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadBinding(channelSource.Server, channel, binding.ReadPreference, binding.Session.Fork()))
            {
                var operation = CreateOperation();
                return await operation.ExecuteAsync(channelBinding, cancellationToken).ConfigureAwait(false);
            }
        }

        private ReadCommandOperation<BsonDocument> CreateOperation()
        {
            var command = CreateCommand();
            return new ReadCommandOperation<BsonDocument>(
                _collectionNamespace.DatabaseNamespace,
                command,
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings)
            {
                RetryRequested = false
            };
        }
    }
}

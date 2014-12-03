/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class AggregateOperation<TResult> : IReadOperation<IAsyncCursor<TResult>>
    {
        // static fields
        private static readonly SemanticVersion __version26 = new SemanticVersion(2, 6, 0);

        // fields
        private bool? _allowDiskUse;
        private int? _batchSize;
        private readonly CollectionNamespace _collectionNamespace;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IReadOnlyList<BsonDocument> _pipeline;
        private readonly IBsonSerializer<TResult> _resultSerializer;
        private bool? _useCursor;

        // constructors
        public AggregateOperation(CollectionNamespace collectionNamespace, IEnumerable<BsonDocument> pipeline, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
        }

        // properties
        public bool? AllowDiskUse
        {
            get { return _allowDiskUse; }
            set { _allowDiskUse = value; }
        }

        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public IReadOnlyList<BsonDocument> Pipeline
        {
            get { return _pipeline; }
        }

        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        public bool? UseCursor
        {
            get { return _useCursor; }
            set { _useCursor = value; }
        }

        // methods
        public async Task<IAsyncCursor<TResult>> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            EnsureIsReadOnlyPipeline();

            using (var channelSource = await binding.GetReadChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            {
                var command = CreateCommand(channelSource.ServerDescription.Version);

                var serializer = new AggregateResultDeserializer(_resultSerializer);
                var operation = new ReadCommandOperation<AggregateResult>(CollectionNamespace.DatabaseNamespace, command, serializer, MessageEncoderSettings);

                var result = await operation.ExecuteAsync(channelSource, binding.ReadPreference, cancellationToken).ConfigureAwait(false);

                return CreateCursor(channelSource, command, result);
            }
        }

        public IReadOperation<BsonDocument> ToExplainOperation(ExplainVerbosity verbosity)
        {
            return new AggregateExplainOperation(_collectionNamespace, _pipeline, _messageEncoderSettings)
            {
                AllowDiskUse = _allowDiskUse,
                MaxTime = _maxTime
            };
        }

        internal BsonDocument CreateCommand(SemanticVersion serverVersion)
        {
            var command = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(_pipeline) },
                { "allowDiskUse", () => _allowDiskUse.Value, _allowDiskUse.HasValue },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };

            var defaultCursorValue = serverVersion >= __version26;
            if (_useCursor.GetValueOrDefault(defaultCursorValue))
            {
                command["cursor"] = new BsonDocument
                {
                    { "batchSize", () => _batchSize.Value, _batchSize.HasValue }
                };
            }
            return command;
        }

        private AsyncCursor<TResult> CreateCursor(IChannelSourceHandle channelSource, BsonDocument command, AggregateResult result)
        {
            if (_useCursor.GetValueOrDefault(true))
            {
                return CreateCursorFromCursorResult(channelSource, command, result);
            }

            return CreateCursorFromInlineResult(command, result);
        }

        private AsyncCursor<TResult> CreateCursorFromCursorResult(IChannelSourceHandle channelSource, BsonDocument command, AggregateResult result)
        {
            return new AsyncCursor<TResult>(
                channelSource.Fork(),
                CollectionNamespace,
                command,
                result.Results,
                result.CursorId.GetValueOrDefault(0),
                _batchSize ?? 0,
                0, // limit
                _resultSerializer,
                MessageEncoderSettings);
        }

        private AsyncCursor<TResult> CreateCursorFromInlineResult(BsonDocument command, AggregateResult result)
        {
            return new AsyncCursor<TResult>(
                null, // channelSource
                CollectionNamespace,
                command,
                result.Results,
                0,
                0, // batchSize
                0, // limit
                _resultSerializer,
                MessageEncoderSettings);
        }

        private void EnsureIsReadOnlyPipeline()
        {
            if (Pipeline.Any(s => s.GetElement(0).Name == "$out"))
            {
                throw new ArgumentException("The pipeline for an AggregateOperation contains a $out operator. Use AggregateOutputToCollectionOperation instead.");
            }
        }

        private class AggregateResult
        {
            public long? CursorId;
            public TResult[] Results;
        }

        private class AggregateResultDeserializer : SerializerBase<AggregateResult>
        {
            private readonly IBsonSerializer<TResult> _resultSerializer;

            public AggregateResultDeserializer(IBsonSerializer<TResult> resultSerializer)
            {
                _resultSerializer = resultSerializer;
            }

            public override AggregateResult Deserialize(BsonDeserializationContext context)
            {
                var reader = context.Reader;
                AggregateResult result = null;
                reader.ReadStartDocument();
                while (reader.ReadBsonType() != 0)
                {
                    var elementName = reader.ReadName();
                    if (elementName == "cursor")
                    {
                        var cursorDeserializer = new CursorDeserializer(_resultSerializer);
                        result = context.DeserializeWithChildContext(cursorDeserializer);
                    }
                    else if (elementName == "result")
                    {
                        var arraySerializer = new ArraySerializer<TResult>(_resultSerializer);
                        result = new AggregateResult();
                        result.Results = context.DeserializeWithChildContext(arraySerializer);
                    }
                    else
                    {
                        reader.SkipValue();
                    }
                }
                reader.ReadEndDocument();
                return result;
            }
        }

        private class CursorDeserializer : SerializerBase<AggregateResult>
        {
            private readonly IBsonSerializer<TResult> _resultSerializer;

            public CursorDeserializer(IBsonSerializer<TResult> resultSerializer)
            {
                _resultSerializer = resultSerializer;
            }

            public override AggregateResult Deserialize(BsonDeserializationContext context)
            {
                var reader = context.Reader;
                var result = new AggregateResult();
                reader.ReadStartDocument();
                while (reader.ReadBsonType() != 0)
                {
                    var elementName = reader.ReadName();
                    if (elementName == "id")
                    {
                        result.CursorId = context.DeserializeWithChildContext<long>(new Int64Serializer());
                    }
                    else if (elementName == "firstBatch")
                    {
                        var arraySerializer = new ArraySerializer<TResult>(_resultSerializer);
                        result.Results = context.DeserializeWithChildContext(arraySerializer);
                    }
                    else
                    {
                        reader.SkipValue();
                    }
                }
                reader.ReadEndDocument();
                return result;
            }
        }
    }
}

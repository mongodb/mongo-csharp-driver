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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class DistinctOperation<TValue> : IReadOperation<IAsyncCursor<TValue>>
    {
        private Collation _collation;
        private CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private BsonDocument _filter;
        private string _fieldName;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private ReadConcern _readConcern = ReadConcern.Default;
        private bool _retryRequested;
        private IBsonSerializer<TValue> _valueSerializer;

        public DistinctOperation(CollectionNamespace collectionNamespace, IBsonSerializer<TValue> valueSerializer, string fieldName, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _valueSerializer = Ensure.IsNotNull(valueSerializer, nameof(valueSerializer));
            _fieldName = Ensure.IsNotNullOrEmpty(fieldName, nameof(fieldName));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public BsonDocument Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        public string FieldName
        {
            get { return _fieldName; }
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

        public string OperationName => "distinct";

        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set { _readConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        public IBsonSerializer<TValue> ValueSerializer
        {
            get { return _valueSerializer; }
        }

        public IAsyncCursor<TValue> Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = RetryableReadContext.Create(operationContext, binding, _retryRequested))
            {
                var operation = CreateOperation(operationContext, context);
                var result = operation.Execute(operationContext, context);

                binding.Session.SetSnapshotTimeIfNeeded(result.AtClusterTime);

                return new SingleBatchAsyncCursor<TValue>(result.Values);
            }
        }

        public async Task<IAsyncCursor<TValue>> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = await RetryableReadContext.CreateAsync(operationContext, binding, _retryRequested).ConfigureAwait(false))
            {
                var operation = CreateOperation(operationContext, context);
                var result = await operation.ExecuteAsync(operationContext, context).ConfigureAwait(false);

                binding.Session.SetSnapshotTimeIfNeeded(result.AtClusterTime);

                return new SingleBatchAsyncCursor<TValue>(result.Values);
            }
        }

        public BsonDocument CreateCommand(OperationContext operationContext, ICoreSession session, ConnectionDescription connectionDescription)
        {
            var readConcern = ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, _readConcern);
            return new BsonDocument
            {
                { "distinct", _collectionNamespace.CollectionName },
                { "key", _fieldName },
                { "query", _filter, _filter != null },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue && !operationContext.IsRootContextTimeoutConfigured() },
                { "collation", () => _collation.ToBsonDocument(), _collation != null },
                { "comment", _comment, _comment != null },
                { "readConcern", readConcern, readConcern != null }
            };
        }

        private EventContext.OperationNameDisposer BeginOperation() => EventContext.BeginOperation(OperationName);

        private ReadCommandOperation<DistinctResult> CreateOperation(OperationContext operationContext, RetryableReadContext context)
        {
            var command = CreateCommand(operationContext, context.Binding.Session, context.Channel.ConnectionDescription);
            var serializer = new DistinctResultDeserializer(_valueSerializer);

            return new ReadCommandOperation<DistinctResult>(_collectionNamespace.DatabaseNamespace, command, serializer, _messageEncoderSettings)
            {
                RetryRequested = _retryRequested // might be overridden by retryable read context
            };
        }

        private sealed class DistinctResult
        {
            public BsonTimestamp AtClusterTime;
            public TValue[] Values;
        }

        private sealed class DistinctResultDeserializer : SerializerBase<DistinctResult>
        {
            private readonly IBsonSerializer<TValue> _valueSerializer;

            public DistinctResultDeserializer(IBsonSerializer<TValue> valuesSerializer)
            {
                _valueSerializer = valuesSerializer;
            }

            public override DistinctResult Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var reader = context.Reader;
                var result = new DistinctResult();
                reader.ReadStartDocument();
                while (reader.ReadBsonType() != 0)
                {
                    var elementName = reader.ReadName();
                    switch (elementName)
                    {
                        case "atClusterTime":
                            result.AtClusterTime = BsonTimestampSerializer.Instance.Deserialize(context);
                            break;

                        case "values":
                            var arraySerializer = new ArraySerializer<TValue>(_valueSerializer);
                            result.Values = arraySerializer.Deserialize(context);
                            break;

                        default:
                            reader.SkipValue();
                            break;
                    }
                }
                reader.ReadEndDocument();
                return result;
            }

            public override bool Equals(object obj)
            {
                if (object.ReferenceEquals(obj, null)) { return false; }
                if (object.ReferenceEquals(this, obj)) { return true; }
                return
                    base.Equals(obj) &&
                    obj is DistinctResultDeserializer other &&
                    object.Equals(_valueSerializer, other._valueSerializer);
            }

            public override int GetHashCode() => 0;
        }
    }
}

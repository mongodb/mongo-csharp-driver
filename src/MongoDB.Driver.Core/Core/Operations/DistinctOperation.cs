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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class DistinctOperation<TValue> : IReadOperation<IReadOnlyList<TValue>>
    {
        // fields
        private CollectionNamespace _collectionNamespace;
        private BsonDocument _criteria;
        private string _fieldName;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private IBsonSerializer<TValue> _valueSerializer;

        // constructors
        public DistinctOperation(CollectionNamespace collectionNamespace, IBsonSerializer<TValue> valueSerializer, string fieldName, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _valueSerializer = Ensure.IsNotNull(valueSerializer, "valueSerializer");
            _fieldName = Ensure.IsNotNullOrEmpty(fieldName, "fieldName");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
            set { _collectionNamespace = Ensure.IsNotNull(value, "value"); }
        }

        public BsonDocument Criteria
        {
            get { return _criteria; }
            set { _criteria = value; }
        }

        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "distinct", _collectionNamespace.CollectionName },
                { "key", _fieldName },
                { "query", _criteria, _criteria != null },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
           };
        }

        public async Task<IReadOnlyList<TValue>> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var valueArraySerializer = new ArraySerializer<TValue>(_valueSerializer);
            var resultSerializer = new ElementDeserializer<TValue[]>("values", valueArraySerializer);
            var operation = new ReadCommandOperation<TValue[]>(_collectionNamespace.DatabaseNamespace, command, resultSerializer, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
        }

        public async Task<BsonDocument> ExecuteCommandAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new ReadCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
        }
    }
}

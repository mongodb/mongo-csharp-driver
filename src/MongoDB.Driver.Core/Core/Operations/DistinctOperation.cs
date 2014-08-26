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
    public class DistinctOperation : DistinctOperation<BsonValue>
    {
        // constructors
        public DistinctOperation(string databaseName, string collectionName, string key, BsonDocument query, MessageEncoderSettings messageEncoderSettings)
            : base(databaseName, collectionName, BsonValueSerializer.Instance, key, query, messageEncoderSettings)
        {
        }
    }

    public class DistinctOperation<TValue> : IReadOperation<IEnumerable<TValue>>
    {
        // fields
        private string _collectionName;
        private string _databaseName;
        private string _key;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private BsonDocument _query;
        private IBsonSerializer<TValue> _valueSerializer;

        // constructors
        public DistinctOperation(string databaseName, string collectionName, IBsonSerializer<TValue> valueSerializer, string key, BsonDocument query, MessageEncoderSettings messageEncoderSettings)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _valueSerializer = Ensure.IsNotNull(valueSerializer, "valueSerializer");
            _key = Ensure.IsNotNullOrEmpty(key, "key");
            _query = query;
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
            set { _collectionName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public string Key
        {
            get { return _key; }
            set { _key = Ensure.IsNotNullOrEmpty(value, "value"); }
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

        public BsonDocument Query
        {
            get { return _query; }
            set { _query = value; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "distinct", _collectionName },
                { "key", _key },
                { "query", _query, _query != null },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
           };
        }

        public async Task<IEnumerable<TValue>> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var valueArraySerializer = new ArraySerializer<TValue>(_valueSerializer);
            var resultSerializer = new ElementDeserializer<TValue[]>("values", valueArraySerializer);
            var operation = new ReadCommandOperation<TValue[]>(_databaseName, command, resultSerializer, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public async Task<BsonDocument> ExecuteCommandAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new ReadCommandOperation(_databaseName, command, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}

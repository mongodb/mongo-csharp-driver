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

namespace MongoDB.Driver.Core.Operations
{
    public class DistinctOperation : DistinctOperation<BsonValue>
    {
        // constructors
        public DistinctOperation(string databaseName, string collectionName, string key, BsonDocument query = null)
            : base(databaseName, collectionName, BsonValueSerializer.Instance, key, query)
        {
        }
    }

    public class DistinctOperation<TValue> : IReadOperation<IEnumerable<TValue>>
    {
        // fields
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly string _key;
        private readonly BsonDocument _query;
        private readonly IBsonSerializer<TValue> _valueSerializer;

        // constructors
        public DistinctOperation(string databaseName, string collectionName, IBsonSerializer<TValue> valueSerializer, string key, BsonDocument query = null)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _valueSerializer = Ensure.IsNotNull(valueSerializer, "valueSerializer");
            _key = Ensure.IsNotNull(key, "key");
            _query = query ?? new BsonDocument();
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public string Key
        {
            get { return _key; }
        }

        public BsonDocument Query
        {
            get { return _query; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "distinct", _collectionName },
                { "key", _key },
                { "query", _query, _query != null }
            };
        }

        public async Task<IEnumerable<TValue>> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var valueArraySerializer = new ArraySerializer<TValue>(_valueSerializer);
            var resultSerializer = new ElementDeserializer<TValue[]>("values", valueArraySerializer);
            var operation = new ReadCommandOperation<TValue[]>(_databaseName, command, resultSerializer);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public async Task<BsonDocument> ExecuteCommandAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new ReadCommandOperation(_databaseName, command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public DistinctOperation<TValue> WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_collectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public DistinctOperation<TValue> WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_collectionName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public DistinctOperation<TValue> WithKey(string value)
        {
            Ensure.IsNotNull(value, "value");
            return (_collectionName == value) ? this : new Builder(this) { _key = value }.Build();
        }

        public DistinctOperation<TValue> WithQuery(BsonDocument value)
        {
            return object.Equals(_query, value) ? this : new Builder(this) { _query = value }.Build();
        }

        public DistinctOperation<TValue> WithValueSerializer(IBsonSerializer<TValue> value)
        {
            return object.ReferenceEquals(_valueSerializer, value) ? this : new Builder(this) { _valueSerializer = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public string _collectionName;
            public string _databaseName;
            public string _key;
            public BsonDocument _query;
            public IBsonSerializer<TValue> _valueSerializer;

            // constructors
            public Builder(DistinctOperation<TValue> other)
            {
                _collectionName = other._collectionName;
                _databaseName = other._databaseName;
                _key = other._key;
                _query = other._query;
                _valueSerializer = other._valueSerializer;
            }

            // methods
            public DistinctOperation<TValue> Build()
            {
                return new DistinctOperation<TValue>(
                    _databaseName,
                    _collectionName,
                    _valueSerializer,
                    _key,
                    _query);
            }
        }
    }
}

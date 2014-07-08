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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class CountOperation : IReadOperation<long>
    {
        // fields
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly string _hint;
        private readonly long? _limit;
        private readonly TimeSpan? _maxTime;
        private readonly BsonDocument _query;
        private readonly long? _skip;

        // constructors
        public CountOperation(string databaseName, string collectionName, BsonDocument query = null)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _query = query ?? new BsonDocument();
        }

        private CountOperation(
            string collectionName,
            string databaseName,
            string hint,
            long? limit,
            TimeSpan? maxTime,
            BsonDocument query,
            long? skip)
        {
            _collectionName = collectionName;
            _databaseName = databaseName;
            _hint = hint;
            _limit = limit;
            _maxTime = maxTime;
            _query = query;
            _skip = skip;
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

        public string Hint
        {
            get { return _hint; }
        }

        public long? Limit
        {
            get { return _limit; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
        }

        public BsonDocument Query
        {
            get { return _query; }
        }

        public long? Skip
        {
            get { return _skip; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "count", _collectionName },
                { "query", _query, _query != null },
                { "limit", () => _limit.Value, _limit.HasValue },
                { "skip", () => _skip.Value, _skip.HasValue },
                { "hint", _hint, _hint != null },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
        }

        public async Task<long> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var document = await ExecuteCommandAsync(binding, timeout, cancellationToken);
            return document["n"].ToInt64();
        }

        public async Task<BsonDocument> ExecuteCommandAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new ReadCommandOperation(_databaseName, command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public CountOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return _collectionName == value ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public CountOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return _databaseName == value ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public CountOperation WithHint(BsonDocument value)
        {
            Ensure.IsNotNull(value, "value");
            var indexName = CreateIndexOperation.GetDefaultIndexName(value);
            return WithHint(indexName);
        }

        public CountOperation WithHint(string value)
        {
            return _hint == value ? this : new Builder(this) { _hint = value }.Build();
        }

        public CountOperation WithLimit(long? value)
        {
            return _limit == value ? this : new Builder(this) { _limit = value }.Build();
        }

        public CountOperation WithMaxTime(TimeSpan? value)
        {
            return _maxTime == value ? this : new Builder(this) { _maxTime = value }.Build();
        }

        public CountOperation WithQuery(BsonDocument value)
        {
            return _query == value ? this : new Builder(this) { _query = value ?? new BsonDocument() }.Build();
        }

        public CountOperation WithSkip(long? value)
        {
            return _skip == value ? this : new Builder(this) { _skip = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public string _collectionName;
            public string _databaseName;
            public string _hint;
            public long? _limit;
            public TimeSpan? _maxTime;
            public BsonDocument _query;
            public long? _skip;

            // constructors
            public Builder(CountOperation other)
            {
                _collectionName = other._collectionName;
                _databaseName = other._databaseName;
                _hint = other._hint;
                _limit = other._limit;
                _maxTime = other._maxTime;
                _query = other._query;
                _skip = other._skip;
            }

            // methods
            public CountOperation Build()
            {
                return new CountOperation(
                    _collectionName,
                    _databaseName,
                    _hint,
                    _limit,
                    _maxTime,
                    _query,
                    _skip);
            }
        }
    }
}

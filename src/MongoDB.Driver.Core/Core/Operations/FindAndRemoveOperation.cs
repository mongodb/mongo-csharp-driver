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
    public class FindAndRemoveOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly BsonDocument _fields;
        private TimeSpan? _maxTime;
        private readonly BsonDocument _query;
        private readonly BsonDocument _sort;

        // constructors
        public FindAndRemoveOperation(
            string databaseName,
            string collectionName,
            BsonDocument query,
            BsonDocument sort = null)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _query = Ensure.IsNotNull(query, "query");
            _sort = sort; // can be null
        }

        private FindAndRemoveOperation(
            string collectionName,
            string databaseName,
            BsonDocument fields,
            BsonDocument query,
            BsonDocument sort)
        {
            _collectionName = collectionName;
            _databaseName = databaseName;
            _fields = fields;
            _query = query;
            _sort = sort;
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

        public BsonDocument Fields
        {
            get { return _fields; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        public BsonDocument Query
        {
            get { return _query; }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "findAndModify", _collectionName },
                { "query", _query, _query != null },
                { "sort", _sort, _sort != null },
                { "remove", true },
                { "field", _fields, _fields != null },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var result = await ExecuteCommandAsync(binding, timeout, cancellationToken);
            return result["value"].AsBsonDocument;
        }

        public async Task<BsonDocument> ExecuteCommandAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation(_databaseName, command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
        public FindAndRemoveOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_collectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public FindAndRemoveOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_databaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public FindAndRemoveOperation WithFields(BsonDocument value)
        {
            return object.ReferenceEquals(_fields, value) ? this : new Builder(this) { _fields = value }.Build();
        }

        public FindAndRemoveOperation WithQuery(BsonDocument value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(_query, value) ? this : new Builder(this) { _query = value }.Build();
        }

        public FindAndRemoveOperation WithSort(BsonDocument value)
        {
            return object.ReferenceEquals(_sort, value) ? this : new Builder(this) { _sort = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public string _collectionName;
            public string _databaseName;
            public BsonDocument _fields;
            public BsonDocument _query;
            public BsonDocument _sort;

            // constructors
            public Builder(FindAndRemoveOperation other)
            {
                _collectionName = other.CollectionName;
                _databaseName = other.DatabaseName;
                _fields = other.Fields;
                _query = other.Query;
                _sort = other.Sort;
            }

            // methods
            public FindAndRemoveOperation Build()
            {
                return new FindAndRemoveOperation(
                    _collectionName,
                    _databaseName,
                    _fields,
                    _query,
                    _sort);
            }
        }
    }
}

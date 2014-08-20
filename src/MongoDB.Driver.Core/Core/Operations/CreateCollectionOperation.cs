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
    public class CreateCollectionOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private readonly bool? _autoIndexId;
        private readonly bool? _capped;
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly long? _maxDocuments;
        private readonly long? _maxSize;

        // constructors
        public CreateCollectionOperation(
            string databaseName,
            string collectionName)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
        }

        private CreateCollectionOperation(
            bool? autoIndexId,
            bool? capped,
            string collectionName,
            string databaseName,
            long? maxDocuments,
            long? maxSize)
        {
            _autoIndexId = autoIndexId;
            _capped = capped;
            _collectionName = collectionName;
            _databaseName = databaseName;
            _maxDocuments = maxDocuments;
            _maxSize = maxSize;
        }

        // properties
        public bool? AutoIndexId
        {
            get { return _autoIndexId; }
        }

        public bool? Capped
        {
            get { return _capped; }
        }

        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public long? MaxDocuments
        {
            get { return _maxDocuments; }
        }

        public long? MaxSize
        {
            get { return _maxSize; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "create", _collectionName },
                { "capped", () => _capped.Value, _capped.HasValue },
                { "autoIndexID", () => _autoIndexId.Value, _autoIndexId.HasValue },
                { "size", () => _maxSize.Value, _maxSize.HasValue },
                { "max", () => _maxDocuments.Value, _maxDocuments.HasValue }
            };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation(_databaseName, command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public CreateCollectionOperation WithAutoIndexId(bool? value)
        {
            return (_autoIndexId == value) ? this : new Builder(this) { _autoIndexId = value }.Build();
        }

        public CreateCollectionOperation WithCapped(bool? value)
        {
            return (_capped == value) ? this : new Builder(this) { _capped = value }.Build();
        }

        public CreateCollectionOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_collectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public CreateCollectionOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_databaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public CreateCollectionOperation WithMaxDocuments(long? value)
        {
            Ensure.IsNullOrGreaterThanZero(value, "value");
            return (_maxDocuments == value) ? this : new Builder(this) { _maxDocuments = value }.Build();
        }

        public CreateCollectionOperation WithMaxSize(long? value)
        {
            Ensure.IsNullOrGreaterThanZero(value, "value");
            return (_maxSize == value) ? this : new Builder(this) { _maxSize = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public bool? _autoIndexId;
            public bool? _capped;
            public string _collectionName;
            public string _databaseName;
            public long? _maxDocuments;
            public long? _maxSize;

            // constructors
            public Builder(CreateCollectionOperation other)
            {
                _autoIndexId = other.AutoIndexId;
                _capped = other.Capped;
                _collectionName = other.CollectionName;
                _databaseName = other.DatabaseName;
                _maxDocuments = other.MaxDocuments;
                _maxSize = other.MaxSize;
            }

            // methods
            public CreateCollectionOperation Build()
            {
                return new CreateCollectionOperation(
                    _autoIndexId,
                    _capped,
                    _collectionName,
                    _databaseName,
                    _maxDocuments,
                    _maxSize);
            }
        }
    }
}

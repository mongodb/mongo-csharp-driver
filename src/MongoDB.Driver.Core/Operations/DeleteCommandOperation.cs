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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class DeleteCommandOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly IEnumerable<DeleteRequest> _deletes;
        private readonly int? _maxBatchCount;
        private readonly int? _maxBatchSize;
        private readonly int? _maxDocumentSize;
        private readonly int? _maxWireDocumentSize;
        private readonly bool _ordered = true;
        private readonly WriteConcern _writeConcern;

        // constructors
        public DeleteCommandOperation(
            string databaseName,
            string collectionName,
            IEnumerable<DeleteRequest> deletes)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _deletes = Ensure.IsNotNull(deletes, "deletes");
            _writeConcern = WriteConcern.Acknowledged;
        }

        private DeleteCommandOperation(
            string collectionName,
            string databaseName,
            IEnumerable<DeleteRequest> deletes,
            int? maxBatchCount,
            int? maxBatchSize,
            int? maxDocumentSize,
            int? maxWireDocumentSize,
            bool ordered,
            WriteConcern writeConcern)
        {
            _collectionName = collectionName;
            _databaseName = databaseName;
            _deletes = deletes;
            _maxBatchCount = maxBatchCount;
            _maxBatchSize = maxBatchSize;
            _maxDocumentSize = maxDocumentSize;
            _maxWireDocumentSize = maxWireDocumentSize;
            _ordered = ordered;
            _writeConcern = writeConcern;
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

        public IEnumerable<DeleteRequest> Deletes
        {
            get { return _deletes; }
        }

        public int? MaxBatchCount
        {
            get { return _maxBatchCount; }
        }

        public int? MaxBatchSize
        {
            get { return _maxBatchSize; }
        }

        public int? MaxDocumentSize
        {
            get { return _maxDocumentSize; }
        }

        public int? MaxWireDocumentSize
        {
            get { return _maxWireDocumentSize; }
        }

        public bool Ordered
        {
            get { return _ordered; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
        }

        // methods
        public Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            throw new NotImplementedException();
        }

        public DeleteCommandOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return _collectionName == value ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public DeleteCommandOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return _databaseName == value ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public DeleteCommandOperation WithDeletes(IEnumerable<DeleteRequest> value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(_deletes, value) ? this : new Builder(this) { _deletes = value }.Build();
        }

        public DeleteCommandOperation WithMaxBatchCount(int? value)
        {
            Ensure.IsNullOrGreaterThanZero(value, "value");
            return _maxBatchCount == value ? this : new Builder(this) { _maxBatchCount = value }.Build();
        }

        public DeleteCommandOperation WithMaxBatchSize(int? value)
        {
            Ensure.IsNullOrGreaterThanZero(value, "value");
            return _maxBatchSize == value ? this : new Builder(this) { _maxBatchSize = value }.Build();
        }

        public DeleteCommandOperation WithMaxDocumentSize(int? value)
        {
            Ensure.IsNullOrGreaterThanZero(value, "value");
            return _maxDocumentSize == value ? this : new Builder(this) { _maxDocumentSize = value }.Build();
        }

        public DeleteCommandOperation WithMaxWireDocumentSize(int? value)
        {
            Ensure.IsNullOrGreaterThanZero(value, "value");
            return _maxWireDocumentSize == value ? this : new Builder(this) { _maxWireDocumentSize = value }.Build();
        }

        public DeleteCommandOperation WithOrdered(bool value)
        {
            return _ordered == value ? this : new Builder(this) { _ordered = value }.Build();
        }

        public DeleteCommandOperation WithWriteConcern(WriteConcern value)
        {
            Ensure.IsNotNull(value, "value");
            return object.Equals(_writeConcern, value) ? this : new Builder(this) { _writeConcern = value }.Build();
        }

        // nested typed
        private struct Builder
        {
            // fields
            public string _collectionName;
            public string _databaseName;
            public IEnumerable<DeleteRequest> _deletes;
            public int? _maxBatchCount;
            public int? _maxBatchSize;
            public int? _maxDocumentSize;
            public int? _maxWireDocumentSize;
            public bool _ordered;
            public WriteConcern _writeConcern;

            // constructors
            public Builder(DeleteCommandOperation other)
            {
                _collectionName = other._collectionName;
                _databaseName = other._databaseName;
                _deletes = other._deletes;
                _maxBatchCount = other._maxBatchCount;
                _maxBatchSize = other._maxBatchSize;
                _maxDocumentSize = other._maxDocumentSize;
                _maxWireDocumentSize = other._maxWireDocumentSize;
                _ordered = other._ordered;
                _writeConcern = other._writeConcern;
            }

            // methods
            public DeleteCommandOperation Build()
            {
                return new DeleteCommandOperation(
                    _collectionName,
                    _databaseName,
                    _deletes,
                    _maxBatchCount,
                    _maxBatchSize,
                    _maxDocumentSize,
                    _maxWireDocumentSize,
                    _ordered,
                    _writeConcern);
            }
        }
    }
}

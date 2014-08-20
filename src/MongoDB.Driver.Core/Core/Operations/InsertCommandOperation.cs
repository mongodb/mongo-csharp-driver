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
    public class InsertCommandOperation : InsertCommandOperation<BsonDocument>
    {
        // constructors
        public InsertCommandOperation(
            string databaseName,
            string collectionName,
            IEnumerable<BsonDocument> documents)
            : base(databaseName, collectionName, BsonDocumentSerializer.Instance, documents)
        {
        }
    }

    public class InsertCommandOperation<TDocument> : IWriteOperation<BsonDocument>
    {
        // fields
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly IEnumerable<TDocument> _documents;
        private readonly int? _maxBatchCount;
        private readonly int? _maxBatchSize;
        private readonly int? _maxDocumentSize;
        private readonly int? _maxWireDocumentSize;
        private readonly bool _ordered = true;
        private readonly IBsonSerializer<TDocument> _serializer;
        private readonly WriteConcern _writeConcern;

        // constructors
        public InsertCommandOperation(
            string databaseName,
            string collectionName,
            IBsonSerializer<TDocument> serializer,
            IEnumerable<TDocument> documents)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _documents = Ensure.IsNotNull(documents, "documents");
            _writeConcern = WriteConcern.Acknowledged;
        }

        private InsertCommandOperation(
            string collectionName,
            string databaseName,
            IEnumerable<TDocument> documents,
            int? maxBatchCount,
            int? maxBatchSize,
            int? maxDocumentSize,
            int? maxWireDocumentSize,
            bool ordered,
            IBsonSerializer<TDocument> serializer,
            WriteConcern writeConcern)
        {
            _collectionName = collectionName;
            _databaseName = databaseName;
            _documents = documents;
            _maxBatchCount = maxBatchCount;
            _maxBatchSize = maxBatchSize;
            _maxDocumentSize = maxDocumentSize;
            _maxWireDocumentSize = maxWireDocumentSize;
            _ordered = ordered;
            _serializer = serializer;
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

        public IEnumerable<TDocument> Documents
        {
            get { return _documents; }
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

        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
        }

        // methods
        public Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public InsertCommandOperation<TDocument> WithCollectionName(string value)
        {
            return (_collectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public InsertCommandOperation<TDocument> WithDatabaseName(string value)
        {
            return (_databaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public InsertCommandOperation<TDocument> WithDocuments(IEnumerable<TDocument> value)
        {
            return object.ReferenceEquals(_documents, value) ? this : new Builder(this) { _documents = value }.Build();
        }

        public InsertCommandOperation<TDocument> WithMaxBatchCount(int? value)
        {
            return (_maxBatchCount == value) ? this : new Builder(this) { _maxBatchCount = value }.Build();
        }

        public InsertCommandOperation<TDocument> WithMaxBatchSize(int? value)
        {
            return (_maxBatchSize == value) ? this : new Builder(this) { _maxBatchSize = value }.Build();
        }

        public InsertCommandOperation<TDocument> WithMaxDocumentSize(int? value)
        {
            return (_maxDocumentSize == value) ? this : new Builder(this) { _maxDocumentSize = value }.Build();
        }

        public InsertCommandOperation<TDocument> WithMaxWireDocumentSize(int? value)
        {
            return (_maxWireDocumentSize == value) ? this : new Builder(this) { _maxWireDocumentSize = value }.Build();
        }

        public InsertCommandOperation<TDocument> WithOrdered(bool value)
        {
            return (_ordered == value) ? this : new Builder(this) { _ordered = value }.Build();
        }

        public InsertCommandOperation<TDocument> WithSerializer(IBsonSerializer<TDocument> value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(_serializer, value) ? this : new Builder(this) { _serializer = value }.Build();
        }

        public InsertCommandOperation<TOther> WithSerializer<TOther>(IBsonSerializer<TOther> value)
        {
            Ensure.IsNotNull(value, "value");
            IEnumerable<TOther> documents = null;
            return new InsertCommandOperation<TOther>(
                _collectionName,
                _databaseName,
                documents,
                _maxBatchCount,
                _maxBatchSize,
                _maxDocumentSize,
                _maxWireDocumentSize,
                _ordered,
                value,
                _writeConcern);
        }

        public InsertCommandOperation<TDocument> WithWriteConcern(WriteConcern value)
        {
            Ensure.IsNotNull(value, "value");
            return object.Equals(_writeConcern, value) ? this : new Builder(this) { _writeConcern = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public string _collectionName;
            public string _databaseName;
            public IEnumerable<TDocument> _documents;
            public int? _maxBatchCount;
            public int? _maxBatchSize;
            public int? _maxDocumentSize;
            public int? _maxWireDocumentSize;
            public bool _ordered;
            public IBsonSerializer<TDocument> _serializer;
            public WriteConcern _writeConcern;

            // constructors
            public Builder(InsertCommandOperation<TDocument> other)
            {
                _collectionName = other._collectionName;
                _databaseName = other._databaseName;
                _documents = other._documents;
                _maxBatchCount = other.MaxBatchCount;
                _maxBatchSize = other.MaxBatchSize;
                _maxDocumentSize = other.MaxDocumentSize;
                _maxWireDocumentSize = other.MaxWireDocumentSize;
                _ordered = other.Ordered;
                _serializer = other._serializer;
                _writeConcern = other.WriteConcern;
            }

            // methods
            public InsertCommandOperation<TDocument> Build()
            {
                return new InsertCommandOperation<TDocument>(
                    _collectionName,
                    _databaseName,
                    _documents,
                    _maxBatchCount,
                    _maxBatchSize,
                    _maxDocumentSize,
                    _maxWireDocumentSize,
                    _ordered,
                    _serializer,
                    _writeConcern);
            }
        }
    }
}

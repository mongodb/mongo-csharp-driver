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
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Operations
{
    public class InsertOpcodeOperation : InsertOpcodeOperation<BsonDocument>
    {
        // constructors
        public InsertOpcodeOperation(
            string databaseName,
            string collectionName,
            BatchableSource<BsonDocument> documentSource)
            : base(databaseName, collectionName, BsonDocumentSerializer.Instance, documentSource)
        {
        }
    }

    public class InsertOpcodeOperation<TDocument> : IWriteOperation<BsonDocument>
    {
        // fields
        private readonly string _collectionName;
        private readonly bool _continueOnError;
        private readonly string _databaseName;
        private readonly BatchableSource<TDocument> _documentSource;
        private readonly int? _maxBatchCount;
        private readonly int? _maxDocumentSize;
        private readonly int? _maxMessageSize;
        private readonly IBsonSerializer<TDocument> _serializer;
        private readonly WriteConcern _writeConcern;

        // constructors
        public InsertOpcodeOperation(
            string databaseName,
            string collectionName,
            IBsonSerializer<TDocument> serializer,
            BatchableSource<TDocument> documentSource)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _documentSource = Ensure.IsNotNull(documentSource, "documentSource");
            _writeConcern = WriteConcern.Acknowledged;
        }

        private InsertOpcodeOperation(
            string collectionName,
            bool continueOnError,
            string databaseName,
            BatchableSource<TDocument> documentSource,
            int? maxBatchCount,
            int? maxDocumentSize,
            int? maxMessageSize,
            IBsonSerializer<TDocument> serializer,
            WriteConcern writeConcern)
        {
            _collectionName = collectionName;
            _continueOnError = continueOnError;
            _databaseName = databaseName;
            _documentSource = documentSource;
            _maxBatchCount = maxBatchCount;
            _maxDocumentSize = maxDocumentSize;
            _maxMessageSize = maxMessageSize;
            _serializer = serializer;
            _writeConcern = writeConcern;
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
        }

        public bool ContinueOnError
        {
            get { return _continueOnError; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public BatchableSource<TDocument> DocumentSource
        {
            get { return _documentSource; }
        }

        public int? MaxBatchCount
        {
            get { return _maxBatchCount; }
        }

        public int? MaxDocumentSize
        {
            get { return _maxDocumentSize; }
        }

        public int? MaxMessageSize
        {
            get { return _maxMessageSize; }
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
        private InsertWireProtocol<TDocument> CreateProtocol()
        {
            return new InsertWireProtocol<TDocument>(
                _databaseName,
                _collectionName,
                _writeConcern,
                _serializer,
                _documentSource,
                _maxBatchCount,
                _maxMessageSize,
                _continueOnError);
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var protocol = CreateProtocol();
            return await protocol.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public InsertOpcodeOperation<TDocument> WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_collectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public InsertOpcodeOperation<TDocument> WithContinueOnError(bool value)
        {
            return (_continueOnError == value) ? this : new Builder(this) { _continueOnError = value }.Build();
        }

        public InsertOpcodeOperation<TDocument> WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_databaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public InsertOpcodeOperation<TDocument> WithDocuments(BatchableSource<TDocument> value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(_documentSource, value) ? this : new Builder(this) { _documentSource = value }.Build();
        }

        public InsertOpcodeOperation<TDocument> WithMaxBatchCount(int? value)
        {
            Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value");
            return (_maxBatchCount == value) ? this : new Builder(this) { _maxBatchCount = value }.Build();
        }

        public InsertOpcodeOperation<TDocument> WithMaxDocumentSize(int? value)
        {
            Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value");
            return (_maxDocumentSize == value) ? this : new Builder(this) { _maxDocumentSize = value }.Build();
        }

        public InsertOpcodeOperation<TDocument> WithMaxMessageSize(int? value)
        {
            Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value");
            return (_maxMessageSize == value) ? this : new Builder(this) { _maxMessageSize = value }.Build();
        }

        public InsertOpcodeOperation<TDocument> WithSerializer(IBsonSerializer<TDocument> value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(_serializer, value) ? this : new Builder(this) { _serializer = value }.Build();
        }

        public InsertOpcodeOperation<TDocument> WithWriteConcern(WriteConcern value)
        {
            Ensure.IsNotNull(value, "value");
            return object.Equals(_writeConcern, value) ? this : new Builder(this) { _writeConcern = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public string _collectionName;
            public bool _continueOnError;
            public string _databaseName;
            public BatchableSource<TDocument> _documentSource;
            public int? _maxBatchCount;
            public int? _maxDocumentSize;
            public int? _maxMessageSize;
            public IBsonSerializer<TDocument> _serializer;
            public WriteConcern _writeConcern;

            // constructors
            public Builder(InsertOpcodeOperation<TDocument> original)
            {
                _collectionName = original._collectionName;
                _continueOnError = original._continueOnError;
                _databaseName = original._databaseName;
                _documentSource = original._documentSource;
                _maxBatchCount = original.MaxBatchCount;
                _maxDocumentSize = original.MaxDocumentSize;
                _maxMessageSize = original.MaxMessageSize;
                _serializer = original._serializer;
                _writeConcern = original.WriteConcern;
            }

            // methods
            public InsertOpcodeOperation<TDocument> Build()
            {
                return new InsertOpcodeOperation<TDocument>(
                    _collectionName,
                    _continueOnError,
                    _databaseName,
                    _documentSource,
                    _maxBatchCount,
                    _maxDocumentSize,
                    _maxMessageSize,
                    _serializer,
                    _writeConcern);
            }
        }
    }
}

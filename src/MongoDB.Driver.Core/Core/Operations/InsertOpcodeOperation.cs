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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
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
        private string _collectionName;
        private bool _continueOnError;
        private string _databaseName;
        private BatchableSource<TDocument> _documentSource;
        private int? _maxBatchCount;
        private int? _maxDocumentSize;
        private int? _maxMessageSize;
        private IBsonSerializer<TDocument> _serializer;
        private Func<bool> _shouldSendGetLastError;
        private WriteConcern _writeConcern;

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

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
            set { _collectionName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public bool ContinueOnError
        {
            get { return _continueOnError; }
            set { _continueOnError = value; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public BatchableSource<TDocument> DocumentSource
        {
            get { return _documentSource; }
            set { _documentSource = Ensure.IsNotNull(value, "value"); }
        }

        public int? MaxBatchCount
        {
            get { return _maxBatchCount; }
            set { _maxBatchCount = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public int? MaxDocumentSize
        {
            get { return _maxDocumentSize; }
            set { _maxDocumentSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public int? MaxMessageSize
        {
            get { return _maxMessageSize; }
            set { _maxMessageSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
            set { _serializer = Ensure.IsNotNull(value, "value"); }
        }

        public Func<bool> ShouldSendGetLastError
        {
            get { return _shouldSendGetLastError; }
            set { _shouldSendGetLastError = value; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
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
                _continueOnError,
                _shouldSendGetLastError);
        }

        public async Task<BsonDocument> ExecuteAsync(IConnectionHandle connection, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(connection, "connection");

            if (connection.Description.BuildInfoResult.ServerVersion >= new SemanticVersion(2, 6, 0) && _writeConcern.IsAcknowledged)
            {
                var emulator = new InsertOpcodeOperationEmulator<TDocument>(_databaseName, _collectionName, _serializer, _documentSource)
                {
                    ContinueOnError = _continueOnError,
                    MaxBatchCount = _maxBatchCount,
                    MaxDocumentSize = _maxDocumentSize,
                    MaxMessageSize = _maxMessageSize,
                    WriteConcern = _writeConcern
                };
                return await emulator.ExecuteAsync(connection, timeout, cancellationToken);
            }
            else
            {
                var protocol = CreateProtocol();
                return await protocol.ExecuteAsync(connection, timeout, cancellationToken);
            }
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(slidingTimeout, cancellationToken))
            using (var connection = await connectionSource.GetConnectionAsync(slidingTimeout, cancellationToken))
            {
                return await ExecuteAsync(connection, slidingTimeout, cancellationToken);
            }
        }
    }
}

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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class InsertOpcodeOperationEmulator<TDocument>
    {
        // fields
        private string _collectionName;
        private bool _continueOnError;
        private string _databaseName;
        private BatchableSource<TDocument> _documentSource;
        private int? _maxBatchCount;
        private int? _maxDocumentSize;
        private int? _maxMessageSize;
        private MessageEncoderSettings _messageEncoderSettings;
        private IBsonSerializer<TDocument> _serializer;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
        public InsertOpcodeOperationEmulator(
            string databaseName,
            string collectionName,
            IBsonSerializer<TDocument> serializer,
            BatchableSource<TDocument> documentSource,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _documentSource = Ensure.IsNotNull(documentSource, "documentSource");
            _messageEncoderSettings = messageEncoderSettings;
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

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
            set { _serializer = Ensure.IsNotNull(value, "value"); }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        public async Task<WriteConcernResult> ExecuteAsync(IConnectionHandle connection, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(connection, "connection");

            var requests = _documentSource.GetRemainingItems().Select(d => new InsertRequest(d, _serializer));
            var operation = new BulkInsertOperation(_databaseName, _collectionName, requests, _messageEncoderSettings)
            {
                // CheckElementNames = ?
                IsOrdered = !_continueOnError,
                MaxBatchCount = _maxBatchCount ?? 0,
                // ReaderSettings = ?
                WriteConcern = _writeConcern,
                // WriteSettings = ?
            };

            BulkWriteResult bulkWriteResult;
            BulkWriteException bulkWriteException = null;
            try
            {
                bulkWriteResult = await operation.ExecuteAsync(connection, timeout, cancellationToken);
            }
            catch (BulkWriteException ex)
            {
                bulkWriteResult = ex.Result;
                bulkWriteException = ex;
            }

            var converter = new BulkWriteResultConverter();
            if (bulkWriteException != null)
            {
                throw converter.ToWriteConcernException(bulkWriteException);
            }
            else
            {
                if (_writeConcern.IsAcknowledged)
                {
                    return converter.ToWriteConcernResult(bulkWriteResult);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}

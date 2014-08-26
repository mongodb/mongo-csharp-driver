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
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateCollectionOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private bool? _autoIndexId;
        private bool? _capped;
        private string _collectionName;
        private string _databaseName;
        private long? _maxDocuments;
        private long? _maxSize;
        private MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public CreateCollectionOperation(
            string databaseName,
            string collectionName,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public bool? AutoIndexId
        {
            get { return _autoIndexId; }
            set { _autoIndexId = value; }
        }

        public bool? Capped
        {
            get { return _capped; }
            set { _capped = value; }
        }

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

        public long? MaxDocuments
        {
            get { return _maxDocuments; }
            set { _maxDocuments = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public long? MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
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
            var operation = new WriteCommandOperation(_databaseName, command, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}

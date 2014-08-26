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
    public class RenameCollectionOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private string _collectionName;
        private string _databaseName;
        private bool? _dropTarget;
        private MessageEncoderSettings _messageEncoderSettings;
        private string _newCollectionName;
        private string _newDatabaseName;

        // constructors
        public RenameCollectionOperation(
            string databaseName,
            string collectionName,
            string newCollectionName,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _newCollectionName = Ensure.IsNotNullOrEmpty(newCollectionName, "newCollectionName");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
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

        public bool? DropTarget
        {
            get { return _dropTarget; }
            set { _dropTarget = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public string NewCollectionName
        {
            get { return _newCollectionName; }
            set { _newCollectionName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public string NewDatabaseName
        {
            get { return _newDatabaseName; }
            set { _newDatabaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "drop", _databaseName + "." + _collectionName },
                { "to", (_newDatabaseName ?? _databaseName) + "." + _newCollectionName },
                { "dropTarget", () => _dropTarget.Value, _dropTarget.HasValue }
            };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation("admin", command, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}

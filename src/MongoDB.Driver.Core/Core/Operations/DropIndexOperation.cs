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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class DropIndexOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private string _collectionName;
        private string _databaseName;
        private string _indexName;
        private MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public DropIndexOperation(
            string databaseName,
            string collectionName,
            BsonDocument keys,
            MessageEncoderSettings messageEncoderSettings)
            : this(databaseName, collectionName, CreateIndexOperation.GetDefaultIndexName(keys), messageEncoderSettings)
        {
        }

        public DropIndexOperation(
            string databaseName,
            string collectionName,
            string indexName,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _indexName = Ensure.IsNotNullOrEmpty(indexName, "indexName");
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

        public string IndexName
        {
            get { return _indexName; }
            set { _indexName = Ensure.IsNotNullOrEmpty(value, "value"); }
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
                { "deleteIndexes", _collectionName },
                { "index", _indexName }
            };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation(_databaseName, command, _messageEncoderSettings);
            try
            {
                return await operation.ExecuteAsync(binding, timeout, cancellationToken);
            }
            catch (MongoCommandException ex)
            {
                var result = ex.Result;
                if ((string)result["errmsg"] == "ns not found")
                {
                    return result;
                }
                throw;
            }
        }
    }
}

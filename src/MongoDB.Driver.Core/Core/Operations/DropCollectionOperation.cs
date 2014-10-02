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
    public class DropCollectionOperation : IWriteOperation<BsonDocument>, ICommandOperation
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public DropCollectionOperation(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument { { "drop", _collectionNamespace.CollectionName } };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation(_collectionNamespace.DatabaseNamespace, command, _messageEncoderSettings);
            try
            {
                return await operation.ExecuteAsync(binding, timeout, cancellationToken).ConfigureAwait(false);
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

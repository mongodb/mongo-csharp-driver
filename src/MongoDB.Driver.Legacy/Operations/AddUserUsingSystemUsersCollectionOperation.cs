/* Copyright 2010-2014 MongoDB Inc.
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Operations
{
    internal class AddUserUsingSystemUsersCollectionOperation : IWriteOperation<bool>
    {
        // fields
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly string _passwordHash;
        private readonly bool _readOnly;
        private readonly string _username;

        // constructors
        public AddUserUsingSystemUsersCollectionOperation(
            DatabaseNamespace databaseNamespace,
            string username,
            string passwordHash,
            bool readOnly,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = databaseNamespace;
            _username = username;
            _passwordHash = passwordHash;
            _readOnly = readOnly;
            _messageEncoderSettings = messageEncoderSettings;
        }

        // methods
        public async Task<bool> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            {
                var collectionNamespace = new CollectionNamespace(_databaseNamespace, "system.users");

                var user = await FindUserAsync(channelSource, collectionNamespace, cancellationToken).ConfigureAwait(false);
                if (user == null)
                {
                    user = new BsonDocument
                    {
                        { "_id", ObjectId.GenerateNewId() },
                        { "user", _username }, 
                        { "pwd", _passwordHash },
                        { "readOnly", _readOnly },
                    };
                    await InsertUserAsync(channelSource, collectionNamespace, user, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    user["pwd"] = _passwordHash;
                    user["readOnly"] = _readOnly;
                    await UpdateUserAsync(channelSource, collectionNamespace, user, cancellationToken).ConfigureAwait(false);
                }
            }

            return true;
        }

        private async Task<BsonDocument> FindUserAsync(IChannelSourceHandle channelSource, CollectionNamespace collectionNamespace, CancellationToken cancellationToken)
        {
            var operation = new FindOperation<BsonDocument>(collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Filter = new BsonDocument("user", _username),
                Limit = -1
            };
            var cursor = await operation.ExecuteAsync(channelSource, ReadPreference.Primary, cancellationToken).ConfigureAwait(false);
            var userDocuments = await cursor.ToListAsync().ConfigureAwait(false);
            return userDocuments.FirstOrDefault();
        }

        private async Task InsertUserAsync(IChannelSourceHandle channelSource, CollectionNamespace collectionNamespace, BsonDocument user, CancellationToken cancellationToken)
        {
            var inserts = new[] { new InsertRequest(user) };
            var operation = new BulkMixedWriteOperation(collectionNamespace, inserts, _messageEncoderSettings) { WriteConcern = WriteConcern.Acknowledged };
            await operation.ExecuteAsync(channelSource, cancellationToken).ConfigureAwait(false);
        }

        private async Task UpdateUserAsync(IChannelSourceHandle channelSource, CollectionNamespace collectionNamespace, BsonDocument user, CancellationToken cancellationToken)
        {
            var filter = new BsonDocument("_id", user["_id"]);
            var updates = new[] { new UpdateRequest(UpdateType.Replacement, filter, user) };
            var operation = new BulkMixedWriteOperation(collectionNamespace, updates, _messageEncoderSettings) { WriteConcern = WriteConcern.Acknowledged };
            await operation.ExecuteAsync(channelSource, cancellationToken).ConfigureAwait(false);
        }
    }
}

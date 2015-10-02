/* Copyright 2010-2015 MongoDB Inc.
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
        public bool Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            using (var channelSource = binding.GetWriteChannelSource(cancellationToken))
            {
                var collectionNamespace = new CollectionNamespace(_databaseNamespace, "system.users");

                var user = FindUser(channelSource, collectionNamespace, cancellationToken);
                if (user == null)
                {
                    user = new BsonDocument
                    {
                        { "_id", ObjectId.GenerateNewId() },
                        { "user", _username },
                        { "pwd", _passwordHash },
                        { "readOnly", _readOnly },
                    };
                    InsertUser(channelSource, collectionNamespace, user, cancellationToken);
                }
                else
                {
                    user["pwd"] = _passwordHash;
                    user["readOnly"] = _readOnly;
                    UpdateUser(channelSource, collectionNamespace, user, cancellationToken);
                }
            }

            return true;
        }

        public Task<bool> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private BsonDocument FindUser(IChannelSourceHandle channelSource, CollectionNamespace collectionNamespace, CancellationToken cancellationToken)
        {
            var operation = new FindOperation<BsonDocument>(collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Filter = new BsonDocument("user", _username),
                Limit = -1
            };
            var cursor = operation.Execute(channelSource, ReadPreference.Primary, cancellationToken);
            var userDocuments = cursor.ToList();
            return userDocuments.FirstOrDefault();
        }

        private void InsertUser(IChannelSourceHandle channelSource, CollectionNamespace collectionNamespace, BsonDocument user, CancellationToken cancellationToken)
        {
            var inserts = new[] { new InsertRequest(user) };
            var operation = new BulkMixedWriteOperation(collectionNamespace, inserts, _messageEncoderSettings) { WriteConcern = WriteConcern.Acknowledged };
            operation.Execute(channelSource, cancellationToken);
        }

        private void UpdateUser(IChannelSourceHandle channelSource, CollectionNamespace collectionNamespace, BsonDocument user, CancellationToken cancellationToken)
        {
            var filter = new BsonDocument("_id", user["_id"]);
            var updates = new[] { new UpdateRequest(UpdateType.Replacement, filter, user) };
            var operation = new BulkMixedWriteOperation(collectionNamespace, updates, _messageEncoderSettings) { WriteConcern = WriteConcern.Acknowledged };
            operation.Execute(channelSource, cancellationToken);
        }
    }
}

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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Operations
{
    internal class AddUserUsingUserManagementCommandsOperation : IWriteOperation<bool>
    {
        // fields
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly string _passwordHash;
        private readonly bool _readOnly;
        private readonly string _username;

        // constructors
        public AddUserUsingUserManagementCommandsOperation(
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
                var userExists = UserExists(channelSource, cancellationToken);

                var roles = new BsonArray();
                if (_databaseNamespace.DatabaseName == "admin")
                {
                    roles.Add(_readOnly ? "readAnyDatabase" : "root");
                }
                else
                {
                    roles.Add(_readOnly ? "read" : "dbOwner");
                }

                var commandName = userExists ? "updateUser" : "createUser";
                var command = new BsonDocument
                {
                    { commandName, _username },
                    { "pwd", _passwordHash },
                    { "digestPassword", false },
                    { "roles", roles }
                };

                var operation = new WriteCommandOperation<BsonDocument>(_databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
                operation.Execute(channelSource, cancellationToken);
            }

            return true;
        }

        public Task<bool> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private bool UserExists(IChannelSourceHandle channelSource, CancellationToken cancellationToken)
        {
            try
            {
                var command = new BsonDocument("usersInfo", _username);
                var operation = new ReadCommandOperation<BsonDocument>(_databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
                var result = operation.Execute(channelSource, ReadPreference.Primary, cancellationToken);

                BsonValue users;
                if (result.TryGetValue("users", out users) && users.IsBsonArray && users.AsBsonArray.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (MongoCommandException ex)
            {
                if (ex.Code == 13)
                {
                    return false;
                }

                throw;
            }
        }
    }
}

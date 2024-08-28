/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Authentication
{
    internal sealed class MongoDBX509Authenticator : IAuthenticator
    {
        public static string MechanismName
        {
            get { return "MONGODB-X509"; }
        }

        private readonly string _username;
        private readonly ServerApi _serverApi;

        public MongoDBX509Authenticator(string username, ServerApi serverApi)
        {
            _username = Ensure.IsNullOrNotEmpty(username, nameof(username));
            _serverApi = serverApi; // can be null
        }

        public string Name
        {
            get { return MechanismName; }
        }

        public void Authenticate(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            if (description.HelloResult.SpeculativeAuthenticate != null)
            {
                return;
            }

            try
            {
                var protocol = CreateAuthenticateProtocol();
                protocol.Execute(connection, cancellationToken);
            }
            catch (MongoCommandException ex)
            {
                throw CreateException(connection, ex);
            }
        }

        public async Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            if (description.HelloResult.SpeculativeAuthenticate != null)
            {
                return;
            }

            try
            {
                var protocol = CreateAuthenticateProtocol();
                await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
            }
            catch (MongoCommandException ex)
            {
                throw CreateException(connection, ex);
            }
        }

        public BsonDocument CustomizeInitialHelloCommand(BsonDocument helloCommand, CancellationToken cancellationToken)
        {
            helloCommand.Add("speculativeAuthenticate", CreateAuthenticateCommand());
            return helloCommand;
        }

        private BsonDocument CreateAuthenticateCommand()
        {
            return new BsonDocument
            {
                { "authenticate", 1 },
                { "mechanism", Name },
                { "user", _username, _username != null }
            };
        }

        private CommandWireProtocol<BsonDocument> CreateAuthenticateProtocol()
        {
            var command = CreateAuthenticateCommand();

            var protocol = new CommandWireProtocol<BsonDocument>(
                databaseNamespace: new DatabaseNamespace("$external"),
                command: command,
                secondaryOk: true,
                resultSerializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: null,
                serverApi: _serverApi);

            return protocol;
        }

        private MongoAuthenticationException CreateException(IConnection connection, Exception ex)
        {
            var message = $"Unable to authenticate username '{_username}' using protocol '{Name}'.";
            return new MongoAuthenticationException(connection.ConnectionId, message, ex);
        }
    }
}

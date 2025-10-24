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
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a connection initializer (opens and authenticates connections).
    /// </summary>
    internal class ConnectionInitializer : IConnectionInitializer
    {
        private readonly BsonDocument _clientDocument;
        private readonly IReadOnlyList<CompressorConfiguration> _compressors;
        private readonly ServerApi _serverApi;

        public ConnectionInitializer(
            string applicationName,
            IReadOnlyList<CompressorConfiguration> compressors,
            ServerApi serverApi,
            LibraryInfo libraryInfo)
        {
            _clientDocument = ClientDocumentHelper.CreateClientDocument(applicationName, libraryInfo);
            _compressors = Ensure.IsNotNull(compressors, nameof(compressors));
            _serverApi = serverApi;
        }

        public ConnectionInitializerContext Authenticate(OperationContext operationContext, IConnection connection, ConnectionInitializerContext connectionInitializerContext)
        {
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(connectionInitializerContext, nameof(connectionInitializerContext));
            var description = Ensure.IsNotNull(connectionInitializerContext.Description, nameof(connectionInitializerContext.Description));

            AuthenticationHelper.Authenticate(operationContext, connection, description, connectionInitializerContext.Authenticator);

            // Connection description should be updated only on the initial handshake and not after reauthentication
            if (!description.IsInitialized())
            {
                var connectionIdServerValue = description.HelloResult.ConnectionIdServerValue;
                if (connectionIdServerValue.HasValue)
                {
                    description = UpdateConnectionIdWithServerValue(description, connectionIdServerValue.Value);
                }
                else if (!description.HelloResult.IsMongocryptd) // mongocryptd doesn't provide ConnectionId
                {
                    try
                    {
                        var getLastErrorProtocol = CreateGetLastErrorProtocol(_serverApi);
                        var getLastErrorResult = getLastErrorProtocol.Execute(operationContext, connection);

                        description = UpdateConnectionIdWithServerValue(description, getLastErrorResult);
                    }
                    catch
                    {
                        // if we couldn't get the server's connection id, so be it.
                    }
                }
            }

            return new ConnectionInitializerContext(description, connectionInitializerContext.Authenticator);
        }

        public async Task<ConnectionInitializerContext> AuthenticateAsync(OperationContext operationContext, IConnection connection, ConnectionInitializerContext connectionInitializerContext)
        {
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(connectionInitializerContext, nameof(connectionInitializerContext));
            var description = Ensure.IsNotNull(connectionInitializerContext.Description, nameof(connectionInitializerContext.Description));

            await AuthenticationHelper.AuthenticateAsync(operationContext, connection, description, connectionInitializerContext.Authenticator).ConfigureAwait(false);

            // Connection description should be updated only on the initial handshake and not while reauthentication
            if (!description.IsInitialized())
            {
                var connectionIdServerValue = description.HelloResult.ConnectionIdServerValue;
                if (connectionIdServerValue.HasValue)
                {
                    description = UpdateConnectionIdWithServerValue(description, connectionIdServerValue.Value);
                }
                else if (!description.HelloResult.IsMongocryptd) // mongocryptd doesn't provide ConnectionId
                {
                    try
                    {
                        var getLastErrorProtocol = CreateGetLastErrorProtocol(_serverApi);
                        var getLastErrorResult = await getLastErrorProtocol
                            .ExecuteAsync(operationContext, connection)
                            .ConfigureAwait(false);

                        description = UpdateConnectionIdWithServerValue(description, getLastErrorResult);
                    }
                    catch
                    {
                        // if we couldn't get the server's connection id, so be it.
                    }
                }
            }

            return new ConnectionInitializerContext(description, connectionInitializerContext.Authenticator);
        }

        public ConnectionInitializerContext SendHello(OperationContext operationContext, IConnection connection)
        {
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            Ensure.IsNotNull(connection, nameof(connection));
            var authenticator = CreateAuthenticator(connection);
            var helloCommand = CreateInitialHelloCommand(operationContext, authenticator, connection.Settings.LoadBalanced);
            var helloProtocol = HelloHelper.CreateProtocol(helloCommand, _serverApi);
            var helloResult = HelloHelper.GetResult(operationContext, connection, helloProtocol);
            if (connection.Settings.LoadBalanced && !helloResult.ServiceId.HasValue)
            {
                throw new InvalidOperationException("Driver attempted to initialize in load balancing mode, but the server does not support this mode.");
            }

            return new (new ConnectionDescription(connection.ConnectionId, helloResult), authenticator);
        }

        public async Task<ConnectionInitializerContext> SendHelloAsync(OperationContext operationContext, IConnection connection)
        {
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            Ensure.IsNotNull(connection, nameof(connection));
            var authenticator = CreateAuthenticator(connection);
            var helloCommand = CreateInitialHelloCommand(operationContext, authenticator, connection.Settings.LoadBalanced);
            var helloProtocol = HelloHelper.CreateProtocol(helloCommand, _serverApi);
            var helloResult = await HelloHelper.GetResultAsync(operationContext, connection, helloProtocol).ConfigureAwait(false);
            if (connection.Settings.LoadBalanced && !helloResult.ServiceId.HasValue)
            {
                throw new InvalidOperationException("Driver attempted to initialize in load balancing mode, but the server does not support this mode.");
            }

            return new (new ConnectionDescription(connection.ConnectionId, helloResult), authenticator);
        }

        // private methods
        private CommandWireProtocol<BsonDocument> CreateGetLastErrorProtocol(ServerApi serverApi)
        {
            var getLastErrorCommand = new BsonDocument("getLastError", 1);
            var getLastErrorProtocol = new CommandWireProtocol<BsonDocument>(
                databaseNamespace: DatabaseNamespace.Admin,
                command: getLastErrorCommand,
                secondaryOk: true,
                resultSerializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: null,
                serverApi: serverApi,
                serializationDomain: BsonSerializer.DefaultSerializationDomain);
            return getLastErrorProtocol;
        }

        private BsonDocument CreateInitialHelloCommand(OperationContext operationContext, IAuthenticator authenticator, bool loadBalanced = false)
        {
            var command = HelloHelper.CreateCommand(_serverApi, loadBalanced: loadBalanced);
            HelloHelper.AddClientDocumentToCommand(command, _clientDocument);
            HelloHelper.AddCompressorsToCommand(command, _compressors);
            return HelloHelper.CustomizeCommand(operationContext, command, authenticator);
        }

        private IAuthenticator CreateAuthenticator(IConnection connection)
        {
            if (connection.Description.IsInitialized())
            {
                // should never be here.
                throw new InvalidOperationException();
            }

            return connection.Settings.AuthenticatorFactory?.Create();
        }

        private ConnectionDescription UpdateConnectionIdWithServerValue(ConnectionDescription description, BsonDocument getLastErrorResult)
        {
            if (getLastErrorResult.TryGetValue("connectionId", out var connectionIdBsonValue))
            {
                description = UpdateConnectionIdWithServerValue(description, connectionIdBsonValue.ToInt32());
            }

            return description;
        }

        private ConnectionDescription UpdateConnectionIdWithServerValue(ConnectionDescription description, long serverValue)
        {
            var connectionId = description.ConnectionId.WithServerValue(serverValue);
            description = description.WithConnectionId(connectionId);

            return description;
        }
    }
}

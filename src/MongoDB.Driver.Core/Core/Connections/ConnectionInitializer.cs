/* Copyright 2013-present MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Authentication;
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
            ServerApi serverApi)
        {
            _clientDocument = ClientDocumentHelper.CreateClientDocument(applicationName);
            _compressors = Ensure.IsNotNull(compressors, nameof(compressors));
            _serverApi = serverApi;
        }

        public ConnectionDescription InitializeConnection(IConnection connection, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            var authenticators = connection.Settings.AuthenticatorFactories.Select(f => f.Create()).ToList();
            var isMasterCommand = CreateInitialIsMasterCommand(authenticators);
            var isMasterProtocol = IsMasterHelper.CreateProtocol(isMasterCommand, _serverApi);
            var isMasterResult = IsMasterHelper.GetResult(connection, isMasterProtocol, cancellationToken);

            var buildInfoProtocol = CreateBuildInfoProtocol(_serverApi);
            var buildInfoResult = new BuildInfoResult(buildInfoProtocol.Execute(connection, cancellationToken));

            var description = new ConnectionDescription(connection.ConnectionId, isMasterResult, buildInfoResult);

            AuthenticationHelper.Authenticate(connection, description, authenticators, cancellationToken);

            var connectionIdServerValue = isMasterResult.ConnectionIdServerValue;
            if (connectionIdServerValue.HasValue)
            {
                description = UpdateConnectionIdWithServerValue(description, connectionIdServerValue.Value);
            }
            else
            {
                try
                {
                    var getLastErrorProtocol = CreateGetLastErrorProtocol(_serverApi);
                    var getLastErrorResult = getLastErrorProtocol.Execute(connection, cancellationToken);

                    description = UpdateConnectionIdWithServerValue(description, getLastErrorResult);
                }
                catch
                {
                    // if we couldn't get the server's connection id, so be it.
                }
            }

            return description;
        }

        public async Task<ConnectionDescription> InitializeConnectionAsync(IConnection connection, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            var authenticators = connection.Settings.AuthenticatorFactories.Select(f => f.Create()).ToList();
            var isMasterCommand = CreateInitialIsMasterCommand(authenticators);
            var isMasterProtocol = IsMasterHelper.CreateProtocol(isMasterCommand, _serverApi);
            var isMasterResult = await IsMasterHelper.GetResultAsync(connection, isMasterProtocol, cancellationToken).ConfigureAwait(false);

            var buildInfoProtocol = CreateBuildInfoProtocol(_serverApi);
            var buildInfoResult = new BuildInfoResult(await buildInfoProtocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false));

            var description = new ConnectionDescription(connection.ConnectionId, isMasterResult, buildInfoResult);

            await AuthenticationHelper.AuthenticateAsync(connection, description, authenticators, cancellationToken).ConfigureAwait(false);

            var connectionIdServerValue = isMasterResult.ConnectionIdServerValue;
            if (connectionIdServerValue.HasValue)
            {
                description = UpdateConnectionIdWithServerValue(description, connectionIdServerValue.Value);
            }
            else
            {
                try
                {
                    var getLastErrorProtocol = CreateGetLastErrorProtocol(_serverApi);
                    var getLastErrorResult = await getLastErrorProtocol
                        .ExecuteAsync(connection, cancellationToken)
                        .ConfigureAwait(false);

                    description = UpdateConnectionIdWithServerValue(description, getLastErrorResult);
                }
                catch
                {
                    // if we couldn't get the server's connection id, so be it.
                }
            }

            return description;
        }

        // private methods
        private CommandWireProtocol<BsonDocument> CreateBuildInfoProtocol(ServerApi serverApi)
        {
            var buildInfoServerApi =
                serverApi != null
                    ? new ServerApi(
                        serverApi.Version,
                        strict: false, // buildInfo is not part of server API version 1 so we must skip strict flag, otherwise initialize will fail
                        deprecationErrors: serverApi.DeprecationErrors)
                    : null;

            var buildInfoCommand = new BsonDocument("buildInfo", 1);
            var buildInfoProtocol = new CommandWireProtocol<BsonDocument>(
                databaseNamespace: DatabaseNamespace.Admin,
                command: buildInfoCommand,
                slaveOk: true,
                resultSerializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: null,
                serverApi: buildInfoServerApi);
            return buildInfoProtocol;
        }

        private CommandWireProtocol<BsonDocument> CreateGetLastErrorProtocol(ServerApi serverApi)
        {
            var getLastErrorCommand = new BsonDocument("getLastError", 1);
            var getLastErrorProtocol = new CommandWireProtocol<BsonDocument>(
                databaseNamespace: DatabaseNamespace.Admin,
                command: getLastErrorCommand,
                slaveOk: true,
                resultSerializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: null,
                serverApi: serverApi);
            return getLastErrorProtocol;
        }

        private BsonDocument CreateInitialIsMasterCommand(IReadOnlyList<IAuthenticator> authenticators)
        {
            var command = IsMasterHelper.CreateCommand();
            IsMasterHelper.AddClientDocumentToCommand(command, _clientDocument);
            IsMasterHelper.AddCompressorsToCommand(command, _compressors);
            return IsMasterHelper.CustomizeCommand(command, authenticators);
        }

        private ConnectionDescription UpdateConnectionIdWithServerValue(ConnectionDescription description, BsonDocument getLastErrorResult)
        {
            if (getLastErrorResult.TryGetValue("connectionId", out var connectionIdBsonValue))
            {
                description = UpdateConnectionIdWithServerValue(description, connectionIdBsonValue.ToInt32());
            }

            return description;
        }

        private ConnectionDescription UpdateConnectionIdWithServerValue(ConnectionDescription description, int serverValue)
        {
            var connectionId = description.ConnectionId.WithServerValue(serverValue);
            description = description.WithConnectionId(connectionId);

            return description;
        }
    }
}

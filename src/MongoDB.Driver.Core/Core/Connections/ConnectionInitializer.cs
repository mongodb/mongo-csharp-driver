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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a connection initializer (opens and authenticates connections).
    /// </summary>
    internal class ConnectionInitializer : IConnectionInitializer
    {
        public async Task<ConnectionDescription> InitializeConnectionAsync(IConnection connection, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");

            var isMasterCommand = new BsonDocument("isMaster", 1);
            var isMasterProtocol = new CommandWireProtocol<BsonDocument>(
                DatabaseNamespace.Admin,
                isMasterCommand,
                true,
                BsonDocumentSerializer.Instance,
                null);
            var isMasterResult = new IsMasterResult(await isMasterProtocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false));

            var buildInfoCommand = new BsonDocument("buildInfo", 1);
            var buildInfoProtocol = new CommandWireProtocol<BsonDocument>(
                DatabaseNamespace.Admin,
                buildInfoCommand,
                true,
                BsonDocumentSerializer.Instance,
               null);
            var buildInfoResult = new BuildInfoResult(await buildInfoProtocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false));

            var connectionId = connection.ConnectionId;
            var description = new ConnectionDescription(connectionId, isMasterResult, buildInfoResult);

            await AuthenticationHelper.AuthenticateAsync(connection, description, cancellationToken).ConfigureAwait(false);

            try
            {
                var getLastErrorCommand = new BsonDocument("getLastError", 1);
                var getLastErrorProtocol = new CommandWireProtocol<BsonDocument>(
                    DatabaseNamespace.Admin,
                    getLastErrorCommand,
                    true,
                    BsonDocumentSerializer.Instance,
                    null);
                var getLastErrorResult = await getLastErrorProtocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);

                BsonValue connectionIdBsonValue;
                if (getLastErrorResult.TryGetValue("connectionId", out connectionIdBsonValue))
                {
                    connectionId = connectionId.WithServerValue(connectionIdBsonValue.ToInt32());
                    description = description.WithConnectionId(connectionId);
                }
            }
            catch
            {
                // if we couldn't get the server's connection id, so be it.
            }

            return description;
        }
    }
}

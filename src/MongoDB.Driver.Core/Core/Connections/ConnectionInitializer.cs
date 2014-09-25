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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a connection initializer (opens and authenticates connections).
    /// </summary>
    internal class ConnectionInitializer : IConnectionInitializer
    {
        public async Task<ConnectionDescription> InitializeConnectionAsync(IConnection connection, ConnectionId connectionId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");
            Ensure.IsNotNull(connectionId, "connectionId");
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(timeout, "timeout");

            var slidingTimeout = new SlidingTimeout(timeout);

            var isMasterCommand = new BsonDocument("isMaster", 1);
            var isMasterProtocol = new CommandWireProtocol(DatabaseNamespace.Admin, isMasterCommand, true, null);
            var isMasterResult = new IsMasterResult(await isMasterProtocol.ExecuteAsync(connection, slidingTimeout, cancellationToken).ConfigureAwait(false));

            // authentication is currently broken on arbiters
            if (!isMasterResult.IsArbiter)
            {
                foreach (var authenticator in connection.Settings.Authenticators)
                {
                    await authenticator.AuthenticateAsync(connection, slidingTimeout, cancellationToken).ConfigureAwait(false);
                }
            }

            var buildInfoCommand = new BsonDocument("buildInfo", 1);
            var buildInfoProtocol = new CommandWireProtocol(DatabaseNamespace.Admin, buildInfoCommand, true, null);
            var buildInfoResult = new BuildInfoResult(await buildInfoProtocol.ExecuteAsync(connection, slidingTimeout, cancellationToken).ConfigureAwait(false));

            var getLastErrorCommand = new BsonDocument("getLastError", 1);
            var getLastErrorProtocol = new CommandWireProtocol(DatabaseNamespace.Admin, getLastErrorCommand, true, null);
            var getLastErrorResult = await getLastErrorProtocol.ExecuteAsync(connection, slidingTimeout, cancellationToken).ConfigureAwait(false);

            BsonValue connectionIdBsonValue;
            if (getLastErrorResult.TryGetValue("connectionId", out connectionIdBsonValue))
            {
                connectionId = connectionId.WithServerValue(connectionIdBsonValue.ToInt32());
            }

            return new ConnectionDescription(connectionId, isMasterResult, buildInfoResult);
        }
    }
}

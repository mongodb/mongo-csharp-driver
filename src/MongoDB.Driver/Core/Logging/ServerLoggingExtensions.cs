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

using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Logging
{
    internal static partial class ServerLoggingExtensions
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "{TopologyId} {ServerHost} {ServerPort} {Message}")]
        internal static partial void ServerDebug(this ILogger logger, int topologyId, string serverHost, int serverPort, string message);

        internal static void ServerDebug(this ILogger logger, ServerId serverId, string message)
        {
            var (clusterId, serverHost, serverPort, formattedMessage) = GetParams(serverId, message);
            logger.ServerDebug(clusterId, serverHost, serverPort, formattedMessage);
        }




        internal static (int ClusterId, string ServerHost, int ServerPort, string Message) GetParams(ServerId serverId, string message)
        {
            var (host, port) = serverId.EndPoint.GetHostAndPort();
            return (serverId.ClusterId.Value, host, port, message);
        }
    }
}

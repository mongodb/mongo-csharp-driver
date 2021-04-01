/* Copyright 2021-present MongoDB Inc.
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
using System.Net;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;

namespace AstrolabeWorkloadExecutor
{
    public class AstrolabeEventsJsonBuilder
    {
        private readonly StringBuilder _builder = new StringBuilder();

        // public methods
        public void Append(object @event)
        {
            var eventDocument = CreateEventDocument(@event);
            _builder.Append(eventDocument);
        }


        // private methods
        private string CreateEventDocument(object @event) =>
            @event switch
            {
                CommandStartedEvent typedEvent =>
                    CreateCommandEventDocument(
                        "CommandStartedEvent",
                        typedEvent.Timestamp,
                        typedEvent.CommandName,
                        typedEvent.RequestId,
                        $", databaseName : '{typedEvent.DatabaseNamespace}'"),

                CommandSucceededEvent typedEvent =>
                    CreateCommandEventDocument(
                        "CommandSucceededEvent",
                        typedEvent.Timestamp,
                        typedEvent.CommandName,
                        typedEvent.RequestId,
                        $", duration : '{typedEvent.Duration.TotalMilliseconds}'"),

                CommandFailedEvent typedEvent =>
                    CreateCommandEventDocument(
                        "CommandFailedEvent",
                        typedEvent.Timestamp,
                        typedEvent.CommandName,
                        typedEvent.RequestId,
                        $", duration : '{typedEvent.Duration.TotalMilliseconds}', failure : '{typedEvent.Failure}'"),

                ConnectionPoolOpenedEvent typedEvent =>
                    CreateCmapEventDocument("PoolCreatedEvent", typedEvent.Timestamp, typedEvent.ServerId),

                ConnectionPoolClearedEvent typedEvent =>
                    CreateCmapEventDocument("PoolClearedEvent", typedEvent.Timestamp, typedEvent.ServerId),

                ConnectionPoolClosedEvent typedEvent =>
                    CreateCmapEventDocument("PoolClosedEvent", typedEvent.Timestamp, typedEvent.ServerId),

                ConnectionCreatedEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionCreatedEvent", typedEvent.Timestamp, typedEvent.ConnectionId),

                ConnectionClosedEvent typedEvent =>
                    CreateCmapEventDocument(
                        "ConnectionClosedEvent",
                        typedEvent.Timestamp,
                        typedEvent.ConnectionId),
                        // $", reason : '{typedEvent.Reason}'"); // TODO: should be implemented in the scope of CSHARP-3219

                ConnectionPoolCheckingOutConnectionEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionCheckOutStartedEvent", typedEvent.Timestamp, typedEvent.ServerId),

                ConnectionPoolCheckingOutConnectionFailedEvent typedEvent =>
                    CreateCmapEventDocument(
                        "ConnectionCheckOutFailedEvent",
                        typedEvent.Timestamp,
                        typedEvent.ServerId,
                        $", reason : '{typedEvent.Reason}'"),

                ConnectionPoolCheckedOutConnectionEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionCheckedOutEvent", typedEvent.Timestamp, typedEvent.ConnectionId),

                ConnectionPoolCheckedInConnectionEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionCheckedInEvent", typedEvent.Timestamp, typedEvent.ConnectionId),

                ConnectionOpenedEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionReadyEvent", typedEvent.Timestamp, typedEvent.ConnectionId),

                _ => throw new FormatException($"Unrecognized event type: '{@event.GetType()}'."),
            };

        private string CreateCmapEventDocument(string eventName, DateTime timestamp, ServerId serverId, string customJsonNodeWithComma = "") =>
            $"{{ name : '{eventName}',  observedAt : '{BsonUtils.ToSecondsSinceEpoch(timestamp)}', address : '{GetAddress(serverId)}'{customJsonNodeWithComma} }}";

        private string CreateCmapEventDocument(string eventName, DateTime timestamp, ConnectionId connectionId) =>
            CreateCmapEventDocument(eventName, timestamp, connectionId.ServerId, $", connectionId : {connectionId.LocalValue}");

        private string CreateCommandEventDocument(string eventName, DateTime timestamp, string commandName, int requestId, string customJsonNodeWithComma = "") =>
            $"{{ name : '{eventName}',  observedAt : '{BsonUtils.ToSecondsSinceEpoch(timestamp)}', commandName : '{commandName}', requestId : '{requestId}'{customJsonNodeWithComma} }}";

        private string GetAddress(ServerId serverId)
        {
            var endpoint = (DnsEndPoint)serverId.EndPoint;
            return $"{endpoint.Host}:{endpoint.Port}";
        }
    }
}

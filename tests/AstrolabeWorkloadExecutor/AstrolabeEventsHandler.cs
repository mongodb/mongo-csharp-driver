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
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;

namespace AstrolabeWorkloadExecutor
{
    public static class AstrolabeEventsHandler
    {
        // public methods
        public static BsonDocument CreateEventDocument(object @event) =>
            @event switch
            {
                CommandStartedEvent typedEvent =>
                    CreateCommandEventDocument("CommandStartedEvent", typedEvent.ObservedAt, typedEvent.CommandName, typedEvent.RequestId)
                    .Add("databaseName", typedEvent.DatabaseNamespace.ToString()),

                CommandSucceededEvent typedEvent =>
                    CreateCommandEventDocument("CommandSucceededEvent", typedEvent.ObservedAt, typedEvent.CommandName, typedEvent.RequestId)
                    .Add("duration", typedEvent.Duration.TotalMilliseconds),

                CommandFailedEvent typedEvent =>
                    CreateCommandEventDocument("CommandFailedEvent", typedEvent.ObservedAt, typedEvent.CommandName, typedEvent.RequestId)
                    .Add("duration", typedEvent.Duration.TotalMilliseconds)
                    .Add("failure", typedEvent.Failure.ToString()),

                ConnectionPoolOpenedEvent typedEvent =>
                    CreateCmapEventDocument("PoolCreatedEvent", typedEvent.ObservedAt, typedEvent.ServerId),

                ConnectionPoolClearedEvent typedEvent =>
                    CreateCmapEventDocument("PoolClearedEvent", typedEvent.ObservedAt, typedEvent.ServerId),

                ConnectionPoolClosedEvent typedEvent =>
                    CreateCmapEventDocument("PoolClosedEvent", typedEvent.ObservedAt, typedEvent.ServerId),

                ConnectionCreatedEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionCreatedEvent", typedEvent.ObservedAt, typedEvent.ConnectionId),

                ConnectionClosedEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionClosedEvent", typedEvent.ObservedAt, typedEvent.ConnectionId),
                    //.Add("reason", typedEvent.Reason) TODO: should be implemented in the scope of CSHARP-3219

                ConnectionPoolCheckingOutConnectionEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionCheckOutStartedEvent", typedEvent.ObservedAt, typedEvent.ServerId),

                ConnectionPoolCheckingOutConnectionFailedEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionCheckOutFailedEvent", typedEvent.ObservedAt, typedEvent.ServerId)
                    .Add("reason", typedEvent.Reason),

                ConnectionPoolCheckedOutConnectionEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionCheckedOutEvent", typedEvent.ObservedAt, typedEvent.ConnectionId),

                ConnectionPoolCheckedInConnectionEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionCheckedInEvent", typedEvent.ObservedAt, typedEvent.ConnectionId),

                ConnectionOpenedEvent typedEvent =>
                    CreateCmapEventDocument("ConnectionReadyEvent", typedEvent.ObservedAt, typedEvent.ConnectionId),

                _ => throw new FormatException($"Unrecognized event type: '{@event.GetType()}'."),
            };

        // private methods
        private static BsonDocument CreateCmapEventDocument(string eventName, DateTime observedAt, ServerId serverId) =>
            new BsonDocument
            {
                { "name", eventName },
                { "observedAt", GetCurrentTimeSeconds(observedAt) },
                { "address", GetAddress(serverId) }
            };

        public static BsonDocument CreateCmapEventDocument(string eventName, DateTime observedAt, ConnectionId connectionId) =>
            CreateCmapEventDocument(eventName, observedAt, connectionId.ServerId)
            .Add("connectionId", connectionId.LocalValue);

        public static BsonDocument CreateCommandEventDocument(string eventName, DateTime observedAt, string commandName, int requestId) =>
            new BsonDocument
            {
                { "name", eventName },
                { "observedAt", GetCurrentTimeSeconds(observedAt) },
                { "commandName", commandName },
                { "requestId", requestId }
            };

        private static string GetAddress(ServerId serverId)
        {
            var endpoint = serverId.EndPoint;
            return ((DnsEndPoint)endpoint).Host + ":" + ((DnsEndPoint)endpoint).Port;
        }

        private static double GetCurrentTimeSeconds(DateTime observedAt) => (double)(observedAt - BsonConstants.UnixEpoch).TotalMilliseconds / 1000;
    }
}

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
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.TestHelpers;

namespace AstrolabeWorkloadExecutor
{
    public static class AstralabeEventsHandler
    {
        // public methods
        public static BsonDocument CreateEventDocument(dynamic @event)
        {
            var eventName = @event.GetType().Name;
            string specEventName = EventSpecMapper.GetSpecEventName(eventName);

            return specEventName switch
            {
                _ when specEventName.StartsWith("Connection") && !ConnectionEventWithOnlyServerId(specEventName)
                => CreateCmapEventDocument(specEventName, @event.ObservedAt, @event.ConnectionId),
                _ when specEventName.StartsWith("Pool") || ConnectionEventWithOnlyServerId(specEventName)
                => CreateCmapEventDocument(specEventName, @event.ObservedAt, @event.ServerId),
                _ when specEventName.StartsWith("Command") => specEventName switch
                {
                    "CommandStartedEvent" =>
                        CreateCommandEventDocument(specEventName, @event.ObservedAt, @event.CommandName, @event.RequestId)
                        .Add("databaseName", @event.DatabaseNamespace.ToString()),
                    "CommandSucceededEvent" => CreateCommandEventDocument(specEventName, @event.ObservedAt, @event.CommandName, @event.RequestId)
                        .Add("duration", @event.Duration),
                    "CommandFailedEvent" => CreateCommandEventDocument(specEventName, @event.ObservedAt, @event.CommandName, @event.RequestId)
                        .Add("duration", @event.Duration)
                        .Add("failure", @event.Failure.ToString()),
                    _ => throw new Exception($"Unsupported command spec event {specEventName}."),
                },
                _ => throw new Exception($"Unexpected spec event {specEventName}."),
            };

            bool ConnectionEventWithOnlyServerId(string name) =>
                name.Contains("Connection")
                &&
                (specEventName.Contains("Failed") || specEventName.EndsWith("StartedEvent"));
        }

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

        // private methods
        private static string GetAddress(ServerId serverId)
        {
            var endpoint = serverId.EndPoint;
            return ((DnsEndPoint)endpoint).Host + ":" + ((DnsEndPoint)endpoint).Port;
        }

        private static double GetCurrentTimeSeconds(DateTime observedAt)
        {
            var Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (double)(observedAt - Jan1st1970).TotalMilliseconds / 1000;
        }
    }
}

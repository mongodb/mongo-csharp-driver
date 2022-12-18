/* Copyright 2020-present MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit.Sdk;

namespace MongoDB.Driver.Tests.UnifiedTestOperations.Matchers
{
    public class UnifiedEventMatcher
    {
        #region static
        private static Dictionary<string, (Func<object, object> GetMappedDriverEvent, EventSetType EventSetType)> __eventsMapWithSpec = new()
        {
            { "commandStartedEvent", ((e) => e as CommandStartedEvent?,  EventSetType.Command) },
            { "commandSucceededEvent", ((e) => e as CommandSucceededEvent?, EventSetType.Command) },
            { "commandFailedEvent", ((e) => e as CommandFailedEvent?, EventSetType.Command) },

            { "connectionReadyEvent", ((e) => e as ConnectionOpenedEvent?, EventSetType.Cmap) },
            { "connectionCheckOutStartedEvent", ((e) => e as ConnectionPoolCheckingOutConnectionEvent?, EventSetType.Cmap) },
            { "connectionCheckedOutEvent", ((e) => e as ConnectionPoolCheckedOutConnectionEvent?, EventSetType.Cmap) },
            { "connectionCheckedInEvent", ((e) => e as ConnectionPoolCheckedInConnectionEvent?, EventSetType.Cmap) },
            { "connectionClosedEvent", ((e) => e as ConnectionClosedEvent?, EventSetType.Cmap) },
            { "connectionCreatedEvent", ((e) => e as ConnectionCreatedEvent?, EventSetType.Cmap) },
            { "connectionCheckOutFailedEvent", (e => e as ConnectionPoolCheckingOutConnectionFailedEvent?, EventSetType.Cmap) },
            { "poolClearedEvent", ((e) => e as ConnectionPoolClearedEvent?, EventSetType.Cmap) },
            { "poolReadyEvent", ((e) => e as ConnectionPoolReadyEvent?, EventSetType.Cmap) },

            { "serverDescriptionChangedEvent", ((e) => e as ServerDescriptionChangedEvent?, EventSetType.Sdam) }
        };

        public static List<object> FilterEventsByType(List<object> events, string eventType)
        {
            if (!Enum.TryParse<EventSetType>(eventType, ignoreCase: true, out var eventTypeEnum))
            {
                throw new FormatException($"Cannot parse {nameof(eventType)} enum from {eventType}.");
            }

            return events
                .Where(e => __eventsMapWithSpec.Values.Where(v => v.GetMappedDriverEvent(e) != null).Should().ContainSingle("because mapping for driver side events should be unique.").Which.EventSetType == eventTypeEnum)
                .ToList();
        }

        public static Func<object, bool> MapEventNameToCondition(BsonDocument expectedEvent)
        {
            var elements = expectedEvent.Elements.Single();           
            var expectedSpecEvent = elements.Name;

            return (actualEvent) =>
            {
                var mappedActualEvent = __eventsMapWithSpec[expectedSpecEvent].GetMappedDriverEvent(actualEvent); // null if event is not expected
                return
                    mappedActualEvent != null &&
                    (mappedActualEvent is ServerDescriptionChangedEvent serverDescriptionChangedEvent
                        ? IsExpectedServerDescriptionChangedEvent(serverDescriptionChangedEvent, elements)
                        : true); // ignore this for non `ServerDescriptionChangedEvent`s
            };

            static bool IsExpectedServerDescriptionChangedEvent(ServerDescriptionChangedEvent serverDescriptionChangedEvent, BsonElement elements)
            {
                var newDescriptionType = GetServerDescriptionChangedFilter(elements);
                return
                    newDescriptionType == null || // no additional filter
                    serverDescriptionChangedEvent.NewDescription.Type == MapServerType(newDescriptionType);
            }

            static string GetServerDescriptionChangedFilter(BsonElement elements)
            {
                var body = elements.Value;
                string newDescriptionType = null;
                var bodyDocument = body.AsBsonDocument;
                if (bodyDocument.Elements.Count() > 0)
                {
                    foreach (var element in bodyDocument.Elements)
                    {
                        switch (element.Name)
                        {
                            case "newDescription": newDescriptionType = element.Value.AsBsonDocument["type"].AsString; break;
                            default: throw new Exception($"Unexpected event filter key: {element.Name}.");
                        }
                    }
                }
                return newDescriptionType;
            }

            static ServerType MapServerType(string value) => value switch
            {
                "Unknown" => ServerType.Unknown,
                _ => throw new Exception($"Unsupported event filter server type: {value}."),
            };
        }
        #endregion

        private readonly UnifiedValueMatcher _valueMatcher;

        public UnifiedEventMatcher(UnifiedValueMatcher valueMatcher)
        {
            _valueMatcher = valueMatcher;
        }

        public void AssertEventsMatch(List<object> actualEvents, BsonArray expectedEventsDocuments, bool ignoreExtraEvents)
        {
            try
            {
                AssertEvents(actualEvents, expectedEventsDocuments, ignoreExtraEvents);
            }
            catch (XunitException exception)
            {
                throw new AssertionException(
                    userMessage: GetAssertionErrorMessage(actualEvents, expectedEventsDocuments),
                    innerException: exception);
            }
        }

        // private methods
        private void AssertEvents(List<object> actualEvents, BsonArray expectedEventsDocuments, bool ignoreExtraEvents)
        {
            if (ignoreExtraEvents)
            {
                actualEvents.Count.Should().BeGreaterOrEqualTo(expectedEventsDocuments.Count);
            }
            else
            {
                actualEvents.Should().HaveSameCount(expectedEventsDocuments);
            }

            for (int i = 0; i < expectedEventsDocuments.Count; i++)
            {
                var actualEvent = actualEvents[i];
                var expectedEventDocument = expectedEventsDocuments[i].AsBsonDocument;
                if (expectedEventDocument.ElementCount != 1)
                {
                    throw new FormatException("Expected event document model must contain a single element.");
                }
                var expectedEventType = expectedEventDocument.GetElement(0).Name;
                var expectedEventValue = expectedEventDocument[0].AsBsonDocument;
                var expectedDriverEvent = __eventsMapWithSpec[expectedEventType].GetMappedDriverEvent(actualEvent).Should().NotBeNull().And.Subject;

                switch (expectedDriverEvent)
                {
                    case CommandStartedEvent commandStartedEvent:
                        foreach (var element in expectedEventValue)
                        {
                            switch (element.Name)
                            {
                                case "command":
                                    _valueMatcher.AssertValuesMatch(commandStartedEvent.Command, element.Value);
                                    break;
                                case "commandName":
                                    commandStartedEvent.CommandName.Should().Be(element.Value.AsString);
                                    break;
                                case "databaseName":
                                    commandStartedEvent.DatabaseNamespace.DatabaseName.Should().Be(element.Value.AsString);
                                    break;
                                case "hasServiceId":
                                    commandStartedEvent.ServiceId.Should().Match<ObjectId?>(s => s.HasValue == element.Value.ToBoolean());
                                    break;
                                case "hasServerConnectionId":
                                    AssertHasServerConnectionId(commandStartedEvent.ConnectionId, element.Value.ToBoolean());
                                    break;
                                default:
                                    throw new FormatException($"Unexpected commandStartedEvent field: '{element.Name}'.");
                            }
                        }
                        break;
                    case CommandSucceededEvent commandSucceededEvent:
                        foreach (var element in expectedEventValue)
                        {
                            switch (element.Name)
                            {
                                case "reply":
                                    _valueMatcher.AssertValuesMatch(commandSucceededEvent.Reply, element.Value);
                                    break;
                                case "commandName":
                                    commandSucceededEvent.CommandName.Should().Be(element.Value.AsString);
                                    break;
                                case "hasServiceId":
                                    commandSucceededEvent.ServiceId.Should().Match<ObjectId?>(s => s.HasValue == element.Value.ToBoolean());
                                    break;
                                case "hasServerConnectionId":
                                    AssertHasServerConnectionId(commandSucceededEvent.ConnectionId, element.Value.ToBoolean());
                                    break;
                                default:
                                    throw new FormatException($"Unexpected commandStartedEvent field: '{element.Name}'.");
                            }
                        }
                        break;
                    case CommandFailedEvent commandFailedEvent:
                        foreach (var element in expectedEventValue)
                        {
                            switch (element.Name)
                            {
                                case "commandName":
                                    commandFailedEvent.CommandName.Should().Be(element.Value.AsString);
                                    break;
                                case "hasServiceId":
                                    commandFailedEvent.ServiceId.Should().Match<ObjectId?>(s => s.HasValue == element.Value.ToBoolean());
                                    break;
                                case "hasServerConnectionId":
                                    AssertHasServerConnectionId(commandFailedEvent.ConnectionId, element.Value.ToBoolean());
                                    break;
                                default:
                                    throw new FormatException($"Unexpected commandStartedEvent field: '{element.Name}'.");
                            }
                        }
                        break;
                    case ConnectionOpenedEvent:
                        expectedEventValue.ElementCount.Should().Be(0); // empty document
                        break;
                    case ConnectionPoolCheckingOutConnectionEvent:
                        expectedEventValue.ElementCount.Should().Be(0); // empty document
                        break;
                    case ConnectionPoolCheckedOutConnectionEvent:
                        expectedEventValue.ElementCount.Should().Be(0); // empty document
                        break;
                    case ConnectionPoolCheckedInConnectionEvent:
                        expectedEventValue.ElementCount.Should().Be(0); // empty document
                        break;
                    case ConnectionClosedEvent connectionClosedEvent:
                        {
                            foreach (var element in expectedEventValue)
                            {
                                switch (element.Name)
                                {
                                    case "reason":
                                        //connectionClosedEvent.Reason.Should().Be(reason); // TODO: should be implemented in the scope of CSHARP-3219
                                        break;
                                    default:
                                        throw new FormatException($"Unexpected {expectedEventType} field: '{element.Name}'.");
                                }
                            }
                        }
                        break;
                    case ConnectionCreatedEvent:
                        expectedEventValue.ElementCount.Should().Be(0); // empty document
                        break;
                    case ConnectionPoolCheckingOutConnectionFailedEvent connectionCheckOutFailedEvent:
                        {
                            foreach (var element in expectedEventValue)
                            {
                                switch (element.Name)
                                {
                                    case "reason":
                                        connectionCheckOutFailedEvent.Reason.ToString().ToLower().Should().Be(element.Value.ToString().ToLower());
                                        break;
                                    default:
                                        throw new FormatException($"Unexpected {expectedEventType} field: '{element.Name}'.");
                                }
                            }
                        }
                        break;
                    case ConnectionPoolClearedEvent poolClearedEvent:
                        foreach (var element in expectedEventValue)
                        {
                            switch (element.Name)
                            {
                                case "hasServiceId":
                                    poolClearedEvent.ServiceId.Should().Match<ObjectId?>(s => s.HasValue == element.Value.ToBoolean());
                                    break;
                                case "interruptInUseConnections":
                                    poolClearedEvent.CloseInUseConnections.Should().Be(element.Value.ToBoolean());
                                    break;
                                default:
                                    throw new FormatException($"Unexpected {expectedEventType} field: '{element.Name}'.");
                            }
                        }
                        break;
                    case ConnectionPoolReadyEvent:
                        expectedEventValue.ElementCount.Should().Be(0); // empty document
                        break;
                    case ServerDescriptionChangedEvent serverDescriptionChangedEvent:
                        if (expectedEventValue.Elements.Any())
                        {
                            throw new FormatException($"Unexpected {expectedEventType} fields.");
                        }
                        break;
                    default:
                        throw new FormatException($"Unrecognized event type: '{expectedEventType}'.");
                }
            }

            void AssertHasServerConnectionId(ConnectionId connectionId, bool value)
            {
                // in c# we have fallback logic to get a server connectionId based on an additional getLastError call which is not expected by the spec.
                // So even though servers less than 4.2 don't provide connectionId, we still have this value through getLastError, so don't assert hasServerConnectionId=false.
                if (value)
                {
                    connectionId.LongServerValue.Should().HaveValue();
                }
            }
        }

        private string GetAssertionErrorMessage(List<object> actualEvents, BsonArray expectedEventsDocuments)
        {
            var jsonWriterSettings = new JsonWriterSettings { Indent = true };

            var actualEventsDocuments = new BsonArray();
            foreach (var actualEvent in actualEvents)
            {
                switch (actualEvent)
                {
                    case CommandStartedEvent commandStartedEvent:
                        var commandStartedDocument = new BsonDocument
                        {
                            { "command", commandStartedEvent.Command },
                            { "commandName", commandStartedEvent.CommandName },
                            { "databaseName", commandStartedEvent.DatabaseNamespace.DatabaseName },
                            { "timestamp", commandStartedEvent.Timestamp.ToString("HH:mm:ss.fffffffK") }
                        };
                        actualEventsDocuments.Add(new BsonDocument("commandStartedEvent", commandStartedDocument));
                        break;
                    case CommandSucceededEvent commandSucceededEvent:
                        var commandSucceededDocument = new BsonDocument
                        {
                            { "reply", commandSucceededEvent.Reply },
                            { "commandName", commandSucceededEvent.CommandName },
                            { "timestamp", commandSucceededEvent.Timestamp.ToString("HH:mm:ss.fffffffK") }
                        };
                        actualEventsDocuments.Add(new BsonDocument("commandSucceededEvent", commandSucceededDocument));
                        break;
                    case CommandFailedEvent commandFailedEvent:
                        var commandFailedDocument = new BsonDocument
                        {
                            { "commandName", commandFailedEvent.CommandName },
                            { "timestamp", commandFailedEvent.Timestamp.ToString("HH:mm:ss.fffffffK") }
                        };
                        actualEventsDocuments.Add(new BsonDocument("commandFailedEvent", commandFailedDocument));
                        break;
                    default:
                        actualEventsDocuments.Add(new BsonDocument(actualEvent.GetType().Name, actualEvent.ToString()));
                        break;
                }
            }

            return
                $"Expected events to be: {expectedEventsDocuments.ToJson(jsonWriterSettings)}{Environment.NewLine}" +
                $"But found: {actualEventsDocuments.ToJson(jsonWriterSettings)}.";
        }

        // nested types
        private enum EventSetType
        {
            Command,
            Cmap,
            Sdam
        }
    }
}

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
        private static readonly Dictionary<string, (EventType EventType, EventSetType EventSetType)> __eventsMapWithSpec = new()
        {
            { MongoUtils.ToCamelCase(nameof(CommandStartedEvent)), (EventType.CommandStarted, EventSetType.Command) },
            { MongoUtils.ToCamelCase(nameof(CommandSucceededEvent)), (EventType.CommandSucceeded, EventSetType.Command) },
            { MongoUtils.ToCamelCase(nameof(CommandFailedEvent)), (EventType.CommandFailed, EventSetType.Command) },

            { "connectionReadyEvent", (EventType.ConnectionOpened, EventSetType.Cmap) },
            { "connectionCheckOutStartedEvent", (EventType.ConnectionPoolCheckingOutConnection, EventSetType.Cmap) },
            { "connectionCheckedOutEvent", (EventType.ConnectionPoolCheckedOutConnection, EventSetType.Cmap) },
            { "connectionCheckedInEvent", (EventType.ConnectionPoolCheckedInConnection, EventSetType.Cmap) },
            { "connectionClosedEvent", (EventType.ConnectionClosed, EventSetType.Cmap) },
            { "connectionCreatedEvent", (EventType.ConnectionCreated, EventSetType.Cmap) },
            { "connectionCheckOutFailedEvent", (EventType.ConnectionPoolCheckingOutConnectionFailed, EventSetType.Cmap) },
            { "poolClearedEvent", (EventType.ConnectionPoolCleared, EventSetType.Cmap) },
            { "poolReadyEvent", (EventType.ConnectionPoolReady, EventSetType.Cmap) },

            { "serverDescriptionChangedEvent", (EventType.ServerDescriptionChanged, EventSetType.Sdam) }
        };

        private static readonly Dictionary<EventSetType, Dictionary<EventType, string>> __eventsMapBySetType;

        static UnifiedEventMatcher()
        {
            __eventsMapWithSpec.Values.Select(i => i.EventType).Should().OnlyHaveUniqueItems(); // smoke test for configuration
            __eventsMapBySetType = __eventsMapWithSpec
                .GroupBy(gi => gi.Value.EventSetType)
                .ToDictionary(dk => dk.Key, dv => dv.ToDictionary(idk => idk.Value.EventType, idv => idv.Key));
        }

        internal static List<object> FilterEventsBySetType(IEnumerable<object> events, string eventSetType)
        {
            var eventTypeEnum = (EventSetType)Enum.Parse(typeof(EventSetType), eventSetType, ignoreCase: true);
            var eventsBySetType = __eventsMapBySetType[eventTypeEnum];

            return events
                .OfType<IEvent>()
                .Where(e => eventsBySetType.ContainsKey(e.Type))
                .Cast<object>()
                .ToList();
        }

        public static Func<object, bool> GetEventFilter(BsonDocument expectedEvent)
        {
            var elements = expectedEvent.Elements.Single();
            var expectedSpecEvent = elements.Name;

            return (actualEvent) =>
            {
                var @event = actualEvent.Should().BeAssignableTo<IEvent>().Subject;
                return
                    __eventsMapWithSpec[expectedSpecEvent].EventType == @event.Type &&
                    MatchIfComplexEvent(@event, elements);
            };

            static bool MatchIfComplexEvent(IEvent @event, BsonElement elements) =>
                @event switch
                {
                    ServerDescriptionChangedEvent serverDescriptionChangedvent => IsExpectedServerDescriptionChangedEvent(serverDescriptionChangedvent, elements),
                    _ => true,// validate only type name in the rest of cases
                };

            static bool IsExpectedServerDescriptionChangedEvent(ServerDescriptionChangedEvent serverDescriptionChangedEvent, BsonElement elements)
            {
                var newDescriptionType = GetServerDescriptionChangedFilter(elements);
                return
                    newDescriptionType == null || // no additional filter
                    serverDescriptionChangedEvent.NewDescription.Type == MapServerType(newDescriptionType);

                static string GetServerDescriptionChangedFilter(BsonElement elements)
                {
                    string newDescriptionType = null;
                    var bodyDocument = elements.Value.AsBsonDocument;
                    foreach (var element in bodyDocument.Elements)
                    {
                        switch (element.Name)
                        {
                            case "newDescription": newDescriptionType = element.Value.AsBsonDocument["type"].AsString; break;
                            default: throw new Exception($"Unexpected event filter key: {element.Name}.");
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
                var actualEvent = actualEvents[i].Should().BeAssignableTo<IEvent>().Subject;
                var expectedEventDocument = expectedEventsDocuments[i].AsBsonDocument;
                var expectedEventType = expectedEventDocument.Elements.Should().ContainSingle().Subject.Name;
                var expectedEventValue = expectedEventDocument[0].AsBsonDocument;
                __eventsMapWithSpec[expectedEventType].EventType.Should().Be(actualEvent.Type); // events match

                switch (actualEvent)
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
                                    throw new FormatException($"Unexpected {expectedEventType} field: '{element.Name}'.");
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
                                    throw new FormatException($"Unexpected {expectedEventType} field: '{element.Name}'.");
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
                                    throw new FormatException($"Unexpected {expectedEventType} field: '{element.Name}'.");
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

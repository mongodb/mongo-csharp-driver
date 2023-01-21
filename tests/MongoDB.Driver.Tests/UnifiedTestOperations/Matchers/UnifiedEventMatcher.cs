﻿/* Copyright 2020-present MongoDB Inc.
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
using System.Xml.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using Xunit.Sdk;

namespace MongoDB.Driver.Tests.UnifiedTestOperations.Matchers
{
    public class UnifiedEventMatcher
    {
        #region static
        private static Dictionary<Type, EventType> __eventsMap = new()
        {
            { typeof(CommandStartedEvent), EventType.Command },
            { typeof(CommandSucceededEvent), EventType.Command },
            { typeof(CommandFailedEvent), EventType.Command },

            { typeof(ConnectionOpenedEvent), EventType.Cmap },  // connectionReadyEvent
            { typeof(ConnectionCreatedEvent), EventType.Cmap },
            { typeof(ConnectionPoolCheckedOutConnectionEvent), EventType.Cmap },
            { typeof(ConnectionPoolCheckedInConnectionEvent), EventType.Cmap },
            { typeof(ConnectionClosedEvent), EventType.Cmap },
            { typeof(ConnectionPoolCheckingOutConnectionEvent), EventType.Cmap },
            { typeof(ConnectionPoolCheckingOutConnectionFailedEvent), EventType.Cmap },
            { typeof(ConnectionPoolClearedEvent), EventType.Cmap },
        };

        public static List<object> FilterEventsByType(List<object> events, string eventType)
        {
            if (!Enum.TryParse<EventType>(eventType, ignoreCase: true, out var eventTypeEnum))
            {
                throw new FormatException($"Cannot parse {nameof(eventType)} enum from {eventType}.");
            }

            return events
                .Where(e => __eventsMap[e.GetType()] == eventTypeEnum)
                .ToList();
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

        public void AssertEventsMatch(object actualEvent, BsonDocument expectedEventDocument)
        {
            if (expectedEventDocument.ElementCount != 1)
            {
                throw new FormatException("Expected event document model must contain a single element.");
            }
            var expectedEventType = expectedEventDocument.GetElement(0).Name;
            var expectedEventValue = expectedEventDocument[0].AsBsonDocument;

            switch (expectedEventType)
            {
                case "commandStartedEvent":
                    var commandStartedEvent = actualEvent.Should().BeOfType<CommandStartedEvent>().Subject;
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
                case "commandSucceededEvent":
                    var commandSucceededEvent = actualEvent.Should().BeOfType<CommandSucceededEvent>().Subject;
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
                case "commandFailedEvent":
                    var commandFailedEvent = actualEvent.Should().BeOfType<CommandFailedEvent>().Subject;
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
                case "connectionReadyEvent":
                    actualEvent.Should().BeOfType<ConnectionOpenedEvent>();
                    expectedEventValue.ElementCount.Should().Be(0); // empty document
                    break;
                case "connectionCheckOutStartedEvent":
                    actualEvent.Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
                    expectedEventValue.ElementCount.Should().Be(0); // empty document
                    break;
                case "connectionCheckedOutEvent":
                    actualEvent.Should().BeOfType<ConnectionPoolCheckedOutConnectionEvent>();
                    expectedEventValue.ElementCount.Should().Be(0); // empty document
                    break;
                case "connectionCheckedInEvent":
                    actualEvent.Should().BeOfType<ConnectionPoolCheckedInConnectionEvent>();
                    expectedEventValue.ElementCount.Should().Be(0); // empty document
                    break;
                case "connectionClosedEvent":
                    {
                        var connectionClosedEvent = actualEvent.Should().BeOfType<ConnectionClosedEvent>().Subject;
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
                case "connectionCreatedEvent":
                    actualEvent.Should().BeOfType<ConnectionCreatedEvent>();
                    expectedEventValue.ElementCount.Should().Be(0); // empty document
                    break;
                case "connectionCheckOutFailedEvent":
                    {
                        var connectionCheckOutFailedEvent = actualEvent.Should().BeOfType<ConnectionPoolCheckingOutConnectionFailedEvent>().Subject;
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
                case "poolClearedEvent":
                    var poolClearedEvent = actualEvent.Should().BeOfType<ConnectionPoolClearedEvent>().Subject;
                    foreach (var element in expectedEventValue)
                    {
                        switch (element.Name)
                        {
                            case "hasServiceId":
                                poolClearedEvent.ServiceId.Should().Match<ObjectId?>(s => s.HasValue == element.Value.ToBoolean());
                                break;
                            default:
                                throw new FormatException($"Unexpected {expectedEventType} field: '{element.Name}'.");
                        }
                    }
                    break;
                case "poolReadyEvent":
                    actualEvent.Should().BeOfType<ConnectionPoolReadyEvent>();
                    expectedEventValue.ElementCount.Should().Be(0); // empty document
                    break;
                case "serverDescriptionChangedEvent":
                    var serverDescriptionChangedEvent = actualEvent.Should().BeOfType<ServerDescriptionChangedEvent>().Subject;
                    if (expectedEventValue.Elements.Any())
                    {
                        throw new FormatException($"Unexpected {expectedEventType} fields.");
                    }
                    break;
                    
                default:
                    throw new FormatException($"Unrecognized event type: '{expectedEventType}'.");
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

        public bool DoEventsMatch(object actualEvent, BsonDocument expectedEventDocument)
        {
            try
            {
                AssertEventsMatch(actualEvent, expectedEventDocument);
                return true;
            }
            catch
            {
                return false;
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

                AssertEventsMatch(actualEvent, expectedEventDocument);
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
                            { "databaseName", commandStartedEvent.DatabaseNamespace.DatabaseName }
                        };
                        actualEventsDocuments.Add(new BsonDocument("commandStartedEvent", commandStartedDocument));
                        break;
                    case CommandSucceededEvent commandSucceededEvent:
                        var commandSucceededDocument = new BsonDocument
                        {
                            { "reply", commandSucceededEvent.Reply },
                            { "commandName", commandSucceededEvent.CommandName }
                        };
                        actualEventsDocuments.Add(new BsonDocument("commandSucceededEvent", commandSucceededDocument));
                        break;
                    case CommandFailedEvent commandFailedEvent:
                        var commandFailedDocument = new BsonDocument
                        {
                            { "commandName", commandFailedEvent.CommandName }
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
        private enum EventType
        {
            Command,
            Cmap
        }
    }
}

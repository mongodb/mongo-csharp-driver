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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Events;
using Xunit.Sdk;

namespace MongoDB.Driver.Tests.Specifications.unified_test_format
{
    public class UnifiedEventMatcher
    {
        private UnifiedValueMatcher _valueMatcher;

        public UnifiedEventMatcher(UnifiedValueMatcher valueMatcher)
        {
            _valueMatcher = valueMatcher;
        }

        public void AssertEventsMatch(List<object> actualEvents, BsonArray expectedEventsDocuments)
        {
            try
            {
                AssertEvents(actualEvents, expectedEventsDocuments);
            }
            catch (XunitException exception)
            {
                throw new AssertionException(
                    userMessage: GetAssertionErrorMessage(actualEvents, expectedEventsDocuments),
                    innerException: exception);
            }
        }

        // private methods
        private void AssertEvents(List<object> actualEvents, BsonArray expectedEventsDocuments)
        {
            actualEvents.Should().HaveSameCount(expectedEventsDocuments);

            for (int i = 0; i < actualEvents.Count; i++)
            {
                var actualEvent = actualEvents[i];
                var expectedEventDocument = expectedEventsDocuments[i].AsBsonDocument;
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
                                default:
                                    throw new FormatException($"Unexpected commandStartedEvent field: '{element.Name}'.");
                            }
                        }
                        break;
                    default:
                        throw new FormatException($"Unrecognized event type: '{expectedEventType}'.");
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
                        throw new FormatException($"Unrecognized event type: '{actualEvent.GetType()}'.");
                }
            }

            return
                $"Expected events to be: {expectedEventsDocuments.ToJson(jsonWriterSettings)}{Environment.NewLine}" +
                $"But found: {actualEventsDocuments.ToJson(jsonWriterSettings)}.";
        }
    }
}

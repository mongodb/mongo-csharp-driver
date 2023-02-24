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

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Tests.UnifiedTestOperations.Matchers;
using MongoDB.TestHelpers;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedWaitForEventOperation : IUnifiedSpecialTestOperation
    {
        private readonly EventCapturer _eventCapturer;
        private readonly int _count;
        private readonly BsonDocument _event;

        public UnifiedWaitForEventOperation(EventCapturer eventCapturer, BsonDocument @event, int? count)
        {
            _eventCapturer = Ensure.IsNotNull(eventCapturer, nameof(eventCapturer));
            _event = Ensure.IsNotNull(@event, nameof(@event));
            _count = Ensure.HasValue(count, nameof(count)).Value;
        }

        public void Execute()
        {
            var eventCondition = UnifiedEventMatcher.GetEventFilter(_event);
            Func<IEnumerable<object>, bool> eventsConditionWithFilterByCount = (events) => events.Count(eventCondition) >= _count;

            _eventCapturer.WaitForOrThrowIfTimeout(
                eventsConditionWithFilterByCount,
                TimeSpan.FromSeconds(10),
                (timeout) =>
                {
                    var triggeredEventsCount = _eventCapturer.Events.Count(eventCondition);
                    return $"Waiting for {_count} of {FluentAssertionsHelper.EscapeBraces(_event.ToString())} exceeded the timeout {timeout}. The number of triggered events is {triggeredEventsCount}.";
                });
        }
    }

    public sealed class UnifiedWaitForEventOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedWaitForEventOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedWaitForEventOperation Build(BsonDocument arguments)
        {
            EventCapturer eventCapturer = null;
            int? count = null;
            BsonDocument @event = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "client":
                        eventCapturer = _entityMap.EventCapturers[argument.Value.AsString];
                        break;
                    case "count":
                        count = argument.Value.AsInt32;
                        break;
                    case "event":
                        @event = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedWaitForEventOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedWaitForEventOperation(eventCapturer, @event, count);
        }
    }
}

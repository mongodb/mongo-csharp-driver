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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Tests.UnifiedTestOperations.Matchers;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedWaitForEventOperation : IUnifiedSpecialTestOperation
    {
        private readonly int _count;
        private readonly BsonDocument _eventDocument;
        private readonly EventCapturer _eventCapturer;
        private readonly UnifiedEventMatcher _unifiedEventMatcher;

        public UnifiedWaitForEventOperation(
            UnifiedEventMatcher unifiedEventMatcher,
            EventCapturer eventCapturer,
            BsonDocument eventDocument,
            int count)
        {
            _count = count;
            _eventCapturer = eventCapturer;
            _eventDocument = eventDocument;
            _unifiedEventMatcher = unifiedEventMatcher;
        }

        public void Execute()
        {
            _eventCapturer.WaitForOrThrowIfTimeout(
                events => events.Where(DoEventsMatch).Take(_count).Count() == _count,
                TimeSpan.FromSeconds(10),
                timeout =>
                {
                    var triggeredEventsCount = _eventCapturer.Events.Count(DoEventsMatch);
                    return $"Waiting for {_count} {_eventDocument} exceeded the timeout {timeout}. The number of triggered events is {triggeredEventsCount}.";
                });

            bool DoEventsMatch(object @event) => _unifiedEventMatcher.DoEventsMatch(@event, _eventDocument);
        }
    }

    public class UnifiedWaitForEventOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedWaitForEventOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedWaitForEventOperation Build(BsonDocument arguments)
        {
            var clientId = arguments["client"].AsString;
            var eventObject = arguments["event"].AsBsonDocument;
            var count = arguments["count"].AsInt32;

            if (arguments.ElementCount != 3)
            {
                throw new FormatException($"Invalid {nameof(UnifiedWaitForEventOperation)} arguments count.");
            }

            var eventCapturer = _entityMap.EventCapturers[clientId];
            var unifiedEventMatcher = new UnifiedEventMatcher(new UnifiedValueMatcher(_entityMap));

            return new UnifiedWaitForEventOperation(unifiedEventMatcher, eventCapturer, eventObject, count);
        }
    }
}

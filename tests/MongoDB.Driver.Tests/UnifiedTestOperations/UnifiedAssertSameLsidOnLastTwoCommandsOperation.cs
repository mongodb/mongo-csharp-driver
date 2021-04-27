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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedAssertSameLsidOnLastTwoCommandsOperation : IUnifiedSpecialTestOperation
    {
        private readonly EventCapturer _eventCapturer;

        public UnifiedAssertSameLsidOnLastTwoCommandsOperation(EventCapturer eventCapturer)
        {
            _eventCapturer = eventCapturer;
        }

        public void Execute()
        {
            var lastTwoLsids = _eventCapturer
                .Events
                .Skip(_eventCapturer.Events.Count - 2)
                .Select(commandStartedEvent => ((CommandStartedEvent)commandStartedEvent).Command["lsid"])
                .ToList();

            lastTwoLsids[0].Should().Be(lastTwoLsids[1]);
        }
    }

    public class UnifiedAssertSameLsidOnLastTwoCommandsOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedAssertSameLsidOnLastTwoCommandsOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedAssertSameLsidOnLastTwoCommandsOperation Build(BsonDocument arguments)
        {
            EventCapturer eventCapturer = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "client":
                        eventCapturer = _entityMap.EventCapturers[argument.Value.AsString];
                        break;
                    default:
                        throw new FormatException($"Invalid AssertSameLsidOnLastTwoCommandsOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedAssertSameLsidOnLastTwoCommandsOperation(eventCapturer);
        }
    }
}

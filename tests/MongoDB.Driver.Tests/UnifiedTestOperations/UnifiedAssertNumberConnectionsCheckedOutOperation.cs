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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedAssertNumberConnectionsCheckedOutOperation : IUnifiedSpecialTestOperation
    {
        private readonly EventCapturer _eventCapturer;
        private readonly int _requiredConnectionNumber;

        public UnifiedAssertNumberConnectionsCheckedOutOperation(EventCapturer eventCapturer, int? requiredConnectionNumber)
        {
            _eventCapturer = Ensure.IsNotNull(eventCapturer, nameof(eventCapturer));
            _requiredConnectionNumber = Ensure.HasValue(requiredConnectionNumber, nameof(requiredConnectionNumber)).Value;
        }

        public void Execute()
        {
            var checkedOutEvents = _eventCapturer.Events.OfType<ConnectionPoolCheckedOutConnectionEvent>().Count();
            var checkedInEvents = _eventCapturer.Events.OfType<ConnectionPoolCheckedInConnectionEvent>().Count();
            (checkedOutEvents - checkedInEvents).Should().Be(_requiredConnectionNumber);
        }
    }

    public class UnifiedAssertNumberConnectionsCheckedOutOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedAssertNumberConnectionsCheckedOutOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedAssertNumberConnectionsCheckedOutOperation Build(BsonDocument arguments)
        {
            EventCapturer eventCapturer = null;
            int? requiredConnectionNumber = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "client":
                        eventCapturer = _entityMap.EventCapturers[argument.Value.AsString];
                        break;
                    case "connections":
                        requiredConnectionNumber = argument.Value.ToInt32();
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedAssertNumberConnectionsCheckedOutOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedAssertNumberConnectionsCheckedOutOperation(eventCapturer, requiredConnectionNumber);
        }
    }
}

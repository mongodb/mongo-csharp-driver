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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.TestHelpers
{
    public static class EventSpecMapper
    {
        private static Dictionary<string, Type> __eventsMap = new Dictionary<string, Type>
        {
            { "PoolCreatedEvent", typeof(ConnectionPoolOpenedEvent) },
            { "PoolReadyEvent", null }, // TODO:
            { "PoolClearedEvent", typeof(ConnectionPoolClearedEvent) },
            { "PoolClosedEvent", typeof(ConnectionPoolClosedEvent) },
            { "ConnectionCreatedEvent", typeof(ConnectionCreatedEvent) },
            { "ConnectionReadyEvent", null }, // TODO:
            { "ConnectionClosedEvent", typeof(ConnectionClosedEvent) },
            { "ConnectionCheckOutStartedEvent", typeof(ConnectionPoolCheckingOutConnectionEvent) },
            { "ConnectionCheckOutFailedEvent", typeof(ConnectionPoolCheckingOutConnectionFailedEvent) },
            { "ConnectionCheckedOutEvent", typeof(ConnectionPoolCheckedOutConnectionEvent) },
            { "ConnectionCheckedInEvent", typeof(ConnectionPoolCheckedInConnectionEvent) },
            { "CommandStartedEvent", typeof(CommandStartedEvent) },
            { "CommandSucceededEvent", typeof(CommandSucceededEvent) },
            { "CommandFailedEvent", typeof(CommandFailedEvent) },
        };

        public static string GetSpecEventName(string eventName) => __eventsMap.Single(e => e.Value.Name.ToLower() == eventName.ToLower()).Key;
        public static Type GetEventType(string specEventName) => __eventsMap.Single(e => e.Key.ToLower() == specEventName.ToLower()).Value;
    }
}

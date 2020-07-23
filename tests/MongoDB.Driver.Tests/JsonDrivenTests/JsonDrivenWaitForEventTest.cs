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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenWaitForEventTest : JsonDrivenTestRunnerTest
    {
        private int _count;
        private string _event;
        private readonly EventCapturer _eventCapturer;

        public JsonDrivenWaitForEventTest(
            IJsonDrivenTestRunner testRunner,
            Dictionary<string, object> objectMap,
            EventCapturer eventCapturer) : base(testRunner, objectMap)
        {
            _eventCapturer = eventCapturer;
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            Wait();
        }

        protected override Task CallMethodAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => Wait());
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "count":
                    _count = value.ToInt32();
                    return;

                case "event":
                    _event = value.ToString();
                    return;
            }

            base.SetArgument(name, value);
        }

        // private methods
        private Func<object, bool> MapEventNameToCondition(string eventName)
        {
            switch (eventName)
            {
                case "ServerMarkedUnknownEvent":
                    return @event =>
                        @event is ServerDescriptionChangedEvent serverDescriptionChangedEvent &&
                        serverDescriptionChangedEvent.NewDescription.Type == ServerType.Unknown;

                case "PoolClearedEvent":
                    return @event => @event is ConnectionPoolClearedEvent;

                default:
                    throw new Exception($"Unexpected event type {eventName}.");
            }
        }

        private void Wait()
        {
            var eventCondition = MapEventNameToCondition(_event);
            Func<IEnumerable<object>, bool> eventsConditionWithFilterByCount = (events) => events.Count(eventCondition) >= _count;

            _eventCapturer.WaitForOrThrowIfTimeout(
                eventsConditionWithFilterByCount,
                TimeSpan.FromSeconds(10),
                (timeout) =>
                {
                    var triggeredEventsCount = _eventCapturer.Events.Count(eventCondition);
                    return $"Waiting for {_count} {_event} exceeded the timeout {timeout}. The number of triggered events is {triggeredEventsCount}.";
                });
        }
    }
}

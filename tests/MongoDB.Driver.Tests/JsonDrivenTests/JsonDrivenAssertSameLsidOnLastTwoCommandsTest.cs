/* Copyright 2019-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public class JsonDrivenAssertSameLsidOnLastTwoCommandsTest : JsonDrivenTestRunnerTest
    {
        private readonly EventCapturer _eventCapturer;

        public JsonDrivenAssertSameLsidOnLastTwoCommandsTest(IJsonDrivenTestRunner testRunner, EventCapturer eventCapturer, Dictionary<string, object> objectMap)
            : base(testRunner, objectMap)
        {
            _eventCapturer = eventCapturer;
        }

        public override void Act(CancellationToken cancellationToken)
        {
            // do nothing
        }

        public override Task ActAsync(CancellationToken cancellationToken)
        {
            // do nothing
            return Task.FromResult(true);
        }

        public override void Assert()
        {
            var lastTwoCommands = _eventCapturer
                .Events
                .Skip(_eventCapturer.Events.Count - 2)
                .Select(commandStartedEvent => ((CommandStartedEvent)commandStartedEvent).Command)
                .ToList();

            AssertSameLsid(lastTwoCommands[0], lastTwoCommands[1]);
        }

        // private methods
        private void AssertSameLsid(BsonDocument first, BsonDocument second)
        {
            first["lsid"].Should().Be(second["lsid"]);
        }
    }
}

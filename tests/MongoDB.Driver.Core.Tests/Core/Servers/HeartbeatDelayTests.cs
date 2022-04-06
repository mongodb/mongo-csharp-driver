/* Copyright 2013-present MongoDB Inc.
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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    public class HeartbeatDelayTests
    {
        [Fact]
        public void Dispose_can_be_called_multiple_times()
        {
            var subject = new HeartbeatDelay(TimeSpan.FromHours(10), TimeSpan.FromMilliseconds(1));

            subject.Dispose();
            subject.Dispose();
        }

        [Theory]
        [ParameterAttributeData]
        public void RequestHeartbeat_should_respect_to_minHeartbeatInterval([Values(10, -1)]int heartbeatIntervalInMinutes)
        {
            var heartbeatInterval = heartbeatIntervalInMinutes == -1 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMinutes(heartbeatIntervalInMinutes);
            var minHeartbeatInterval = TimeSpan.FromSeconds(2);

            var stopwatch = Stopwatch.StartNew();
            var subject = new HeartbeatDelay(heartbeatInterval, minHeartbeatInterval);
            subject.RequestHeartbeat();
            var timeout = TimeSpan.FromMinutes(1);
            var result = Task.WaitAny(subject.Task, Task.Delay(timeout));
            if (result != 0)
            {
                throw new Exception($"The test timeout {timeout} is exceeded.");
            }
            stopwatch.Stop();

            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(minHeartbeatInterval - TimeSpan.FromMilliseconds(15));
        }

        [Fact]
        public void Task_should_complete_when_heartbeatInterval_has_expired()
        {
            var subject = new HeartbeatDelay(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

            subject.Task.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();
        }

        [Fact]
        public void Task_should_complete_when_early_heartbeat_is_requested()
        {
            var subject = new HeartbeatDelay(TimeSpan.FromHours(10), TimeSpan.FromMilliseconds(1));

            subject.RequestHeartbeat();

            subject.Task.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();
        }
    }
}

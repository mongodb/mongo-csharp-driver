/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Async;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Async
{
    [TestFixture]
    public class AsyncBackgroundTasksTests
    {

        [Test]
        public void Start_should_throw_ArgumentNullException_when_action_is_null()
        {
            Action act = () => AsyncBackgroundTask.Start(null, Timeout.InfiniteTimeSpan, CancellationToken.None);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Start_should_run_the_action_on_the_specified_interval()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            int count = 0;
            AsyncBackgroundTask.Start(
                ct => 
                { 
                    count++; 
                    if (count == 5)
                    {
                        cancellationTokenSource.Cancel();
                    }
                    return Task.FromResult(true);
                },
                TimeSpan.FromMilliseconds(5),
                cancellationTokenSource.Token);

            SpinWait.SpinUntil(() => count >= 5, 4000);
            count.Should().Be(5);
        }

        [Test]
        public async Task Start_should_run_the_action_only_once_when_the_delay_is_infinite()
        {
            int count = 0;
            AsyncBackgroundTask.Start(
                ct =>
                {
                    count++;
                    return Task.FromResult(true);
                },
                Timeout.InfiniteTimeSpan,
                CancellationToken.None);

            SpinWait.SpinUntil(() => count >= 1, 4000);
            count.Should().Be(1);
        }
    }
}
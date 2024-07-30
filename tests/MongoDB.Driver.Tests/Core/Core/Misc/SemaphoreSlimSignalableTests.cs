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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class SemaphoreSlimSignalableTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Constructor_should_check_arguments([Values(-2, -1)] int count)
        {
            var exception = Record.Exception(() => new SemaphoreSlimSignalable(count));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("initialCount");
            e.Message.Should().StartWith("Value is not greater than or equal to 0");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Reset_should_clear_signal(
            [Values(true, false)] bool async)
        {
            const int threadsCount = 4;
            var semaphore = new SemaphoreSlimSignalable(0);

            semaphore.Signal();
            semaphore.Reset();

            var waitTasks = WaitAsync(semaphore, async, true, threadsCount, TimeSpan.FromSeconds(5));

            for (int i = 0; i < threadsCount; i++)
            {
                semaphore.Release();
            }

            var results = await waitTasks;

            Assert(results, SemaphoreSlimSignalable.SemaphoreWaitResult.Entered);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Reset_should_not_reset_non_signaled(
            [Values(true, false)] bool async)
        {
            const int threadsCount = 4;
            var semaphore = new SemaphoreSlimSignalable(0);

            var waitTasks = WaitAsync(semaphore, async, true, threadsCount, TimeSpan.FromSeconds(5));

            semaphore.Reset();
            semaphore.Signal();

            var results = await waitTasks;

            Assert(results, SemaphoreSlimSignalable.SemaphoreWaitResult.Signaled);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_enter(
            [Values(true, false)] bool async,
            [Values(true, false)] bool isSignaledWait,
            [Values(0, 1, 2)] int initialCount,
            [Values(2, 4)] int threadsCount)
        {
            var semaphore = new SemaphoreSlimSignalable(initialCount);

            var resultsTask = WaitAsync(semaphore, async, isSignaledWait, threadsCount, Timeout.InfiniteTimeSpan);

            for (int i = 0; i < threadsCount - initialCount; i++)
            {
                semaphore.Release();
            }

            var results = await resultsTask;

            Assert(results, SemaphoreSlimSignalable.SemaphoreWaitResult.Entered);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_timeout(
            [Values(true, false)] bool async,
            [Values(true, false)] bool isSignaledWait,
            [Values(5, 10)] int timeoutMS)
        {
            const int threadsCount = 4;
            var semaphore = new SemaphoreSlimSignalable(0);

            var results = await WaitAsync(semaphore, async, isSignaledWait, threadsCount, TimeSpan.FromMilliseconds(timeoutMS));
            Assert(results, SemaphoreSlimSignalable.SemaphoreWaitResult.TimedOut);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_signal(
            [Values(true, false)] bool async,
            [Values(true, false)] bool signalBeforeWait)
        {
            const int threadsCount = 4;
            var semaphore = new SemaphoreSlimSignalable(0);

            if (signalBeforeWait)
            {
                semaphore.Signal();
            }
            var waitTask = WaitAsync(semaphore, async, true, threadsCount, Timeout.InfiniteTimeSpan);

            if (!signalBeforeWait)
            {
                semaphore.Signal();
            }

            var results = await waitTask;
            Assert(results, SemaphoreSlimSignalable.SemaphoreWaitResult.Signaled);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_cancel(
            [Values(true, false)] bool async,
            [Values(true, false)] bool isSignaledWait)
        {
            const int threadsCount = 4;
            var semaphore = new SemaphoreSlimSignalable(0);

            using var cancelationTokenSource = new CancellationTokenSource();

            var tasks = CreateWaitTasks(semaphore, async, isSignaledWait, threadsCount, Timeout.InfiniteTimeSpan, cancelationTokenSource.Token);

            cancelationTokenSource.Cancel();

            foreach (var task in tasks)
            {
                var exception = await Record.ExceptionAsync(() => task);

                exception.Should().BeOfType<OperationCanceledException>();
            }
        }

        [Fact]
        public async Task WaitSignaledAsync_should_not_continue_on_signal_thread()
        {
            using var cancelationTokenSource = new CancellationTokenSource();

            var semaphore = new SemaphoreSlimSignalable(0);
            var waitStartedEvent = new ManualResetEventSlim(false);

            var signalThreadTerminated = false;
            var signalThread = SignalThread();

            for (int i = 0; i < 100; i++)
            {
                semaphore.Reset();
                waitStartedEvent.Reset();

                var waitSignaledTask = WaitSignaledTask();
                await waitSignaledTask;

                waitSignaledTask.Result.Should().NotBe(signalThread.ManagedThreadId);
            }

            Volatile.Write(ref signalThreadTerminated, true);
            waitStartedEvent.Set();
            signalThread.Join(100);

            async Task<int> WaitSignaledTask()
            {
                await Task.Yield();

                var waitTask = semaphore.WaitSignaledAsync(Timeout.InfiniteTimeSpan, cancelationTokenSource.Token);
                waitStartedEvent.Set();

                var waitResult = await waitTask;
                waitResult.Should().Be(SemaphoreSlimSignalable.SemaphoreWaitResult.Signaled);

                return Thread.CurrentThread.ManagedThreadId;
            }

            Thread SignalThread()
            {
                var thread = new Thread(_ =>
                {
                    while (!Volatile.Read(ref signalThreadTerminated))
                    {
                        waitStartedEvent.Wait();
                        waitStartedEvent.Reset();

                        semaphore.Signal();
                    }
                });

                thread.IsBackground = true;
                thread.Start();

                return thread;
            }
        }

        // private methods
        private Task<SemaphoreSlimSignalable.SemaphoreWaitResult[]> WaitAsync(
            SemaphoreSlimSignalable semaphore,
            bool async,
            bool isSignaledWait,
            int threadsCount,
            TimeSpan timeout,
            CancellationToken cancellationToken = default) =>
            Task.WhenAll(CreateWaitTasks(semaphore, async, isSignaledWait, threadsCount, timeout, cancellationToken));

        private Task<SemaphoreSlimSignalable.SemaphoreWaitResult>[] CreateWaitTasks(
            SemaphoreSlimSignalable semaphore,
            bool async,
            bool isSignaledWait,
            int threadsCount,
            TimeSpan timeout,
            CancellationToken cancellationToken = default) =>
            async ?
                TasksUtils.CreateTasks(threadsCount, _ => isSignaledWait ? semaphore.WaitSignaledAsync(timeout, cancellationToken) : semaphore.WaitAsync(timeout, cancellationToken)) :
                TasksUtils.CreateTasksOnOwnThread(threadsCount, _ => isSignaledWait ? semaphore.WaitSignaled(timeout, cancellationToken) : semaphore.Wait(timeout, cancellationToken));

        private void Assert(SemaphoreSlimSignalable.SemaphoreWaitResult[] actual, SemaphoreSlimSignalable.SemaphoreWaitResult expected) =>
            actual.All(r => r == expected).Should().BeTrue();
    }
}

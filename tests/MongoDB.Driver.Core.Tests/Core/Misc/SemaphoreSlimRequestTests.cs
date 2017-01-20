/* Copyright 2015-2016 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class SemaphoreSlimRequestTests
    {
        // public methods
        [Fact]
        public void constructor_should_initialize_instance_with_completed_task_when_semaphore_is_available()
        {
            var semaphore = new SemaphoreSlim(1);

            var result = new SemaphoreSlimRequest(semaphore, CancellationToken.None);

            result.Task.Status.Should().Be(TaskStatus.RanToCompletion);
            semaphore.CurrentCount.Should().Be(0);
        }

        [Fact]
        public void constructor_should_initialize_instance_with_incompleted_task_when_semaphore_is_not_available()
        {
            var semaphore = new SemaphoreSlim(1);
            semaphore.Wait();

            var result = new SemaphoreSlimRequest(semaphore, CancellationToken.None);

            result.Task.IsCompleted.Should().BeFalse();
            semaphore.CurrentCount.Should().Be(0);
        }

        [Fact]
        public void constructor_should_throw_when_semaphore_is_null()
        {
            Action action = () => new SemaphoreSlimRequest(null, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("semaphore");
        }

        [Fact]
        public void Dispose_should_cancel_pending_request()
        {
            var semaphore = new SemaphoreSlim(1);
            semaphore.Wait();
            var subject = new SemaphoreSlimRequest(semaphore, CancellationToken.None);

            subject.Dispose();
            semaphore.Release();

            subject.Task.Status.Should().Be(TaskStatus.Canceled);
            semaphore.CurrentCount.Should().Be(1);
        }

        [Fact]
        public void Dispose_should_release_semaphore()
        {
            var semaphore = new SemaphoreSlim(1);
            var subject = new SemaphoreSlimRequest(semaphore, CancellationToken.None);

            subject.Dispose();

            semaphore.CurrentCount.Should().Be(1);
        }

        [Fact]
        public void Sempahore_should_not_be_released_when_cancellation_is_requested_after_semaphore_is_acquired()
        {
            var semaphore = new SemaphoreSlim(1);
            var cancellationTokenSource = new CancellationTokenSource();
            var subject = new SemaphoreSlimRequest(semaphore, cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();

            semaphore.CurrentCount.Should().Be(0);
        }

        [Fact]
        public void Task_should_be_cancelled_when_cancellationToken_requests_cancellation()
        {
            var semaphore = new SemaphoreSlim(1);
            var cancellationTokenSource = new CancellationTokenSource();
            semaphore.Wait();
            var subject = new SemaphoreSlimRequest(semaphore, cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();
            SpinWait.SpinUntil(() => subject.Task.IsCompleted, TimeSpan.FromSeconds(5)).Should().BeTrue();
            semaphore.Release();

            subject.Task.Status.Should().Be(TaskStatus.Canceled);
            semaphore.CurrentCount.Should().Be(1);
        }

        [Fact]
        public void Task_should_be_completed_when_semaphore_becomes_available()
        {
            var semaphore = new SemaphoreSlim(1);
            semaphore.Wait();
            var subject = new SemaphoreSlimRequest(semaphore, CancellationToken.None);

            semaphore.Release();
            SpinWait.SpinUntil(() => subject.Task.IsCompleted, TimeSpan.FromSeconds(5)).Should().BeTrue();

            subject.Task.Status.Should().Be(TaskStatus.RanToCompletion);
            semaphore.CurrentCount.Should().Be(0);
        }
    }
}

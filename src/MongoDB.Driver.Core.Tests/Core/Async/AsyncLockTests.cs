/* Copyright 2015 MongoDB Inc.
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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Async
{
    [TestFixture]
    public class AsyncLockTests
    {
        [Test]
        public void Request_Dispose_should_cancel_request_when_lock_was_not_taken()
        {
            var subject = new AsyncLock();
            var request1 = subject.Request();
            var request2 = subject.Request();
            var request3 = subject.Request();

            request2.Dispose();
            request1.Dispose();

            request2.Task.Status.Should().Be(TaskStatus.Canceled);
            request3.Task.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Test]
        public void Request_Dispose_should_give_lock_to_next_waiter_when_there_is_a_waiter()
        {
            var subject = new AsyncLock();
            var request1 = subject.Request();
            var request2 = subject.Request();

            request1.Dispose();

            request2.Task.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Test]
        public void Request_Dispose_should_release_lock_when_there_is_no_waiter()
        {
            var subject = new AsyncLock();

            subject.Request().Dispose();

            subject.Request().Task.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Test]
        public void Request_should_be_cancelled_when_cancellation_is_requested_before_lock_was_taken()
        {
            var subject = new AsyncLock();
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var request1 = subject.Request();
            var request2 = subject.Request(cancellationToken);
            var request3 = subject.Request();

            cts.Cancel();
            request1.Dispose();

            request3.Task.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Test]
        public void Request_should_not_be_cancelled_when_cancellation_is_requested_after_lock_was_taken()
        {
            var subject = new AsyncLock();
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var request = subject.Request(cancellationToken);

            cts.Cancel();

            request.Task.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Test]
        public void Request_should_queue_waiter_when_lock_is_alread_taken()
        {
            var subject = new AsyncLock();
            subject.Request();

            var result = subject.Request();

            result.Task.Status.Should().Be(TaskStatus.WaitingForActivation);
        }

        [Test]
        public void Request_should_take_lock_when_lock_is_not_already_taken()
        {
            var subject = new AsyncLock();

            var result = subject.Request();

            result.Task.Status.Should().Be(TaskStatus.RanToCompletion);
        }
    }
}

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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Async;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Async
{
    [TestFixture]
    public class AsyncDropboxTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void GetAwaiters_should_return_the_right_number_of_awaiters(int n)
        {
            var subject = new AsyncDropbox<int, int>();
            for (var i = 0; i < n; i++)
            {
                subject.ReceiveAsync(i, Timeout.InfiniteTimeSpan, CancellationToken.None);
            }

            var count = subject.GetAwaiters().Count();
            count.Should().Be(n);
        }

        [Test]
        public void Receive_async_should_return_an_uncompleted_task_when_the_message_does_not_exists()
        {
            var subject = new AsyncDropbox<int, int>();

            var result = subject.ReceiveAsync(10, Timeout.InfiniteTimeSpan, CancellationToken.None);

            result.IsCompleted.Should().BeFalse();
        }

        [Test]
        public void Receive_async_should_return_a_completed_task_when_then_message_already_exists()
        {
            var subject = new AsyncDropbox<int, int>();
            subject.Post(10, 11);

            var result = subject.ReceiveAsync(10, Timeout.InfiniteTimeSpan, CancellationToken.None);

            result.Result.Should().Be(11);
        }

        [Test]
        public async Task Receive_async_should_remove_message_from_dropbox()
        {
            var subject = new AsyncDropbox<int, int>();
            subject.Post(10, 11);

            subject.MessageCount.Should().Be(1);
            var result = await subject.ReceiveAsync(10, Timeout.InfiniteTimeSpan, CancellationToken.None);
            subject.MessageCount.Should().Be(0);

            result.Should().Be(11);
        }

        [Test]
        public void Receive_async_should_complete_when_message_is_posted()
        {
            var subject = new AsyncDropbox<int, int>();

            var result = subject.ReceiveAsync(10, Timeout.InfiniteTimeSpan, CancellationToken.None);
            subject.Post(10, 11);

            result.Result.Should().Be(11);
        }

        [Test]
        public void Received_messages_with_the_same_id_should_throw()
        {
            var subject = new AsyncDropbox<int, int>();

            subject.Post(10, 11);
            Action act = () => subject.Post(10, 12);
            act.ShouldThrow<ArgumentException>();
        }
    }
}
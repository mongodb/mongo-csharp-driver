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
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public void Receive_async_should_complete_when_message_is_posted()
        {
            var subject = new AsyncDropbox<int, int>();

            var result = subject.ReceiveAsync(10, Timeout.InfiniteTimeSpan, CancellationToken.None);
            subject.Post(10, 11);

            result.Result.Should().Be(11);
        }

        [Test]
        public void Items_with_the_same_id_should_be_completed_in_the_order_they_were_posted()
        {
            var subject = new AsyncDropbox<int, int>();

            subject.Post(10, 11);
            subject.Post(10, 12);

            var result = subject.ReceiveAsync(10, Timeout.InfiniteTimeSpan, CancellationToken.None);
            var result2 = subject.ReceiveAsync(10, Timeout.InfiniteTimeSpan, CancellationToken.None);

            result.Result.Should().Be(11);
            result2.Result.Should().Be(12);
        }
    }
}
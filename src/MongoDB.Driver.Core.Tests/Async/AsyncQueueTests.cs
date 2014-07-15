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

using FluentAssertions;
using MongoDB.Driver.Core.Async;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Async
{
    [TestFixture]
    public class AsyncQueueTests
    {
        [Test]
        public void Items_should_be_dequeued_in_the_order_they_were_enqueued()
        {
            var subject = new AsyncQueue<int>();
            subject.Enqueue(10);
            subject.Enqueue(11);
            var result = subject.DequeueAsync();
            var result2 = subject.DequeueAsync();

            result.Result.Should().Be(10);
            result2.Result.Should().Be(11);
        }

        [Test]
        public void Items_should_be_dequeued_in_the_order_they_were_enqueued_2()
        {
            var subject = new AsyncQueue<int>();
            var result = subject.DequeueAsync();
            var result2 = subject.DequeueAsync();
            subject.Enqueue(10);
            subject.Enqueue(11);

            result.Result.Should().Be(10);
            result2.Result.Should().Be(11);
        }

        [Test]
        public void Dequeue_should_return_an_uncompleted_task_when_no_items_exist()
        {
            var subject = new AsyncQueue<int>();
            var result = subject.DequeueAsync();

            result.IsCompleted.Should().BeFalse();
        }

        [Test]
        public void Dequeue_should_complete_when_an_item_is_added_to_the_queue()
        {
            var subject = new AsyncQueue<int>();
            var result = subject.DequeueAsync();
            subject.Enqueue(10);

            result.Result.Should().Be(10);
        }
    }
}
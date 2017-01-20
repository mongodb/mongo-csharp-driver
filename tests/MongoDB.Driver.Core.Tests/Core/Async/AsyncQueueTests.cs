/* Copyright 2013-2016 MongoDB Inc.
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Async;
using Xunit;

namespace MongoDB.Driver.Core.Async
{
    public class AsyncQueueTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void DequeueAll_should_dequeue_all_items(int n)
        {
            var subject = new AsyncQueue<int>();
            for (var i = 0; i < n; i++)
            {
                subject.Enqueue(i);
            }

            var count = subject.DequeueAll().Count();
            count.Should().Be(n);
        }

        [Fact]
        public void Items_should_be_dequeued_in_the_order_they_were_enqueued()
        {
            var subject = new AsyncQueue<int>();
            subject.Enqueue(10);
            subject.Enqueue(11);
            var result = subject.DequeueAsync(CancellationToken.None);
            var result2 = subject.DequeueAsync(CancellationToken.None);

            result.Result.Should().Be(10);
            result2.Result.Should().Be(11);
        }

        [Fact]
        public void Items_should_be_dequeued_in_the_order_they_were_enqueued_2()
        {
            var subject = new AsyncQueue<int>();
            var result = subject.DequeueAsync(CancellationToken.None);
            var result2 = subject.DequeueAsync(CancellationToken.None);
            subject.Enqueue(10);
            subject.Enqueue(11);

            result.Result.Should().Be(10);
            result2.Result.Should().Be(11);
        }

        [Fact]
        public void Dequeue_should_return_an_uncompleted_task_when_no_items_exist()
        {
            var subject = new AsyncQueue<int>();
            var result = subject.DequeueAsync(CancellationToken.None);

            result.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public void Dequeue_should_complete_when_an_item_is_added_to_the_queue()
        {
            var subject = new AsyncQueue<int>();
            var result = subject.DequeueAsync(CancellationToken.None);
            subject.Enqueue(10);

            result.Result.Should().Be(10);
        }

        [Fact]
        public void Dequeue_should_cancel_when_cancellation_is_requested()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var subject = new AsyncQueue<int>();
            var result = subject.DequeueAsync(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();

            Assert.Throws<TaskCanceledException>(() => result.GetAwaiter().GetResult());
        }
    }
}
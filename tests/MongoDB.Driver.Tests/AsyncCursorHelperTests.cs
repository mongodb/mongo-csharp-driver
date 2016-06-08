/* Copyright 2016 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AsyncCursorHelperTests
    {
        [Fact]
        public void AnyAsync_should_return_true_when_a_result_exists()
        {
            var task = SetupResultInFirstBatch();

            var result = AsyncCursorHelper.AnyAsync(task, CancellationToken.None).Result;

            result.Should().Be(true);
        }

        [Fact]
        public void AnyAsync_should_return_false_when_no_result_exists()
        {
            var task = SetupNoResultIn1Batch();

            var result = AsyncCursorHelper.AnyAsync(task, CancellationToken.None).Result;

            result.Should().Be(false);
        }

        [Fact]
        public void AnyAsync_should_return_true_when_result_exists_but_is_in_the_second_batch()
        {
            var task = SetupResultInSecondBatch();

            var result = AsyncCursorHelper.AnyAsync(task, CancellationToken.None).Result;

            result.Should().Be(true);
        }

        [Fact]
        public void AnyAsync_should_return_false_when_no_result_exists_delayed_to_the_second_batch()
        {
            var task = SetupNoResultInTwoBatches();

            var result = AsyncCursorHelper.AnyAsync(task, CancellationToken.None).Result;

            result.Should().Be(false);
        }

        [Fact]
        public void FirstAsync_should_return_first_result_when_one_exists()
        {
            var task = SetupResultInFirstBatch();

            var result = AsyncCursorHelper.FirstAsync(task, CancellationToken.None).Result;

            result.Should().Be(1);
        }

        [Fact]
        public void FirstAsync_should_throw_an_exception_when_no_results_exist()
        {
            var task = SetupNoResultIn1Batch();

            Action act = () => AsyncCursorHelper.FirstAsync(task, CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<Exception>();
        }

        [Fact]
        public void FirstAsync_should_return_first_result_when_one_exists_but_is_in_the_second_batch()
        {
            var task = SetupResultInSecondBatch();

            var result = AsyncCursorHelper.FirstAsync(task, CancellationToken.None).Result;

            result.Should().Be(1);
        }

        [Fact]
        public void FirstAsync_should_throw_an_exception_when_no_result_exists_delayed_to_the_second_batch()
        {
            var task = SetupNoResultInTwoBatches();

            Action act = () => AsyncCursorHelper.FirstAsync(task, CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<Exception>();
        }

        [Fact]
        public void FirstOrDefaultAsync_should_return_first_result_when_one_exists()
        {
            var task = SetupResultInFirstBatch();

            var result = AsyncCursorHelper.FirstOrDefaultAsync(task, CancellationToken.None).Result;

            result.Should().Be(1);
        }

        [Fact]
        public void FirstOrDefaultAsync_should_return_default_when_no_results_exist()
        {
            var task = SetupNoResultIn1Batch();

            var result = AsyncCursorHelper.FirstOrDefaultAsync(task, CancellationToken.None).Result;

            result.Should().Be(0);
        }

        [Fact]
        public void FirstOrDefaultAsync_should_return_first_result_when_one_exists_but_is_in_the_second_batch()
        {
            var task = SetupResultInSecondBatch();

            var result = AsyncCursorHelper.FirstOrDefaultAsync(task, CancellationToken.None).Result;

            result.Should().Be(1);
        }

        [Fact]
        public void FirstOrDefaultAsync_should_return_default_value_when_no_result_exists_delayed_to_the_second_batch()
        {
            var task = SetupNoResultInTwoBatches();

            var result = AsyncCursorHelper.FirstOrDefaultAsync(task, CancellationToken.None).Result;

            result.Should().Be(0);
        }

        [Fact]
        public void SingleAsync_should_return_first_result_when_one_exists()
        {
            var task = SetupResultInFirstBatch();

            var result = AsyncCursorHelper.SingleAsync(task, CancellationToken.None).Result;

            result.Should().Be(1);
        }

        [Fact]
        public void SingleAsync_should_throw_an_exception_when_no_results_exist()
        {
            var task = SetupNoResultIn1Batch();

            Action act = () => AsyncCursorHelper.SingleAsync(task, CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<Exception>();
        }

        [Fact]
        public void SingleAsync_should_return_first_result_when_one_exists_but_is_in_the_second_batch()
        {
            var task = SetupResultInSecondBatch();

            var result = AsyncCursorHelper.SingleAsync(task, CancellationToken.None).Result;

            result.Should().Be(1);
        }

        [Fact]
        public void SingleAsync_should_throw_an_exception_when_no_result_exists_delayed_to_the_second_batch()
        {
            var task = SetupNoResultInTwoBatches();

            Action act = () => AsyncCursorHelper.SingleAsync(task, CancellationToken.None).GetAwaiter().GetResult();

            act.ShouldThrow<Exception>();
        }

        [Fact]
        public void SingleOrDefaultAsync_should_return_first_result_when_one_exists()
        {
            var task = SetupResultInFirstBatch();

            var result = AsyncCursorHelper.SingleOrDefaultAsync(task, CancellationToken.None).Result;

            result.Should().Be(1);
        }

        [Fact]
        public void SingleOrDefaultAsync_should_return_default_when_no_results_exist()
        {
            var task = SetupNoResultIn1Batch();

            var result = AsyncCursorHelper.SingleOrDefaultAsync(task, CancellationToken.None).Result;

            result.Should().Be(0);
        }

        [Fact]
        public void SingleOrDefaultAsync_should_return_first_result_when_one_exists_but_is_in_the_second_batch()
        {
            var task = SetupResultInSecondBatch();

            var result = AsyncCursorHelper.SingleOrDefaultAsync(task, CancellationToken.None).Result;

            result.Should().Be(1);
        }

        [Fact]
        public void SingleOrDefaultAsync_should_return_default_value_when_no_result_exists_delayed_to_the_second_batch()
        {
            var task = SetupNoResultInTwoBatches();

            var result = AsyncCursorHelper.SingleOrDefaultAsync(task, CancellationToken.None).Result;

            result.Should().Be(0);
        }

        private Task<IAsyncCursor<int>> SetupResultInFirstBatch()
        {
            var mockCursor = new Mock<IAsyncCursor<int>>();
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true)).Returns(Task.FromResult(false));
            mockCursor.SetupSequence(c => c.Current).Returns(new List<int> { 1 }).Returns(null);
            return Task.FromResult(mockCursor.Object);
        }

        private Task<IAsyncCursor<int>> SetupResultInSecondBatch()
        {
            var mockCursor = new Mock<IAsyncCursor<int>>();
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true)).Returns(Task.FromResult(true)).Returns(Task.FromResult(false));
            mockCursor.SetupSequence(c => c.Current).Returns(new List<int>()).Returns(new List<int> { 1 }).Returns(null);
            return Task.FromResult(mockCursor.Object);
        }

        private Task<IAsyncCursor<int>> SetupNoResultIn1Batch()
        {
            var mockCursor = new Mock<IAsyncCursor<int>>();
            mockCursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));
            mockCursor.SetupGet(c => c.Current).Returns((IEnumerable<int>)null);
            return Task.FromResult(mockCursor.Object);
        }

        private Task<IAsyncCursor<int>> SetupNoResultInTwoBatches()
        {
            var mockCursor = new Mock<IAsyncCursor<int>>();
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true)).Returns(Task.FromResult(false));
            mockCursor.SetupSequence(c => c.Current).Returns(new List<int>()).Returns(null);
            return Task.FromResult(mockCursor.Object);
        }
    }
}

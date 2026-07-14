/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class DeferredAsyncCursorTests
    {
        [Fact]
        public void Dispose_should_invoke_dispose_action_when_never_iterated()
        {
            var disposeActionCallCount = 0;
            var subject = CreateSubject(() => disposeActionCallCount++);

            subject.Dispose();

            disposeActionCallCount.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Dispose_should_invoke_dispose_action_when_iterated(
            [Values(false, true)] bool async)
        {
            var disposeActionCallCount = 0;
            var subject = CreateSubject(() => disposeActionCallCount++);

            if (async)
            {
                await subject.MoveNextAsync(CancellationToken.None);
            }
            else
            {
                subject.MoveNext(CancellationToken.None);
            }
            subject.Dispose();

            disposeActionCallCount.Should().Be(1);
        }

        [Fact]
        public void Dispose_should_be_idempotent()
        {
            var disposeActionCallCount = 0;
            var subject = CreateSubject(() => disposeActionCallCount++);

            subject.Dispose();
            subject.Dispose();

            disposeActionCallCount.Should().Be(1);
        }

        [Fact]
        public void Dispose_should_dispose_cursor_and_stay_disposed_when_dispose_action_throws()
        {
            var innerCursor = new Mock<IAsyncCursor<BsonDocument>>();
            var disposeActionCallCount = 0;
            var subject = new DeferredAsyncCursor<BsonDocument>(
                () => { disposeActionCallCount++; throw new InvalidOperationException(); },
                _ => innerCursor.Object,
                _ => Task.FromResult(innerCursor.Object));
            subject.MoveNext(CancellationToken.None); // populate the underlying cursor

            Record.Exception(() => subject.Dispose()).Should().BeOfType<InvalidOperationException>();
            subject.Dispose(); // must not run the dispose action a second time

            disposeActionCallCount.Should().Be(1);
            innerCursor.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_should_invoke_dispose_action_when_never_iterated()
        {
            var disposeActionCallCount = 0;
            var subject = CreateSubject(() => disposeActionCallCount++);

            await subject.DisposeAsync();

            disposeActionCallCount.Should().Be(1);
        }

        [Fact]
        public async Task DisposeAsync_should_dispose_cursor_and_invoke_dispose_action_when_iterated()
        {
            var disposeActionCallCount = 0;
            var innerCursor = new Mock<IAsyncCursor<BsonDocument>>();
            innerCursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            var subject = new DeferredAsyncCursor<BsonDocument>(
                () => disposeActionCallCount++,
                _ => innerCursor.Object,
                _ => Task.FromResult(innerCursor.Object));

            await subject.MoveNextAsync(CancellationToken.None);
            await subject.DisposeAsync();

            disposeActionCallCount.Should().Be(1);
            innerCursor.Verify(c => c.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_should_be_idempotent()
        {
            var disposeActionCallCount = 0;
            var subject = CreateSubject(() => disposeActionCallCount++);

            await subject.DisposeAsync();
            await subject.DisposeAsync();

            disposeActionCallCount.Should().Be(1);
        }

        [Fact]
        public void MoveNext_should_throw_when_disposed()
        {
            var subject = CreateSubject(() => { });
            subject.Dispose();

            var exception = Record.Exception(() => subject.MoveNext(CancellationToken.None));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Fact]
        public void Current_should_throw_when_disposed()
        {
            var subject = CreateSubject(() => { });
            subject.Dispose();

            var exception = Record.Exception(() => { _ = subject.Current; });

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        // private methods
        private DeferredAsyncCursor<BsonDocument> CreateSubject(Action disposeAction)
        {
            return new DeferredAsyncCursor<BsonDocument>(
                disposeAction,
                _ => Mock.Of<IAsyncCursor<BsonDocument>>(),
                _ => Task.FromResult(Mock.Of<IAsyncCursor<BsonDocument>>()));
        }
    }
}

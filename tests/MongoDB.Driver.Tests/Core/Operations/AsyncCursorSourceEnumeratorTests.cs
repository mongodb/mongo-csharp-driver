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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Core.Operations
{
    public class AsyncCursorSourceEnumeratorTests
    {
        [Fact]
        public void Constructor_should_throw_when_cursorSource_is_null()
        {
            var exception = Record.Exception(() => new AsyncCursorSourceEnumerator<BsonDocument>(null, CancellationToken.None));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Current_should_throw_when_enumeration_has_not_started()
        {
            var source = CreateCursorSource(1);
            var enumerator = new AsyncCursorSourceEnumerator<BsonDocument>(source, CancellationToken.None);

            var exception = Record.Exception(() => enumerator.Current);

            exception.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Contain("Enumeration has not started");
        }

        [Fact]
        public async Task Current_should_return_expected_document_after_MoveNextAsync()
        {
            var source = CreateCursorSource(1);
            var enumerator = new AsyncCursorSourceEnumerator<BsonDocument>(source, CancellationToken.None);
            var expectedDocument = new BsonDocument("_id", 0);

            await enumerator.MoveNextAsync();
            var result = enumerator.Current;

            result.Should().Be(expectedDocument);
        }

        [Fact]
        public async Task MoveNextAsync_should_execute_query_on_first_call()
        {
            var mockSource = new Mock<IAsyncCursorSource<BsonDocument>>();
            var cursor = CreateCursor(1);
            mockSource.Setup(s => s.ToCursorAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursor);

            var enumerator = new AsyncCursorSourceEnumerator<BsonDocument>(mockSource.Object, CancellationToken.None);

            // Query should not execute until first MoveNextAsync call
            mockSource.Verify(s => s.ToCursorAsync(It.IsAny<CancellationToken>()), Times.Never);

            await enumerator.MoveNextAsync();

            // Query should execute exactly once
            mockSource.Verify(s => s.ToCursorAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MoveNextAsync_should_not_execute_query_on_subsequent_calls()
        {
            var mockSource = new Mock<IAsyncCursorSource<BsonDocument>>();
            var cursor = CreateCursor(2);
            mockSource.Setup(s => s.ToCursorAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursor);

            var enumerator = new AsyncCursorSourceEnumerator<BsonDocument>(mockSource.Object, CancellationToken.None);

            await enumerator.MoveNextAsync(); // First call
            await enumerator.MoveNextAsync(); // Second call

            // Query should execute exactly once, not twice
            mockSource.Verify(s => s.ToCursorAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MoveNextAsync_should_enumerate_all_documents()
        {
            var source = CreateCursorSource(3);
            var enumerator = new AsyncCursorSourceEnumerator<BsonDocument>(source, CancellationToken.None);
            var expectedDocuments = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1),
                new BsonDocument("_id", 2)
            };

            var actualDocuments = new List<BsonDocument>();
            while (await enumerator.MoveNextAsync())
            {
                actualDocuments.Add(enumerator.Current);
            }

            actualDocuments.Should().Equal(expectedDocuments);
        }

        [Fact]
        public async Task MoveNextAsync_should_throw_when_disposed()
        {
            var source = CreateCursorSource(1);
            var enumerator = new AsyncCursorSourceEnumerator<BsonDocument>(source, CancellationToken.None);

            await enumerator.DisposeAsync();

            var exception = await Record.ExceptionAsync(async () => await enumerator.MoveNextAsync());

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Fact]
        public async Task MoveNextAsync_should_respect_cancellation_token()
        {
            var mockSource = new Mock<IAsyncCursorSource<BsonDocument>>();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            mockSource.Setup(s => s.ToCursorAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var enumerator = new AsyncCursorSourceEnumerator<BsonDocument>(mockSource.Object, cts.Token);

            var exception = await Record.ExceptionAsync(async () => await enumerator.MoveNextAsync());

            exception.Should().BeOfType<OperationCanceledException>();
        }

        [Fact]
        public void Reset_should_throw_NotSupportedException()
        {
            var source = CreateCursorSource(1);
            var enumerator = new AsyncCursorSourceEnumerator<BsonDocument>(source, CancellationToken.None);

            var exception = Record.Exception(() => enumerator.Reset());

            exception.Should().BeOfType<NotSupportedException>();
        }

        // Helper methods
        private IAsyncCursor<BsonDocument> CreateCursor(int count)
        {
            var firstBatch = Enumerable.Range(0, count)
                .Select(i => new BsonDocument("_id", i))
                .ToArray();

            return new AsyncCursor<BsonDocument>(
                channelSource: new Mock<IChannelSource>().Object,
                collectionNamespace: new CollectionNamespace("test", "collection"),
                comment: null,
                firstBatch: firstBatch,
                cursorId: 0,
                batchSize: null,
                limit: null,
                serializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: new MessageEncoderSettings(),
                maxTime: null);
        }

        private IAsyncCursorSource<BsonDocument> CreateCursorSource(int count)
        {
            var mockCursorSource = new Mock<IAsyncCursorSource<BsonDocument>>();
            mockCursorSource.Setup(s => s.ToCursorAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CreateCursor(count));

            return mockCursorSource.Object;
        }
    }
}
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
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver
{
    public class IAsyncCursorExtensionsTests
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void Any_should_return_expected_result(
            [Values(0, 1, 2)] int count,
            [Values(false, true)] bool async)
        {
            var cursor = CreateCursor(count);
            var expectedResult = count > 0;

            bool result;
            if (async)
            {
                result = cursor.AnyAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = cursor.Any();
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void First_should_return_expected_result(
            [Values(1, 2)] int count,
            [Values(false, true)] bool async)
        {
            var cursor = CreateCursor(count);
            var expectedResult = new BsonDocument("_id", 0);

            BsonDocument result;
            if (async)
            {
                result = cursor.FirstAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = cursor.First();
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void First_should_throw_when_cursor_has_no_documents(
            [Values(false, true)] bool async)
        {
            var cursor = CreateCursor(0);

            Action action;
            if (async)
            {
                action = () => cursor.FirstAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => cursor.First();
            }

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void FirstOrDefault_should_return_expected_result(
            [Values(0, 1, 2)] int count,
            [Values(false, true)] bool async)
        {
            var cursor = CreateCursor(count);
            var expectedResult = count == 0 ? null : new BsonDocument("_id", 0);

            BsonDocument result;
            if (async)
            {
                result = cursor.FirstOrDefaultAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = cursor.FirstOrDefault();
            }

            result.Should().Be(expectedResult);
        }

        [Fact]
        public async Task ForEachAsync_should_dispose_cursor_through_the_asynchronous_path()
        {
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            var batch = new[] { new BsonDocument("_id", 1) };
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);
            mockCursor.Setup(c => c.Current).Returns(batch);

            await mockCursor.Object.ForEachAsync(_ => { });

            mockCursor.Verify(c => c.DisposeAsync(), Times.Once);
            mockCursor.Verify(c => c.Dispose(), Times.Never);
        }

        [Theory]
        [ParameterAttributeData]
        public void Single_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var cursor = CreateCursor(1);
            var expectedResult = new BsonDocument("_id", 0);

            BsonDocument result;
            if (async)
            {
                result = cursor.SingleAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = cursor.Single();
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void Single_should_throw_when_cursor_has_wrong_number_of_documents(
            [Values(0, 2)] int count,
            [Values(false, true)] bool async)
        {
            var cursor = CreateCursor(count);

            Action action;
            if (async)
            {
                action = () => cursor.SingleAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => cursor.Single();
            }

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void SingleOrDefault_should_return_expected_result(
            [Values(0, 1)] int count,
            [Values(false, true)] bool async)
        {
            var cursor = CreateCursor(count);
            var expectedResult = count == 0 ? null : new BsonDocument("_id", 0);

            BsonDocument result;
            if (async)
            {
                result = cursor.SingleOrDefaultAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = cursor.SingleOrDefault();
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void SingleOrDefault_should_throw_when_cursor_has_wrong_number_of_documents(
            [Values(false, true)] bool async)
        {
            var cursor = CreateCursor(2);

            Action action;
            if (async)
            {
                action = () => cursor.SingleOrDefaultAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => cursor.SingleOrDefault();
            }

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Terminal_operator_should_dispose_cursor_through_the_expected_path(
            [Values(
                TerminalOperator.Any,
                TerminalOperator.First,
                TerminalOperator.FirstOrDefault,
                TerminalOperator.Single,
                TerminalOperator.SingleOrDefault,
                TerminalOperator.ToList)]
            TerminalOperator terminalOperator,
            [Values(false, true)] bool async)
        {
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            var batch = new[] { new BsonDocument("_id", 1) };
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);
            mockCursor.Setup(c => c.Current).Returns(batch);
            var cursor = mockCursor.Object;

            if (async)
            {
                await InvokeAsync(terminalOperator, cursor);
                mockCursor.Verify(c => c.DisposeAsync(), Times.Once);
                mockCursor.Verify(c => c.Dispose(), Times.Never);
            }
            else
            {
                Invoke(terminalOperator, cursor);
                mockCursor.Verify(c => c.Dispose(), Times.Once);
                mockCursor.Verify(c => c.DisposeAsync(), Times.Never);
            }
        }

        [Fact]
        public void ToAsyncEnumerable_result_should_only_be_enumerable_one_time()
        {
            var cursor = CreateCursor(2);
            var enumerable = cursor.ToAsyncEnumerable();
            enumerable.GetAsyncEnumerator();

            Record.Exception(() => enumerable.GetAsyncEnumerator()).Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public async Task ToAsyncEnumerable_should_respect_cancellation_token()
        {
            var source = CreateCursor(5);
            using var cts = new CancellationTokenSource();

            var count = 0;
            var exception = await Record.ExceptionAsync(async () =>
            {
                await foreach (var doc in source.ToAsyncEnumerable().WithCancellation(cts.Token))
                {
                    count++;
                    if (count == 2)
                        cts.Cancel();
                }
            });

            exception.Should().BeOfType<OperationCanceledException>();
        }

        [Fact]
        public async Task ToAsyncEnumerable_should_return_expected_result()
        {
            var cursor = CreateCursor(2);
            var expectedDocuments = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1)
            };

            var result = new List<BsonDocument>();
            await foreach (var doc in cursor.ToAsyncEnumerable())
            {
                result.Add(doc);
            }

            result.Should().Equal(expectedDocuments);
        }

        [Fact]
        public void ToEnumerable_result_should_only_be_enumerable_one_time()
        {
            var cursor = CreateCursor(2);
            var enumerable = cursor.ToEnumerable();
            enumerable.GetEnumerator();

            Action action = () => enumerable.GetEnumerator();

            action.ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void ToEnumerable_should_return_expected_result()
        {
            var cursor = CreateCursor(2);
            var expectedDocuments = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1)
            };

            var result = cursor.ToEnumerable();

            result.ToList().Should().Equal(expectedDocuments);
        }

        [Theory]
        [ParameterAttributeData]
        public void ToList_should_only_be_callable_one_time(
            [Values(false, true)] bool async)
        {
            var cursor = CreateCursor(2);
            cursor.ToList();

            Action action;
            if (async)
            {
                action = () => cursor.ToListAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => cursor.ToList();
            }

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ToList_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var cursor = CreateCursor(2);
            var expectedResult = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1)
            };

            List<BsonDocument> result;
            if (async)
            {
                result = cursor.ToListAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = cursor.ToList();
            }

            result.Should().Equal(expectedResult);
        }

        // private methods
        private IAsyncCursor<BsonDocument> CreateCursor(int count)
        {
            var firstBatch = Enumerable.Range(0, count)
                .Select(i => new BsonDocument("_id", i))
                .ToArray();

            return new AsyncCursor<BsonDocument>(
                channelSource: new Mock<IChannelSource>().Object,
                session: new Mock<ICoreSessionHandle>().Object,
                collectionNamespace: new CollectionNamespace("foo", "bar"),
                comment: null,
                firstBatch: firstBatch,
                cursorId: 0,
                batchSize: null,
                limit: null,
                serializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: new MessageEncoderSettings(),
                maxTime: null,
                retryRequested: false,
                maxAdaptiveRetries: 2,
                enableOverloadRetargeting: false);
        }

        private static void Invoke(TerminalOperator terminalOperator, IAsyncCursor<BsonDocument> cursor)
        {
            switch (terminalOperator)
            {
                case TerminalOperator.Any: cursor.Any(); break;
                case TerminalOperator.First: cursor.First(); break;
                case TerminalOperator.FirstOrDefault: cursor.FirstOrDefault(); break;
                case TerminalOperator.Single: cursor.Single(); break;
                case TerminalOperator.SingleOrDefault: cursor.SingleOrDefault(); break;
                case TerminalOperator.ToList: cursor.ToList(); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(terminalOperator), terminalOperator, null);
            }
        }

        private static async Task InvokeAsync(TerminalOperator terminalOperator, IAsyncCursor<BsonDocument> cursor)
        {
            switch (terminalOperator)
            {
                case TerminalOperator.Any: await cursor.AnyAsync(); break;
                case TerminalOperator.First: await cursor.FirstAsync(); break;
                case TerminalOperator.FirstOrDefault: await cursor.FirstOrDefaultAsync(); break;
                case TerminalOperator.Single: await cursor.SingleAsync(); break;
                case TerminalOperator.SingleOrDefault: await cursor.SingleOrDefaultAsync(); break;
                case TerminalOperator.ToList: await cursor.ToListAsync(); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(terminalOperator), terminalOperator, null);
            }
        }

        public enum TerminalOperator
        {
            Any,
            First,
            FirstOrDefault,
            Single,
            SingleOrDefault,
            ToList
        }
    }
}

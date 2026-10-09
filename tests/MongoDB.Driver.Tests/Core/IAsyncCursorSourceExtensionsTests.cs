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
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver
{
    public class IAsyncCursorSourceExtensionsTests
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void Any_should_return_expected_result(
            [Values(0, 1, 2)] int count,
            [Values(false, true)] bool async)
        {
            var source = CreateCursorSource(count);
            var expectedResult = count > 0;

            bool result;
            if (async)
            {
                result = source.AnyAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = source.Any();
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void First_should_return_expected_result(
          [Values(1, 2)] int count,
          [Values(false, true)] bool async)
        {
            var source = CreateCursorSource(count);
            var expectedResult = new BsonDocument("_id", 0);

            BsonDocument result;
            if (async)
            {
                result = source.FirstAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = source.First();
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void First_should_throw_when_cursor_has_no_documents(
            [Values(false, true)] bool async)
        {
            var source = CreateCursorSource(0);

            Action action;
            if (async)
            {
                action = () => source.FirstAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => source.First();
            }

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void FirstOrDefault_should_return_expected_result(
            [Values(0, 1, 2)] int count,
            [Values(false, true)] bool async)
        {
            var source = CreateCursorSource(count);
            var expectedResult = count == 0 ? null : new BsonDocument("_id", 0);

            BsonDocument result;
            if (async)
            {
                result = source.FirstOrDefaultAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = source.FirstOrDefault();
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ForEachAsync_should_not_execute_the_query_when_processor_is_null(
            [Values(
                ForEachAsyncOverload.Action,
                ForEachAsyncOverload.ActionWithIndex,
                ForEachAsyncOverload.Func,
                ForEachAsyncOverload.FuncWithIndex)]
            ForEachAsyncOverload overload)
        {
            var mockCursorSource = new Mock<IAsyncCursorSource<BsonDocument>>();

            await Record.ExceptionAsync(() => InvokeForEachAsync(overload, mockCursorSource.Object, nullProcessor: true));

            mockCursorSource.Verify(s => s.ToCursorAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ForEachAsync_should_throw_when_argument_is_null(
            [Values(
                ForEachAsyncOverload.Action,
                ForEachAsyncOverload.ActionWithIndex,
                ForEachAsyncOverload.Func,
                ForEachAsyncOverload.FuncWithIndex)]
            ForEachAsyncOverload overload,
            [Values("processor", "source")] string nullArgument)
        {
            var source = nullArgument == "source" ? null : CreateCursorSource(1);

            var exception = await Record.ExceptionAsync(() => InvokeForEachAsync(overload, source, nullProcessor: nullArgument == "processor"));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be(nullArgument);
        }

        [Theory]
        [ParameterAttributeData]
        public void Single_should_return_expected_result(
            [Values(false, true)] bool async)
        {
            var source = CreateCursorSource(1);
            var expectedResult = new BsonDocument("_id", 0);

            BsonDocument result;
            if (async)
            {
                result = source.SingleAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = source.Single();
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void Single_should_throw_when_cursor_has_wrong_number_of_documents(
            [Values(0, 2)] int count,
            [Values(false, true)] bool async)
        {
            var source = CreateCursorSource(count);

            Action action;
            if (async)
            {
                action = () => source.SingleAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => source.Single();
            }

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void SingleOrDefault_should_return_expected_result(
            [Values(0, 1)] int count,
            [Values(false, true)] bool async)
        {
            var source = CreateCursorSource(count);
            var expectedResult = count == 0 ? null : new BsonDocument("_id", 0);

            BsonDocument result;
            if (async)
            {
                result = source.SingleOrDefaultAsync().GetAwaiter().GetResult();
            }
            else
            {
                result = source.SingleOrDefault();
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void SingleOrDefault_should_throw_when_cursor_has_wrong_number_of_documents(
            [Values(false, true)] bool async)
        {
            var source = CreateCursorSource(2);

            Action action;
            if (async)
            {
                action = () => source.SingleOrDefaultAsync().GetAwaiter().GetResult();
            }
            else
            {
                action = () => source.SingleOrDefault();
            }

            action.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Terminal_operator_should_throw_when_source_is_null(
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
            IAsyncCursorSource<BsonDocument> source = null;

            var exception = async ?
                await Record.ExceptionAsync(() => InvokeAsync(terminalOperator, source)) :
                Record.Exception(() => Invoke(terminalOperator, source));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("source");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ToAsyncEnumerable_result_should_be_enumerable_multiple_times(
            [Values(1, 2)] int times)
        {
            var source = CreateCursorSource(2);
            var expectedDocuments = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1)
            };

            var result = new List<BsonDocument>();
            for (var i = 0; i < times; i++)
            {
                await foreach (var doc in source.ToAsyncEnumerable())
                {
                    result.Add(doc);
                }

                result.Should().Equal(expectedDocuments);
                result.Clear();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ToEnumerable_result_should_be_enumerable_multiple_times(
            [Values(1, 2)] int times)
        {
            var source = CreateCursorSource(2);
            var expectedDocuments = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1)
            };

            IEnumerable<BsonDocument> result = null;
            for (var i = 0; i < times; i++)
            {
                result = source.ToEnumerable();

                result.ToList().Should().Equal(expectedDocuments);
            }
        }

        [Fact]
        public void ToEnumerable_should_return_expected_result()
        {
            var source = CreateCursorSource(2);
            var expectedDocuments = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1)
            };

            var result = source.ToEnumerable();

            result.ToList().Should().Equal(expectedDocuments);
        }

        [Theory]
        [ParameterAttributeData]
        public void ToEnumerable_should_throw_when_source_is_null(
            [Values(false, true)] bool async)
        {
            IAsyncCursorSource<BsonDocument> source = null;

            var exception = async ?
                Record.Exception(() => source.ToAsyncEnumerable()) :
                Record.Exception(() => source.ToEnumerable());

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("source");
        }

        [Theory]
        [ParameterAttributeData]
        public void ToList_should_be_callable_multiple_times(
            [Values(1, 2)] int times)
        {
            var source = CreateCursorSource(2);
            var expectedResult = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1)
            };

            List<BsonDocument> result = null;
            for (var i = 0; i < times; i++)
            {
                result = source.ToList();
            }

            result.Should().Equal(expectedResult);
        }

        [Fact]
        public void ToList_should_return_expected_result()
        {
            var source = CreateCursorSource(2);
            var expectedResult = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1)
            };

            var result = source.ToList();

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

        private IAsyncCursorSource<BsonDocument> CreateCursorSource(int count)
        {

            var mockCursorSource = new Mock<IAsyncCursorSource<BsonDocument>>();
            mockCursorSource.Setup(s => s.ToCursor(It.IsAny<CancellationToken>())).Returns(() => CreateCursor(count));
            mockCursorSource.Setup(s => s.ToCursorAsync(It.IsAny<CancellationToken>())).Returns(() => Task.FromResult<IAsyncCursor<BsonDocument>>(CreateCursor(count)));

            return mockCursorSource.Object;
        }

        private static void Invoke(TerminalOperator terminalOperator, IAsyncCursorSource<BsonDocument> source)
        {
            switch (terminalOperator)
            {
                case TerminalOperator.Any: source.Any(); break;
                case TerminalOperator.First: source.First(); break;
                case TerminalOperator.FirstOrDefault: source.FirstOrDefault(); break;
                case TerminalOperator.Single: source.Single(); break;
                case TerminalOperator.SingleOrDefault: source.SingleOrDefault(); break;
                case TerminalOperator.ToList: source.ToList(); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(terminalOperator), terminalOperator, null);
            }
        }

        private static async Task InvokeAsync(TerminalOperator terminalOperator, IAsyncCursorSource<BsonDocument> source)
        {
            switch (terminalOperator)
            {
                case TerminalOperator.Any: await source.AnyAsync(); break;
                case TerminalOperator.First: await source.FirstAsync(); break;
                case TerminalOperator.FirstOrDefault: await source.FirstOrDefaultAsync(); break;
                case TerminalOperator.Single: await source.SingleAsync(); break;
                case TerminalOperator.SingleOrDefault: await source.SingleOrDefaultAsync(); break;
                case TerminalOperator.ToList: await source.ToListAsync(); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(terminalOperator), terminalOperator, null);
            }
        }

        private static Task InvokeForEachAsync(ForEachAsyncOverload overload, IAsyncCursorSource<BsonDocument> source, bool nullProcessor)
        {
            switch (overload)
            {
                case ForEachAsyncOverload.Action:
                    Action<BsonDocument> action = _ => { };
                    return source.ForEachAsync(nullProcessor ? null : action);

                case ForEachAsyncOverload.ActionWithIndex:
                    Action<BsonDocument, int> actionWithIndex = (_, _) => { };
                    return source.ForEachAsync(nullProcessor ? null : actionWithIndex);

                case ForEachAsyncOverload.Func:
                    Func<BsonDocument, Task> func = _ => Task.CompletedTask;
                    return source.ForEachAsync(nullProcessor ? null : func);

                case ForEachAsyncOverload.FuncWithIndex:
                    Func<BsonDocument, int, Task> funcWithIndex = (_, _) => Task.CompletedTask;
                    return source.ForEachAsync(nullProcessor ? null : funcWithIndex);

                default:
                    throw new ArgumentOutOfRangeException(nameof(overload), overload, null);
            }
        }

        public enum ForEachAsyncOverload
        {
            Action,
            ActionWithIndex,
            Func,
            FuncWithIndex
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

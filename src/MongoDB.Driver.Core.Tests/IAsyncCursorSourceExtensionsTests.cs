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
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver
{
    [TestFixture]
    public class IAsyncCursorSourceExtensionsTests
    {
        // public methods
        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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
                channelSource: Substitute.For<IChannelSource>(),
                collectionNamespace: new CollectionNamespace("foo", "bar"),
                query: new BsonDocument(),
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

            var source = Substitute.For<IAsyncCursorSource<BsonDocument>>();
            source.ToCursor(Arg.Any<CancellationToken>()).Returns(_ => CreateCursor(count));
            source.ToCursorAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<IAsyncCursor<BsonDocument>>(CreateCursor(count)));

            return source;
        }
    }
}

/* Copyright 2015-2016 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
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
    }
}

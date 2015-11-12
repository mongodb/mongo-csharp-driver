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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class AsyncCursorEnumerableOneTimeAdapterTests
    {
        [Test]
        public void constructor_should_throw_when_cursor_is_null()
        {
            Action action = () => new AsyncCursorEnumerableOneTimeAdapter<BsonDocument>(null, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("cursor");
        }

        [Test]
        public void GetEnumerator_should_return_expected_result()
        {
            var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            cursor.MoveNext().Returns(true, false);
            cursor.Current.Returns(new[] { new BsonDocument("_id", 0) });
            var subject = new AsyncCursorEnumerableOneTimeAdapter<BsonDocument>(cursor, CancellationToken.None);

            var result = subject.GetEnumerator();

            result.MoveNext().Should().BeTrue();
            result.Current.Should().Be(new BsonDocument("_id", 0));
            result.MoveNext().Should().BeFalse();
        }

        [Test]
        public void GetEnumerator_should_throw_when_called_more_than_once()
        {
            var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            var subject = new AsyncCursorEnumerableOneTimeAdapter<BsonDocument>(cursor, CancellationToken.None);
            subject.GetEnumerator();

            Action action = () => subject.GetEnumerator();

            action.ShouldThrow<InvalidOperationException>();
        }
    }
}

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
    public class AsyncCursorSourceEnumerableAdapterTests
    {
        [Test]
        public void constructor_should_throw_when_source_is_null()
        {
            Action action = () => new AsyncCursorSourceEnumerableAdapter<BsonDocument>(null, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("source");
        }

        [Test]
        public void GetEnumerator_should_call_ToCursor_each_time(
            [Values(1, 2)] int times)
        {
            var source = Substitute.For<IAsyncCursorSource<BsonDocument>>();
            var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            source.ToCursor().Returns(cursor);
            var subject = new AsyncCursorSourceEnumerableAdapter<BsonDocument>(source, CancellationToken.None);

            for (var i = 0; i < times; i++)
            {
                subject.GetEnumerator();
            }

            source.Received(times).ToCursor();
        }
    }
}

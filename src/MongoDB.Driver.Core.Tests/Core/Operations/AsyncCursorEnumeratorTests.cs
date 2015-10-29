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
using System.Linq;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class AsyncCursorEnumeratorTests
    {
        // public methods
        [Test]
        public void constructor_should_throw_when_cursor_is_null()
        {
            Action action = () => new AsyncCursorEnumerator<BsonDocument>(null, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("cursor");
        }

        [Test]
        public void Current_should_return_expected_result()
        {
            var subject = CreateSubject(2);

            subject.MoveNext();
            subject.Current.Should().Be(new BsonDocument("_id", 0));
            subject.MoveNext();
            subject.Current.Should().Be(new BsonDocument("_id", 1));
        }

        [Test]
        public void Current_should_return_expected_result_when_there_are_two_batches()
        {
            var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            var firstBatch = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1)
            };
            var secondBatch = new[]
            {
                new BsonDocument("_id", 2)
            };
            cursor.MoveNext().Returns(true, true, false);
            cursor.Current.Returns(firstBatch, secondBatch);
            var subject = new AsyncCursorEnumerator<BsonDocument>(cursor, CancellationToken.None);

            subject.MoveNext();
            subject.Current.Should().Be(new BsonDocument("_id", 0));
            subject.MoveNext();
            subject.Current.Should().Be(new BsonDocument("_id", 1));
            subject.MoveNext();
            subject.Current.Should().Be(new BsonDocument("_id", 2));
        }

        [Test]
        public void Current_should_throw_when_MoveNext_has_not_been_called_first()
        {
            var subject = CreateSubject(1);

            Action action = () => { var ignore = subject.Current; };

            action.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void Current_should_throw_when_MoveNext_returns_false()
        {
            var subject = CreateSubject(0);
            subject.MoveNext();

            Action action = () => { var ignore = subject.Current; };

            action.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void Current_should_throw_when_subject_has_been_disposed()
        {
            var subject = CreateSubject(0);
            subject.Dispose();

            Action action = () => { var ignore = subject.Current; };

            action.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void Dispose_should_dispose_cursor()
        {
            var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            var subject = new AsyncCursorEnumerator<BsonDocument>(cursor, CancellationToken.None);

            subject.Dispose();

            cursor.Received(1).Dispose();
        }

        [Test]
        public void MoveNext_should_return_expected_result()
        {
            var subject = CreateSubject(2);

            subject.MoveNext().Should().BeTrue();
            subject.MoveNext().Should().BeTrue();
            subject.MoveNext().Should().BeFalse();
        }

        [Test]
        public void MoveNext_should_return_expected_result_when_there_are_two_batches()
        {
            var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            var firstBatch = new[]
            {
                new BsonDocument("_id", 0),
                new BsonDocument("_id", 1)
            };
            var secondBatch = new[]
            {
                new BsonDocument("_id", 2)
            };
            cursor.MoveNext().Returns(true, true, false);
            cursor.Current.Returns(firstBatch, secondBatch);
            var subject = new AsyncCursorEnumerator<BsonDocument>(cursor, CancellationToken.None);

            subject.MoveNext().Should().BeTrue();
            subject.MoveNext().Should().BeTrue();
            subject.MoveNext().Should().BeTrue();
            subject.MoveNext().Should().BeFalse();
        }

        [Test]
        public void MoveNext_should_throw_when_subject_has_been_disposed()
        {
            var subject = CreateSubject(0);
            subject.Dispose();

            Action action = () => subject.MoveNext();

            action.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void Reset_should_throw()
        {
            var subject = CreateSubject(1);

            Action action = () => subject.Reset();

            action.ShouldThrow<NotSupportedException>();
        }

        // private methods
        private AsyncCursorEnumerator<BsonDocument> CreateSubject(int count)
        {
            var firstBatch = Enumerable.Range(0, count)
                .Select(i => new BsonDocument("_id", i))
                .ToArray();

            var cursor = new AsyncCursor<BsonDocument>(
                channelSource: Substitute.For<IChannelSource>(),
                collectionNamespace: new CollectionNamespace("foo", "bar"),
                query: new BsonDocument(),
                firstBatch: firstBatch,
                cursorId: 0,
                batchSize: null,
                limit: null,
                serializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: new MessageEncoderSettings());

            return new AsyncCursorEnumerator<BsonDocument>(cursor, CancellationToken.None);
        }
    }
}

/* Copyright 2013-2014 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Tests.Helpers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Connections
{
    [TestFixture]
    public class BinaryConnectionTests
    {
        private DnsEndPoint _endPoint;
        private IStreamFactory _streamFactory;
        private BinaryConnection _subject;

        [SetUp]
        public void Setup()
        {
            _streamFactory = Substitute.For<IStreamFactory>();

            _endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), _endPoint);

            _subject = new BinaryConnection(
                serverId: serverId,
                endPoint: _endPoint,
                settings: new ConnectionSettings(),
                streamFactory: _streamFactory,
                listener: new NoOpMessageListener());
        }

        [Test]
        public void OpenAsync_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed()
        {
            _subject.Dispose();

            Action act = () => _subject.OpenAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        [TestCase(-100)]
        [TestCase(-2)]
        public void OpenAsync_should_throw_an_ArgumentOutOfRangeException_if_the_timeout_is_invalid(int timeoutMilliseconds)
        {
            Action act = () => _subject.OpenAsync(TimeSpan.FromMilliseconds(timeoutMilliseconds), CancellationToken.None).Wait();

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void OpenAsync_should_create_a_stream()
        {
            _subject.OpenAsync(TimeSpan.FromMinutes(2), CancellationToken.None).Wait();

            _streamFactory.Received(1).CreateStreamAsync(_endPoint, TimeSpan.FromMinutes(2), CancellationToken.None);
        }

        [Test]
        public void OpenAsync_should_do_nothing_if_open_is_called_more_than_once()
        {
            _subject.OpenAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            Action act = () => _subject.OpenAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldNotThrow();
            _streamFactory.ReceivedWithAnyArgs(1).CreateStreamAsync(null, Timeout.InfiniteTimeSpan, CancellationToken.None);
        }

        [Test]
        [TestCase(-100)]
        [TestCase(-2)]
        public void ReceiveMessageAsync_should_throw_an_ArgumentOutOfRangeException_when_timeout_is_out_of_range(int timeoutMilliseconds)
        {
            var serializer = Substitute.For<IBsonSerializer<BsonDocument>>();
            Action act = () => _subject.ReceiveMessageAsync(10, serializer, TimeSpan.FromMilliseconds(timeoutMilliseconds), CancellationToken.None).Wait();

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void ReceiveMessageAsync_should_throw_an_ArgumentNullException_when_the_serializer_is_null()
        {
            IBsonSerializer<int> serializer = null;
            Action act = () => _subject.ReceiveMessageAsync(10, serializer, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void ReceiveMessageAsync_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed()
        {
            var serializer = Substitute.For<IBsonSerializer<BsonDocument>>();
            _subject.Dispose();

            Action act = () => _subject.ReceiveMessageAsync(10, serializer, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void ReceiveMessageAsync_should_throw_an_InvalidOperationException_if_the_connection_is_not_open()
        {
            var serializer = Substitute.For<IBsonSerializer<BsonDocument>>();

            Action act = () => _subject.ReceiveMessageAsync(10, serializer, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void ReceiveMessageAsync_should_complete_when_reply_is_already_on_the_stream()
        {
            using (var stream = new BlockingMemoryStream())
            {
                _streamFactory.CreateStreamAsync(null, Timeout.InfiniteTimeSpan, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                _subject.OpenAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

                var messageToReceive = MessageHelper.BuildSuccessReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 10);
                MessageHelper.WriteRepliesToStream(stream, new[] { messageToReceive });

                var received = _subject.ReceiveMessageAsync(10, BsonDocumentSerializer.Instance, Timeout.InfiniteTimeSpan, CancellationToken.None).Result;

                var expected = MessageHelper.TranslateMessagesToBsonDocuments(new[] { messageToReceive });
                var actual = MessageHelper.TranslateMessagesToBsonDocuments(new[] { received });

                actual.Should().BeEquivalentTo(expected);
            }
        }

        [Test]
        public void ReceiveMessageAsync_should_complete_when_reply_is_not_already_on_the_stream()
        {
            using (var stream = new BlockingMemoryStream())
            {
                _streamFactory.CreateStreamAsync(null, Timeout.InfiniteTimeSpan, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                _subject.OpenAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

                var receivedTask = _subject.ReceiveMessageAsync(10, BsonDocumentSerializer.Instance, Timeout.InfiniteTimeSpan, CancellationToken.None);

                receivedTask.IsCompleted.Should().BeFalse();

                var messageToReceive = MessageHelper.BuildSuccessReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 10);
                MessageHelper.WriteRepliesToStream(stream, new[] { messageToReceive });

                var received = receivedTask.Result;

                var expected = MessageHelper.TranslateMessagesToBsonDocuments(new[] { messageToReceive });
                var actual = MessageHelper.TranslateMessagesToBsonDocuments(new[] { received });

                actual.Should().BeEquivalentTo(expected);
            }
        }

        [Test]
        public void ReceiveMessageAsync_should_handle_out_of_order_replies()
        {
            using (var stream = new BlockingMemoryStream())
            {
                _streamFactory.CreateStreamAsync(null, Timeout.InfiniteTimeSpan, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                _subject.OpenAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

                var receivedTask11 = _subject.ReceiveMessageAsync(11, BsonDocumentSerializer.Instance, Timeout.InfiniteTimeSpan, CancellationToken.None);
                var receivedTask10 = _subject.ReceiveMessageAsync(10, BsonDocumentSerializer.Instance, Timeout.InfiniteTimeSpan, CancellationToken.None);

                var messageToReceive10 = MessageHelper.BuildSuccessReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 10);
                var messageToReceive11 = MessageHelper.BuildSuccessReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 11);
                MessageHelper.WriteRepliesToStream(stream, new[] { messageToReceive10, messageToReceive11 });

                var received11 = receivedTask11.Result;
                var received10 = receivedTask10.Result;

                var expected = MessageHelper.TranslateMessagesToBsonDocuments(new[] { messageToReceive11, messageToReceive10 });
                var actual = MessageHelper.TranslateMessagesToBsonDocuments(new[] { received11, received10 });

                actual.Should().BeEquivalentTo(expected);
            }
        }

        [Test]
        public void SendMessagesAsync_should_throw_an_ArgumentNullException_if_messages_is_null()
        {
            Action act = () => _subject.SendMessagesAsync(null, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        [TestCase(-100)]
        [TestCase(-2)]
        public void SendMessagesAsync_should_throw_an_ArgumentOutOfRangeException_when_timeout_is_out_of_range(int timeoutMilliseconds)
        {
            Action act = () => _subject.SendMessagesAsync(Enumerable.Empty<RequestMessage>(), TimeSpan.FromMilliseconds(timeoutMilliseconds), CancellationToken.None).Wait();

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        public void SendMessagesAsync_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed()
        {
            var message = MessageHelper.BuildQueryMessage();
            _subject.Dispose();

            Action act = () => _subject.SendMessagesAsync(new[] { message }, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void SendMessagesAsync_should_throw_an_InvalidOperationException_if_the_connection_is_not_open()
        {
            var message = MessageHelper.BuildQueryMessage();

            Action act = () => _subject.SendMessagesAsync(new[] { message }, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void SendMessagesAsync_should_put_the_messages_on_the_stream()
        {
            using (var stream = new MemoryStream())
            {
                _streamFactory.CreateStreamAsync(null, Timeout.InfiniteTimeSpan, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                var message1 = MessageHelper.BuildQueryMessage(query: new BsonDocument("x", 1));
                var message2 = MessageHelper.BuildQueryMessage(query: new BsonDocument("y", 2));

                _subject.OpenAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();
                _subject.SendMessagesAsync(new[] { message1, message2 }, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

                var expectedRequests = MessageHelper.TranslateMessagesToBsonDocuments(new[] { message1, message2 });
                var sentRequests = MessageHelper.TranslateMessagesToBsonDocuments(stream.ToArray());

                sentRequests.Should().BeEquivalentTo(expectedRequests);
            }
        }
    }
}
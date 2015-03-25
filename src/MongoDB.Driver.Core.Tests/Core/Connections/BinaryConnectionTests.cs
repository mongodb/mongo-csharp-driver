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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class BinaryConnectionTests
    {
        private IConnectionInitializer _connectionInitializer;
        private DnsEndPoint _endPoint;
        private IConnectionListener _listener;
        private MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();
        private IStreamFactory _streamFactory;
        private BinaryConnection _subject;

        [SetUp]
        public void Setup()
        {
            _listener = Substitute.For<IConnectionListener>();
            _streamFactory = Substitute.For<IStreamFactory>();

            _endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), _endPoint);

            _connectionInitializer = Substitute.For<IConnectionInitializer>();
            _connectionInitializer.InitializeConnectionAsync(null, CancellationToken.None)
                .ReturnsForAnyArgs(Task.FromResult(new ConnectionDescription(
                    new ConnectionId(serverId),
                    new IsMasterResult(new BsonDocument()),
                    new BuildInfoResult(new BsonDocument("version", "2.6.3")))));

            _subject = new BinaryConnection(
                serverId: serverId,
                endPoint: _endPoint,
                settings: new ConnectionSettings(),
                streamFactory: _streamFactory,
                connectionInitializer: _connectionInitializer,
                listener: _listener);
        }

        [Test]
        public void Dispose_should_raise_the_correct_events()
        {
            _subject.Dispose();

            _listener.ReceivedWithAnyArgs().ConnectionBeforeClosing(default(ConnectionBeforeClosingEvent));
            _listener.ReceivedWithAnyArgs().ConnectionAfterClosing(default(ConnectionAfterClosingEvent));
        }

        [Test]
        public void OpenAsync_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed()
        {
            _subject.Dispose();

            Action act = () => _subject.OpenAsync(CancellationToken.None).Wait();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void OpenAsync_should_raise_the_correct_events_upon_failure()
        {
            var result = new TaskCompletionSource<ConnectionDescription>();
            result.SetException(new SocketException());
            _connectionInitializer.InitializeConnectionAsync(null, CancellationToken.None)
                .ReturnsForAnyArgs(result.Task);

            Func<Task> act = () => _subject.OpenAsync(CancellationToken.None);

            act.ShouldThrow<MongoConnectionException>()
                .WithInnerException<SocketException>()
                .And.ConnectionId.Should().Be(_subject.ConnectionId);

            _listener.ReceivedWithAnyArgs().ConnectionBeforeOpening(default(ConnectionBeforeOpeningEvent));
            _listener.ReceivedWithAnyArgs().ConnectionErrorOpening(default(ConnectionErrorOpeningEvent));
            _listener.ReceivedWithAnyArgs().ConnectionFailed(default(ConnectionFailedEvent));
        }

        [Test]
        public void OpenAsync_should_setup_the_description()
        {
            _subject.OpenAsync(CancellationToken.None).Wait();

            _subject.Description.Should().NotBeNull();
        }

        [Test]
        public void OpenAsync_should_raise_the_correct_events_on_success()
        {
            _subject.OpenAsync(CancellationToken.None).Wait();

            _listener.ReceivedWithAnyArgs().ConnectionBeforeOpening(default(ConnectionBeforeOpeningEvent));
            _listener.ReceivedWithAnyArgs().ConnectionAfterOpening(default(ConnectionAfterOpeningEvent));
        }

        [Test]
        public void OpenAsync_should_not_complete_the_second_call_until_the_first_is_completed()
        {
            var completionSource = new TaskCompletionSource<Stream>();
            _streamFactory.CreateStreamAsync(null, CancellationToken.None)
                .ReturnsForAnyArgs(completionSource.Task);

            _subject.OpenAsync(CancellationToken.None);
            var openTask2 = _subject.OpenAsync(CancellationToken.None);

            openTask2.IsCompleted.Should().BeFalse();

            _subject.Description.Should().BeNull();

            completionSource.SetResult(Substitute.For<Stream>());

            openTask2.IsCompleted.Should().BeTrue();

            _subject.Description.Should().NotBeNull();
        }

        [Test]
        public void ReceiveMessageAsync_should_throw_an_ArgumentNullException_when_the_encoderSelector_is_null()
        {
            IMessageEncoderSelector encoderSelector = null;
            Action act = () => _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None).Wait();

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void ReceiveMessageAsync_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed()
        {
            var encoderSelector = Substitute.For<IMessageEncoderSelector>();
            _subject.Dispose();

            Action act = () => _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None).Wait();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void ReceiveMessageAsync_should_throw_an_InvalidOperationException_if_the_connection_is_not_open()
        {
            var encoderSelector = Substitute.For<IMessageEncoderSelector>();

            Action act = () => _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None).Wait();

            act.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void ReceiveMessageAsync_should_complete_when_reply_is_already_on_the_stream()
        {
            using (var stream = new BlockingMemoryStream())
            {
                _streamFactory.CreateStreamAsync(null, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                _subject.OpenAsync(CancellationToken.None).Wait();

                var messageToReceive = MessageHelper.BuildSuccessReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 10);
                MessageHelper.WriteResponsesToStream(stream, new[] { messageToReceive });

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);
                var received = _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None).Result;

                var expected = MessageHelper.TranslateMessagesToBsonDocuments(new[] { messageToReceive });
                var actual = MessageHelper.TranslateMessagesToBsonDocuments(new[] { received });

                actual.Should().BeEquivalentTo(expected);

                _listener.ReceivedWithAnyArgs().ConnectionBeforeReceivingMessage(default(ConnectionBeforeReceivingMessageEvent));
                _listener.ReceivedWithAnyArgs().ConnectionAfterReceivingMessage(default(ConnectionAfterReceivingMessageEvent));
            }
        }

        [Test]
        public void ReceiveMessageAsync_should_complete_when_reply_is_not_already_on_the_stream()
        {
            using (var stream = new BlockingMemoryStream())
            {
                _streamFactory.CreateStreamAsync(null, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                _subject.OpenAsync(CancellationToken.None).Wait();

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);
                var receivedTask = _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None);

                receivedTask.IsCompleted.Should().BeFalse();

                var messageToReceive = MessageHelper.BuildSuccessReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 10);
                MessageHelper.WriteResponsesToStream(stream, new[] { messageToReceive });

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
                _streamFactory.CreateStreamAsync(null, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                _subject.OpenAsync(CancellationToken.None).Wait();

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);
                var receivedTask11 = _subject.ReceiveMessageAsync(11, encoderSelector, _messageEncoderSettings, CancellationToken.None);
                var receivedTask10 = _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None);

                var messageToReceive10 = MessageHelper.BuildSuccessReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 10);
                var messageToReceive11 = MessageHelper.BuildSuccessReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 11);
                MessageHelper.WriteResponsesToStream(stream, new[] { messageToReceive10, messageToReceive11 });

                var received11 = receivedTask11.Result;
                var received10 = receivedTask10.Result;

                var expected = MessageHelper.TranslateMessagesToBsonDocuments(new[] { messageToReceive11, messageToReceive10 });
                var actual = MessageHelper.TranslateMessagesToBsonDocuments(new[] { received11, received10 });

                actual.Should().BeEquivalentTo(expected);
            }
        }

        [Test]
        public async Task ReceiveMessageAsync_should_throw_network_exception_to_all_awaiters()
        {
            using (var stream = Substitute.For<Stream>())
            {
                _streamFactory.CreateStreamAsync(null, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                var readTcs = new TaskCompletionSource<int>();
                stream.ReadAsync(null, 0, 0, CancellationToken.None)
                    .ReturnsForAnyArgs(readTcs.Task);

                var writeTcs = new TaskCompletionSource<int>();
                stream.WriteAsync(null, 0, 0, CancellationToken.None)
                    .ReturnsForAnyArgs(writeTcs.Task);

                await _subject.OpenAsync(CancellationToken.None);

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);
                var task1 = _subject.ReceiveMessageAsync(1, encoderSelector, _messageEncoderSettings, CancellationToken.None);
                var task2 = _subject.ReceiveMessageAsync(2, encoderSelector, _messageEncoderSettings, CancellationToken.None);

                readTcs.SetException(new SocketException());

                Func<Task> act1 = () => task1;
                act1.ShouldThrow<MongoConnectionException>()
                    .WithInnerException<SocketException>()
                    .And.ConnectionId.Should().Be(_subject.ConnectionId);

                Func<Task> act2 = () => task2;
                act2.ShouldThrow<MongoConnectionException>()
                    .WithInnerException<SocketException>()
                    .And.ConnectionId.Should().Be(_subject.ConnectionId);

                _listener.ReceivedWithAnyArgs(2).ConnectionBeforeReceivingMessage(default(ConnectionBeforeReceivingMessageEvent));
                _listener.ReceivedWithAnyArgs(2).ConnectionErrorReceivingMessage(default(ConnectionErrorReceivingMessageEvent));
            }
        }

        [Test]
        public async Task ReceiveMessageAsync_should_throw_MongoConnectionClosedException_when_connection_has_failed()
        {
            using (var stream = Substitute.For<Stream>())
            {
                _streamFactory.CreateStreamAsync(null, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                var readTcs = new TaskCompletionSource<int>();
                stream.ReadAsync(null, 0, 0, CancellationToken.None)
                    .ReturnsForAnyArgs(readTcs.Task);

                var writeTcs = new TaskCompletionSource<int>();
                stream.WriteAsync(null, 0, 0, CancellationToken.None)
                    .ReturnsForAnyArgs(writeTcs.Task);

                await _subject.OpenAsync(CancellationToken.None);

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);
                var task1 = _subject.ReceiveMessageAsync(1, encoderSelector, _messageEncoderSettings, CancellationToken.None);

                readTcs.SetException(new SocketException());

                Func<Task> act1 = () => task1;
                act1.ShouldThrow<MongoConnectionException>()
                    .WithInnerException<SocketException>()
                    .And.ConnectionId.Should().Be(_subject.ConnectionId);

                var task2 = _subject.ReceiveMessageAsync(2, encoderSelector, _messageEncoderSettings, CancellationToken.None);

                Func<Task> act2 = () => task2;
                act2.ShouldThrow<MongoConnectionClosedException>()
                    .And.ConnectionId.Should().Be(_subject.ConnectionId);

                _listener.ReceivedWithAnyArgs().ConnectionBeforeReceivingMessage(default(ConnectionBeforeReceivingMessageEvent));
                _listener.ReceivedWithAnyArgs().ConnectionErrorReceivingMessage(default(ConnectionErrorReceivingMessageEvent));
            }
        }

        [Test]
        public void SendMessagesAsync_should_throw_an_ArgumentNullException_if_messages_is_null()
        {
            Action act = () => _subject.SendMessagesAsync(null, _messageEncoderSettings, CancellationToken.None).Wait();

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void SendMessagesAsync_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed()
        {
            var message = MessageHelper.BuildQueryMessage();
            _subject.Dispose();

            Action act = () => _subject.SendMessagesAsync(new[] { message }, _messageEncoderSettings, CancellationToken.None).Wait();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void SendMessagesAsync_should_throw_an_InvalidOperationException_if_the_connection_is_not_open()
        {
            var message = MessageHelper.BuildQueryMessage();

            Action act = () => _subject.SendMessagesAsync(new[] { message }, _messageEncoderSettings, CancellationToken.None).Wait();

            act.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void SendMessagesAsync_should_put_the_messages_on_the_stream_and_raise_the_correct_events()
        {
            using (var stream = new MemoryStream())
            {
                _streamFactory.CreateStreamAsync(null, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                var message1 = MessageHelper.BuildQueryMessage(query: new BsonDocument("x", 1));
                var message2 = MessageHelper.BuildQueryMessage(query: new BsonDocument("y", 2));

                _subject.OpenAsync(CancellationToken.None).Wait();
                _subject.SendMessagesAsync(new[] { message1, message2 }, _messageEncoderSettings, CancellationToken.None).Wait();

                var expectedRequests = MessageHelper.TranslateMessagesToBsonDocuments(new[] { message1, message2 });
                var sentRequests = MessageHelper.TranslateMessagesToBsonDocuments(stream.ToArray());

                sentRequests.Should().BeEquivalentTo(expectedRequests);
                _listener.ReceivedWithAnyArgs().ConnectionBeforeSendingMessages(default(ConnectionBeforeSendingMessagesEvent));
                _listener.ReceivedWithAnyArgs().ConnectionAfterSendingMessages(default(ConnectionAfterSendingMessagesEvent));
            }
        }

        [Test]
        public async Task SendMessageAsync_should_throw_MongoConnectionClosedException_for_waiting_tasks()
        {
            using (var stream = Substitute.For<Stream>())
            {
                _streamFactory.CreateStreamAsync(null, CancellationToken.None)
                    .ReturnsForAnyArgs(Task.FromResult<Stream>(stream));

                var readTcs = new TaskCompletionSource<int>();
                stream.ReadAsync(null, 0, 0, CancellationToken.None)
                    .ReturnsForAnyArgs(readTcs.Task);

                var writeTcs = new TaskCompletionSource<int>();
                stream.WriteAsync(null, 0, 0, CancellationToken.None)
                    .ReturnsForAnyArgs(writeTcs.Task);

                await _subject.OpenAsync(CancellationToken.None);

                var message1 = new KillCursorsMessage(1, new[] { 1L });
                var task1 = _subject.SendMessageAsync(message1, _messageEncoderSettings, CancellationToken.None);

                var message2 = new KillCursorsMessage(2, new[] { 2L });
                var task2 = _subject.SendMessageAsync(message2, _messageEncoderSettings, CancellationToken.None);

                writeTcs.SetException(new SocketException());

                Func<Task> act1 = () => task1;
                act1.ShouldThrow<MongoConnectionException>()
                    .WithInnerException<SocketException>()
                    .And.ConnectionId.Should().Be(_subject.ConnectionId);

                Func<Task> act2 = () => task2;
                act2.ShouldThrow<MongoConnectionClosedException>();

                _listener.ReceivedWithAnyArgs().ConnectionBeforeSendingMessages(default(ConnectionBeforeSendingMessagesEvent));
                _listener.ReceivedWithAnyArgs().ConnectionErrorSendingMessages(default(ConnectionErrorSendingMessagesEvent));
                _listener.ReceivedWithAnyArgs().ConnectionFailed(default(ConnectionFailedEvent));
            }
        }
    }
}
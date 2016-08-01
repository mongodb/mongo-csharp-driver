/* Copyright 2013-2016 MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class BinaryConnectionTests
    {
        private Mock<IConnectionInitializer> _mockConnectionInitializer;
        private DnsEndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();
        private Mock<IStreamFactory> _mockStreamFactory;
        private BinaryConnection _subject;

        public BinaryConnectionTests()
        {
            _capturedEvents = new EventCapturer();
            _mockStreamFactory = new Mock<IStreamFactory>();

            _endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), _endPoint);

            _mockConnectionInitializer = new Mock<IConnectionInitializer>();
            _mockConnectionInitializer.Setup(i => i.InitializeConnection(It.IsAny<IConnection>(), CancellationToken.None))
                .Returns(() => new ConnectionDescription(
                    new ConnectionId(serverId),
                    new IsMasterResult(new BsonDocument()),
                    new BuildInfoResult(new BsonDocument("version", "2.6.3"))));
            _mockConnectionInitializer.Setup(i => i.InitializeConnectionAsync(It.IsAny<IConnection>(), CancellationToken.None))
                .Returns(() => Task.FromResult(new ConnectionDescription(
                    new ConnectionId(serverId),
                    new IsMasterResult(new BsonDocument()),
                    new BuildInfoResult(new BsonDocument("version", "2.6.3")))));

            _subject = new BinaryConnection(
                serverId: serverId,
                endPoint: _endPoint,
                settings: new ConnectionSettings(),
                streamFactory: _mockStreamFactory.Object,
                connectionInitializer: _mockConnectionInitializer.Object,
                eventSubscriber: _capturedEvents);
        }

        [Fact]
        public void Dispose_should_raise_the_correct_events()
        {
            _subject.Dispose();

            _capturedEvents.Next().Should().BeOfType<ConnectionClosingEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionClosedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void Open_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed(
            [Values(false, true)]
            bool async)
        {
            _subject.Dispose();

            Action act;
            if (async)
            {
                act = () => _subject.OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.Open(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Open_should_raise_the_correct_events_upon_failure(
            [Values(false, true)]
            bool async)
        {
            Action act;
            if (async)
            {
                var result = new TaskCompletionSource<ConnectionDescription>();
                result.SetException(new SocketException());
                _mockConnectionInitializer.Setup(i => i.InitializeConnectionAsync(It.IsAny<IConnection>(), It.IsAny<CancellationToken>()))
                    .Returns(result.Task);

                act = () => _subject.OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                _mockConnectionInitializer.Setup(i => i.InitializeConnection(It.IsAny<IConnection>(), It.IsAny<CancellationToken>()))
                    .Throws<SocketException>();

                act = () => _subject.Open(CancellationToken.None);
            }

            act.ShouldThrow<MongoConnectionException>()
                .WithInnerException<SocketException>()
                .And.ConnectionId.Should().Be(_subject.ConnectionId);

            _capturedEvents.Next().Should().BeOfType<ConnectionOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionOpeningFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void Open_should_setup_the_description(
            [Values(false, true)]
            bool async)
        {
            if (async)
            {
                _subject.OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                _subject.Open(CancellationToken.None);
            }

            _subject.Description.Should().NotBeNull();

            _capturedEvents.Next().Should().BeOfType<ConnectionOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void Open_should_not_complete_the_second_call_until_the_first_is_completed(
            [Values(false, true)]
            bool async1,
            [Values(false, true)]
            bool async2)
        {
            var task1IsBlocked = false;
            var completionSource = new TaskCompletionSource<Stream>();
            _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, CancellationToken.None))
               .Returns(() => { task1IsBlocked = true; return completionSource.Task.GetAwaiter().GetResult(); });
            _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, CancellationToken.None))
                .Returns(() => { task1IsBlocked = true; return completionSource.Task; });

            Task openTask1;
            if (async1)
            {

                openTask1 = _subject.OpenAsync(CancellationToken.None);
            }
            else
            {
                openTask1 = Task.Run(() => _subject.Open(CancellationToken.None));
            }
            SpinWait.SpinUntil(() => task1IsBlocked, TimeSpan.FromSeconds(5)).Should().BeTrue();

            Task openTask2;
            if (async2)
            {
                openTask2 = _subject.OpenAsync(CancellationToken.None);
            }
            else
            {
                openTask2 = Task.Run(() => _subject.Open(CancellationToken.None));
            }

            openTask1.IsCompleted.Should().BeFalse();
            openTask2.IsCompleted.Should().BeFalse();
            _subject.Description.Should().BeNull();

            completionSource.SetResult(new Mock<Stream>().Object);
            SpinWait.SpinUntil(() => openTask1.IsCompleted, TimeSpan.FromSeconds(5)).Should().BeTrue();
            SpinWait.SpinUntil(() => openTask2.IsCompleted, TimeSpan.FromSeconds(5)).Should().BeTrue();
            _subject.Description.Should().NotBeNull();

            _capturedEvents.Next().Should().BeOfType<ConnectionOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void ReceiveMessage_should_throw_an_ArgumentNullException_when_the_encoderSelector_is_null(
            [Values(false, true)]
            bool async)
        {
            IMessageEncoderSelector encoderSelector = null;

            Action act;
            if (async)
            {
                act = () => _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.ReceiveMessage(10, encoderSelector, _messageEncoderSettings, CancellationToken.None);
            }

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ReceiveMessage_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed(
            [Values(false, true)]
            bool async)
        {
            var encoderSelector = new Mock<IMessageEncoderSelector>().Object;
            _subject.Dispose();

            Action act;
            if (async)
            {
                act = () => _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.ReceiveMessage(10, encoderSelector, _messageEncoderSettings, CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ReceiveMessage_should_throw_an_InvalidOperationException_if_the_connection_is_not_open(
            [Values(false, true)]
            bool async)
        {
            var encoderSelector = new Mock<IMessageEncoderSelector>().Object;

            Action act;
            if (async)
            {
                act = () => _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.ReceiveMessage(10, encoderSelector, _messageEncoderSettings, CancellationToken.None);
            }

            act.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ReceiveMessage_should_complete_when_reply_is_already_on_the_stream(
            [Values(false, true)]
            bool async)
        {
            using (var stream = new BlockingMemoryStream())
            {
                var messageToReceive = MessageHelper.BuildReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 10);
                MessageHelper.WriteResponsesToStream(stream, new[] { messageToReceive });

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                ResponseMessage received;
                if (async)
                {
                    _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, CancellationToken.None))
                        .Returns(Task.FromResult<Stream>(stream));
                    _subject.OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
                    _capturedEvents.Clear();

                    received = _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, CancellationToken.None))
                        .Returns(stream);
                    _subject.Open(CancellationToken.None);
                    _capturedEvents.Clear();

                    received = _subject.ReceiveMessage(10, encoderSelector, _messageEncoderSettings, CancellationToken.None);
                }

                var expected = MessageHelper.TranslateMessagesToBsonDocuments(new[] { messageToReceive });
                var actual = MessageHelper.TranslateMessagesToBsonDocuments(new[] { received });

                actual.Should().BeEquivalentTo(expected);

                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivedMessageEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ReceiveMessage_should_complete_when_reply_is_not_already_on_the_stream(
            [Values(false, true)]
            bool async)
        {
            using (var stream = new BlockingMemoryStream())
            {
                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                Task<ResponseMessage> receiveMessageTask;
                if (async)
                {
                    _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, CancellationToken.None))
                       .Returns(Task.FromResult<Stream>(stream));
                    _subject.OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
                    _capturedEvents.Clear();

                    receiveMessageTask = _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None);
                }
                else
                {
                    _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, CancellationToken.None))
                       .Returns(stream);
                    _subject.Open(CancellationToken.None);
                    _capturedEvents.Clear();

                    receiveMessageTask = Task.Run(() => _subject.ReceiveMessage(10, encoderSelector, _messageEncoderSettings, CancellationToken.None));
                }

                receiveMessageTask.IsCompleted.Should().BeFalse();

                var messageToReceive = MessageHelper.BuildReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 10);
                MessageHelper.WriteResponsesToStream(stream, new[] { messageToReceive });

                var received = receiveMessageTask.GetAwaiter().GetResult();

                var expected = MessageHelper.TranslateMessagesToBsonDocuments(new[] { messageToReceive });
                var actual = MessageHelper.TranslateMessagesToBsonDocuments(new[] { received });

                actual.Should().BeEquivalentTo(expected);

                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivedMessageEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ReceiveMessage_should_handle_out_of_order_replies(
            [Values(false, true)]
            bool async1,
            [Values(false, true)]
            bool async2)
        {
            using (var stream = new BlockingMemoryStream())
            {
                _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, CancellationToken.None))
                    .Returns(stream);
                _subject.Open(CancellationToken.None);
                _capturedEvents.Clear();

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                Task<ResponseMessage> receivedTask10;
                if (async1)
                {
                    receivedTask10 = _subject.ReceiveMessageAsync(10, encoderSelector, _messageEncoderSettings, CancellationToken.None);
                }
                else
                {
                    var receivedTask10IsRunning = 0;
                    receivedTask10 = Task.Run(() => { Interlocked.Exchange(ref receivedTask10IsRunning, 1); return _subject.ReceiveMessage(10, encoderSelector, _messageEncoderSettings, CancellationToken.None); });
                    SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref receivedTask10IsRunning, 0, 0) == 1, TimeSpan.FromSeconds(5)).Should().BeTrue();
                }

                Task<ResponseMessage> receivedTask11;
                if (async2)
                {
                    receivedTask11 = _subject.ReceiveMessageAsync(11, encoderSelector, _messageEncoderSettings, CancellationToken.None);
                }
                else
                {
                    var receivedTask11IsRunning = 0;
                    receivedTask11 = Task.Run(() => { Interlocked.Exchange(ref receivedTask11IsRunning, 1); return _subject.ReceiveMessage(11, encoderSelector, _messageEncoderSettings, CancellationToken.None); });
                    SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref receivedTask11IsRunning, 0, 0) == 1, TimeSpan.FromSeconds(5)).Should().BeTrue();
                }

                var messageToReceive10 = MessageHelper.BuildReply<BsonDocument>(new BsonDocument("_id", 10), BsonDocumentSerializer.Instance, responseTo: 10);
                var messageToReceive11 = MessageHelper.BuildReply<BsonDocument>(new BsonDocument("_id", 11), BsonDocumentSerializer.Instance, responseTo: 11);
                MessageHelper.WriteResponsesToStream(stream, new[] { messageToReceive11, messageToReceive10 }); // out of order

                var received10 = receivedTask10.GetAwaiter().GetResult();
                var received11 = receivedTask11.GetAwaiter().GetResult();

                var expected = MessageHelper.TranslateMessagesToBsonDocuments(new[] { messageToReceive10, messageToReceive11 });
                var actual = MessageHelper.TranslateMessagesToBsonDocuments(new[] { received10, received11 });

                actual.Should().BeEquivalentTo(expected);

                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivedMessageEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivedMessageEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ReceiveMessage_should_throw_network_exception_to_all_awaiters(
            [Values(false, true)]
            bool async1,
            [Values(false, true)]
            bool async2)
        {
            var mockStream = new Mock<Stream>();
            using (mockStream.Object)
            {
                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, CancellationToken.None))
                  .Returns(mockStream.Object);
                var readTcs = new TaskCompletionSource<int>();
                mockStream.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Returns(() => readTcs.Task.GetAwaiter().GetResult());
                mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(readTcs.Task);
                _subject.Open(CancellationToken.None);
                _capturedEvents.Clear();

                Task task1;
                if (async1)
                {
                    task1 = _subject.ReceiveMessageAsync(1, encoderSelector, _messageEncoderSettings, It.IsAny<CancellationToken>());
                }
                else
                {
                    var task1IsRunning = 0;
                    task1 = Task.Run(() => { Interlocked.Exchange(ref task1IsRunning, 1); return _subject.ReceiveMessage(1, encoderSelector, _messageEncoderSettings, CancellationToken.None); });
                    SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref task1IsRunning, 0, 0) == 1, TimeSpan.FromSeconds(5)).Should().BeTrue();
                }

                Task task2;
                if (async2)
                {
                    task2 = _subject.ReceiveMessageAsync(2, encoderSelector, _messageEncoderSettings, CancellationToken.None);
                }
                else
                {
                    var task2IsRunning = 0;
                    task2 = Task.Run(() => { Interlocked.Exchange(ref task2IsRunning, 1); return _subject.ReceiveMessage(2, encoderSelector, _messageEncoderSettings, CancellationToken.None); });
                    SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref task2IsRunning, 0, 0) == 1, TimeSpan.FromSeconds(5)).Should().BeTrue();
                }

                readTcs.SetException(new SocketException());

                Func<Task> act1 = () => task1;
                act1.ShouldThrow<MongoConnectionException>()
                    .WithInnerException<SocketException>()
                    .And.ConnectionId.Should().Be(_subject.ConnectionId);

                Func<Task> act2 = () => task2;
                act2.ShouldThrow<MongoConnectionException>()
                    .WithInnerException<SocketException>()
                    .And.ConnectionId.Should().Be(_subject.ConnectionId);

                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionFailedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageFailedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageFailedEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ReceiveMessage_should_throw_MongoConnectionClosedException_when_connection_has_failed(
            [Values(false, true)]
            bool async1,
            [Values(false, true)]
            bool async2)
        {
            var mockStream = new Mock<Stream>();
            using (mockStream.Object)
            {
                _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, CancellationToken.None))
                   .Returns(mockStream.Object);
                var readTcs = new TaskCompletionSource<int>();
                readTcs.SetException(new SocketException());
                mockStream.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Returns(() => readTcs.Task.GetAwaiter().GetResult());
                mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(readTcs.Task);
                _subject.Open(CancellationToken.None);
                _capturedEvents.Clear();

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                Action act1;
                if (async1)
                {
                    act1 = () => _subject.ReceiveMessageAsync(1, encoderSelector, _messageEncoderSettings, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    act1 = () => _subject.ReceiveMessage(1, encoderSelector, _messageEncoderSettings, CancellationToken.None);
                }

                Action act2;
                if (async2)
                {
                    act2 = () => _subject.ReceiveMessageAsync(2, encoderSelector, _messageEncoderSettings, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    act2 = () => _subject.ReceiveMessage(2, encoderSelector, _messageEncoderSettings, CancellationToken.None);
                }

                act1.ShouldThrow<MongoConnectionException>()
                    .WithInnerException<SocketException>()
                    .And.ConnectionId.Should().Be(_subject.ConnectionId);

                act2.ShouldThrow<MongoConnectionClosedException>()
                    .And.ConnectionId.Should().Be(_subject.ConnectionId);

                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionFailedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageFailedEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void SendMessages_should_throw_an_ArgumentNullException_if_messages_is_null(
            [Values(false, true)]
            bool async)
        {
            Action act;
            if (async)
            {
                act = () => _subject.SendMessagesAsync(null, _messageEncoderSettings, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.SendMessages(null, _messageEncoderSettings, CancellationToken.None);
            }

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void SendMessages_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed(
            [Values(false, true)]
            bool async)
        {
            var message = MessageHelper.BuildQuery();
            _subject.Dispose();

            Action act;
            if (async)
            {
                act = () => _subject.SendMessagesAsync(new[] { message }, _messageEncoderSettings, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.SendMessages(new[] { message }, _messageEncoderSettings, CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void SendMessages_should_throw_an_InvalidOperationException_if_the_connection_is_not_open(
            [Values(false, true)]
            bool async)
        {
            var message = MessageHelper.BuildQuery();

            Action act;
            if (async)
            {
                act = () => _subject.SendMessagesAsync(new[] { message }, _messageEncoderSettings, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.SendMessages(new[] { message }, _messageEncoderSettings, CancellationToken.None);
            }

            act.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void SendMessages_should_put_the_messages_on_the_stream_and_raise_the_correct_events(
            [Values(false, true)]
            bool async)
        {
            using (var stream = new MemoryStream())
            {
                var message1 = MessageHelper.BuildQuery(query: new BsonDocument("x", 1));
                var message2 = MessageHelper.BuildQuery(query: new BsonDocument("y", 2));

                if (async)
                {
                    _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, CancellationToken.None))
                        .Returns(Task.FromResult<Stream>(stream));
                    _subject.OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
                    _capturedEvents.Clear();

                    _subject.SendMessagesAsync(new[] { message1, message2 }, _messageEncoderSettings, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, CancellationToken.None))
                        .Returns(stream);
                    _subject.Open(CancellationToken.None);
                    _capturedEvents.Clear();

                    _subject.SendMessages(new[] { message1, message2 }, _messageEncoderSettings, CancellationToken.None);
                }

                var expectedRequests = MessageHelper.TranslateMessagesToBsonDocuments(new[] { message1, message2 });
                var sentRequests = MessageHelper.TranslateMessagesToBsonDocuments(stream.ToArray());

                sentRequests.Should().BeEquivalentTo(expectedRequests);
                _capturedEvents.Next().Should().BeOfType<ConnectionSendingMessagesEvent>();
                _capturedEvents.Next().Should().BeOfType<CommandStartedEvent>();
                _capturedEvents.Next().Should().BeOfType<CommandStartedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionSentMessagesEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void SendMessageshould_throw_MongoConnectionClosedException_for_waiting_tasks(
            [Values(false, true)]
            bool async1,
            [Values(false, true)]
            bool async2)
        {
            var mockStream = new Mock<Stream>();
            using (mockStream.Object)
            {
                var message1 = new KillCursorsMessage(1, new[] { 1L });
                var message2 = new KillCursorsMessage(2, new[] { 2L });

                _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, CancellationToken.None))
                    .Returns(mockStream.Object);
                var task1IsBlocked = false;
                var writeTcs = new TaskCompletionSource<int>();
                mockStream.Setup(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Callback(() => { task1IsBlocked = true; writeTcs.Task.GetAwaiter().GetResult(); });
                mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(() => { task1IsBlocked = true; return writeTcs.Task; });
                _subject.Open(CancellationToken.None);
                _capturedEvents.Clear();


                Task task1;
                if (async1)
                {
                    task1 = _subject.SendMessageAsync(message1, _messageEncoderSettings, CancellationToken.None);
                }
                else
                {
                    task1 = Task.Run(() => { _subject.SendMessage(message1, _messageEncoderSettings, CancellationToken.None); });
                }

                SpinWait.SpinUntil(() => task1IsBlocked, TimeSpan.FromSeconds(5)).Should().BeTrue();
                task1IsBlocked.Should().BeTrue();

                Task task2;
                if (async2)
                {
                    task2 = _subject.SendMessageAsync(message2, _messageEncoderSettings, CancellationToken.None);
                }
                else
                {
                    var task2IsRunning = 0;
                    task2 = Task.Run(() => { Interlocked.Exchange(ref task2IsRunning, 1); _subject.SendMessage(message2, _messageEncoderSettings, CancellationToken.None); });
                    SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref task2IsRunning, 0, 0) == 1, TimeSpan.FromSeconds(5)).Should().BeTrue();
                }

                writeTcs.SetException(new SocketException());

                Func<Task> act1 = () => task1;
                act1.ShouldThrow<MongoConnectionException>()
                    .WithInnerException<SocketException>()
                    .And.ConnectionId.Should().Be(_subject.ConnectionId);

                Func<Task> act2 = () => task2;
                act2.ShouldThrow<MongoConnectionClosedException>();

                _capturedEvents.Next().Should().BeOfType<ConnectionSendingMessagesEvent>();
                _capturedEvents.Next().Should().BeOfType<CommandStartedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionSendingMessagesEvent>();
                _capturedEvents.Next().Should().BeOfType<CommandStartedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionFailedEvent>();
                _capturedEvents.Next().Should().BeOfType<CommandFailedEvent>();
                var events7And8Types = new Type[]
                {
                    _capturedEvents.Next().GetType(),
                    _capturedEvents.Next().GetType()
                };
                var expectedEventTypes = new Type[]
                {
                    typeof(CommandFailedEvent),
                    typeof(ConnectionSendingMessagesFailedEvent)
                };
                events7And8Types.Should().BeEquivalentTo(expectedEventTypes);
                _capturedEvents.Next().Should().BeOfType<ConnectionSendingMessagesFailedEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }
    }
}
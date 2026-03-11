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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class BinaryConnectionTests : LoggableTestClass
    {
        private ConnectionInitializerContext _connectionInitializerContext;
        private ConnectionInitializerContext _connectionInitializerContextAfterAuthentication;
        private Mock<IConnectionInitializer> _mockConnectionInitializer;
        private ConnectionDescription _connectionDescription;
        private DnsEndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();
        private Mock<IStreamFactory> _mockStreamFactory;
        private readonly ServerId _serverId;

        private BinaryConnection _subject;

        public BinaryConnectionTests(Xunit.Abstractions.ITestOutputHelper output) : base(output)
        {
            _capturedEvents = new EventCapturer();
            _mockStreamFactory = new Mock<IStreamFactory>();
            var stream = new MemoryStream();
            _mockStreamFactory
                .Setup(s => s.CreateStream(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(stream);
            _mockStreamFactory
                .Setup(s => s.CreateStreamAsync(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stream);

            _endPoint = new DnsEndPoint("localhost", 27017);
            _serverId = new ServerId(new ClusterId(), _endPoint);
            var connectionId = new ConnectionId(_serverId);
            var helloResult = new HelloResult(new BsonDocument { { "ok", 1 }, { "maxMessageSizeBytes", 48000000 }, { "maxWireVersion", WireVersion.Server36 } });
            _connectionDescription = new ConnectionDescription(connectionId, helloResult);
            _connectionInitializerContext = new ConnectionInitializerContext(_connectionDescription, null);
            _connectionInitializerContextAfterAuthentication = new ConnectionInitializerContext(_connectionDescription, null);

            _mockConnectionInitializer = new Mock<IConnectionInitializer>();
            _mockConnectionInitializer
                .Setup(i => i.SendHello(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
                .Returns(_connectionInitializerContext);
            _mockConnectionInitializer
                .Setup(i => i.Authenticate(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionInitializerContext>()))
                .Returns(_connectionInitializerContextAfterAuthentication);
            _mockConnectionInitializer
                .Setup(i => i.SendHelloAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
                .ReturnsAsync(_connectionInitializerContext);
            _mockConnectionInitializer
                .Setup(i => i.AuthenticateAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionInitializerContext>()))
                .ReturnsAsync(_connectionInitializerContextAfterAuthentication);

            _subject = new BinaryConnection(
                serverId: _serverId,
                endPoint: _endPoint,
                settings: new ConnectionSettings(),
                streamFactory: _mockStreamFactory.Object,
                connectionInitializer: _mockConnectionInitializer.Object,
                eventSubscriber: _capturedEvents,
                loggerFactory: LoggerFactory,
                tracingOptions: null,
                socketReadTimeout: TimeSpan.FromMilliseconds(1000),
                socketWriteTimeout: TimeSpan.FromMilliseconds(1000));
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
        public async Task Open_should_always_create_description_if_handshake_was_successful([Values(false, true)] bool async)
        {
            var serviceId = ObjectId.GenerateNewId();
            var connectionDescription = new ConnectionDescription(
                new ConnectionId(new ServerId(new ClusterId(), _endPoint)),
                new HelloResult(new BsonDocument("serviceId", serviceId)));

            var socketException = new SocketException();
            _mockConnectionInitializer
                .Setup(i => i.SendHello(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
                .Returns(new ConnectionInitializerContext(connectionDescription, null));
            _mockConnectionInitializer
                .Setup(i => i.SendHelloAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
                .ReturnsAsync(new ConnectionInitializerContext(connectionDescription, null));
            _mockConnectionInitializer
                .Setup(i => i.Authenticate(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionInitializerContext>()))
                .Throws(socketException);
            _mockConnectionInitializer
                .Setup(i => i.AuthenticateAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionInitializerContext>()))
                .ThrowsAsync(socketException);

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.OpenAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => _subject.Open(OperationContext.NoTimeout));

            _subject.Description.Should().Be(connectionDescription);
            var ex = exception.Should().BeOfType<MongoConnectionException>().Subject;
            ex.InnerException.Should().BeOfType<SocketException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Open_should_create_authenticators_only_once(
            [Values(false, true)] bool async)
        {
            using var memoryStream = new MemoryStream();
            var clonedMessageEncoderSettings = _messageEncoderSettings.Clone();
            var encoderFactory = new BinaryMessageEncoderFactory(memoryStream, clonedMessageEncoderSettings, compressorSource: null);
            var encoder = encoderFactory.GetCommandResponseMessageEncoder();
            encoder.WriteMessage(CreateResponseMessage());
            var mockStreamFactory = new Mock<IStreamFactory>();
            using var stream = new IgnoreWritesMemoryStream(memoryStream.ToArray());
            mockStreamFactory
                .Setup(s => s.CreateStream(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(stream);
            mockStreamFactory
                .Setup(s => s.CreateStreamAsync(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stream);

            var connectionInitializer = new ConnectionInitializer(
                null,
                new CompressorConfiguration[0],
                new ServerApi(ServerApiVersion.V1), // use serverApi to choose command message protocol
                null);

            var authenticatorMock = new Mock<IAuthenticator>();
            authenticatorMock
                .Setup(a => a.CustomizeInitialHelloCommand(It.IsAny<OperationContext>(), It.IsAny<BsonDocument>()))
                .Returns(new BsonDocument(OppressiveLanguageConstants.LegacyHelloCommandName, 1));

            var authenticatorFactoryMock = new Mock<IAuthenticatorFactory>();
            authenticatorFactoryMock
                .Setup(a => a.Create())
                .Returns(authenticatorMock.Object);

            using var subject = new BinaryConnection(
                serverId: _serverId,
                endPoint: _endPoint,
                settings: new ConnectionSettings(authenticatorFactoryMock.Object),
                streamFactory: mockStreamFactory.Object,
                connectionInitializer: connectionInitializer,
                eventSubscriber: _capturedEvents,
                loggerFactory: LoggerFactory,
                tracingOptions: null,
                socketReadTimeout: Timeout.InfiniteTimeSpan,
                socketWriteTimeout: Timeout.InfiniteTimeSpan);

            if (async)
            {
                await subject.OpenAsync(OperationContext.NoTimeout);
            }
            else
            {
                subject.Open(OperationContext.NoTimeout);
            }

            authenticatorFactoryMock.Verify(f => f.Create(), Times.Once());

            ResponseMessage CreateResponseMessage()
            {
                var section0Document = $"{{ {OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName} : true, ok : 1, connectionId : 1 }}";
                var section0 = new Type0CommandMessageSection<RawBsonDocument>(
                    new RawBsonDocument(BsonDocument.Parse(section0Document).ToBson()),
                    RawBsonDocumentSerializer.Instance);
                return new CommandResponseMessage(new CommandMessage(1, 1 /* will be overriden by IgnoreWritesMemoryStream */, new[] { section0 }, false));
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Open_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed(
            [Values(false, true)]
            bool async)
        {
            _subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.OpenAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => _subject.Open(OperationContext.NoTimeout));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Open_should_raise_the_correct_events_upon_failure(
            [Values(false, true)]
            bool async)
        {
            _mockConnectionInitializer.Setup(i => i.SendHelloAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
                .Throws<SocketException>();
            _mockConnectionInitializer.Setup(i => i.SendHello(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
                .Throws<SocketException>();

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.OpenAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => _subject.Open(OperationContext.NoTimeout));

            exception.Should().BeOfType<MongoConnectionException>().Subject
                .ConnectionId.Should().Be(_subject.ConnectionId);
            exception.InnerException.Should().BeOfType<SocketException>();

            _capturedEvents.Next().Should().BeOfType<ConnectionOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionOpeningFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Open_should_setup_the_description(
            [Values(false, true)]
            bool async)
        {
            if (async)
            {
                await _subject.OpenAsync(OperationContext.NoTimeout);
            }
            else
            {
                _subject.Open(OperationContext.NoTimeout);
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
            _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, It.IsAny<CancellationToken>()))
               .Returns(() =>
               {
                   task1IsBlocked = true;
                   return completionSource.Task.GetAwaiter().GetResult();
               });
            _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    task1IsBlocked = true;
                    return completionSource.Task;
                });

            var openTask1 = async1 ?
                _subject.OpenAsync(OperationContext.NoTimeout) :
                Task.Run(() => _subject.Open(OperationContext.NoTimeout));
            SpinWait.SpinUntil(() => task1IsBlocked, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var openTask2 = async2 ?
                _subject.OpenAsync(OperationContext.NoTimeout) :
                Task.Run(() => _subject.Open(OperationContext.NoTimeout));

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
        public async Task Reauthentication_should_use_the_same_auth_context_as_in_initial_authentication(
            [Values(false, true)] bool async)
        {
            _subject._connectionInitializerContext().Should().BeNull();

            if (async)
            {
                await _subject.OpenAsync(OperationContext.NoTimeout);
            }
            else
            {
                _subject.Open(OperationContext.NoTimeout);
            }

            _subject._connectionInitializerContext().Should().Be(_connectionInitializerContextAfterAuthentication);

            if (async)
            {
                await _subject.ReauthenticateAsync(OperationContext.NoTimeout);
                _mockConnectionInitializer.Verify(c => c.AuthenticateAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), _connectionInitializerContext), Times.Once);
                _mockConnectionInitializer.Verify(c => c.AuthenticateAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), _connectionInitializerContextAfterAuthentication), Times.Once);
            }
            else
            {
                _subject.Reauthenticate(OperationContext.NoTimeout);
                _mockConnectionInitializer.Verify(c => c.Authenticate(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), _connectionInitializerContext), Times.Once);
                _mockConnectionInitializer.Verify(c => c.Authenticate(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), _connectionInitializerContextAfterAuthentication), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReceiveMessage_should_throw_a_FormatException_when_message_is_an_invalid_size(
            [Values(-1, 48000001)]
            int length,
            [Values(false, true)]
            bool async)
        {
            using (var stream = new BlockingMemoryStream())
            {
                var bytes = BitConverter.GetBytes(length);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);

                _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, It.IsAny<CancellationToken>())).ReturnsAsync(stream);
                _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, It.IsAny<CancellationToken>())).Returns(stream);
                await _subject.OpenAsync(OperationContext.NoTimeout);
                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                var exception = async ?
                    await Record.ExceptionAsync(() => _subject.ReceiveMessageAsync(OperationContext.NoTimeout, 10, encoderSelector, _messageEncoderSettings)) :
                    Record.Exception(() => _subject.ReceiveMessage(OperationContext.NoTimeout, 10, encoderSelector, _messageEncoderSettings));

                exception.Should().BeOfType<MongoConnectionException>();
                var e = exception.InnerException.Should().BeOfType<FormatException>().Subject;
                e.Message.Should().Be("The size of the message is invalid.");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReceiveMessage_should_throw_an_ArgumentNullException_when_the_encoderSelector_is_null(
            [Values(false, true)]
            bool async)
        {
            var exception = async ?
                await Record.ExceptionAsync(() => _subject.ReceiveMessageAsync(OperationContext.NoTimeout, 10, null, _messageEncoderSettings)) :
                Record.Exception(() => _subject.ReceiveMessage(OperationContext.NoTimeout, 10, null, _messageEncoderSettings));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("encoderSelector");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReceiveMessage_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed(
            [Values(false, true)]
            bool async)
        {
            var encoderSelector = new Mock<IMessageEncoderSelector>().Object;
            _subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.ReceiveMessageAsync(OperationContext.NoTimeout, 10, encoderSelector, _messageEncoderSettings)) :
                Record.Exception(() => _subject.ReceiveMessage(OperationContext.NoTimeout, 10, encoderSelector, _messageEncoderSettings));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReceiveMessage_should_throw_an_InvalidOperationException_if_the_connection_is_not_open(
            [Values(false, true)]
            bool async)
        {
            var encoderSelector = new Mock<IMessageEncoderSelector>().Object;

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.ReceiveMessageAsync(OperationContext.NoTimeout, 10, encoderSelector, _messageEncoderSettings)) :
                Record.Exception(() => _subject.ReceiveMessage(OperationContext.NoTimeout, 10, encoderSelector, _messageEncoderSettings));

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReceiveMessage_should_complete_when_reply_is_already_on_the_stream(
            [Values(false, true)]
            bool async)
        {
            using (var stream = new BlockingMemoryStream())
            {
                var messageToReceive = MessageHelper.BuildReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 10);
                MessageHelper.WriteResponsesToStream(stream, messageToReceive);

                _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stream);
                _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, It.IsAny<CancellationToken>()))
                    .Returns(stream);
                await _subject.OpenAsync(OperationContext.NoTimeout);
                _capturedEvents.Clear();

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                var received = async ?
                    await _subject.ReceiveMessageAsync(OperationContext.NoTimeout, 10, encoderSelector, _messageEncoderSettings) :
                    _subject.ReceiveMessage(OperationContext.NoTimeout, 10, encoderSelector, _messageEncoderSettings);

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
        public async Task ReceiveMessage_should_complete_when_reply_is_not_already_on_the_stream(
            [Values(false, true)]
            bool async)
        {
            using (var stream = new BlockingMemoryStream())
            {
                _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stream);
                _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, It.IsAny<CancellationToken>()))
                    .Returns(stream);
                await _subject.OpenAsync(OperationContext.NoTimeout);
                _capturedEvents.Clear();

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                var receiveMessageTask = async ?
                    _subject.ReceiveMessageAsync(OperationContext.NoTimeout, 10, encoderSelector, _messageEncoderSettings) :
                    Task.Run(() => _subject.ReceiveMessage(OperationContext.NoTimeout, 10, encoderSelector, _messageEncoderSettings));

                receiveMessageTask.IsCompleted.Should().BeFalse();

                var messageToReceive = MessageHelper.BuildReply<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance, responseTo: 10);
                MessageHelper.WriteResponsesToStream(stream, messageToReceive);

                var received = await receiveMessageTask;

                var expected = MessageHelper.TranslateMessagesToBsonDocuments(new[] { messageToReceive });
                var actual = MessageHelper.TranslateMessagesToBsonDocuments(new[] { received });

                actual.Should().BeEquivalentTo(expected);

                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivedMessageEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        [Fact]
        public async Task ReceiveMessage_should_not_produce_unobserved_task_exceptions_on_fail()
        {
            var unobservedTaskExceptionRaised = false;
            var mockStream = new Mock<Stream>();
            EventHandler<UnobservedTaskExceptionEventArgs> eventHandler = (s, args) =>
            {
                if (args.Exception.InnerException is MongoConnectionException)
                {
                    unobservedTaskExceptionRaised = true;
                }
            };

            try
            {
                TaskScheduler.UnobservedTaskException += eventHandler;
                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                _mockStreamFactory
                    .Setup(f => f.CreateStream(_endPoint, CancellationToken.None))
                    .Returns(mockStream.Object);

                var tcs = new TaskCompletionSource<int>();
                tcs.SetException(new SocketException());
                SetupStreamRead(mockStream, tcs);

                _subject.Open(OperationContext.NoTimeout);

                var exception = await Record.ExceptionAsync(() => _subject.ReceiveMessageAsync(OperationContext.NoTimeout, 1, encoderSelector, _messageEncoderSettings));
                exception.Should().BeOfType<MongoConnectionException>();

                GC.Collect(); // Collects the unobserved tasks
                GC.WaitForPendingFinalizers(); // Assures finilizers are executed

                unobservedTaskExceptionRaised.Should().BeFalse();
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= eventHandler;
                mockStream.Object?.Dispose();
            }
        }

        [Fact]
        public async Task ReceiveMessageAsync_should_not_produce_unobserved_task_exceptions_on_timeout()
        {
            GC.Collect(); // Collects the unobserved tasks
            GC.WaitForPendingFinalizers(); // Assures finalizers are executed

            Exception ex = null;
            var mockStream = new Mock<Stream>();
            EventHandler<UnobservedTaskExceptionEventArgs> eventHandler = (s, args) =>
            {
                ex = args.Exception;
            };

            try
            {
                TaskScheduler.UnobservedTaskException += eventHandler;
                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                _mockStreamFactory
                    .Setup(f => f.CreateStream(_endPoint, It.IsAny<CancellationToken>()))
                    .Returns(mockStream.Object);

                var tcs = new TaskCompletionSource<int>();
                SetupStreamRead(mockStream, tcs);
                _subject.Open(OperationContext.NoTimeout);

                var exception = await Record.ExceptionAsync(() => _subject.ReceiveMessageAsync(OperationContext.NoTimeout, 1, encoderSelector, _messageEncoderSettings));
                exception.Should().BeOfType<MongoConnectionException>();
                exception.InnerException.Should().BeOfType<TimeoutException>();

                tcs = null;
                mockStream.Reset();
                GC.Collect(); // Collects the unobserved tasks
                GC.WaitForPendingFinalizers(); // Assures finalizers are executed

                if (ex != null)
                {
                    Assert.Fail($"{ex.Message} - {ex}");
                }
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= eventHandler;
                mockStream.Object?.Dispose();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReceiveMessage_should_throw_MongoConnectionClosedException_when_connection_has_failed(
            [Values(false, true)]
            bool async1,
            [Values(false, true)]
            bool async2)
        {
            var mockStream = new Mock<Stream>();
            using (mockStream.Object)
            {
                _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, It.IsAny<CancellationToken>())).ReturnsAsync(mockStream.Object);
                var readTcs = new TaskCompletionSource<int>();
                readTcs.SetException(new SocketException());
                SetupStreamRead(mockStream, readTcs);
                await _subject.OpenAsync(OperationContext.NoTimeout);
                _capturedEvents.Clear();

                var encoderSelector = new ReplyMessageEncoderSelector<BsonDocument>(BsonDocumentSerializer.Instance);

                var exception1 = async1 ?
                    await Record.ExceptionAsync(() => _subject.ReceiveMessageAsync(OperationContext.NoTimeout, 1, encoderSelector, _messageEncoderSettings)) :
                    Record.Exception(() => _subject.ReceiveMessage(OperationContext.NoTimeout, 1, encoderSelector, _messageEncoderSettings));

                var exception2 = async2 ?
                    await Record.ExceptionAsync(() => _subject.ReceiveMessageAsync(OperationContext.NoTimeout, 2, encoderSelector, _messageEncoderSettings)) :
                    Record.Exception(() => _subject.ReceiveMessage(OperationContext.NoTimeout, 2, encoderSelector, _messageEncoderSettings));

                exception1.Should().BeOfType<MongoConnectionException>().Subject
                    .ConnectionId.Should().Be(_subject.ConnectionId);
                exception1.InnerException.Should().BeOfType<SocketException>();

                exception2.Should().BeOfType<MongoConnectionClosedException>().Subject
                    .ConnectionId.Should().Be(_subject.ConnectionId);

                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionFailedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionReceivingMessageFailedEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SendMessage_should_throw_an_ArgumentNullException_if_message_is_null(
            [Values(false, true)]
            bool async)
        {
            var exception = async ?
                await Record.ExceptionAsync(() => _subject.SendMessageAsync(OperationContext.NoTimeout, null, _messageEncoderSettings)) :
                Record.Exception(() => _subject.SendMessage(OperationContext.NoTimeout, null, _messageEncoderSettings));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SendMessage_should_throw_an_ObjectDisposedException_if_the_connection_is_disposed(
            [Values(false, true)]
            bool async)
        {
            var message = MessageHelper.BuildQuery();
            _subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.SendMessageAsync(OperationContext.NoTimeout, message, _messageEncoderSettings)) :
                Record.Exception(() => _subject.SendMessage(OperationContext.NoTimeout, message, _messageEncoderSettings));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SendMessage_should_throw_an_InvalidOperationException_if_the_connection_is_not_open(
            [Values(false, true)]
            bool async)
        {
            var message = MessageHelper.BuildQuery();

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.SendMessageAsync(OperationContext.NoTimeout, message, _messageEncoderSettings)) :
                Record.Exception(() => _subject.SendMessage(OperationContext.NoTimeout, message, _messageEncoderSettings));

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SendMessage_should_put_the_message_on_the_stream_and_raise_the_correct_events(
            [Values(false, true)]
            bool async)
        {
            using (var stream = new MemoryStream())
            {
                _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, It.IsAny<CancellationToken>())).ReturnsAsync(stream);
                _mockStreamFactory.Setup(f => f.CreateStream(_endPoint, It.IsAny<CancellationToken>())).Returns(stream);
                _subject.OpenAsync(OperationContext.NoTimeout).GetAwaiter().GetResult();
                _capturedEvents.Clear();

                var message = MessageHelper.BuildQuery(query: new BsonDocument("x", 1));

                if (async)
                {
                    await _subject.SendMessageAsync(OperationContext.NoTimeout, message, _messageEncoderSettings);
                }
                else
                {
                    _subject.SendMessage(OperationContext.NoTimeout, message, _messageEncoderSettings);
                }

                var expectedRequests = MessageHelper.TranslateMessagesToBsonDocuments(new[] { message });
                var sentRequests = MessageHelper.TranslateMessagesToBsonDocuments(stream.ToArray());

                sentRequests.Should().BeEquivalentTo(expectedRequests);
                _capturedEvents.Next().Should().BeOfType<ConnectionSendingMessagesEvent>();
                _capturedEvents.Next().Should().BeOfType<CommandStartedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionSentMessagesEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        private void SetupStreamRead(Mock<Stream> streamMock, TaskCompletionSource<int> tcs)
        {
            streamMock.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((byte[] _, int __, int ___) => tcs.Task.GetAwaiter().GetResult());
            streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            streamMock.Setup(s => s.Close()).Callback(() => tcs.TrySetException(new ObjectDisposedException("stream")));
        }

        // nested type
        private sealed class IgnoreWritesMemoryStream : MemoryStream
        {
            public IgnoreWritesMemoryStream(byte[] bytes) : base(bytes) { }

            public override void Write(byte[] buffer, int offset, int count)
            {
                Position = 4;
                base.Write(buffer, 4, 4); // copy requestId
                base.Write(buffer, 4, 4); // set responseTo to requestId
                Position = 0;
                // do nothing else
            }
        }
    }

    internal static class BinaryConnectionReflector
    {
        public static ConnectionInitializerContext _connectionInitializerContext(this BinaryConnection subject)
            => (ConnectionInitializerContext)Reflector.GetFieldValue(subject, nameof(_connectionInitializerContext));
    }
}

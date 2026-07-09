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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Core.Connections
{
    public class BinaryConnection_CommandEventTests : LoggableTestClass
    {
        private Mock<IConnectionInitializer> _mockConnectionInitializer;
        private DnsEndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();
        private BlockingMemoryStream _stream;
        private Mock<IStreamFactory> _mockStreamFactory;
        private BinaryConnection _subject;
        private EventContext.OperationIdDisposer _operationIdDisposer;

        public static IEnumerable<object[]> GetPotentiallyRedactedCommandTestCases()
        {
            return new object[][]
            {
                // string commandJson, bool shouldBeRedacted
                new object[] { "{ xyz : 1 }", false },
                new object[] { "{ authenticate : 1 }", true },
                new object[] { "{ saslStart : 1 }", true },
                new object[] { "{ saslContinue : 1 }", true },
                new object[] { "{ getnonce : 1 }", true },
                new object[] { "{ createUser : 1 }", true },
                new object[] { "{ updateUser : 1 }", true },
                new object[] { "{ copydbsaslstart : 1 }", true },
                new object[] { "{ copydb : 1 }", true },
                new object[] { "{ hello : 1, helloOk : true }", false },
                new object[] { "{ hello : 1, helloOk : true, speculativeAuthenticate : { } }", true },
                new object[] { $"{{ {OppressiveLanguageConstants.LegacyHelloCommandName} : 1, helloOk : true }}", false },
                new object[] { $"{{ {OppressiveLanguageConstants.LegacyHelloCommandName} : 1, helloOk : true, speculativeAuthenticate : {{ }} }}", true },
            };
        }

        public BinaryConnection_CommandEventTests(ITestOutputHelper output) : base(output)
        {
            _capturedEvents = new EventCapturer()
                .Capture<CommandStartedEvent>()
                .Capture<CommandSucceededEvent>()
                .Capture<CommandFailedEvent>();

            _mockStreamFactory = new Mock<IStreamFactory>();

            _endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), _endPoint);
            Func<ConnectionDescription> connectionDescriptionFunc = () =>
                new ConnectionDescription(
                    new ConnectionId(new ServerId(new ClusterId(), _endPoint)),
                    new HelloResult(new BsonDocument { { "maxWireVersion", WireVersion.Server36 } }));

            _mockConnectionInitializer = new Mock<IConnectionInitializer>();
            _mockConnectionInitializer.Setup(i => i.SendHelloAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>()))
                .Returns(() => Task.FromResult(new ConnectionInitializerContext(connectionDescriptionFunc(), null)));
            _mockConnectionInitializer.Setup(i => i.AuthenticateAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionInitializerContext>()))
                .Returns(() => Task.FromResult(new ConnectionInitializerContext(connectionDescriptionFunc(), null)));

            _subject = new BinaryConnection(
                serverId: serverId,
                endPoint: _endPoint,
                settings: new ConnectionSettings(),
                streamFactory: _mockStreamFactory.Object,
                connectionInitializer: _mockConnectionInitializer.Object,
                eventSubscriber: _capturedEvents,
                loggerFactory: LoggerFactory,
                tracingOptions: null,
                socketReadTimeout: Timeout.InfiniteTimeSpan,
                socketWriteTimeout: Timeout.InfiniteTimeSpan);

            _stream = new BlockingMemoryStream();
            _mockStreamFactory.Setup(f => f.CreateStreamAsync(_endPoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Stream>(_stream));
            _subject.OpenAsync(OperationContext.NoTimeout).Wait();
            _capturedEvents.Clear();

            _operationIdDisposer = EventContext.BeginOperation();
        }

        protected override void DisposeInternal()
        {
            _stream.Dispose();
            _operationIdDisposer.Dispose();
        }

        [Fact]
        public void Should_process_a_command()
        {
            var expectedCommand = BsonDocument.Parse("{ hello : 1, helloOk : true }");
            var expectedReplyDoc = BsonDocument.Parse("{ ok: 1 }");

            var requestMessage = MessageHelper.BuildCommand(
                expectedCommand,
                requestId: 10);
            SendMessage(requestMessage);

            var replyMessage = MessageHelper.BuildCommandResponse(
                new RawBsonDocument(expectedReplyDoc.ToBson()),
                responseTo: requestMessage.RequestId);
            ReceiveMessage(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(expectedReplyDoc);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Theory]
        [MemberData(nameof(GetPotentiallyRedactedCommandTestCases))]
        public void Should_process_a_redacted_command(string commandJson, bool shouldBeRedacted)
        {
            var command = BsonDocument.Parse(commandJson);
            var reply = RawBsonDocumentHelper.FromJson("{ ok: 1, extra: true }");

            var requestMessage = MessageHelper.BuildCommand(
                command,
                requestId: 10);
            SendMessage(requestMessage);

            var replyMessage = MessageHelper.BuildCommandResponse(
                reply,
                responseTo: requestMessage.RequestId);
            ReceiveMessage(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandSucceededEvent = (CommandSucceededEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(command.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(shouldBeRedacted ? new BsonDocument() : command);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandSucceededEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandSucceededEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandSucceededEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandSucceededEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandSucceededEvent.Reply.Should().Be(shouldBeRedacted ? new BsonDocument() : reply);
            commandSucceededEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
        }

        [Fact]
        public void Should_process_a_failed_command()
        {
            var expectedCommand = BsonDocument.Parse("{ hello : 1, helloOk : true }");
            var expectedReply = RawBsonDocumentHelper.FromJson("{ ok: 0 }");

            var requestMessage = MessageHelper.BuildCommand(
                expectedCommand,
                requestId: 10);
            SendMessage(requestMessage);

            var replyMessage = MessageHelper.BuildCommandResponse(
                expectedReply,
                responseTo: requestMessage.RequestId);
            ReceiveMessage(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandFailedEvent = (CommandFailedEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(expectedCommand.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(expectedCommand);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandFailedEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandFailedEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandFailedEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandFailedEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandFailedEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
            commandFailedEvent.Failure.Should().BeOfType<MongoCommandException>();
        }

        [Theory]
        [MemberData(nameof(GetPotentiallyRedactedCommandTestCases))]
        public void Should_process_a_redacted_failed_command(string commandJson, bool shouldBeRedacted)
        {
            var command = BsonDocument.Parse(commandJson);
            var reply = RawBsonDocumentHelper.FromJson("{ ok: 0, extra: true }");

            var requestMessage = MessageHelper.BuildCommand(
                command,
                requestId: 10);
            SendMessage(requestMessage);

            var replyMessage = MessageHelper.BuildCommandResponse(
                reply,
                responseTo: requestMessage.RequestId);
            ReceiveMessage(replyMessage);

            var commandStartedEvent = (CommandStartedEvent)_capturedEvents.Next();
            var commandFailedEvent = (CommandFailedEvent)_capturedEvents.Next();

            commandStartedEvent.CommandName.Should().Be(command.GetElement(0).Name);
            commandStartedEvent.Command.Should().Be(shouldBeRedacted ? new BsonDocument() : command);
            commandStartedEvent.ConnectionId.Should().Be(_subject.ConnectionId);
            commandStartedEvent.DatabaseNamespace.Should().Be(MessageHelper.DefaultDatabaseNamespace);
            commandStartedEvent.OperationId.Should().Be(EventContext.OperationId);
            commandStartedEvent.RequestId.Should().Be(requestMessage.RequestId);

            commandFailedEvent.CommandName.Should().Be(commandStartedEvent.CommandName);
            commandFailedEvent.ConnectionId.Should().Be(commandStartedEvent.ConnectionId);
            commandFailedEvent.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            commandFailedEvent.OperationId.Should().Be(commandStartedEvent.OperationId);
            commandFailedEvent.RequestId.Should().Be(commandStartedEvent.RequestId);
            commandFailedEvent.Failure.Should().BeOfType<MongoCommandException>();
            var exception = (MongoCommandException)commandFailedEvent.Failure;
            exception.Result.Should().Be(shouldBeRedacted ? new BsonDocument() : reply);
        }

        private void SendMessage(RequestCommandMessage message)
        {
            _subject.SendMessageAsync(OperationContext.NoTimeout, message, _messageEncoderSettings).Wait();
        }

        private void ReceiveMessage(ResponseCommandMessage message)
        {
            lock (_stream.Lock)
            {
                var startPosition = _stream.Position;
                var encoderFactory = new MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders.BinaryMessageEncoderFactory(_stream, null);
                var encoder = encoderFactory.GetCommandMessageEncoder();
                encoder.WriteMessage(message);
                _stream.Position = startPosition;
            }
            _subject.ReceiveMessageAsync(OperationContext.NoTimeout, message.ResponseTo, _messageEncoderSettings).Wait();
        }
    }
}

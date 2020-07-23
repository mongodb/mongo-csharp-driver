/* Copyright 2015-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class CommandWriteProtocolTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Execute_should_use_cached_IWireProtocol_if_available([Values(false, true)] bool withSameConnection)
        {
            var session = NoCoreSession.Instance;
            var responseHandling = CommandResponseHandling.Return;

            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                session,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                null, // commandPayloads
                NoOpElementNameValidator.Instance,
                null, // additionalOptions
                null, // postWriteAction
                responseHandling,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings);

            var mockConnection = new Mock<IConnection>();
            var commandResponse = MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1)));
            var connectionId = SetupConnection(mockConnection);

            var result = subject.Execute(mockConnection.Object, CancellationToken.None);

            var cachedWireProtocol = subject._cachedWireProtocol();
            cachedWireProtocol.Should().NotBeNull();
            var cachedConnectionId = subject._cachedConnectionId();
            cachedConnectionId.Should().NotBeNull();
            subject._cachedConnectionId().Should().BeSameAs(connectionId);
            result.Should().Be("{ ok : 1 }");

            commandResponse = MessageHelper.BuildCommandResponse(CreateRawBsonDocument(new BsonDocument("ok", 1)));
            _ = SetupConnection(mockConnection, connectionId);
            subject._responseHandling(CommandResponseHandling.Ignore); // will trigger the exception if the CommandUsingCommandMessageWireProtocol ctor will be called

            result = null;
            var exception = Record.Exception(() => { result = subject.Execute(mockConnection.Object, CancellationToken.None); });

            if (withSameConnection)
            {
                exception.Should().BeNull();
                subject._cachedWireProtocol().Should().BeSameAs(cachedWireProtocol);
                subject._cachedConnectionId().Should().BeSameAs(connectionId);
                result.Should().Be("{ ok : 1 }");
            }
            else
            {
                var e = exception.Should().BeOfType<ArgumentException>().Subject;
                e.Message.Should().Be("CommandResponseHandling must be Return, NoneExpected or ExhaustAllowed.\r\nParameter name: responseHandling");
                subject._cachedConnectionId().Should().NotBeSameAs(cachedWireProtocol);
                subject._cachedConnectionId().Should().NotBeSameAs(connectionId);
                result.Should().BeNull();
            }

            ConnectionId SetupConnection(Mock<IConnection> connection, ConnectionId id = null)
            {
                if (id == null || !withSameConnection)
                {
                    id = new ConnectionId(new ServerId(new ClusterId(IdGenerator<ClusterId>.GetNextId()), new DnsEndPoint("localhost", 27017)));
                }

                connection
                    .Setup(c => c.ReceiveMessage(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), messageEncoderSettings, CancellationToken.None))
                    .Returns(commandResponse);
                connection.SetupGet(c => c.ConnectionId).Returns(id);
                connection
                    .SetupGet(c => c.Description)
                    .Returns(
                        new ConnectionDescription(
                            id,
                            new IsMasterResult(new BsonDocument("ok", 1)),
                            new BuildInfoResult(new BsonDocument("version", "4.4"))));
                return id;
            }
        }

        [Fact]
        public void Execute_should_wait_for_response_when_CommandResponseHandling_is_Return()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                NoCoreSession.Instance,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                null, // commandPayloads
                NoOpElementNameValidator.Instance,
                null, // additionalOptions
                null, // postWriteAction
                CommandResponseHandling.Return,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings);

            var mockConnection = new Mock<IConnection>();

            var commandResponse = MessageHelper.BuildReply(CreateRawBsonDocument(new BsonDocument("ok", 1)));
            mockConnection
                .Setup(c => c.ReceiveMessage(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), messageEncoderSettings, CancellationToken.None))
                .Returns(commandResponse);

            var result = subject.Execute(mockConnection.Object, CancellationToken.None);
            result.Should().Be("{ok: 1}");
        }

        [Fact]
        public void Execute_should_not_wait_for_response_when_CommandResponseHandling_is_NoResponseExpected()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                NoCoreSession.Instance,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                null, // commandPayloads
                NoOpElementNameValidator.Instance,
                null, // additionalOptions
                null, // postWriteAction
                CommandResponseHandling.NoResponseExpected,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings);

            var mockConnection = new Mock<IConnection>();

            var result = subject.Execute(mockConnection.Object, CancellationToken.None);
            result.Should().BeNull();

            mockConnection.Verify(
                c => c.ReceiveMessageAsync(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), messageEncoderSettings, CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public void ExecuteAsync_should_wait_for_response_when_CommandResponseHandling_is_Return()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                NoCoreSession.Instance,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                null, // commandPayloads
                NoOpElementNameValidator.Instance,
                null, // additionalOptions
                null, // postWriteAction
                CommandResponseHandling.Return,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings);

            var mockConnection = new Mock<IConnection>();

            var commandResponse = MessageHelper.BuildReply(CreateRawBsonDocument(new BsonDocument("ok", 1)));
            mockConnection
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), messageEncoderSettings, CancellationToken.None))
                .Returns(Task.FromResult<ResponseMessage>(commandResponse));

            var result = subject.ExecuteAsync(mockConnection.Object, CancellationToken.None).GetAwaiter().GetResult();
            result.Should().Be("{ok: 1}");
        }

        [Fact]
        public void ExecuteAsync_should_not_wait_for_response_when_CommandResponseHandling_is_NoResponseExpected()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                NoCoreSession.Instance,
                ReadPreference.Primary,
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                null, // commandPayloads
                NoOpElementNameValidator.Instance,
                null, // additionalOptions
                null, // postWriteAction
                CommandResponseHandling.NoResponseExpected,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings);

            var mockConnection = new Mock<IConnection>();

            var result = subject.ExecuteAsync(mockConnection.Object, CancellationToken.None).GetAwaiter().GetResult();
            result.Should().BeNull();

            mockConnection.Verify(c => c.ReceiveMessageAsync(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), messageEncoderSettings, CancellationToken.None), Times.Once);
        }

        // private methods
        private RawBsonDocument CreateRawBsonDocument(BsonDocument doc)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var bsonWriter = new BsonBinaryWriter(memoryStream, BsonBinaryWriterSettings.Defaults))
                {
                    var context = BsonSerializationContext.CreateRoot(bsonWriter);
                    BsonDocumentSerializer.Instance.Serialize(context, doc);
                }

                return new RawBsonDocument(memoryStream.ToArray());
            }
        }
    }

    internal static class CommandWireProtocolReflector
    {
        public static ConnectionId _cachedConnectionId(this CommandWireProtocol<BsonDocument> commandWireProtocol)
        {
            return (ConnectionId)Reflector.GetFieldValue(commandWireProtocol, nameof(_cachedConnectionId));
        }

        public static IWireProtocol<BsonDocument> _cachedWireProtocol(this CommandWireProtocol<BsonDocument> commandWireProtocol)
        {
            return (IWireProtocol<BsonDocument>)Reflector.GetFieldValue(commandWireProtocol, nameof(_cachedWireProtocol));
        }

        public static BsonDocument _command(this CommandWireProtocol<BsonDocument> commandWireProtocol)
        {
            return (BsonDocument)Reflector.GetFieldValue(commandWireProtocol, nameof(_command));
        }

        public static void _responseHandling(this CommandWireProtocol<BsonDocument> commandWireProtocol, CommandResponseHandling commandResponseHandling)
        {
            Reflector.SetFieldValue(commandWireProtocol, nameof(_responseHandling), commandResponseHandling);
        }

        public static CommandResponseHandling _responseHandling(this CommandWireProtocol<BsonDocument> commandWireProtocol)
        {
            return (CommandResponseHandling)Reflector.GetFieldValue(commandWireProtocol, nameof(_responseHandling));
        }
    }
}

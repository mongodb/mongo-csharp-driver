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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class CommandWriteProtocolTests
    {
        [Fact]
        public void Execute_should_wait_for_response_when_CommandResponseHandling_is_Return()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                NoOpElementNameValidator.Instance,
                () => CommandResponseHandling.Return,
                true,
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
        public void Execute_should_not_wait_for_response_when_CommandResponseHandling_is_Ignore()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                NoOpElementNameValidator.Instance,
                () => CommandResponseHandling.Ignore,
                true,
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
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                NoOpElementNameValidator.Instance,
                () => CommandResponseHandling.Return,
                true,
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
        public void ExecuteAsync_should_not_wait_for_response_when_CommandResponseHandling_is_Ignore()
        {
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new CommandWireProtocol<BsonDocument>(
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                NoOpElementNameValidator.Instance,
                () => CommandResponseHandling.Ignore,
                true,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings);

            var mockConnection = new Mock<IConnection>();

            var result = subject.ExecuteAsync(mockConnection.Object, CancellationToken.None).GetAwaiter().GetResult();
            result.Should().BeNull();

            mockConnection.Verify(c => c.ReceiveMessageAsync(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), messageEncoderSettings, CancellationToken.None), Times.Once);
        }

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
}
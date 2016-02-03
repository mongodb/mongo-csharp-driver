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
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.WireProtocol
{
    [TestFixture]
    public class CommandWriteProtocolTests
    {
        [Test]
        public void Execute_should_wait_for_response_when_CommandResponseHandling_is_Return()
        {
            var subject = new CommandWireProtocol<BsonDocument>(
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                NoOpElementNameValidator.Instance,
                () => CommandResponseHandling.Return,
                true,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings());

            var connection = Substitute.For<IConnection>();

            var cmdResponse = MessageHelper.BuildReply(CreateRawBsonDocument(new BsonDocument("ok", 1)));
            connection.ReceiveMessage(0, null, null, CancellationToken.None).ReturnsForAnyArgs(cmdResponse);

            var result = subject.Execute(connection, CancellationToken.None);
            result.Should().Be("{ok: 1}");
        }

        [Test]
        public void Execute_should_not_wait_for_response_when_CommandResponseHandling_is_Ignore()
        {
            var subject = new CommandWireProtocol<BsonDocument>(
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                NoOpElementNameValidator.Instance,
                () => CommandResponseHandling.Ignore,
                true,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings());

            var connection = Substitute.For<IConnection>();

            var result = subject.Execute(connection, CancellationToken.None);
            result.Should().BeNull();

            connection.ReceivedWithAnyArgs().ReceiveMessageAsync(0, null, null, CancellationToken.None);
        }

        [Test]
        public void ExecuteAsync_should_wait_for_response_when_CommandResponseHandling_is_Return()
        {
            var subject = new CommandWireProtocol<BsonDocument>(
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                NoOpElementNameValidator.Instance,
                () => CommandResponseHandling.Return,
                true,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings());

            var connection = Substitute.For<IConnection>();

            var cmdResponse = MessageHelper.BuildReply(CreateRawBsonDocument(new BsonDocument("ok", 1)));
            connection.ReceiveMessageAsync(0, null, null, CancellationToken.None).ReturnsForAnyArgs(Task.FromResult<ResponseMessage>(cmdResponse));

            var result = subject.ExecuteAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
            result.Should().Be("{ok: 1}");
        }

        [Test]
        public void ExecuteAsync_should_not_wait_for_response_when_CommandResponseHandling_is_Ignore()
        {
            var subject = new CommandWireProtocol<BsonDocument>(
                new DatabaseNamespace("test"),
                new BsonDocument("cmd", 1),
                NoOpElementNameValidator.Instance,
                () => CommandResponseHandling.Ignore,
                true,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings());

            var connection = Substitute.For<IConnection>();

            var result = subject.ExecuteAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
            result.Should().BeNull();

            connection.ReceivedWithAnyArgs().ReceiveMessageAsync(0, null, null, CancellationToken.None);
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
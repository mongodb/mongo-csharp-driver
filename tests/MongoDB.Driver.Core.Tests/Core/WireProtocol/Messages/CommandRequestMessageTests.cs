/* Copyright 2018-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public class CommandRequestMessageTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var sections = new[] { new Type0CommandMessageSection<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance) };
            var wrappedMessage = new CommandMessage(1, 2, sections, false);
            Func<bool> shouldBeSent = () => true;

            var result = new CommandRequestMessage(wrappedMessage, shouldBeSent);

            result.WrappedMessage.Should().BeSameAs(wrappedMessage);
            result.ShouldBeSent.Should().BeSameAs(shouldBeSent);
        }

        [Fact]
        public void MessageType_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.MessageType;

            result.Should().Be(MongoDBMessageType.Command);
        }

        [Fact]
        public void WrappedMessage_should_return_expected_result()
        {
            var sections = new[] { new Type0CommandMessageSection<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance) };
            var wrappedMessage = new CommandMessage(1, 2, sections, false);
            var subject = CreateSubject(wrappedMessage: wrappedMessage);

            var result = subject.WrappedMessage;

            result.Should().BeSameAs(wrappedMessage);
        }

        [Fact]
        public void GetEncoder_should_return_expected_result()
        {
            var subject = CreateSubject();
            var stream = new MemoryStream();
            var encoderSettings = new MessageEncoderSettings();
            var encoderFactory = new BinaryMessageEncoderFactory(stream, encoderSettings);

            var result = subject.GetEncoder(encoderFactory);

            result.Should().BeOfType<CommandRequestMessageBinaryEncoder>();
        }

        // private methods
        private CommandRequestMessage CreateSubject(
            CommandMessage wrappedMessage = null,
            Func<bool> shouldBeSent = null)
        {
            if (wrappedMessage == null)
            {
                var sections = new[] { new Type0CommandMessageSection<BsonDocument>(new BsonDocument(), BsonDocumentSerializer.Instance) };
                wrappedMessage = new CommandMessage(1, 2, sections, false);
            }
            shouldBeSent = shouldBeSent ?? (() => true);
            return new CommandRequestMessage(wrappedMessage, shouldBeSent);
        }
    }
}

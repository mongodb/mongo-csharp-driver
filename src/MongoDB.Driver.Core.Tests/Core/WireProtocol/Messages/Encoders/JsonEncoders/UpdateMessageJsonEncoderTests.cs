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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class UpdateMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static readonly CollectionNamespace __collectionNamespace = new CollectionNamespace("d", "c");
        private static readonly bool __isMulti = true;
        private static readonly bool __isUpsert = true;
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly BsonDocument __query = new BsonDocument("x", 1);
        private static readonly int __requestId = 1;
        private static readonly UpdateMessage __testMessage;
        private static readonly string __testMessageJson;
        private static readonly BsonDocument __update = new BsonDocument("y", 1);
        private static readonly IElementNameValidator _updateValidator = NoOpElementNameValidator.Instance;

        // static constructor
        static UpdateMessageJsonEncoderTests()
        {
            __testMessage = new UpdateMessage(__requestId, __collectionNamespace, __query, __update, _updateValidator, __isMulti, __isUpsert);

            __testMessageJson =
                "{ " +
                    "\"opcode\" : \"update\", " +
                    "\"requestId\" : 1, " +
                    "\"database\" : \"d\", " +
                    "\"collection\" : \"c\", " +
                    "\"isMulti\" : true, " +
                    "\"isUpsert\" : true, " +
                    "\"query\" : { \"x\" : 1 }, " +
                    "\"update\" : { \"y\" : 1 }" +
                " }";
        }
        #endregion

        [Fact]
        public void Constructor_should_not_throw_if_textReader_and_textWriter_are_both_provided()
        {
            using (var textReader = new StringReader(""))
            using (var textWriter = new StringWriter())
            {
                Action action = () => new UpdateMessageJsonEncoder(textReader, textWriter, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textReader_is_provided()
        {
            using (var textReader = new StringReader(""))
            {
                Action action = () => new UpdateMessageJsonEncoder(textReader, null, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textWriter_is_provided()
        {
            using (var textWriter = new StringWriter())
            {
                Action action = () => new UpdateMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_textReader_and_textWriter_are_both_null()
        {
            Action action = () => new UpdateMessageJsonEncoder(null, null, __messageEncoderSettings);
            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var textReader = new StringReader(__testMessageJson))
            {
                var subject = new UpdateMessageJsonEncoder(textReader, null, __messageEncoderSettings);
                var message = subject.ReadMessage();
                message.CollectionNamespace.Should().Be(__collectionNamespace);
                message.IsMulti.Should().Be(__isMulti);
                message.IsUpsert.Should().Be(__isUpsert);
                message.Query.Should().Be(__query);
                message.RequestId.Should().Be(__requestId);
                message.Update.Should().Be(__update);
            }
        }

        [Fact]
        public void ReadMessage_should_throw_if_textReader_was_not_provided()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new UpdateMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                Action action = () => subject.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_textWriter_was_not_provided()
        {
            using (var textReader = new StringReader(""))
            {
                var subject = new UpdateMessageJsonEncoder(textReader, null, __messageEncoderSettings);
                Action action = () => subject.WriteMessage(__testMessage);
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new UpdateMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new UpdateMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                subject.WriteMessage(__testMessage);
                var json = textWriter.ToString();
                json.Should().Be(__testMessageJson);
            }
        }
    }
}

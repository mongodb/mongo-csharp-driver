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
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class ReplyMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static readonly bool __awaitCapable = true;
        private static readonly long __cursorId = 3;
        private static readonly bool __cursorNotFound = false;
        private static readonly List<BsonDocument> __documents = new List<BsonDocument>(new[] { new BsonDocument("_id", 1), new BsonDocument("_id", 2) });
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly int __numberReturned = 2;
        private static readonly BsonDocument __queryFailureDocument = new BsonDocument("ok", 0);
        private static readonly ReplyMessage<BsonDocument> __queryFailureMessage;
        private static readonly string __queryFailureMessageJson;
        private static readonly int __requestId = 1;
        private static readonly int __responseTo = 2;
        private static readonly IBsonSerializer<BsonDocument> __serializer = BsonDocumentSerializer.Instance;
        private static readonly int __startingFrom = 4;
        private static readonly ReplyMessage<BsonDocument> __testMessage;
        private static readonly string __testMessageJson;

        // static constructor
        static ReplyMessageJsonEncoderTests()
        {
            __testMessage = new ReplyMessage<BsonDocument>(__awaitCapable, __cursorId, __cursorNotFound, __documents, __numberReturned, false, null, __requestId, __responseTo, __serializer, __startingFrom);

            __testMessageJson =
                "{ " +
                    "\"opcode\" : \"reply\", " +
                    "\"requestId\" : 1, " +
                    "\"responseTo\" : 2, " +
                    "\"cursorId\" : NumberLong(3), " +
                    "\"numberReturned\" : 2, " +
                    "\"startingFrom\" : 4, " +
                    "\"awaitCapable\" : true, " +
                    "\"documents\" : [{ \"_id\" : 1 }, { \"_id\" : 2 }]" +
                " }";

            __queryFailureMessage = new ReplyMessage<BsonDocument>(false, __cursorId, true, null, 0, true, __queryFailureDocument, __requestId, __responseTo, __serializer, 0);

            __queryFailureMessageJson =
                "{ " +
                    "\"opcode\" : \"reply\", " +
                    "\"requestId\" : 1, " +
                    "\"responseTo\" : 2, " +
                    "\"cursorId\" : NumberLong(3), " +
                    "\"cursorNotFound\" : true, " +
                    "\"numberReturned\" : 0, " +
                    "\"queryFailure\" : { \"ok\" : 0 }" +
               " }";
        }
        #endregion

        [Fact]
        public void Constructor_should_not_throw_if_textReader_and_textWriter_are_both_provided()
        {
            using (var textReader = new StringReader(""))
            using (var textWriter = new StringWriter())
            {
                Action action = () => new ReplyMessageJsonEncoder<BsonDocument>(textReader, textWriter, __messageEncoderSettings, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textReader_is_provided()
        {
            using (var textReader = new StringReader(""))
            {
                Action action = () => new ReplyMessageJsonEncoder<BsonDocument>(textReader, null, __messageEncoderSettings, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textWriter_is_provided()
        {
            using (var textWriter = new StringWriter())
            {
                Action action = () => new ReplyMessageJsonEncoder<BsonDocument>(null, textWriter, __messageEncoderSettings, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_textReader_and_textWriter_are_both_null()
        {
            Action action = () => new ReplyMessageJsonEncoder<BsonDocument>(null, null, __messageEncoderSettings, __serializer);
            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Constructor_should_throw_if_serializer_is_null()
        {
            using (var textReader = new StringReader(""))
            using (var textWriter = new StringWriter())
            {
                Action action = () => new ReplyMessageJsonEncoder<BsonDocument>(textReader, textWriter, __messageEncoderSettings, null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var textReader = new StringReader(__testMessageJson))
            {
                var subject = new ReplyMessageJsonEncoder<BsonDocument>(textReader, null, __messageEncoderSettings, __serializer);
                var message = subject.ReadMessage();
                message.AwaitCapable.Should().Be(__awaitCapable);
                message.CursorId.Should().Be(__cursorId);
                message.CursorNotFound.Should().Be(__cursorNotFound);
                message.Documents.Should().Equal(__documents);
                message.NumberReturned.Should().Be(__numberReturned);
                message.QueryFailure.Should().Be(false);
                message.QueryFailureDocument.Should().BeNull();
                message.Serializer.Should().BeSameAs(__serializer);
                message.StartingFrom.Should().Be(__startingFrom);
                message.RequestId.Should().Be(__requestId);
                message.ResponseTo.Should().Be(__responseTo);
            }
        }

        [Fact]
        public void ReadMessage_should_read_a_query_failure_message()
        {
            using (var textReader = new StringReader(__queryFailureMessageJson))
            {
                var subject = new ReplyMessageJsonEncoder<BsonDocument>(textReader, null, __messageEncoderSettings, __serializer);
                var message = subject.ReadMessage();
                message.AwaitCapable.Should().Be(false);
                message.CursorId.Should().Be(__cursorId);
                message.CursorNotFound.Should().Be(true);
                message.Documents.Should().BeNull();
                message.NumberReturned.Should().Be(0);
                message.QueryFailure.Should().Be(true);
                message.QueryFailureDocument.Should().Be(__queryFailureDocument);
                message.Serializer.Should().BeSameAs(__serializer);
                message.StartingFrom.Should().Be(0);
                message.RequestId.Should().Be(__requestId);
                message.ResponseTo.Should().Be(__responseTo);
            }
        }

        [Fact]
        public void ReadMessage_should_throw_if_textReader_was_not_provided()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new ReplyMessageJsonEncoder<BsonDocument>(null, textWriter, __messageEncoderSettings, __serializer);
                Action action = () => subject.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_textWriter_was_not_provided()
        {
            using (var textReader = new StringReader(""))
            {
                var subject = new ReplyMessageJsonEncoder<BsonDocument>(textReader, null, __messageEncoderSettings, __serializer);
                Action action = () => subject.WriteMessage(__testMessage);
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new ReplyMessageJsonEncoder<BsonDocument>(null, textWriter, __messageEncoderSettings, __serializer);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new ReplyMessageJsonEncoder<BsonDocument>(null, textWriter, __messageEncoderSettings, __serializer);
                subject.WriteMessage(__testMessage);
                var json = textWriter.ToString();
                json.Should().Be(__testMessageJson);
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_query_failure_message()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new ReplyMessageJsonEncoder<BsonDocument>(null, textWriter, __messageEncoderSettings, __serializer);
                subject.WriteMessage(__queryFailureMessage);
                var json = textWriter.ToString();
                json.Should().Be(__queryFailureMessageJson);
            }
        }
    }
}

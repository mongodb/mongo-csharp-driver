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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class KillCursorsMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static readonly long[] __cursorIds = new[] { 2L };
        private static readonly int __requestId = 1;
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly KillCursorsMessage __testMessage;
        private static readonly string __testMessageJson;

        // static constructor
        static KillCursorsMessageJsonEncoderTests()
        {
            __testMessage = new KillCursorsMessage(__requestId, __cursorIds);

            __testMessageJson =
                "{ " +
                    "\"opcode\" : \"killCursors\", " +
                    "\"requestId\" : 1, " +
                    "\"cursorIds\" : [NumberLong(2)]" +
                " }";
        }
        #endregion

        [Fact]
        public void Constructor_should_not_throw_if_textReader_and_textWriter_are_both_provided()
        {
            using (var textReader = new StringReader(""))
            using (var textWriter = new StringWriter())
            {
                Action action = () => new KillCursorsMessageJsonEncoder(textReader, textWriter, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textReader_is_provided()
        {
            using (var textReader = new StringReader(""))
            {
                Action action = () => new KillCursorsMessageJsonEncoder(textReader, null, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textWriter_is_provided()
        {
            using (var textWriter = new StringWriter())
            {
                Action action = () => new KillCursorsMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_textReader_and_textWriter_are_both_null()
        {
            Action action = () => new KillCursorsMessageJsonEncoder(null, null, __messageEncoderSettings);
            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var textReader = new StringReader(__testMessageJson))
            {
                var subject = new KillCursorsMessageJsonEncoder(textReader, null, __messageEncoderSettings);
                var message = subject.ReadMessage();
                message.CursorIds.Should().Equal(__cursorIds);
                message.RequestId.Should().Be(__testMessage.RequestId);
            }
        }

        [Fact]
        public void ReadMessage_should_throw_if_textReader_was_not_provided()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new KillCursorsMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                Action action = () => subject.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_textWriter_was_not_provided()
        {
            using (var textReader = new StringReader(""))
            {
                var subject = new KillCursorsMessageJsonEncoder(textReader, null, __messageEncoderSettings);
                Action action = () => subject.WriteMessage(__testMessage);
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new KillCursorsMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new KillCursorsMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                subject.WriteMessage(__testMessage);
                var json = textWriter.ToString();
                json.Should().Be(__testMessageJson);
            }
        }
    }
}

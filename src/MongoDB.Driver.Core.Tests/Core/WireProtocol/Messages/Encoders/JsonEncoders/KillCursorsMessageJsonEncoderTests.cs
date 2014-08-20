/* Copyright 2013-2014 MongoDB Inc.
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
using NUnit.Framework;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    [TestFixture]
    public class KillCursorsMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static readonly long[] __cursorIds = new[] { 2L };
        private static readonly int __requestId = 1;
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

        [Test]
        public void Constructor_should_not_throw_if_jsonReader_and_jsonWriter_are_both_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var stringWriter = new StringWriter())
            using (var jsonReader = new JsonReader(stringReader))
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new KillCursorsMessageJsonEncoder(jsonReader, jsonWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_jsonReader_is_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var jsonReader = new JsonReader(stringReader))
            {
                Action action = () => new KillCursorsMessageJsonEncoder(jsonReader, null);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_jsonWriter_is_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new KillCursorsMessageJsonEncoder(null, jsonWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_throw_if_jsonReader_and_jsonWriter_are_both_null()
        {
            Action action = () => new KillCursorsMessageJsonEncoder(null, null);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ReadMessage_should_read_a_message()
        {
            using (var stringReader = new StringReader(__testMessageJson))
            using (var jsonReader = new JsonReader(stringReader))
            {
                var subject = new KillCursorsMessageJsonEncoder(jsonReader, null);
                var message = subject.ReadMessage();
                message.CursorIds.Should().Equal(__cursorIds);
                message.RequestId.Should().Be(__testMessage.RequestId);
            }
        }

        [Test]
        public void ReadMessage_should_throw_if_jsonReader_was_not_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var subject = new KillCursorsMessageJsonEncoder(null, jsonWriter);
                Action action = () => subject.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Test]
        public void WriteMessage_should_throw_if_jsonWriter_was_not_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var jsonReader = new JsonReader(stringReader))
            {
                var subject = new KillCursorsMessageJsonEncoder(jsonReader, null);
                Action action = () => subject.WriteMessage(__testMessage);
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Test]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var subject = new KillCursorsMessageJsonEncoder(null, jsonWriter);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Test]
        public void WriteMessage_should_write_a_message()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var subject = new KillCursorsMessageJsonEncoder(null, jsonWriter);
                subject.WriteMessage(__testMessage);
                var json = stringWriter.ToString();
                json.Should().Be(__testMessageJson);
            }
        }
    }
}

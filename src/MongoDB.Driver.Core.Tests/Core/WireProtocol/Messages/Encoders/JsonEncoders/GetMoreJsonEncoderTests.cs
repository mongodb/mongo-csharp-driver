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
    public class GetMoreMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static readonly int __batchSize = 3;
        private static readonly string __collectionName = "c";
        private static readonly long __cursorId = 2;
        private static readonly string __databaseName = "d";
        private static readonly int __requestId = 1;
        private static readonly GetMoreMessage __testMessage;
        private static readonly string __testMessageJson;

        // static constructor
        static GetMoreMessageJsonEncoderTests()
        {
            __testMessage = new GetMoreMessage(__requestId, __databaseName, __collectionName, __cursorId, __batchSize);

            __testMessageJson = 
                "{ " +
                    "\"opcode\" : \"getMore\", " +
                    "\"requestId\" : 1, " +
                    "\"database\" : \"d\", " +
                    "\"collection\" : \"c\", " +
                    "\"cursorId\" : NumberLong(2), " +
                    "\"batchSize\" : 3" +
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
                Action action = () => new GetMoreMessageJsonEncoder(jsonReader, jsonWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_jsonReader_is_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var jsonReader = new JsonReader(stringReader))
            {
                Action action = () => new GetMoreMessageJsonEncoder(jsonReader, null);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_jsonWriter_is_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new GetMoreMessageJsonEncoder(null, jsonWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_throw_if_jsonReader_and_jsonWriter_are_both_null()
        {
            Action action = () => new GetMoreMessageJsonEncoder(null, null);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ReadMessage_should_read_a_message()
        {
            using (var stringReader = new StringReader(__testMessageJson))
            using (var jsonReader = new JsonReader(stringReader))
            {
                var subject = new GetMoreMessageJsonEncoder(jsonReader, null);
                var message = subject.ReadMessage();
                message.BatchSize.Should().Be(__batchSize);
                message.CollectionName.Should().Be(__collectionName);
                message.CursorId.Should().Be(__cursorId);
                message.DatabaseName.Should().Be(__databaseName);
                message.RequestId.Should().Be(__requestId);
            }
        }

        [Test]
        public void ReadMessage_should_throw_if_jsonReader_was_not_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var subject = new GetMoreMessageJsonEncoder(null, jsonWriter);
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
                var subject = new GetMoreMessageJsonEncoder(jsonReader, null);
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
                var subject = new GetMoreMessageJsonEncoder(null, jsonWriter);
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
                var subject = new GetMoreMessageJsonEncoder(null, jsonWriter);
                subject.WriteMessage(__testMessage);
                var json = stringWriter.ToString();
                json.Should().Be(__testMessageJson);
            }
        }
    }
}

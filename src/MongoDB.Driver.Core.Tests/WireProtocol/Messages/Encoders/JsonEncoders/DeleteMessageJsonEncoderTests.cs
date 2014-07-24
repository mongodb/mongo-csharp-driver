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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.WireProtocol.Messages.Encoders.JsonEncoders
{
    [TestFixture]
    public class DeleteMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static DeleteMessage __testMessage;
        private static readonly string __testMessageJson;

        // static constructor
        static DeleteMessageJsonEncoderTests()
        {
            var query = new BsonDocument("x", 1);
            __testMessage = new DeleteMessage(1, "d", "c", query, false);

            __testMessageJson = "{ \"Opcode\" : \"Delete\", \"RequestId\" : 1, \"DatabaseName\" : \"d\", \"CollectionName\" : \"c\", \"Query\" : { \"x\" : 1 }, \"IsMulti\" : false }";
        }
        #endregion

        [Test]
        public void Constructor_should_not_throw_if_both_arguments_are_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var stringWriter = new StringWriter())
            using (var jsonReader = new JsonReader(stringReader))
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new DeleteMessageJsonEncoder(jsonReader, jsonWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_jsonReader_is_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var jsonReader = new JsonReader(stringReader))
            {
                Action action = () => new DeleteMessageJsonEncoder(jsonReader, null);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_jsonWriter_is_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new DeleteMessageJsonEncoder(null, jsonWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_throw_if_both_arguments_are_null()
        {
            Action action = () => new DeleteMessageJsonEncoder(null, null);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ReadMessage_should_read_a_properly_formatted_json_message()
        {
            using (var stringReader = new StringReader(__testMessageJson))
            using (var jsonReader = new JsonReader(stringReader))
            {
                var encoder = new DeleteMessageJsonEncoder(jsonReader, null);
                var message = encoder.ReadMessage();
                message.RequestId.Should().Be(__testMessage.RequestId);
                message.DatabaseName.Should().Be(__testMessage.DatabaseName);
                message.CollectionName.Should().Be(__testMessage.CollectionName);
                message.IsMulti.Should().Be(__testMessage.IsMulti);
                message.Query.Should().Be(__testMessage.Query);
            }
        }

        [Test]
        public void ReadMessage_should_throw_if_jsonReader_was_not_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var encoder = new DeleteMessageJsonEncoder(null, jsonWriter);
                Action action = () => encoder.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Test]
        public void WriteMessage_should_throw_if_jsonWriter_was_not_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var jsonReader = new JsonReader(stringReader))
            {
                var encoder = new DeleteMessageJsonEncoder(jsonReader, null);
                var message = new DeleteMessage(1, "database", "collection", new BsonDocument(), false);
                Action action = () => encoder.WriteMessage(message);
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Test]
        public void WriteMessage_should_write_a_properly_formatted_json_message()
        {
            string json;
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var encoder = new DeleteMessageJsonEncoder(null, jsonWriter);
                encoder.WriteMessage(__testMessage);
                json = stringWriter.ToString();
            }

            json.Should().Be(__testMessageJson);
        }

        [Test]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var stringReader = new StringReader(""))
            using (var jsonReader = new JsonReader(stringReader))
            {
                var encoder = new DeleteMessageJsonEncoder(jsonReader, null);
                Action action = () => encoder.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }
    }
}

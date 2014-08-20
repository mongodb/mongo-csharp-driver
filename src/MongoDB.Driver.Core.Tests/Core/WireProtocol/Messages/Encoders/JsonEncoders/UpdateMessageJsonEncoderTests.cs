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

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    [TestFixture]
    public class UpdateMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static readonly string __collectionName = "c";
        private static readonly string __databaseName = "d";
        private static readonly bool __isMulti = true;
        private static readonly bool __isUpsert = true;
        private static readonly BsonDocument __query = new BsonDocument("x", 1);
        private static readonly int __requestId = 1;
        private static readonly UpdateMessage __testMessage;
        private static readonly string __testMessageJson;
        private static readonly BsonDocument __update = new BsonDocument("y", 1);

        // static constructor
        static UpdateMessageJsonEncoderTests()
        {
            __testMessage = new UpdateMessage(__requestId, __databaseName, __collectionName, __query, __update, __isMulti, __isUpsert);

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

        [Test]
        public void Constructor_should_not_throw_if_jsonReader_and_jsonWriter_are_both_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var stringWriter = new StringWriter())
            using (var jsonReader = new JsonReader(stringReader))
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new UpdateMessageJsonEncoder(jsonReader, jsonWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_jsonReader_is_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var jsonReader = new JsonReader(stringReader))
            {
                Action action = () => new UpdateMessageJsonEncoder(jsonReader, null);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_jsonWriter_is_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new UpdateMessageJsonEncoder(null, jsonWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_throw_if_jsonReader_and_jsonWriter_are_both_null()
        {
            Action action = () => new UpdateMessageJsonEncoder(null, null);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ReadMessage_should_read_a_message()
        {
            using (var stringReader = new StringReader(__testMessageJson))
            using (var jsonReader = new JsonReader(stringReader))
            {
                var subject = new UpdateMessageJsonEncoder(jsonReader, null);
                var message = subject.ReadMessage();
                message.DatabaseName.Should().Be(__databaseName);
                message.CollectionName.Should().Be(__collectionName);
                message.IsMulti.Should().Be(__isMulti);
                message.IsUpsert.Should().Be(__isUpsert);
                message.Query.Should().Be(__query);
                message.RequestId.Should().Be(__requestId);
                message.Update.Should().Be(__update);
            }
        }

        [Test]
        public void ReadMessage_should_throw_if_jsonReader_was_not_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var subject = new UpdateMessageJsonEncoder(null, jsonWriter);
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
                var subject = new UpdateMessageJsonEncoder(jsonReader, null);
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
                var subject = new UpdateMessageJsonEncoder(null, jsonWriter);
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
                var subject = new UpdateMessageJsonEncoder(null, jsonWriter);
                subject.WriteMessage(__testMessage);
                var json = stringWriter.ToString();
                json.Should().Be(__testMessageJson);
            }
        }
    }
}

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
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    [TestFixture]
    public class InsertMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static readonly string __collectionName = "c";
        private static readonly bool __continueOnError = true;
        private static readonly string __databaseName = "d";
        private static readonly BsonDocument[] __documents = new[] { new BsonDocument("_id", 1), new BsonDocument("_id", 2) };
        private static readonly BatchableSource<BsonDocument> __documentSource = new BatchableSource<BsonDocument>(__documents);
        private static readonly int __maxBatchCount = 1000;
        private static readonly int __maxMessageSize = 40000000;
        private static readonly int __requestId = 1;
        private static readonly IBsonSerializer<BsonDocument> __serializer = BsonDocumentSerializer.Instance;
        private static readonly InsertMessage<BsonDocument> __testMessage;
        private static readonly string __testMessageJson;

        // static constructor
        static InsertMessageJsonEncoderTests()
        {
            __testMessage = new InsertMessage<BsonDocument>(__requestId, __databaseName, __collectionName, __serializer, __documentSource, __maxBatchCount, __maxMessageSize, __continueOnError);

            __testMessageJson =
                "{ " +
                    "\"opcode\" : \"insert\", " +
                    "\"requestId\" : 1, " +
                    "\"database\" : \"d\", " +
                    "\"collection\" : \"c\", " +
                    "\"maxBatchCount\" : 1000, " +
                    "\"maxMessageSize\" : 40000000, " +
                    "\"continueOnError\" : true, " +
                    "\"documents\" : [{ \"_id\" : 1 }, { \"_id\" : 2 }]" +
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
                Action action = () => new InsertMessageJsonEncoder<BsonDocument>(jsonReader, jsonWriter, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_jsonReader_is_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var jsonReader = new JsonReader(stringReader))
            {
                Action action = () => new InsertMessageJsonEncoder<BsonDocument>(jsonReader, null, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_not_throw_if_only_jsonWriter_is_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new InsertMessageJsonEncoder<BsonDocument>(null, jsonWriter, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_should_throw_if_jsonReader_and_jsonWriter_are_both_null()
        {
            Action action = () => new InsertMessageJsonEncoder<BsonDocument>(null, null, __serializer);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_should_throw_if_serializer_is_null()
        {
            using (var stringReader = new StringReader(""))
            using (var stringWriter = new StringWriter())
            using (var jsonReader = new JsonReader(stringReader))
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new InsertMessageJsonEncoder<BsonDocument>(jsonReader, jsonWriter, null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Test]
        public void ReadMessage_should_read_a_message()
        {
            using (var stringReader = new StringReader(__testMessageJson))
            using (var jsonReader = new JsonReader(stringReader))
            {
                var subject = new InsertMessageJsonEncoder<BsonDocument>(jsonReader, null, __serializer);
                var message = subject.ReadMessage();
                message.CollectionName.Should().Be(__collectionName);
                message.ContinueOnError.Should().Be(__continueOnError);
                message.DatabaseName.Should().Be(__databaseName);
                message.DocumentSource.Batch.Should().Equal(__documentSource.Batch);
                message.MaxBatchCount.Should().Be(__maxBatchCount);
                message.MaxMessageSize.Should().Be(__maxMessageSize);
                message.RequestId.Should().Be(__requestId);
                message.Serializer.Should().BeSameAs(__serializer);
            }
        }

        [Test]
        public void ReadMessage_should_throw_if_jsonReader_was_not_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var subject = new InsertMessageJsonEncoder<BsonDocument>(null, jsonWriter, __serializer);
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
                var subject = new InsertMessageJsonEncoder<BsonDocument>(jsonReader, null, __serializer);
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
                var subject = new InsertMessageJsonEncoder<BsonDocument>(null, jsonWriter, __serializer);
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
                var subject = new InsertMessageJsonEncoder<BsonDocument>(null, jsonWriter, __serializer);
                subject.WriteMessage(__testMessage);
                var json = stringWriter.ToString();
                json.Should().Be(__testMessageJson);
            }
        }
    }
}
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class InsertMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static readonly CollectionNamespace __collectionNamespace = new CollectionNamespace("d", "c");
        private static readonly bool __continueOnError = true;
        private static readonly BsonDocument[] __documents = new[] { new BsonDocument("_id", 1), new BsonDocument("_id", 2) };
        private static readonly BatchableSource<BsonDocument> __documentSource = new BatchableSource<BsonDocument>(__documents);
        private static readonly int __maxBatchCount = 1000;
        private static readonly int __maxMessageSize = 40000000;
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly int __requestId = 1;
        private static readonly IBsonSerializer<BsonDocument> __serializer = BsonDocumentSerializer.Instance;
        private static readonly InsertMessage<BsonDocument> __testMessage;
        private static readonly string __testMessageJson;

        // static constructor
        static InsertMessageJsonEncoderTests()
        {
            __testMessage = new InsertMessage<BsonDocument>(__requestId, __collectionNamespace, __serializer, __documentSource, __maxBatchCount, __maxMessageSize, __continueOnError);

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

        [Fact]
        public void Constructor_should_not_throw_if_textReader_and_textWriter_are_both_provided()
        {
            using (var textReader = new StringReader(""))
            using (var textWriter = new StringWriter())
            {
                Action action = () => new InsertMessageJsonEncoder<BsonDocument>(textReader, textWriter, __messageEncoderSettings, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textReader_is_provided()
        {
            using (var textReader = new StringReader(""))
            {
                Action action = () => new InsertMessageJsonEncoder<BsonDocument>(textReader, null, __messageEncoderSettings, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textWriter_is_provided()
        {
            using (var textWriter = new StringWriter())
            {
                Action action = () => new InsertMessageJsonEncoder<BsonDocument>(null, textWriter, __messageEncoderSettings, __serializer);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_textReader_and_textWriter_are_both_null()
        {
            Action action = () => new InsertMessageJsonEncoder<BsonDocument>(null, null, __messageEncoderSettings, __serializer);
            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Constructor_should_throw_if_serializer_is_null()
        {
            using (var textReader = new StringReader(""))
            using (var textWriter = new StringWriter())
            {
                Action action = () => new InsertMessageJsonEncoder<BsonDocument>(textReader, textWriter, __messageEncoderSettings, null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var textReader = new StringReader(__testMessageJson))
            {
                var subject = new InsertMessageJsonEncoder<BsonDocument>(textReader, null, __messageEncoderSettings, __serializer);
                var message = subject.ReadMessage();
                message.CollectionNamespace.Should().Be(__collectionNamespace);
                message.ContinueOnError.Should().Be(__continueOnError);
                message.DocumentSource.Batch.Should().Equal(__documentSource.Batch);
                message.MaxBatchCount.Should().Be(__maxBatchCount);
                message.MaxMessageSize.Should().Be(__maxMessageSize);
                message.RequestId.Should().Be(__requestId);
                message.Serializer.Should().BeSameAs(__serializer);
            }
        }

        [Fact]
        public void ReadMessage_should_throw_if_textReader_was_not_provided()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new InsertMessageJsonEncoder<BsonDocument>(null, textWriter, __messageEncoderSettings, __serializer);
                Action action = () => subject.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_textWriter_was_not_provided()
        {
            using (var textReader = new StringReader(""))
            {
                var subject = new InsertMessageJsonEncoder<BsonDocument>(textReader, null, __messageEncoderSettings, __serializer);
                Action action = () => subject.WriteMessage(__testMessage);
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new InsertMessageJsonEncoder<BsonDocument>(null, textWriter, __messageEncoderSettings, __serializer);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new InsertMessageJsonEncoder<BsonDocument>(null, textWriter, __messageEncoderSettings, __serializer);
                subject.WriteMessage(__testMessage);
                var json = textWriter.ToString();
                json.Should().Be(__testMessageJson);
            }
        }
    }
}
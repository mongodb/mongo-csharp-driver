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
    public class QueryMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static readonly bool __awaitData = true;
        private static readonly int __batchSize = 3;
        private static readonly CollectionNamespace __collectionNamespace = new CollectionNamespace("d", "c");
        private static readonly BsonDocument __fields = new BsonDocument("f", 1);
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly bool __noCursorTimeout = true;
        private static readonly bool __oplogReplay = true;
        private static readonly bool __partialOk = true;
        private static readonly BsonDocument __query = new BsonDocument("x", 1);
        private static readonly IElementNameValidator __queryValidator = NoOpElementNameValidator.Instance;
        private static readonly int __requestId = 1;
        private static readonly int __skip = 2;
        private static readonly bool __slaveOk = true;
        private static readonly bool __tailableCursor = true;
        private static readonly QueryMessage __testMessage;
        private static readonly string __testMessageJson;

        // static constructor
        static QueryMessageJsonEncoderTests()
        {
            __testMessage = new QueryMessage(__requestId, __collectionNamespace, __query, __fields, __queryValidator, __skip, __batchSize, __slaveOk, __partialOk, __noCursorTimeout, __oplogReplay, __tailableCursor, __awaitData);

            __testMessageJson =
                "{ " +
                    "\"opcode\" : \"query\", " +
                    "\"requestId\" : 1, " +
                    "\"database\" : \"d\", " +
                    "\"collection\" : \"c\", " +
                    "\"fields\" : { \"f\" : 1 }, " +
                    "\"skip\" : 2, " +
                    "\"batchSize\" : 3, " +
                    "\"slaveOk\" : true, " +
                    "\"partialOk\" : true, " +
                    "\"noCursorTimeout\" : true, " +
                    "\"oplogReplay\" : true, " +
                    "\"tailableCursor\" : true, " +
                    "\"awaitData\" : true, " +
                    "\"query\" : { \"x\" : 1 }" +
                " }";
        }
        #endregion

        [Fact]
        public void Constructor_should_not_throw_if_textReader_and_textWriter_are_both_provided()
        {
            using (var textReader = new StringReader(""))
            using (var textWriter = new StringWriter())
            {
                Action action = () => new QueryMessageJsonEncoder(textReader, textWriter, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textReader_is_provided()
        {
            using (var textReader = new StringReader(""))
            {
                Action action = () => new QueryMessageJsonEncoder(textReader, null, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textWriter_is_provided()
        {
            using (var textWriter = new StringWriter())
            {
                Action action = () => new QueryMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_should_throw_if_textReader_and_textWriter_are_both_null()
        {
            Action action = () => new QueryMessageJsonEncoder(null, null, __messageEncoderSettings);
            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var textReader = new StringReader(__testMessageJson))
            {
                var subject = new QueryMessageJsonEncoder(textReader, null, __messageEncoderSettings);
                var message = subject.ReadMessage();
                message.CollectionNamespace.Should().Be(__collectionNamespace);
                message.AwaitData.Should().Be(__awaitData);
                message.BatchSize.Should().Be(__batchSize);
                message.Fields.Should().Be(__fields);
                message.NoCursorTimeout.Should().Be(__noCursorTimeout);
                message.OplogReplay.Should().Be(__oplogReplay);
                message.PartialOk.Should().Be(__partialOk);
                message.Query.Should().Be(__query);
                message.RequestId.Should().Be(__requestId);
                message.Skip.Should().Be(__skip);
                message.SlaveOk.Should().Be(__slaveOk);
                message.TailableCursor.Should().Be(__tailableCursor);
            }
        }

        [Fact]
        public void ReadMessage_should_throw_if_textReader_was_not_provided()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new QueryMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                Action action = () => subject.ReadMessage();
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_textWriter_was_not_provided()
        {
            using (var textReader = new StringReader(""))
            {
                var subject = new QueryMessageJsonEncoder(textReader, null, __messageEncoderSettings);
                Action action = () => subject.WriteMessage(__testMessage);
                action.ShouldThrow<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new QueryMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                Action action = () => subject.WriteMessage(null);
                action.ShouldThrow<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new QueryMessageJsonEncoder(null, textWriter, __messageEncoderSettings);
                subject.WriteMessage(__testMessage);
                var json = textWriter.ToString();
                json.Should().Be(__testMessageJson);
            }
        }
    }
}

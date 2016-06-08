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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class JsonMessageEncoderFactoryTests
    {
        #region static
        // static fields
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        #endregion

        [Fact]
        public void Constructor_with_textReader_parameter_should_not_throw_if_textReader_is_not_null()
        {
            using (var textReader = new StringReader(""))
            {
                Action action = () => new JsonMessageEncoderFactory(textReader, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_with_textReader_parameter_should_throw_if_textReader_is_null()
        {
            TextReader textReader = null;
            Action action = () => new JsonMessageEncoderFactory(textReader, __messageEncoderSettings);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_textWriter_parameter_should_not_throw_if_textWriter_is_not_null()
        {
            using (var textWriter = new StringWriter())
            {
                Action action = () => new JsonMessageEncoderFactory(textWriter, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_with_textWriter_parameter_should_throw_if_textWriter_is_null()
        {
            TextWriter textWriter = null;
            Action action = () => new JsonMessageEncoderFactory(textWriter, __messageEncoderSettings);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_two_parameters_should_not_throw_if_only_textReader_is_provided()
        {
            using (var textReader = new StringReader(""))
            {
                Action action = () => new JsonMessageEncoderFactory(textReader, null, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_with_two_parameters_should_not_throw_if_only_textWriter_is_provided()
        {
            using (var textWriter = new StringWriter())
            {
                Action action = () => new JsonMessageEncoderFactory(null, textWriter, __messageEncoderSettings);
                action.ShouldNotThrow();
            }
        }

        [Fact]
        public void Constructor_with_two_parameters_should_throw_if_both_values_are_null()
        {
            Action action = () => new JsonMessageEncoderFactory(null, null, __messageEncoderSettings);
            action.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void GetDeleteMessageEncoder_should_return_a_DeleteMessageJsonEncoder()
        {
            using (var textWriter = new StringWriter())
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, textWriter, __messageEncoderSettings);
                var encoder = encoderFactory.GetDeleteMessageEncoder();
                encoder.Should().BeOfType<DeleteMessageJsonEncoder>();
            }
        }

        [Fact]
        public void GetGetMoreMessageEncoder_should_return_a_GetMoreMessageJsonEncoder()
        {
            using (var textWriter = new StringWriter())
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, textWriter, __messageEncoderSettings);
                var encoder = encoderFactory.GetGetMoreMessageEncoder();
                encoder.Should().BeOfType<GetMoreMessageJsonEncoder>();
            }
        }

        [Fact]
        public void GetInsertMessageEncoder_should_return_a_InsertMessageJsonEncoder()
        {
            using (var textWriter = new StringWriter())
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, textWriter, __messageEncoderSettings);
                var encoder = encoderFactory.GetInsertMessageEncoder<BsonDocument>(BsonDocumentSerializer.Instance);
                encoder.Should().BeOfType<InsertMessageJsonEncoder<BsonDocument>>();
            }
        }

        [Fact]
        public void GetKillCursorsMessageEncoder_should_return_a_KillCursorsMessageJsonEncoder()
        {
            using (var textWriter = new StringWriter())
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, textWriter, __messageEncoderSettings);
                var encoder = encoderFactory.GetKillCursorsMessageEncoder();
                encoder.Should().BeOfType<KillCursorsMessageJsonEncoder>();
            }
        }

        [Fact]
        public void GetQueryMessageEncoder_should_return_a_QueryMessageJsonEncoder()
        {
            using (var textWriter = new StringWriter())
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, textWriter, __messageEncoderSettings);
                var encoder = encoderFactory.GetQueryMessageEncoder();
                encoder.Should().BeOfType<QueryMessageJsonEncoder>();
            }
        }

        [Fact]
        public void GetReplyMessageEncoder_should_return_a_ReplyMessageJsonEncoder()
        {
            using (var textReader = new StringReader(""))
            {
                var encoderFactory = new JsonMessageEncoderFactory(textReader, null, __messageEncoderSettings);
                var encoder = encoderFactory.GetReplyMessageEncoder<BsonDocument>(BsonDocumentSerializer.Instance);
                encoder.Should().BeOfType<ReplyMessageJsonEncoder<BsonDocument>>();
            }
        }

        [Fact]
        public void GetUpdateMessageEncoder_should_return_a_UpdateMessageJsonEncoder()
        {
            using (var textWriter = new StringWriter())
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, textWriter, __messageEncoderSettings);
                var encoder = encoderFactory.GetUpdateMessageEncoder();
                encoder.Should().BeOfType<UpdateMessageJsonEncoder>();
            }
        }
    }
}

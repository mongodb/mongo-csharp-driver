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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.WireProtocol.Messages.Encoders.JsonEncoders
{
    [TestFixture]
    public class JsonMessageEncoderFactoryTests
    {
        [Test]
        public void Constructor_with_jsonReader_parameter_should_not_throw_if_jsonReader_is_not_null()
        {
            using (var stringReader = new StringReader(""))
            using (var jsonReader = new JsonReader(stringReader))
            {
                Action action = () => new JsonMessageEncoderFactory(jsonReader);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_with_jsonReader_parameter_should_throw_if_jsonReader_is_null()
        {
            JsonReader jsonReader = null;
            Action action = () => new JsonMessageEncoderFactory(jsonReader);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_jsonWriter_parameter_should_not_throw_if_jsonWriter_is_not_null()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new JsonMessageEncoderFactory(jsonWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_with_jsonWriter_parameter_should_throw_if_jsonWriter_is_null()
        {
            JsonWriter jsonWriter = null;
            Action action = () => new JsonMessageEncoderFactory(jsonWriter);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_two_parameters_should_not_throw_if_only_jsonReader_is_provided()
        {
            using (var stringReader = new StringReader(""))
            using (var jsonReader = new JsonReader(stringReader))
            {
                Action action = () => new JsonMessageEncoderFactory(jsonReader, null);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_with_two_parameters_should_not_throw_if_only_jsonWriter_is_provided()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                Action action = () => new JsonMessageEncoderFactory(null, jsonWriter);
                action.ShouldNotThrow();
            }
        }

        [Test]
        public void Constructor_with_two_parameters_should_throw_if_both_values_are_null()
        {
            Action action = () => new JsonMessageEncoderFactory(null, null);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void GetDeleteMessageEncoder_should_return_a_DeleteMessageJsonEncoder()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, jsonWriter);
                var encoder = encoderFactory.GetDeleteMessageEncoder();
                encoder.Should().BeOfType<DeleteMessageJsonEncoder>();
            }
        }

        [Test]
        public void GetGetMoreMessageEncoder_should_return_a_GetMoreMessageJsonEncoder()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, jsonWriter);
                var encoder = encoderFactory.GetGetMoreMessageEncoder();
                encoder.Should().BeOfType<GetMoreMessageJsonEncoder>();
            }
        }

        [Test]
        public void GetInsertMessageEncoder_should_return_a_InsertMessageJsonEncoder()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, jsonWriter);
                var encoder = encoderFactory.GetInsertMessageEncoder<BsonDocument>(BsonDocumentSerializer.Instance);
                encoder.Should().BeOfType<InsertMessageJsonEncoder<BsonDocument>>();
            }
        }

        [Test]
        public void GetKillCursorsMessageEncoder_should_return_a_KillCursorsMessageJsonEncoder()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, jsonWriter);
                var encoder = encoderFactory.GetKillCursorsMessageEncoder();
                encoder.Should().BeOfType<KillCursorsMessageJsonEncoder>();
            }
        }

        [Test]
        public void GetQueryMessageEncoder_should_return_a_QueryMessageJsonEncoder()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, jsonWriter);
                var encoder = encoderFactory.GetQueryMessageEncoder();
                encoder.Should().BeOfType<QueryMessageJsonEncoder>();
            }
        }

        [Test]
        public void GetReplyMessageEncoder_should_return_a_ReplyMessageJsonEncoder()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, jsonWriter);
                var encoder = encoderFactory.GetReplyMessageEncoder<BsonDocument>(BsonDocumentSerializer.Instance);
                encoder.Should().BeOfType<ReplyMessageJsonEncoder<BsonDocument>>();
            }
        }

        [Test]
        public void GetUpdateMessageEncoder_should_return_a_UpdateMessageJsonEncoder()
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var encoderFactory = new JsonMessageEncoderFactory(null, jsonWriter);
                var encoder = encoderFactory.GetUpdateMessageEncoder();
                encoder.Should().BeOfType<UpdateMessageJsonEncoder>();
            }
        }
    }
}

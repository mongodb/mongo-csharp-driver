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
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.WireProtocol.Messages
{
    [TestFixture]
    public class DeleteMessageTests
    {
        [Test]
        public void Constructor_should_initialize_instance()
        {
            var query = new BsonDocument("_id", 1);
            var message = new DeleteMessage(1, "database", "collection", query, true);
            message.CollectionName.Should().Be("collection");
            message.DatabaseName.Should().Be("database");
            message.IsMulti.Should().BeTrue();
            message.Query.Equals(query).Should().BeTrue();
            message.RequestId.Should().Be(1);
        }

        [Test]
        public void Constructor_with_null_collectionName_should_throw()
        {
            var query = new BsonDocument("_id", 1);
            Action action = () => new DeleteMessage(1, "database", null, query, true);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_with_null_databaseName_should_throw()
        {
            var query = new BsonDocument("_id", 1);
            Action action = () => new DeleteMessage(1, null, "collection", query, true);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void GetEncoder_with_binary_encoder_factory_should_return_binary_encoder()
        {
            var query = new BsonDocument("_id", 1);
            var message = new DeleteMessage(1, "database", "collection", query, true);

            using (var stream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(stream))
            {
                var binaryEncoderFactory = new BinaryMessageEncoderFactory(writer);
                var encoder = message.GetEncoder(binaryEncoderFactory);
                encoder.Should().BeOfType<DeleteMessageBinaryEncoder>();
            }
        }

        [Test]
        public void GetEncoder_with_json_encoder_factory_should_return_json_encoder()
        {
            var query = new BsonDocument("_id", 1);
            var message = new DeleteMessage(1, "database", "collection", query, true);

            using (var stringWriter = new StringWriter())
            using (var writer = new JsonWriter(stringWriter))
            {
                var jsonEncoderFactory = new JsonMessageEncoderFactory(writer);
                var encoder = message.GetEncoder(jsonEncoderFactory);
                encoder.Should().BeOfType<DeleteMessageJsonEncoder>();
            }
        }
    }
}

/* Copyright 2015-2016 MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CursorBatchDeserializationHelperTests
    {
        [Fact]
        public void DeserializeBatch_should_return_expected_result_when_batch_has_one_document()
        {
            var document = BsonDocument.Parse("{ batch : [ { a : 1 } ] }");
            var bson = document.ToBson();
            var rawDocument = new RawBsonDocument(bson);
            var batch = (RawBsonArray)rawDocument["batch"];
            var documentSerializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = new MessageEncoderSettings();

            var result = CursorBatchDeserializationHelper.DeserializeBatch<BsonDocument>(batch, documentSerializer, messageEncoderSettings);

            result.Count.Should().Be(1);
            result[0].Should().BeOfType<BsonDocument>();
            result[0].Should().Be("{ a :  1 }");
        }

        [Fact]
        public void DeserializeBatch_should_return_expected_result_when_batch_has_two_documents()
        {
            var document = BsonDocument.Parse("{ batch : [ { a : 1 }, { b : 2 } ] }");
            var bson = document.ToBson();
            var rawDocument = new RawBsonDocument(bson);
            var batch = (RawBsonArray)rawDocument["batch"];
            var documentSerializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = new MessageEncoderSettings();

            var result = CursorBatchDeserializationHelper.DeserializeBatch<BsonDocument>(batch, documentSerializer, messageEncoderSettings);

            result.Count.Should().Be(2);
            result[0].Should().BeOfType<BsonDocument>();
            result[1].Should().BeOfType<BsonDocument>();
            result[0].Should().Be("{ a :  1 }");
            result[1].Should().Be("{ b :  2 }");
        }

        [Fact]
        public void DeserializeBatch_should_return_expected_result_when_batch_is_empty()
        {
            var document = BsonDocument.Parse("{ batch : [ ] }");
            var bson = document.ToBson();
            var rawDocument = new RawBsonDocument(bson);
            var batch = (RawBsonArray)rawDocument["batch"];
            var documentSerializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = new MessageEncoderSettings();

            var result = CursorBatchDeserializationHelper.DeserializeBatch<BsonDocument>(batch, documentSerializer, messageEncoderSettings);

            result.Count.Should().Be(0);
        }

        [Fact]
        public void DeserializeBatch_should_return_expected_result_when_GuidRepresentation_is_Standard()
        {
            var document = BsonDocument.Parse("{ batch : [ { a : HexData(4, \"0102030405060708090a0b0c0d0e0f10\") } ] }");
            var writerSettings = new BsonBinaryWriterSettings { GuidRepresentation = GuidRepresentation.Standard };
            var bson = document.ToBson(writerSettings: writerSettings);
            var rawDocument = new RawBsonDocument(bson);
            var batch = (RawBsonArray)rawDocument["batch"];
            var documentSerializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = new MessageEncoderSettings { { "GuidRepresentation", GuidRepresentation.Standard } };

            var result = CursorBatchDeserializationHelper.DeserializeBatch<BsonDocument>(batch, documentSerializer, messageEncoderSettings);

            result.Count.Should().Be(1);
            result[0].Should().BeOfType<BsonDocument>();
            result[0].Should().Be("{ a : HexData(4, \"0102030405060708090a0b0c0d0e0f10\") }");
        }
    }
}

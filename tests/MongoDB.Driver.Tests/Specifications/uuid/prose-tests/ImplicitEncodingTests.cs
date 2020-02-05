/* Copyright 2019-present MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.uuid.prose_tests
{
    public class ImplicitEncodingTests
    {
        [Fact]
        [ResetGuidModeAfterTest]
        public void Implicit_encoding_with_csharp_legacy_representation_should_work_as_expected()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var collection = GetCollection<ClassWithGuidIdUsingCSharpLegacyRepresentation>();
            var guid = Guid.Parse("00112233445566778899aabbccddeeff");
            var document = new ClassWithGuidIdUsingCSharpLegacyRepresentation { Id = guid };

            DropCollection(collection);
            collection.InsertOne(document);

            var insertedDocument = FindSingleDocument(collection);
            insertedDocument.Id.Should().Be(guid);

            var insertedDocumentAsBsonDocument = FindSingleDocumentAsBsonDocument(collection);
            var binaryData = (BsonBinaryData)insertedDocumentAsBsonDocument["_id"];
            binaryData.SubType.Should().Be(BsonBinarySubType.UuidLegacy);
            binaryData.Bytes.Should().Equal(BsonUtils.ParseHexString("33221100554477668899aabbccddeeff"));
        }

        [Fact]
        [ResetGuidModeAfterTest]
        public void Implicit_encoding_with_java_legacy_representation_should_work_as_expected()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var collection = GetCollection<ClassWithGuidIdUsingJavaLegacyRepresentation>();
            var guid = Guid.Parse("00112233445566778899aabbccddeeff");
            var document = new ClassWithGuidIdUsingJavaLegacyRepresentation { Id = guid };

            DropCollection(collection);
            collection.InsertOne(document);

            var insertedDocument = FindSingleDocument(collection);
            insertedDocument.Id.Should().Be(guid);

            var insertedDocumentAsBsonDocument = FindSingleDocumentAsBsonDocument(collection);
            var binaryData = (BsonBinaryData)insertedDocumentAsBsonDocument["_id"];
            binaryData.SubType.Should().Be(BsonBinarySubType.UuidLegacy);
            binaryData.Bytes.Should().Equal(BsonUtils.ParseHexString("7766554433221100ffeeddccbbaa9988"));
        }

        [Fact]
        [ResetGuidModeAfterTest]
        public void Implicit_encoding_with_pyton_legacy_representation_should_work_as_expected()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var collection = GetCollection<ClassWithGuidIdUsingPythonLegacyRepresentation>();
            var guid = Guid.Parse("00112233445566778899aabbccddeeff");
            var document = new ClassWithGuidIdUsingPythonLegacyRepresentation { Id = guid };

            DropCollection(collection);
            collection.InsertOne(document);

            var insertedDocument = FindSingleDocument(collection);
            insertedDocument.Id.Should().Be(guid);

            var insertedDocumentAsBsonDocument = FindSingleDocumentAsBsonDocument(collection);
            var binaryData = (BsonBinaryData)insertedDocumentAsBsonDocument["_id"];
            binaryData.SubType.Should().Be(BsonBinarySubType.UuidLegacy);
            binaryData.Bytes.Should().Equal(BsonUtils.ParseHexString("00112233445566778899aabbccddeeff"));
        }

        [Fact]
        [ResetGuidModeAfterTest]
        public void Implicit_encoding_with_standard_representation_should_work_as_expected()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var collection = GetCollection<ClassWithGuidIdUsingStandardRepresentation>();
            var guid = Guid.Parse("00112233445566778899aabbccddeeff");
            var document = new ClassWithGuidIdUsingStandardRepresentation { Id = guid };

            DropCollection(collection);
            collection.InsertOne(document);

            var insertedDocument = FindSingleDocument(collection);
            insertedDocument.Id.Should().Be(guid);

            var insertedDocumentAsBsonDocument = FindSingleDocumentAsBsonDocument(collection);
            var binaryData = (BsonBinaryData)insertedDocumentAsBsonDocument["_id"];
            binaryData.SubType.Should().Be(BsonBinarySubType.UuidStandard);
            binaryData.Bytes.Should().Equal(BsonUtils.ParseHexString("00112233445566778899aabbccddeeff"));
        }

        [Fact]
        [ResetGuidModeAfterTest]
        public void Implicit_encoding_with_unspecified_representation_should_work_as_expected()
        {
            GuidMode.Set(GuidRepresentationMode.V3);

            var collection = GetCollection<ClassWithGuidIdUsingUnspecifiedRepresentation>();
            var guid = Guid.Parse("00112233445566778899aabbccddeeff");
            var document = new ClassWithGuidIdUsingUnspecifiedRepresentation { Id = guid };

            DropCollection(collection);
            var exception = Record.Exception(() => collection.InsertOne(document));

            exception.Should().BeOfType<BsonSerializationException>();
        }

        // private methods
        private void DropCollection<TDocument>(IMongoCollection<TDocument> collection)
        {
            var database = collection.Database;
            database.DropCollection(collection.CollectionNamespace.CollectionName);
        }

        private TDocument FindSingleDocument<TDocument>(IMongoCollection<TDocument> collection)
        {
            return collection.FindSync("{}").ToList().Single();
        }

        private IMongoCollection<TDocument> GetCollection<TDocument>()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<TDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            return collection;
        }

        private BsonDocument FindSingleDocumentAsBsonDocument<TDocument>(IMongoCollection<TDocument> collection)
        {
            var database = collection.Database;
            var bsonDocumentCollection = database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
            return bsonDocumentCollection.FindSync("{}").ToList().Single();
        }

        // nested types
        private class ClassWithGuidIdUsingCSharpLegacyRepresentation
        {
            [BsonGuidRepresentation(GuidRepresentation.CSharpLegacy)]
            public Guid Id { get; set; }
        }

        private class ClassWithGuidIdUsingJavaLegacyRepresentation
        {
            [BsonGuidRepresentation(GuidRepresentation.JavaLegacy)]
            public Guid Id { get; set; }
        }

        private class ClassWithGuidIdUsingPythonLegacyRepresentation
        {
            [BsonGuidRepresentation(GuidRepresentation.PythonLegacy)]
            public Guid Id { get; set; }
        }

        private class ClassWithGuidIdUsingStandardRepresentation
        {
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid Id { get; set; }
        }

        private class ClassWithGuidIdUsingUnspecifiedRepresentation
        {
            [BsonGuidRepresentation(GuidRepresentation.Unspecified)]
            public Guid Id { get; set; }
        }
    }
}

/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.Tests.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver
{
    public class ChangeStreamDocumentSerializerTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var documentSerializer = new BsonDocumentSerializer();

            var result = new ChangeStreamDocumentSerializer<BsonDocument>(documentSerializer);

            result._documentSerializer().Should().BeSameAs(documentSerializer);
            result._memberSerializationInfo().Count.Should().Be(8);
            AssertRegisteredMember(result, "ClusterTime", "clusterTime", BsonTimestampSerializer.Instance);
            AssertRegisteredMember(result, "CollectionNamespace", "ns", ChangeStreamDocumentCollectionNamespaceSerializer.Instance);
            AssertRegisteredMember(result, "DocumentKey", "documentKey", BsonDocumentSerializer.Instance);
            AssertRegisteredMember(result, "FullDocument", "fullDocument", documentSerializer);
            AssertRegisteredMember(result, "OperationType", "operationType", ChangeStreamOperationTypeSerializer.Instance);
            AssertRegisteredMember(result, "RenameTo", "to", ChangeStreamDocumentCollectionNamespaceSerializer.Instance);
            AssertRegisteredMember(result, "ResumeToken", "_id", BsonDocumentSerializer.Instance);
            AssertRegisteredMember(result, "UpdateDescription", "updateDescription", ChangeStreamUpdateDescriptionSerializer.Instance);
        }

        [Fact]
        public void constructor_should_throw_when_documentSerializer_is_null()
        {
            var exception = Record.Exception(() => new ChangeStreamDocumentSerializer<BsonDocument>(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("documentSerializer");
        }

        [Fact]
        public void Deserialize_should_return_expected_result()
        {
            var json = "{ x : 1 }";
            var subject = CreateSubject();

            ChangeStreamDocument<BsonDocument> result;
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                result = subject.Deserialize(context);
            }

            result.BackingDocument.Should().Be(json);
        }

        [Fact]
        public void Deserialize_should_return_expected_result_when_value_is_null()
        {
            var json = "null";
            var subject = CreateSubject();

            ChangeStreamDocument<BsonDocument> result;
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                result = subject.Deserialize(context);
            }

            result.Should().BeNull();
        }

        [Fact]
        public void Deserialize_should_support_duplicate_element_names_in_full_document()
        {
            var json = "{ fullDocument : { x : 1, x : 2 } }";
            var subject = CreateSubject();

            ChangeStreamDocument<BsonDocument> result;
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                result = subject.Deserialize(context);
            }

            var fullDocument = result.FullDocument;
            fullDocument.ElementCount.Should().Be(2);
            var firstElement = fullDocument.GetElement(0);
            firstElement.Name.Should().Be("x");
            firstElement.Value.Should().Be(1);
            var secondElement = fullDocument.GetElement(1);
            secondElement.Name.Should().Be("x");
            secondElement.Value.Should().Be(2);
        }

        [Fact]
        public void Serialize_should_have_expected_result()
        {
            var subject = CreateSubject();
            var backingDocument = BsonDocument.Parse("{ x : 1 }");
            var value = new ChangeStreamDocument<BsonDocument>(backingDocument, BsonDocumentSerializer.Instance);

            string json;
            using (var textWriter = new StringWriter())
            using (var writer = new JsonWriter(textWriter))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                subject.Serialize(context, value);
                json = textWriter.ToString();
            }

            json.Should().Be("{ \"x\" : 1 }");
        }

        [Fact]
        public void Serialize_should_have_expected_result_when_value_is_null()
        {
            var subject = CreateSubject();

            string json;
            using (var textWriter = new StringWriter())
            using (var writer = new JsonWriter(textWriter))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                subject.Serialize(context, null);
                json = textWriter.ToString();
            }

            json.Should().Be("null");
        }

        // private methods
        private void AssertRegisteredMember(ChangeStreamDocumentSerializer<BsonDocument> changeStreamDocumentSerializer, string memberName, string elementName, IBsonSerializer memberSerializer)
        {
            var serializationInfo = changeStreamDocumentSerializer._memberSerializationInfo()[memberName];
            serializationInfo.ElementName.Should().Be(elementName);
            serializationInfo.Serializer.Should().BeSameAs(memberSerializer);
            serializationInfo.NominalType.Should().Be(memberSerializer.ValueType);
        }

        private ChangeStreamDocumentSerializer<BsonDocument> CreateSubject()
        {
            return new ChangeStreamDocumentSerializer<BsonDocument>(BsonDocumentSerializer.Instance);
        }
    }

    public static class ChangeStreamDocumentSerializerReflector
    {
        public static IBsonSerializer<BsonDocument> _documentSerializer(this ChangeStreamDocumentSerializer<BsonDocument> obj) => (IBsonSerializer<BsonDocument>)Reflector.GetFieldValue(obj, nameof(_documentSerializer));
    }
}

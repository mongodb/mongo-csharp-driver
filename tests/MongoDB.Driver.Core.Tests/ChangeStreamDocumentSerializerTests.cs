/* Copyright 2017 MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MongoDB.Driver
{
    public class ChangeStreamDocumentSerializerTests
    {
        [Theory]
        [InlineData("{ _id : { id : 1 }, operationType : \"delete\", ns : { db : \"db\", coll : \"collection\" }, documentKey : { k : 1 } }", "{ id : 1 }", ChangeStreamOperationType.Delete, "db", "collection", "{ k : 1 }", null, null)]
        [InlineData("{ _id : { id : 1 }, operationType : \"insert\", ns : { db : \"db\", coll : \"collection\" }, documentKey : { k : 1 }, fullDocument : { _id : 2 } }", "{ id : 1 }", ChangeStreamOperationType.Insert, "db", "collection", "{ k : 1 }", null, "{ _id : 2 }")]
        [InlineData("{ _id : { id : 1 }, operationType : \"invalidate\" }", "{ id : 1 }", ChangeStreamOperationType.Invalidate, null, null, null, null, null)]
        [InlineData("{ _id : { id : 1 }, operationType : \"replace\", ns : { db : \"db\", coll : \"collection\" }, documentKey : { k : 1 } }", "{ id : 1 }", ChangeStreamOperationType.Replace, "db", "collection", "{ k : 1 }", null, null)]
        [InlineData("{ _id : { id : 1 }, operationType : \"update\", ns : { db : \"db\", coll : \"collection\" }, documentKey : { k : 1 }, updateDescription : { updatedFields : { f : 1 }, removedFields : [\"r\"] } }", "{ id : 1 }", ChangeStreamOperationType.Update, "db", "collection", "{ k : 1 }", "{ updatedFields : { f : 1 }, removedFields : [\"r\"] }", null)]
        [InlineData("{ _id : { id : 1 }, operationType : \"update\", ns : { db : \"db\", coll : \"collection\" }, documentKey : { k : 1 }, updateDescription : { updatedFields : { f : 1 }, removedFields : [\"r\"] }, fullDocument : null }", "{ id : 1 }", ChangeStreamOperationType.Update, "db", "collection", "{ k : 1 }", "{ updatedFields : { f : 1 }, removedFields : [\"r\"] }", null)]
        [InlineData("{ _id : { id : 1 }, operationType : \"update\", ns : { db : \"db\", coll : \"collection\" }, documentKey : { k : 1 }, updateDescription : { updatedFields : { f : 1 }, removedFields : [\"r\"] }, fullDocument : { _id : 2 } }", "{ id : 1 }", ChangeStreamOperationType.Update, "db", "collection", "{ k : 1 }", "{ updatedFields : { f : 1 }, removedFields : [\"r\"] }", "{ _id : 2 }")]
        public void Deserialize_should_return_expected_result(
            string json,
            string expectedResumeTokenJson,
            ChangeStreamOperationType expectedOperationType,
            string expectedDatabaseName,
            string expectedCollectionName,
            string expectedDocumentKeyJson,
            string expectedUpdateDescriptionJson,
            string expectedFullDocumentJson
            )
        {
            var subject = CreateSubject();
            var expectedCollectionNamespace = CreateCollectionNamespace(expectedDatabaseName, expectedCollectionName);
            var expectedDocumentKey = ParseBsonDocument(expectedDocumentKeyJson);
            var expectedFullDocument = ParseBsonDocument(expectedFullDocumentJson);
            var expectedResumeToken = ParseBsonDocument(expectedResumeTokenJson);
            var expectedUpdateDescription = ParseUpdateDescription(expectedUpdateDescriptionJson);

            ChangeStreamDocument<BsonDocument> result;
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                result = subject.Deserialize(context);
            }

            result.CollectionNamespace.Should().Be(expectedCollectionNamespace);
            result.DocumentKey.Should().Be(expectedDocumentKey);
            result.FullDocument.Should().Be(expectedFullDocument);
            result.OperationType.Should().Be(expectedOperationType);
            result.ResumeToken.Should().Be(expectedResumeToken);
            result.UpdateDescription.ShouldBeEquivalentTo(expectedUpdateDescription);
        }

        [Fact]
        public void Deserialize_should_return_expected_result_when_value_is_null()
        {
            var subject = CreateSubject();
            var json = "null";

            ChangeStreamDocument<BsonDocument> result;
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                result = subject.Deserialize(context);
            }

            result.Should().BeNull();
        }

        [Fact]
        public void Deserialize_should_throw_when_an_invalid_element_name_is_present()
        {
            var subject = CreateSubject();
            var json = "{ x : 1 }";

            Exception exception;
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                exception = Record.Exception(() => subject.Deserialize(context));
            }

            var formatException = exception.Should().BeOfType<FormatException>().Subject;
            formatException.Message.Should().Be("Invalid field name: \"x\".");
        }

        [Theory]
        [InlineData(ChangeStreamOperationType.Delete, "{ _id : 1 }", "db", "collection", "{ k : 1 }", null, null, "{ \"_id\" : { \"_id\" : 1 }, \"operationType\" : \"delete\", \"ns\" : { \"db\" : \"db\", \"coll\" : \"collection\" }, \"documentKey\" : { \"k\" : 1 } }")]
        [InlineData(ChangeStreamOperationType.Insert, "{ _id : 1 }", "db", "collection", "{ k : 1 }", null, "{ _id : 2 }", "{ \"_id\" : { \"_id\" : 1 }, \"operationType\" : \"insert\", \"ns\" : { \"db\" : \"db\", \"coll\" : \"collection\" }, \"documentKey\" : { \"k\" : 1 }, \"fullDocument\" : { \"_id\" : 2 } }")]
        [InlineData(ChangeStreamOperationType.Invalidate, "{ _id : 1 }", null, null, null, null, null, "{ \"_id\" : { \"_id\" : 1 }, \"operationType\" : \"invalidate\" }")]
        [InlineData(ChangeStreamOperationType.Replace, "{ _id : 1 }", "db", "collection", "{ k : 1 }", null, "{ _id : 2 }", "{ \"_id\" : { \"_id\" : 1 }, \"operationType\" : \"replace\", \"ns\" : { \"db\" : \"db\", \"coll\" : \"collection\" }, \"documentKey\" : { \"k\" : 1 }, \"fullDocument\" : { \"_id\" : 2 } }")]
        [InlineData(ChangeStreamOperationType.Update, "{ _id : 1 }", "db", "collection", "{ k : 1 }", "{ updatedFields : { f : 1 }, removedFields : [\"f\"] }", null, "{ \"_id\" : { \"_id\" : 1 }, \"operationType\" : \"update\", \"ns\" : { \"db\" : \"db\", \"coll\" : \"collection\" }, \"documentKey\" : { \"k\" : 1 }, \"updateDescription\" : { \"updatedFields\" : { \"f\" : 1 }, \"removedFields\" : [\"f\"] } }")]
        [InlineData(ChangeStreamOperationType.Update, "{ _id : 1 }", "db", "collection", "{ k : 1 }", "{ updatedFields : { f : 1 }, removedFields : [\"f\"] }", "{ _id : 2 }", "{ \"_id\" : { \"_id\" : 1 }, \"operationType\" : \"update\", \"ns\" : { \"db\" : \"db\", \"coll\" : \"collection\" }, \"documentKey\" : { \"k\" : 1 }, \"updateDescription\" : { \"updatedFields\" : { \"f\" : 1 }, \"removedFields\" : [\"f\"] }, \"fullDocument\" : { \"_id\" : 2 } }")]
        public void Serialize_should_have_expected_result(
            ChangeStreamOperationType operationType,
            string resumeTokenJson,
            string databaseName,
            string collectionName,
            string documentKeyJson,
            string updateDescriptionJson,
            string fullDocumentJson,
            string expectedJson)
        {
            var subject = CreateSubject();
            var value = new ChangeStreamDocument<BsonDocument>(
                ParseBsonDocument(resumeTokenJson),
                operationType,
                CreateCollectionNamespace(databaseName, collectionName),
                ParseBsonDocument(documentKeyJson),
                ParseUpdateDescription(updateDescriptionJson),
                ParseBsonDocument(fullDocumentJson));

            string json;
            using (var textWriter = new StringWriter())
            using (var writer = new JsonWriter(textWriter))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                subject.Serialize(context, value);
                json = textWriter.ToString();
            }

            json.Should().Be(expectedJson);
        }

        [Fact]
        public void Serialize_should_have_expected_result_when_value_is_null()
        {
            var subject = CreateSubject();
            ChangeStreamDocument<BsonDocument> value = null;

            string json;
            using (var textWriter = new StringWriter())
            using (var writer = new JsonWriter(textWriter))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                subject.Serialize(context, value);
                json = textWriter.ToString();
            }

            json.Should().Be("null");
        }

        // private methods
        private CollectionNamespace CreateCollectionNamespace(string databaseName, string collectionName)
        {
            if (databaseName == null && collectionName == null)
            {
                return null;
            }
            else
            {
                var databaseNamespace = new DatabaseNamespace(databaseName);
                return new CollectionNamespace(databaseNamespace, collectionName);
            }
        }

        private ChangeStreamDocumentSerializer<BsonDocument> CreateSubject()
        {
            return new ChangeStreamDocumentSerializer<BsonDocument>(BsonDocumentSerializer.Instance);
        }

        private BsonDocument ParseBsonDocument(string json)
        {
            return json == null ? null : BsonDocument.Parse(json);
        }

        private ChangeStreamUpdateDescription ParseUpdateDescription(string json)
        {
            if (json == null)
            {
                return null;
            }
            else
            {
                var document = BsonDocument.Parse(json);
                var updatedFields = document["updatedFields"].AsBsonDocument;
                var removedFields = document["removedFields"].AsBsonArray.Select(f => f.AsString).ToArray();

                return new ChangeStreamUpdateDescription(updatedFields, removedFields);
            }
        }
    }
}

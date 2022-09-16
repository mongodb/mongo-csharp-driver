/* Copyright 2018-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.Reflectors;
using Moq;
using Xunit;

namespace MongoDB.Driver
{
    public class ChangeStreamDocumentTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var backingDocument = new BsonDocument();
            var documentSerializer = Mock.Of<IBsonSerializer<BsonDocument>>();

            var result = new ChangeStreamDocument<BsonDocument>(backingDocument, documentSerializer);

            result.BackingDocument.Should().BeSameAs(backingDocument);
            var changeStreamDocumentSerializer = result._serializer().Should().BeOfType<ChangeStreamDocumentSerializer<BsonDocument>>().Subject;
            changeStreamDocumentSerializer._documentSerializer().Should().BeSameAs(documentSerializer);
        }

        [Fact]
        public void constructor_should_throw_when_backingDocument_is_null()
        {
            var documentSerializer = Mock.Of<IBsonSerializer<BsonDocument>>();

            var exception = Record.Exception(() => new ChangeStreamDocument<BsonDocument>(null, documentSerializer));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("backingDocument");
        }

        [Fact]
        public void constructor_should_throw_when_documentSerializer_is_null()
        {
            var backingDocument = new BsonDocument();

            var exception = Record.Exception(() => new ChangeStreamDocument<BsonDocument>(backingDocument, null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("documentSerializer");
        }

        [Fact]
        public void BackingDocument_should_return_expected_result()
        {
            var backingDocument = new BsonDocument();
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.BackingDocument;

            result.Should().BeSameAs(backingDocument);
        }

        [Fact]
        public void BackingDocument_should_allow_duplicate_elements_in_full_document()
        {
            var fullDocument = new BsonDocument(allowDuplicateNames: true) { { "x", 1 }, { "x", 2 } };
            var backingDocument = new BsonDocument("fullDocument", fullDocument);
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.BackingDocument;

            result.Should().BeSameAs(backingDocument);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(3, 4)]
        public void ClusterTime_should_return_expected_result(int timestamp, int increment)
        {
            var value = new BsonTimestamp(timestamp, increment);
            var backingDocument = new BsonDocument { { "other", 1 }, { "clusterTime", value } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.ClusterTime;

            result.Should().Be(value);
        }

        [Fact]
        public void ClusterTime_should_return_null_when_not_present()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.ClusterTime;

            result.Should().BeNull();
        }

        [Fact]
        public void CollectionNamespace_should_return_expected_result()
        {
            var value = new CollectionNamespace(new DatabaseNamespace("database"), "collection");
            var ns = new BsonDocument { { "db", value.DatabaseNamespace.DatabaseName }, { "coll", value.CollectionName } };
            var backingDocument = new BsonDocument { { "other", 1 }, { "ns", ns } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.CollectionNamespace;

            result.Should().Be(value);
        }

        [Fact]
        public void CollectionNamespace_should_return_null_when_not_present()
        {
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.CollectionNamespace;

            result.Should().BeNull();
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{ db: \"db\" }")]
        [InlineData("{ db: \"db\", coll : 1 }")]
        [InlineData("{ db: \"db\", coll : { x : 1 } }")]
        public void CollectionNamespace_should_return_null_if_invalid(string nsDocument)
        {
            var ns = BsonDocument.Parse(nsDocument);
            var backingDocument = new BsonDocument { { "other", 1 }, { "ns", ns } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.CollectionNamespace;

            result.Should().BeNull();
        }

        [Fact]
        public void CollectionNamespace_should_allow_extra_elements()
        {
            var ns = new BsonDocument
            {
                { "db", "db" },
                { "coll", "coll" },
                { "newField1", "newFieldValue" },
                { "newField2", new BsonDocument { { "x", 1 } } }
            };
            var backingDocument = new BsonDocument { { "other", 1 }, { "ns", ns } };
            var subject = CreateSubject(backingDocument: backingDocument);

            subject.CollectionNamespace.CollectionName.Should().Be("coll");
            subject.CollectionNamespace.DatabaseNamespace.DatabaseName.Should().Be("db");
            subject.BackingDocument["ns"]["newField1"].AsString.Should().Be("newFieldValue");
            subject.BackingDocument["ns"]["newField2"]["x"].AsInt32.Should().Be(1);
        }

        [Fact]
        public void CollectionUuid_should_return_expected_result()
        {
            var value = Guid.NewGuid();
            var backingDocument = new BsonDocument { { "other", 1 }, { "ui", new BsonBinaryData(value, GuidRepresentation.Standard) } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.CollectionUuid;

            result.Should().Be(value);
        }

        [Fact]
        public void CollectionUuid_should_return_null_when_not_present()
        {
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.CollectionUuid;

            result.Should().NotHaveValue();
        }

        [Fact]
        public void DatabaseNamespace_should_return_expected_result()
        {
            var value = new DatabaseNamespace("database");
            var ns = new BsonDocument { { "db", value.DatabaseName } };
            var backingDocument = new BsonDocument { { "other", 1 }, { "ns", ns } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.DatabaseNamespace;

            result.Should().Be(value);
        }

        [Fact]
        public void DatabaseNamespace_should_be_same_as_in_CollectionNamespace()
        {
            var value = new CollectionNamespace(new DatabaseNamespace("database"), "collection");
            var ns = new BsonDocument { { "db", value.DatabaseNamespace.DatabaseName }, { "coll", value.CollectionName } };
            var backingDocument = new BsonDocument { { "other", 1 }, { "ns", ns } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var collectionNamespace = subject.CollectionNamespace;
            var databseNamespace = subject.DatabaseNamespace;

            collectionNamespace.Should().Be(value);
            databseNamespace.Should().Be(value.DatabaseNamespace);
        }

        [Fact]
        public void DatabaseNamespace_should_return_null_if_not_present()
        {
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.DatabaseNamespace;

            result.Should().BeNull();
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{ other : 1 }")]
        [InlineData("{ db: { x : 1 } }")]
        public void DatabaseNamespace_should_return_null_if_invalid(string databaseName)
        {
            var invalidDatabaseName = BsonDocument.Parse(databaseName);
            var backingDocument = new BsonDocument { { "other", 1 }, { "ns", invalidDatabaseName } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.DatabaseNamespace;

            result.Should().BeNull();
        }

        [Fact]
        public void DisambiguatedPaths_should_return_expected_result()
        {
            var value = new BsonDocument("a.0", new BsonArray {"a", "0"});
            var backingDocument = new BsonDocument { { "other", 1 }, { "disambiguatedPaths", value } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.DisambiguatedPaths;

            result.Should().Be(value);
        }

        [Fact]
        public void DisambiguatedPaths_should_return_null_if_not_present()
        {
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.DisambiguatedPaths;

            result.Should().BeNull();
        }

        [Fact]
        public void DocumentKey_should_return_expected_result()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 }, { "documentKey", value } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.DocumentKey;

            result.Should().Be(value);
        }

        [Fact]
        public void DocumentKey_should_return_null_when_not_present()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.DocumentKey;

            result.Should().BeNull();
        }

        [Fact]
        public void FullDocument_should_return_expected_result()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 }, { "fullDocument", value } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.FullDocument;

            result.Should().Be(value);
        }

        [Fact]
        public void FullDocument_should_allow_duplicate_elements()
        {
            var fullDocument = new BsonDocument(allowDuplicateNames: true) { { "x", 1 }, { "x", 2 } };
            var backingDocument = new BsonDocument { { "other", 1 }, { "fullDocument", fullDocument } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.FullDocument;

            result.ElementCount.Should().Be(2);
            var firstElement = result.GetElement(0);
            firstElement.Name.Should().Be("x");
            firstElement.Value.Should().Be(1);
            var secondElement = result.GetElement(1);
            secondElement.Name.Should().Be("x");
            secondElement.Value.Should().Be(2);
        }

        [Fact]
        public void FullDocument_should_return_null_when_not_present()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.FullDocument;

            result.Should().BeNull();
        }

        [Fact]
        public void OperationDescription_should_return_expected_result()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 }, { "operationDescription", value } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.OperationDescription;

            result.Should().Be(value);
        }

        [Fact]
        public void OperationDescription_should_return_null_when_not_present()
        {
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.OperationDescription;

            result.Should().BeNull();
        }

        [Theory]
        [InlineData("insert", ChangeStreamOperationType.Insert)]
        [InlineData("update", ChangeStreamOperationType.Update)]
        [InlineData("replace", ChangeStreamOperationType.Replace)]
        [InlineData("delete", ChangeStreamOperationType.Delete)]
        [InlineData("rename", ChangeStreamOperationType.Rename)]
        [InlineData("drop", ChangeStreamOperationType.Drop)]
        public void OperationType_should_return_expected_result(string operationTypeName, ChangeStreamOperationType expectedResult)
        {
            var backingDocument = new BsonDocument { { "other", 1 }, { "operationType", operationTypeName } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.OperationType;

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void OperationType_should_return_minus_one_when_not_present()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.OperationType;

            result.Should().Be((ChangeStreamOperationType)(-1));
        }

        [Fact]
        public void RenameTo_should_return_expected_result()
        {
            var value = new CollectionNamespace(new DatabaseNamespace("database"), "collection");
            var to = new BsonDocument { { "db", value.DatabaseNamespace.DatabaseName }, { "coll", value.CollectionName } };
            var backingDocument = new BsonDocument { { "other", 1 }, { "to", to } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.RenameTo;

            result.Should().Be(value);
        }

        [Fact]
        public void RenameTo_should_return_null_when_not_present()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.RenameTo;

            result.Should().BeNull();
        }

        [Fact]
        public void ResumeToken_should_return_expected_result()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 }, { "_id", value } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.ResumeToken;

            result.Should().Be(value);
        }

        [Fact]
        public void ResumeToken_should_return_null_when_not_present()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.ResumeToken;

            result.Should().BeNull();
        }

        [Theory]
        [InlineData("{}", new string[0], null)]
        [InlineData("{ x : 1 }", new[] { "a", "b", "c" }, null)]
        [InlineData("{ x : 1 }", new[] { "a", "b", "c" }, "[ ]")]
        [InlineData("{ x : 1 }", new[] { "a", "b", "c" }, "[ { field : 'arrayForSubdiff', newSize : 2 } ]")]
        public void UpdateDescription_should_return_expected_result(string updatedFieldsJson, string[] removedFields, string truncatedArrays)
        {
            var updatedFields = BsonDocument.Parse(updatedFieldsJson);
            var truncatedArraysBsonArray = truncatedArrays != null ? BsonSerializer.Deserialize<BsonArray>(truncatedArrays) : null;
            var updateDescriptionDocument = new BsonDocument
            {
                { "updatedFields", updatedFields },
                { "removedFields", new BsonArray(removedFields) },
                { "truncatedArrays", truncatedArraysBsonArray, truncatedArraysBsonArray != null }
            };
            var backingDocument = new BsonDocument { { "other", 1 }, { "updateDescription", updateDescriptionDocument } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.UpdateDescription;

            var expectedResult = new ChangeStreamUpdateDescription(updatedFields, removedFields, truncatedArraysBsonArray);
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void UpdateDescription_should_return_null_when_not_present()
        {
            var value = new BsonDocument("x", 1234);
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.UpdateDescription;

            result.Should().BeNull();
        }

        [Fact]
        public void WallTime_should_return_expected_result()
        {
            var value = new DateTime(2022, 5, 25, 12, 35, 42, 144, DateTimeKind.Utc);
            var backingDocument = new BsonDocument { { "other", 1 }, { "wallTime", value } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.WallTime;

            result.Should().Be(value);
        }

        [Fact]
        public void WallTime_should_return_null_when_not_present()
        {
            var backingDocument = new BsonDocument { { "other", 1 } };
            var subject = CreateSubject(backingDocument: backingDocument);

            var result = subject.WallTime;

            result.Should().NotHaveValue();
        }

        // private methods
        private ChangeStreamDocument<BsonDocument> CreateSubject(
            BsonDocument backingDocument = null,
            IBsonSerializer<BsonDocument> documentSerializer = null)
        {
            backingDocument = backingDocument ?? new BsonDocument();
            documentSerializer = documentSerializer ?? BsonDocumentSerializer.Instance;

            return new ChangeStreamDocument<BsonDocument>(backingDocument, documentSerializer);
        }
    }
}

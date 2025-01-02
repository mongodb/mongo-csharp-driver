/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp5442Tests
    {
        [Fact]
        public void Search_Operators_use_correct_serializers_when_using_attributes_and_expression_path()
        {
            var testGuid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var collection = new Mock<IMongoCollection<TestClass>>().Object;
            var searchDefinition = Builders<TestClass>
                .Search
                .Compound()
                .Must(Builders<TestClass>.Search.Equals(t => t.DefaultGuid, testGuid))
                .Must(Builders<TestClass>.Search.Equals(t => t.StringGuid, testGuid));

            var result = collection.Aggregate().Search(searchDefinition).ToString();

            const string expected = """aggregate([{ "$search" : { "compound" : { "must" : [{ "equals" : { "value" : { "$binary" : { "base64" : "AQIDBAUGBwgJCgsMDQ4PEA==", "subType" : "04" } }, "path" : "DefaultGuid" } }, { "equals" : { "value" : "01020304-0506-0708-090a-0b0c0d0e0f10", "path" : "StringGuid" } }] } } }])""";

            result.Should().Be(expected);
        }

        [Fact]
        public void Search_Operators_use_correct_serializers_when_using_attributes_and_string_path()
        {
            var testGuid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var collection = new Mock<IMongoCollection<TestClass>>().Object;
            var searchDefinition = Builders<TestClass>
                .Search
                .Compound()
                .Must(Builders<TestClass>.Search.Equals("DefaultGuid", testGuid))
                .Must(Builders<TestClass>.Search.Equals("StringGuid", testGuid));

            var result = collection.Aggregate().Search(searchDefinition).ToString();

            const string expected = """aggregate([{ "$search" : { "compound" : { "must" : [{ "equals" : { "value" : { "$binary" : { "base64" : "AQIDBAUGBwgJCgsMDQ4PEA==", "subType" : "04" } }, "path" : "DefaultGuid" } }, { "equals" : { "value" : "01020304-0506-0708-090a-0b0c0d0e0f10", "path" : "StringGuid" } }] } } }])""";

            result.Should().Be(expected);
        }

        //[Fact(Skip = "This should only be run manually due to the use of BsonSerializer.RegisterSerializer")]  //TODO Put back skip afterwards
        [Fact]
        public void Search_Operators_use_correct_serializers_when_using_serializer_registry()
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
            var testGuid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var collection = new Mock<IMongoCollection<TestClass>>().Object;
            var searchDefinition = Builders<TestClass>
                .Search
                .Equals(t => t.UndefinedRepresentationGuid, testGuid);

            var result = collection.Aggregate().Search(searchDefinition).ToString();

            const string expected = """aggregate([{ "$search" : { "equals" : { "value" : "01020304-0506-0708-090a-0b0c0d0e0f10", "path" : "UndefinedRepresentationGuid" } } }])""";

            result.Should().Be(expected);
        }

        private class TestClass
        {
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid DefaultGuid { get; set; }

            [BsonRepresentation(BsonType.String)]
            public Guid StringGuid { get; set; }

            public Guid UndefinedRepresentationGuid { get; set; }
        }
    }
}
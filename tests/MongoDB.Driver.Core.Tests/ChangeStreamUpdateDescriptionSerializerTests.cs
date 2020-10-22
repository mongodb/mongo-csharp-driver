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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MongoDB.Driver
{
    public class ChangeStreamUpdateDescriptionSerializerTests
    {
        [Theory]
        [InlineData("{ updatedFields : { f : 1 }, removedFields : [\"f\"] }")]
        [InlineData("{ updatedFields : { f : 1 }, removedFields : [\"f\"], truncatedArrays : [ { field : \"arrayForSubdiff\", newSize : 2 } ] }")]
        public void Deserialize_should_return_expected_result(
            string json)
        {
            var subject = CreateSubject();
            var expectedResult = ParseUpdateDescription(json);

            ChangeStreamUpdateDescription result;
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                result = subject.Deserialize(context);
            }

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Deserialize_should_return_expected_result_when_value_is_null()
        {
            var subject = CreateSubject();
            var json = "null";

            ChangeStreamUpdateDescription result;
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                result = subject.Deserialize(context);
            }

            result.Should().BeNull();
        }

        [Fact]
        public void Deserialize_should_throw_when_an_invalid_field_is_present()
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

        [Fact]
        public void Serialize_should_have_expected_result()
        {
            var subject = CreateSubject();
            var value = new ChangeStreamUpdateDescription(
                new BsonDocument("f", 1),
                new[] { "f" },
                BsonArray.Create(new[] { BsonDocument.Parse("{ field : 'arrayForSubdiff', newSize : 2 }") }));
            var expectedResult = "{ \"updatedFields\" : { \"f\" : 1 }, \"removedFields\" : [\"f\"], \"truncatedArrays\" : [{ \"field\" : \"arrayForSubdiff\", \"newSize\" : 2 }] }";

            string result;
            using (var textWriter = new StringWriter())
            using (var writer = new JsonWriter(textWriter))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                subject.Serialize(context, value);
                result = textWriter.ToString();
            }

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Serialize_should_have_expected_result_when_value_is_null()
        {
            var subject = CreateSubject();
            ChangeStreamUpdateDescription value = null;

            string result;
            using (var textWriter = new StringWriter())
            using (var writer = new JsonWriter(textWriter))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                subject.Serialize(context, value);
                result = textWriter.ToString();
            }

            result.Should().Be("null");
        }

        // private methods
        private ChangeStreamUpdateDescriptionSerializer CreateSubject()
        {
            return new ChangeStreamUpdateDescriptionSerializer();
        }

        private ChangeStreamUpdateDescription ParseUpdateDescription(string json)
        {
            var document = BsonDocument.Parse(json);
            var updatedFields = document["updatedFields"].AsBsonDocument;
            var removedFields = document["removedFields"].AsBsonArray.Select(f => f.AsString).ToArray();
            var truncatedArrays = document.GetValue("truncatedArrays", null)?.AsBsonArray;

            return new ChangeStreamUpdateDescription(updatedFields, removedFields, truncatedArrays);
        }
    }
}

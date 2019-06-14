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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System;
using System.IO;
using Xunit;

namespace MongoDB.Driver
{
    public class ChangeStreamOperationTypeSerializerTests
    {
        [Theory]
        [InlineData("\"delete\"", ChangeStreamOperationType.Delete)]
        [InlineData("\"insert\"", ChangeStreamOperationType.Insert)]
        [InlineData("\"invalidate\"", ChangeStreamOperationType.Invalidate)]
        [InlineData("\"replace\"", ChangeStreamOperationType.Replace)]
        [InlineData("\"update\"", ChangeStreamOperationType.Update)]
        [InlineData("\"rename\"", ChangeStreamOperationType.Rename)]
        public void Deserialize_should_return_expected_result(string json, ChangeStreamOperationType expectedResult)
        {
            var subject = CreateSubject();

            ChangeStreamOperationType result;
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                result = subject.Deserialize(context);
            }

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Deserialize_should_throw_when_input_is_invalid()
        {
            var subject = CreateSubject();
            var json = "\"invalid\"";

            Exception exception;
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                exception = Record.Exception(() => subject.Deserialize(context));
            }

            var formatException = exception.Should().BeOfType<FormatException>().Subject;
            formatException.Message.Should().Be("Invalid ChangeStreamOperationType: \"invalid\".");
        }

        [Theory]
        [InlineData(ChangeStreamOperationType.Delete, "\"delete\"")]
        [InlineData(ChangeStreamOperationType.Insert, "\"insert\"")]
        [InlineData(ChangeStreamOperationType.Invalidate, "\"invalidate\"")]
        [InlineData(ChangeStreamOperationType.Replace, "\"replace\"")]
        [InlineData(ChangeStreamOperationType.Update, "\"update\"")]
        [InlineData(ChangeStreamOperationType.Rename, "\"rename\"")]
        public void Serialize_should_have_expected_result(ChangeStreamOperationType value, string expectedResult)
        {
            var subject = CreateSubject();

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

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        public void Serialize_should_throw_when_value_is_invalid(int valueAsInt)
        {
            var subject = CreateSubject();
            var value = (ChangeStreamOperationType)valueAsInt;

            Exception exception;
            using (var textWriter = new StringWriter())
            using (var writer = new JsonWriter(textWriter))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                exception = Record.Exception(() => subject.Serialize(context, value));
            }

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.Message.Should().StartWith($"Invalid ChangeStreamOperationType: {value}.");
            argumentException.ParamName.Should().Be("value");
        }

        // private methods
        private ChangeStreamOperationTypeSerializer CreateSubject()
        {
            return new ChangeStreamOperationTypeSerializer();
        }
    }
}

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
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver
{
    public class ChangeStreamSplitEventSerializerTests
    {
        [Theory]
        [InlineData("{ fragment : 1, of: 2 }", 1, 2)]
        [InlineData("{ fragment : 3, of: 3 }", 3, 3)]
        public void Deserialize_should_return_expected_result(string json, int expectedFragment, int expectedOf)
        {
            var splitEvent = Deserialize(json);

            splitEvent.Fragment.Should().Be(expectedFragment);
            splitEvent.Of.Should().Be(expectedOf);
        }

        [Fact]
        public void Deserialize_should_return_expected_result_when_value_is_null()
        {
            const string json = "null";

            var splitEvent = Deserialize(json);

            splitEvent.Should().BeNull();
        }

        [Fact]
        public void Deserialize_should_throw_when_an_invalid_field_is_present()
        {
            const string json = "{ x : 1 }";

            var exception = Record.Exception(() => Deserialize(json));
            var formatException = exception.Should().BeOfType<FormatException>().Subject;
            formatException.Message.Should().Be("Invalid field name: \"x\".");
        }

        [Fact]
        public void Serialize_should_have_expected_result()
        {
            var subject = CreateSubject();
            var value = new ChangeStreamSplitEvent(1, 2);
            var expectedResult = "{ \"fragment\" : 1, \"of\" : 2 }";

            using var textWriter = new StringWriter();
            using var writer = new JsonWriter(textWriter);
            var context = BsonSerializationContext.CreateRoot(writer);
            subject.Serialize(context, value);
            var result = textWriter.ToString();

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Serialize_should_have_expected_result_when_value_is_null()
        {
            var subject = CreateSubject();
            var expectedResult = "null";

            using var textWriter = new StringWriter();
            using var writer = new JsonWriter(textWriter);
            var context = BsonSerializationContext.CreateRoot(writer);
            subject.Serialize(context, null);
            var result = textWriter.ToString();

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new ChangeStreamOperationTypeSerializer();
            var y = new DerivedFromChangeStreamOperationTypeSerializer();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ChangeStreamSplitEventSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ChangeStreamSplitEventSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ChangeStreamOperationTypeSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ChangeStreamOperationTypeSerializer();
            var y = new ChangeStreamOperationTypeSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ChangeStreamOperationTypeSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class DerivedFromChangeStreamOperationTypeSerializer : ChangeStreamOperationTypeSerializer
        {
        }

        private ChangeStreamSplitEvent Deserialize(string json)
        {
            var serializer = new ChangeStreamSplitEventSerializer();
            using var reader = new JsonReader(json);
            var context = BsonDeserializationContext.CreateRoot(reader);
            return serializer.Deserialize(context);
        }

        private ChangeStreamSplitEventSerializer CreateSubject() => new ChangeStreamSplitEventSerializer();
    }
}

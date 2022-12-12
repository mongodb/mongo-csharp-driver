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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class DateTimeOffsetSerializerTests
    {
        [Fact]
        public void constructor_with_no_arguments_should_return_expected_result()
        {
            var subject = new DateTimeOffsetSerializer();

            subject.Representation.Should().Be(BsonType.Array);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_representation_should_return_expected_result(
            [Values(BsonType.Array, BsonType.DateTime, BsonType.Document, BsonType.String)] BsonType representation)
        {
            var subject = new DateTimeOffsetSerializer(representation);

            subject.Representation.Should().Be(representation);
        }

        [Fact]
        public void constructor_with_representation_should_throw_when_representation_is_invalid()
        {
            var exception = Record.Exception(() => new DateTimeOffsetSerializer(BsonType.Null));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData("{ x : [{ $numberLong : '0' }, 0] }", "0001-01-01T00:00:00+00:00")]
        [InlineData("{ x : [{ $numberLong : '621355968000000000' }, 0] }", "1970-01-01T00:00:00+00:00")]
        [InlineData("{ x : [{ $numberLong : '621355968000000000' }, 60] }", "1970-01-01T00:00:00+01:00")]
        [InlineData("{ x : [{ $numberLong : '621355968000000000' }, -60] }", "1970-01-01T00:00:00-01:00")]
        [InlineData("{ x : { $date : { $numberLong : '0' } } }", "1970-01-01T00:00:00Z")]
        [InlineData("{ x : { $date : { $numberLong : '60000' } } }", "1970-01-01T00:01:00Z")]
        [InlineData("{ x : { $date : { $numberLong : '1640995200000' } } }", "2022-01-01T00:00:00Z")]
        [InlineData("{ x : { DateTime : 'ignored', Ticks : { $numberLong : 0 }, Offset : 0 } }", "0001-01-01T00:00:00Z")]
        [InlineData("{ x : { DateTime : 'ignored', Ticks : { $numberLong : '621355968000000000' }, Offset : 0 } }", "1970-01-01T00:00:00Z")]
        [InlineData("{ x : { DateTime : 'ignored', Ticks : { $numberLong : '621355968000000000' }, Offset : 60 } }", "1970-01-01T00:00:00+01:00")]
        [InlineData("{ x : { DateTime : 'ignored', Ticks : { $numberLong : '621355968000000000' }, Offset : -60 } }", "1970-01-01T00:00:00-01:00")]
        [InlineData("{ x : '0001-01-01T00:00:00+00:00' }", "0001-01-01T00:00:00+00:00")]
        [InlineData("{ x : '1970-01-01T00:00:00+00:00' }", "1970-01-01T00:00:00+00:00")]
        [InlineData("{ x : '1970-01-01T00:00:00+01:00' }", "1970-01-01T00:00:00+01:00")]
        [InlineData("{ x : '1970-01-01T00:00:00-01:00' }", "1970-01-01T00:00:00-01:00")]
        public void Deserialize_should_return_expected_result(string json, string expectedResult)
        {
            var x = DateTimeOffset.Parse(expectedResult);
            var m = BsonUtils.ToMillisecondsSinceEpoch(x.UtcDateTime);
            var subject = new DateTimeOffsetSerializer();

            DateTimeOffset result;
            using (var reader = new JsonReader(json))
            {
                reader.ReadStartDocument();
                reader.ReadName("x");
                var context = BsonDeserializationContext.CreateRoot(reader);
                result = subject.Deserialize(context);
                reader.ReadEndDocument();
            }

            result.Should().Be(DateTimeOffset.Parse(expectedResult));
        }

        [Theory]
        [InlineData("{ x : [{ $numberDouble : '0' }, { $numberDouble : '0' }] }", "0001-01-01T00:00:00+00:00")]
        [InlineData("{ x : [{ $numberDouble : '621355968000000000' }, { $numberDouble : '0' }] }", "1970-01-01T00:00:00+00:00")]
        [InlineData("{ x : [{ $numberDouble : '621355968000000000' }, { $numberDouble : '60' }] }", "1970-01-01T00:00:00+01:00")]
        [InlineData("{ x : [{ $numberDouble : '621355968000000000' }, { $numberDouble : '-60' }] }", "1970-01-01T00:00:00-01:00")]
        [InlineData("{ x : { DateTime : 'ignored', Ticks : { $numberDouble : 0 }, Offset : { $numberDouble : '0' } } }", "0001-01-01T00:00:00Z")]
        [InlineData("{ x : { DateTime : 'ignored', Ticks : { $numberDouble : '621355968000000000' }, Offset : { $numberDouble : '0' } } }", "1970-01-01T00:00:00Z")]
        [InlineData("{ x : { DateTime : 'ignored', Ticks : { $numberDouble : '621355968000000000' }, Offset : { $numberDouble : '60' } } }", "1970-01-01T00:00:00+01:00")]
        [InlineData("{ x : { DateTime : 'ignored', Ticks : { $numberDouble : '621355968000000000' }, Offset : { $numberDouble : '-60' } } }", "1970-01-01T00:00:00-01:00")]
        public void Deserialize_should_be_forgiving_of_actual_numeric_types(string json, string expectedResult)
        {
            var x = DateTimeOffset.Parse(expectedResult);
            var m = BsonUtils.ToMillisecondsSinceEpoch(x.UtcDateTime);
            var subject = new DateTimeOffsetSerializer();

            DateTimeOffset result;
            using (var reader = new JsonReader(json))
            {
                reader.ReadStartDocument();
                reader.ReadName("x");
                var context = BsonDeserializationContext.CreateRoot(reader);
                result = subject.Deserialize(context);
                reader.ReadEndDocument();
            }

            result.Should().Be(DateTimeOffset.Parse(expectedResult));
        }

        [Theory]
        [InlineData(BsonType.Array, "0001-01-01T00:00:00Z", "{ \"x\" : [{ \"$numberLong\" : \"0\" }, { \"$numberInt\" : \"0\" }] }")]
        [InlineData(BsonType.Array, "1970-01-01T00:00:00Z", "{ \"x\" : [{ \"$numberLong\" : \"621355968000000000\" }, { \"$numberInt\" : \"0\" }] }")]
        [InlineData(BsonType.Array, "1970-01-01T00:00:00+01:00", "{ \"x\" : [{ \"$numberLong\" : \"621355968000000000\" }, { \"$numberInt\" : \"60\" }] }")]
        [InlineData(BsonType.Array, "1970-01-01T00:00:00-01:00", "{ \"x\" : [{ \"$numberLong\" : \"621355968000000000\" }, { \"$numberInt\" : \"-60\" }] }")]
        [InlineData(BsonType.DateTime, "0001-01-01T00:00:00Z", "{ \"x\" : { \"$date\" : { \"$numberLong\" : \"-62135596800000\" } } }")]
        [InlineData(BsonType.DateTime, "1970-01-01T00:00:00Z", "{ \"x\" : { \"$date\" : { \"$numberLong\" : \"0\" } } }")]
        [InlineData(BsonType.DateTime, "1970-01-01T00:00:00+01:00", "{ \"x\" : { \"$date\" : { \"$numberLong\" : \"-3600000\" } } }")]
        [InlineData(BsonType.DateTime, "1970-01-01T00:00:00-01:00", "{ \"x\" : { \"$date\" : { \"$numberLong\" : \"3600000\" } } }")]
        [InlineData(BsonType.Document, "0001-01-01T00:00:00Z", "{ \"x\" : { \"DateTime\" : { \"$date\" : { \"$numberLong\" : \"-62135596800000\" } }, \"Ticks\" : { \"$numberLong\" : \"0\" }, \"Offset\" : { \"$numberInt\" : \"0\" } } }")]
        [InlineData(BsonType.Document, "1970-01-01T00:00:00Z", "{ \"x\" : { \"DateTime\" : { \"$date\" : { \"$numberLong\" : \"0\" } }, \"Ticks\" : { \"$numberLong\" : \"621355968000000000\" }, \"Offset\" : { \"$numberInt\" : \"0\" } } }")]
        [InlineData(BsonType.Document, "1970-01-01T00:00:00+01:00", "{ \"x\" : { \"DateTime\" : { \"$date\" : { \"$numberLong\" : \"-3600000\" } }, \"Ticks\" : { \"$numberLong\" : \"621355968000000000\" }, \"Offset\" : { \"$numberInt\" : \"60\" } } }")]
        [InlineData(BsonType.Document, "1970-01-01T00:00:00-01:00", "{ \"x\" : { \"DateTime\" : { \"$date\" : { \"$numberLong\" : \"3600000\" } }, \"Ticks\" : { \"$numberLong\" : \"621355968000000000\" }, \"Offset\" : { \"$numberInt\" : \"-60\" } } }")]
        [InlineData(BsonType.String, "0001-01-01T00:00:00Z", "{ \"x\" : \"0001-01-01T00:00:00+00:00\" }")]
        [InlineData(BsonType.String, "1970-01-01T00:00:00Z", "{ \"x\" : \"1970-01-01T00:00:00+00:00\" }")]
        [InlineData(BsonType.String, "1970-01-01T00:00:00+01:00", "{ \"x\" : \"1970-01-01T00:00:00+01:00\" }")]
        [InlineData(BsonType.String, "1970-01-01T00:00:00-01:00", "{ \"x\" : \"1970-01-01T00:00:00-01:00\" }")]
        public void Serialize_should_have_expected_result(BsonType representation, string valueString, string expectedResult)
        {
            var subject = new DateTimeOffsetSerializer(representation);
            var value = DateTimeOffset.Parse(valueString);

            string result;
            using (var textWriter = new StringWriter())
            using (var writer = new JsonWriter(textWriter, new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson }))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                writer.WriteStartDocument();
                writer.WriteName("x");
                subject.Serialize(context, value);
                writer.WriteEndDocument();
                result = textWriter.ToString();
            }

            result.Should().Be(expectedResult);
        }
        [Theory]
        [ParameterAttributeData]
        public void WithRepresentation_should_return_expected_result(
            [Values(BsonType.Array, BsonType.DateTime, BsonType.Document, BsonType.String)] BsonType oldRepresentation,
            [Values(BsonType.Array, BsonType.DateTime, BsonType.Document, BsonType.String)] BsonType newRepresentation)
        {
            var subject = new DateTimeOffsetSerializer(oldRepresentation);

            var result = subject.WithRepresentation(newRepresentation);

            result.Representation.Should().Be(newRepresentation);
            if (newRepresentation == oldRepresentation)
            {
                result.Should().BeSameAs(subject);
            }
        }
    }
}

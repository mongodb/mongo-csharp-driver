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
using System.Globalization;
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
#if NET6_0_OR_GREATER
    public class DateOnlySerializerTests
    {
        [Fact]
        public void Attribute_should_set_correct_format()
        {
            var dateOnly = new DateOnly(2024, 10, 05);

            var testObj = new TestClass
            {
                DateTimeTicksFormat = dateOnly,
                YearMonthDayFormat = dateOnly,
                IgnoredFormat = dateOnly,
                NullableYearMonthDayFormat = dateOnly,
                ArrayYearMonthDayFormat = [dateOnly, dateOnly]
            };

            var json = testObj.ToJson();
            const string expected = """
                                    { "DateTimeTicksFormat" : { "DateTime" : { "$date" : "2024-10-05T00:00:00Z" }, "Ticks" : 638636832000000000 }, "YearMonthDayFormat" : { "Year" : 2024, "Month" : 10, "Day" : 5 }, "IgnoredFormat" : 638636832000000000, "NullableYearMonthDayFormat" : { "Year" : 2024, "Month" : 10, "Day" : 5 }, "ArrayYearMonthDayFormat" : [{ "Year" : 2024, "Month" : 10, "Day" : 5 }, { "Year" : 2024, "Month" : 10, "Day" : 5 }] }
                                    """;
            Assert.Equal(expected, json);
        }

        [Fact]
        public void Constructor_with_no_arguments_should_return_expected_result()
        {
            var subject = new DateOnlySerializer();

            subject.Representation.Should().Be(BsonType.DateTime);
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_with_representation_should_return_expected_result(
            [Values(BsonType.DateTime, BsonType.String, BsonType.Int64, BsonType.Document)]
            BsonType representation)
        {
            var subject = new DateOnlySerializer(representation);

            subject.Representation.Should().Be(representation);
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_with_representation_and_format_should_return_expected_result(
            [Values(BsonType.DateTime, BsonType.String, BsonType.Int64, BsonType.Document)]
            BsonType representation,
            [Values(DateOnlyDocumentFormat.DateTimeTicks, DateOnlyDocumentFormat.YearMonthDay)]
            DateOnlyDocumentFormat format)
        {
            var subject = new DateOnlySerializer(representation, format);

            subject.Representation.Should().Be(representation);
            subject.DocumentFormat.Should().Be(format);
        }

        [Theory]
        [InlineData("""{ "x" : { "$numberDouble" : "638649792000000000" } }""","2024-10-20" )]
        [InlineData("""{ "x" : { "$numberDecimal" : "638649792000000000" } }""","2024-10-20" )]
        [InlineData("""{ "x" : { "$numberInt" : "0" } }""","0001-01-01" )]
        [InlineData("""{ "x" : { "DateTime" : "ignored", "Ticks" : { "$numberLong" : "638649792000000000" } } }""","2024-10-20" )]
        [InlineData("""{ "x" : { "DateTime" : "ignored", "Ticks" : { "$numberDecimal" : "638649792000000000" } } }""","2024-10-20" )]
        [InlineData("""{ "x" : { "DateTime" : "ignored", "Ticks" : { "$numberInt" : "0" } } }""","0001-01-01" )]
        public void Deserialize_should_be_forgiving_of_actual_numeric_types(string json, string expectedResult)
        {
            var subject = new DateOnlySerializer();

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);
            reader.ReadEndDocument();

            result.Should().Be(DateOnly.Parse(expectedResult, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("""{ "x" : { "$date" : { "$numberLong" : "1729382400000" } } }""","2024-10-20" )]
        [InlineData("""{ "x" : { "$date" : { "$numberLong" : "-62135596800000" } } }""","0001-01-01" )]
        [InlineData("""{ "x" : { "$date" : { "$numberLong" : "253402214400000" } } }""","9999-12-31" )]
        [InlineData("""{ "x" : "2024-10-20" }""","2024-10-20" )]
        [InlineData("""{ "x" : "0001-01-01" }""","0001-01-01")]
        [InlineData("""{ "x" : "9999-12-31" }""","9999-12-31" )]
        [InlineData("""{ "x" : { "$numberLong" : "638649792000000000" } }""","2024-10-20" )]
        [InlineData("""{ "x" : { "$numberLong" : "0" } }""","0001-01-01" )]
        [InlineData("""{ "x" : { "$numberLong" : "3155378112000000000" } }""","9999-12-31" )]
        [InlineData("""{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "1729382400000" } }, "Ticks" : { "$numberLong" : "638649792000000000" } } }""","2024-10-20" )]
        [InlineData("""{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "-62135596800000" } }, "Ticks" : { "$numberLong" : "0" } } }""","0001-01-01" )]
        [InlineData("""{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "253402214400000" } }, "Ticks" : { "$numberLong" : "3155378112000000000" } } }""","9999-12-31" )]
        [InlineData("""{ "x" : { "Year" : 2024, "Month" : 10, "Day" : 5 } }""","2024-10-05" )]
        [InlineData("""{ "x" : { "Year" : 9999, "Month" : 12, "Day" : 31 } }""","9999-12-31" )]
        [InlineData("""{ "x" : { "Year" : 1, "Month" : 1, "Day" : 1 } }""","0001-01-01" )]
        public void Deserialize_should_have_expected_result(string json, string expectedResult)
        {
            var subject = new DateOnlySerializer();

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);
            reader.ReadEndDocument();

            result.Should().Be(DateOnly.Parse(expectedResult, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("""{ "x" : { "Year" : 2024, "Month" : 10, "Day" : 5 } }""","2024-10-05" )]
        [InlineData("""{ "x" : { "Year" : 9999, "Month" : 12, "Day" : 31 } }""","9999-12-31" )]
        [InlineData("""{ "x" : { "Year" : 1, "Month" : 1, "Day" : 1 } }""","0001-01-01" )]
        [InlineData("""{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "1729382400000" } }, "Ticks" : { "$numberLong" : "638649792000000000" } } }""","2024-10-20" )]
        [InlineData("""{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "-62135596800000" } }, "Ticks" : { "$numberLong" : "0" } } }""","0001-01-01" )]
        [InlineData("""{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "253402214400000" } }, "Ticks" : { "$numberLong" : "3155378112000000000" } } }""","9999-12-31" )]
        public void Deserialize_with_human_readable_should_have_expected_result(string json, string expectedResult)
        {
            var subject = new DateOnlySerializer(BsonType.Document, DateOnlyDocumentFormat.YearMonthDay);

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);
            reader.ReadEndDocument();

            result.Should().Be(DateOnly.Parse(expectedResult, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("""{ "x" : { "$date" : { "$numberLong" : "1729382410000" } } }""")]
        [InlineData("""{ "x" : { "$numberLong" : "638649792100000000" } }""")]
        [InlineData("""{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "1729382400000" } }, "Ticks" : { "$numberLong" : "638649792100000000" } } }""")]
        public void Deserialize_should_throw_when_date_has_time(string json)
        {
            var subject = new DateOnlySerializer();

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);

            var exception = Record.Exception(() => subject.Deserialize(context));
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Be("Deserialized value has a non-zero time component.");
        }

        [Theory]
        [InlineData("""{ "x" : { "Year": 2024, "DateTime" : { "$date" : { "$numberLong" : "1729382400000" } }, "Ticks" : { "$numberLong" : "638649792100000000" } } }""")]
        public void Deserialize_should_throw_when_document_format_is_invalid(string json)
        {
            var subject = new DateOnlySerializer();

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);

            var exception = Record.Exception(() => subject.Deserialize(context));
            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Be("Invalid document format.");
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new DateOnlySerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new DateOnlySerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new DateOnlySerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new DateOnlySerializer();
            var y = new DateOnlySerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_with_different_representation_and_format_should_return_correct(
            [Values(BsonType.DateTime, BsonType.String, BsonType.Int64, BsonType.Document)]
            BsonType representation,
            [Values(DateOnlyDocumentFormat.DateTimeTicks, DateOnlyDocumentFormat.YearMonthDay)]
            DateOnlyDocumentFormat format)
        {
            var x = new DateOnlySerializer();
            var y = new DateOnlySerializer(representation, format);

            var result = x.Equals(y);
            var result2 = y.Equals(x);
            result.Should().Be(result2);

            if (representation == BsonType.DateTime && format == DateOnlyDocumentFormat.DateTimeTicks)
            {
                result.Should().Be(true);
            }
            else
            {
                result.Should().Be(false);
            }
        }

        [Fact]
        public void Instance_should_return_default_serializer()
        {
            var subject = DateOnlySerializer.Instance;

            subject.Should().Be(new DateOnlySerializer());
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new DateOnlySerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Theory]
        [InlineData(BsonType.DateTime, "2024-10-20", """{ "x" : { "$date" : { "$numberLong" : "1729382400000" } } }""")]
        [InlineData(BsonType.DateTime, "0001-01-01", """{ "x" : { "$date" : { "$numberLong" : "-62135596800000" } } }""")]
        [InlineData(BsonType.DateTime, "9999-12-31", """{ "x" : { "$date" : { "$numberLong" : "253402214400000" } } }""")]
        [InlineData(BsonType.String, "2024-10-20", """{ "x" : "2024-10-20" }""")]
        [InlineData(BsonType.String, "0001-01-01", """{ "x" : "0001-01-01" }""")]
        [InlineData(BsonType.String, "9999-12-31", """{ "x" : "9999-12-31" }""")]
        [InlineData(BsonType.Int64, "2024-10-20", """{ "x" : { "$numberLong" : "638649792000000000" } }""")]
        [InlineData(BsonType.Int64, "0001-01-01", """{ "x" : { "$numberLong" : "0" } }""")]
        [InlineData(BsonType.Int64, "9999-12-31", """{ "x" : { "$numberLong" : "3155378112000000000" } }""")]
        [InlineData(BsonType.Document, "2024-10-20", """{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "1729382400000" } }, "Ticks" : { "$numberLong" : "638649792000000000" } } }""")]
        [InlineData(BsonType.Document, "0001-01-01", """{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "-62135596800000" } }, "Ticks" : { "$numberLong" : "0" } } }""")]
        [InlineData(BsonType.Document, "9999-12-31", """{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "253402214400000" } }, "Ticks" : { "$numberLong" : "3155378112000000000" } } }""")]
        public void Serialize_should_have_expected_result(BsonType representation, string valueString,
            string expectedResult)
        {
            var subject = new DateOnlySerializer(representation);
            var value = DateOnly.Parse(valueString, CultureInfo.InvariantCulture);

            using var textWriter = new StringWriter();
            using var writer = new JsonWriter(textWriter,
                new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson });

            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName("x");
            subject.Serialize(context, value);
            writer.WriteEndDocument();
            var result = textWriter.ToString();

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("2024-10-20", """{ "x" : { "Year" : { "$numberInt" : "2024" }, "Month" : { "$numberInt" : "10" }, "Day" : { "$numberInt" : "20" } } }""")]
        [InlineData( "0001-01-01", """{ "x" : { "Year" : { "$numberInt" : "1" }, "Month" : { "$numberInt" : "1" }, "Day" : { "$numberInt" : "1" } } }""")]
        [InlineData("9999-12-31", """{ "x" : { "Year" : { "$numberInt" : "9999" }, "Month" : { "$numberInt" : "12" }, "Day" : { "$numberInt" : "31" } } }""")]
        public void Serialize_human_readable_should_have_expected_result(string valueString, string expectedResult)
        {
            var subject = new DateOnlySerializer(BsonType.Document, DateOnlyDocumentFormat.YearMonthDay);
            var value = DateOnly.Parse(valueString, CultureInfo.InvariantCulture);

            using var textWriter = new StringWriter();
            using var writer = new JsonWriter(textWriter,
                new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson });

            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName("x");
            subject.Serialize(context, value);
            writer.WriteEndDocument();
            var result = textWriter.ToString();

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Serializer_should_be_registered()
        {
            var serializer = BsonSerializer.LookupSerializer(typeof(DateOnly));

            serializer.Should().Be(new DateOnlySerializer());
        }

        [Theory]
        [ParameterAttributeData]
        public void WithRepresentation_should_return_expected_result(
            [Values(BsonType.Document, BsonType.DateTime, BsonType.Document, BsonType.String)] BsonType oldRepresentation,
            [Values(BsonType.Document, BsonType.DateTime, BsonType.Document, BsonType.String)] BsonType newRepresentation)
        {
            var subject = new DateOnlySerializer(oldRepresentation);

            var result = subject.WithRepresentation(newRepresentation);

            result.Representation.Should().Be(newRepresentation);
            if (newRepresentation == oldRepresentation)
            {
                result.Should().BeSameAs(subject);
            }
        }

        private class TestClass
        {
            [BsonDateOnlyOptions(BsonType.Document, DateOnlyDocumentFormat.DateTimeTicks)]
            public DateOnly DateTimeTicksFormat { get; set; }

            [BsonDateOnlyOptions(BsonType.Document, DateOnlyDocumentFormat.YearMonthDay)]
            public DateOnly YearMonthDayFormat { get; set; }

            [BsonDateOnlyOptions(BsonType.Int64, DateOnlyDocumentFormat.YearMonthDay)]
            public DateOnly IgnoredFormat { get; set; }

            [BsonDateOnlyOptions(BsonType.Document, DateOnlyDocumentFormat.YearMonthDay)]
            public DateOnly? NullableYearMonthDayFormat { get; set; }

            [BsonDateOnlyOptions(BsonType.Document, DateOnlyDocumentFormat.YearMonthDay)]
            public DateOnly[] ArrayYearMonthDayFormat { get; set; }
        }
    }
#endif
}
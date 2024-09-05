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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    #if NET6_0_OR_GREATER
    public class DateOnlySerializerTests
    {
        private class DerivedFromDateOnlySerializer : DateOnlySerializer
        {
        }

        private class TestClass
        {
            public DateOnly DefaultDate { get; set; }

            [BsonRepresentation(BsonType.DateTime)]
            public DateOnly DateTimeDate { get; set; }

            [BsonRepresentation(BsonType.Int64)]
            public DateOnly IntDate { get; set; }

            [BsonRepresentation(BsonType.String)]
            public DateOnly StringDate { get; set; }

            [BsonRepresentation(BsonType.Document)]
            public DateOnly DocumentDate { get; set; }

            public override bool Equals(object obj)
            {
                return obj is TestClass to &&
                       DefaultDate.Equals(to.DefaultDate) &&
                       DateTimeDate.Equals(to.DateTimeDate) &&
                       IntDate.Equals(to.IntDate) &&
                       StringDate.Equals(to.StringDate) &&
                       DocumentDate.Equals(to.DocumentDate);
            }

            public override int GetHashCode() => base.GetHashCode();
        }

        public static readonly IEnumerable<object[]> DateOnlyValues =
        [
            [DateOnly.MinValue],
            [DateOnly.MaxValue],
            [DateOnly.FromDateTime(DateTime.Today)],
        ];

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

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new DateOnlySerializer();
            var y = new DerivedFromDateOnlySerializer();

            var result = x.Equals(y);

            result.Should().Be(false);
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
        [InlineData(BsonType.String)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.Document)]
        public void Equals_with_not_equal_fields_should_return_true(BsonType representation)
        {
            var x = new DateOnlySerializer();
            var y = new DateOnlySerializer(representation);

            var result = x.Equals(y);

            result.Should().Be(false);
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

        //TODO Order alphabetically
        //TODO We could remove the input, if we just want to test for the correct representation
        //TODO We could have specific tests for min value max value?
        [Theory]
        [MemberData(nameof(DateOnlyValues))]
        public void BsonRepresentationAttribute_should_set_correct_representation(DateOnly testValue)
        {
            var testObj = new TestClass
            {
                DefaultDate = testValue,
                DateTimeDate = testValue,
                IntDate = testValue,
                StringDate = testValue,
                DocumentDate = testValue,
            };

            var bsonDocument = testObj.ToBsonDocument();

            Assert.Equal(bsonDocument["DefaultDate"].BsonType, BsonType.DateTime);
            Assert.Equal(bsonDocument["DateTimeDate"].BsonType, BsonType.DateTime);
            Assert.Equal(bsonDocument["IntDate"].BsonType, BsonType.Int64);
            Assert.Equal(bsonDocument["StringDate"].BsonType, BsonType.String);
            Assert.Equal(bsonDocument["DocumentDate"].BsonType, BsonType.Document);

            var rehydrated = BsonSerializer.Deserialize<TestClass>(bsonDocument);
            Assert.Equal(testObj, rehydrated);
        }

        [Fact]
        public void Test2()  //TODO Need a better name (throw exception when date has time part)
        {
            var dateTime = new DateTime(2024, 10, 2, 1, 1, 1, DateTimeKind.Utc);
            var bsonDocument = new BsonDocument
            {
                { "DateTimeDate", dateTime}
            };

            var rehydrated = BsonSerializer.Deserialize<TestClass>(bsonDocument);
        }

        [Theory]
        [InlineData(BsonType.DateTime, "10/20/2024", """{ "x" : { "$date" : { "$numberLong" : "1729382400000" } } }""")]
        [InlineData(BsonType.DateTime, "01/01/0001", """{ "x" : { "$date" : { "$numberLong" : "-62135596800000" } } }""")]
        [InlineData(BsonType.DateTime, "12/31/9999", """{ "x" : { "$date" : { "$numberLong" : "253402214400000" } } }""")]
        [InlineData(BsonType.String, "10/20/2024", """{ "x" : "2024-10-20" }""")]
        [InlineData(BsonType.String, "01/01/0001", """{ "x" : "0001-01-01" }""")]
        [InlineData(BsonType.String, "12/31/9999", """{ "x" : "9999-12-31" }""")]
        [InlineData(BsonType.Int64, "10/20/2024", """{ "x" : { "$numberLong" : "638649792000000000" } }""")]
        [InlineData(BsonType.Int64, "01/01/0001", """{ "x" : { "$numberLong" : "0" } }""")]
        [InlineData(BsonType.Int64, "12/31/9999", """{ "x" : { "$numberLong" : "3155378112000000000" } }""")]
        [InlineData(BsonType.Document, "10/20/2024", """{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "1729382400000" } }, "Ticks" : { "$numberLong" : "638649792000000000" } } }""")]
        [InlineData(BsonType.Document, "01/01/0001", """{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "-62135596800000" } }, "Ticks" : { "$numberLong" : "0" } } }""")]
        [InlineData(BsonType.Document, "12/31/9999", """{ "x" : { "DateTime" : { "$date" : { "$numberLong" : "253402214400000" } }, "Ticks" : { "$numberLong" : "3155378112000000000" } } }""")]
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
    }
    #endif
}
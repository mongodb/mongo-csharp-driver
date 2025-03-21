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

#if NET6_0_OR_GREATER
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class ImmutableSortedDictionarySerializerTests
    {
        [Fact]
        public void Deserialize_should_have_expected_result()
        {
            const string json = """{ "x" : { "1" : { "$numberInt" : "1" }, "2" : { "$numberInt" : "2" }, "3" : { "$numberInt" : "3" }, "4" : { "$numberInt" : "4" } } }""";
            var subject = new ImmutableSortedDictionarySerializer<string, int>();

            using var reader = new JsonReader(json);
            reader.ReadStartDocument();
            reader.ReadName("x");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var result = subject.Deserialize(context);
            reader.ReadEndDocument();

            var expectedResult = ImmutableSortedDictionary.CreateRange(
                new [] {
                    KeyValuePair.Create("1", 1),
                    KeyValuePair.Create("2", 2),
                    KeyValuePair.Create("3", 3),
                    KeyValuePair.Create("4", 4)
                });
            result.Should().Equal(expectedResult);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ImmutableSortedDictionarySerializer<string, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ImmutableSortedDictionarySerializer<string, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ImmutableSortedDictionarySerializer<string, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ImmutableSortedDictionarySerializer<string, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void Serialize_should_have_expected_result()
        {
            var subject = new ImmutableSortedDictionarySerializer<string, int>();
            var value = ImmutableSortedDictionary.CreateRange(
                new [] {
                    KeyValuePair.Create("1", 1),
                    KeyValuePair.Create("2", 2),
                    KeyValuePair.Create("3", 3),
                    KeyValuePair.Create("4", 4)
                });

            using var textWriter = new StringWriter();
            using var writer = new JsonWriter(textWriter,
                new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson });

            var context = BsonSerializationContext.CreateRoot(writer);
            writer.WriteStartDocument();
            writer.WriteName("x");
            subject.Serialize(context, value);
            writer.WriteEndDocument();
            var result = textWriter.ToString();

            const string expectedResult = """{ "x" : { "1" : { "$numberInt" : "1" }, "2" : { "$numberInt" : "2" }, "3" : { "$numberInt" : "3" }, "4" : { "$numberInt" : "4" } } }""";
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Serializer_should_be_registered()
        {
            var serializer = BsonSerializer.LookupSerializer(typeof(ImmutableSortedDictionary<string, int>));

            serializer.Should().Be(new ImmutableSortedDictionarySerializer<string, int>());
        }
    }
}
#endif
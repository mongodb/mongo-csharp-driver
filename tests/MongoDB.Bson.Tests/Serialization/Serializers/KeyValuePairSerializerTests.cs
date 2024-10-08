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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.DefaultSerializer.Serializers
{
    public class KeyValuePairSerializerTests
    {
        [Fact]
        public void TestNullKey()
        {
            var kvp = new KeyValuePair<string, object>(null, "value");
            var json = kvp.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'k' : null, 'v' : 'value' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = kvp.ToBson();
            var rehydrated = BsonSerializer.Deserialize<KeyValuePair<string, object>>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNullValue()
        {
            var kvp = new KeyValuePair<string, object>("key", null);
            var json = kvp.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'k' : 'key', 'v' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = kvp.ToBson();
            var rehydrated = BsonSerializer.Deserialize<KeyValuePair<string, object>>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new KeyValuePairSerializer<int, int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new KeyValuePairSerializer<int, int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new KeyValuePairSerializer<int, int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new KeyValuePairSerializer<int, int>();
            var y = new KeyValuePairSerializer<int, int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Theory]
        [InlineData("representation")]
        [InlineData("keySerializer")]
        [InlineData("valueSerializer")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var representation1 = BsonType.Array;
            var representation2 = BsonType.Document;
            var keySerializer1 = new Int32Serializer(BsonType.Int32);
            var keySerializer2 = new Int32Serializer(BsonType.String);
            var valueSerializer1 = new Int32Serializer(BsonType.Int32);
            var valueSerializer2 = new Int32Serializer(BsonType.String);
            var x = new KeyValuePairSerializer<int, int>(representation1, keySerializer1, valueSerializer1);
            var y = notEqualFieldName switch
            {
                "representation" => new KeyValuePairSerializer<int, int>(representation2, keySerializer1, valueSerializer1),
                "keySerializer" => new KeyValuePairSerializer<int, int>(representation1, keySerializer2, valueSerializer1),
                "valueSerializer" => new KeyValuePairSerializer<int, int>(representation1, keySerializer1, valueSerializer2),
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new KeyValuePairSerializer<int, int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }
}

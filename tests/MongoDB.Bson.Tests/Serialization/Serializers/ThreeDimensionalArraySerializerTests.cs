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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.ArraySerializer
{
    public class ThreeDimensionalArraySerializerTests
    {
        private class C
        {
            public int[,,] A;
        }

        [Fact]
        public void TestNull()
        {
            var c = new C { A = null };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test0x0x0()
        {
            var c = new C { A = new int[0, 0, 0] };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test1x0x0()
        {
            var c = new C { A = new int[1, 0, 0] };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test1x1x0()
        {
            var c = new C { A = new int[1, 1, 0] };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[]]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test1x1x1()
        {
            var c = new C { A = new int[1, 1, 1] { { { 1 } } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[1]]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test1x1x2()
        {
            var c = new C { A = new int[1, 1, 2] { { { 1, 2 } } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[1, 2]]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test1x2x0()
        {
            var c = new C { A = new int[1, 2, 0] };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[], []]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test1x2x1()
        {
            var c = new C { A = new int[1, 2, 1] { { { 1 }, { 2 } } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[1], [2]]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test1x2x2()
        {
            var c = new C { A = new int[1, 2, 2] { { { 1, 2 }, { 3, 4 } } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[1, 2], [3, 4]]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test2x0x0()
        {
            var c = new C { A = new int[2, 0, 0] };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[], []] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test2x1x0()
        {
            var c = new C { A = new int[2, 1, 0] };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[]], [[]]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test2x1x1()
        {
            var c = new C { A = new int[2, 1, 1] { { { 1 } }, { { 2 } } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[1]], [[2]]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test2x1x2()
        {
            var c = new C { A = new int[2, 1, 2] { { { 1, 2 } }, { { 3, 4 } } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[1, 2]], [[3, 4]]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test2x2x0()
        {
            var c = new C { A = new int[2, 2, 0] };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[], []], [[], []]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test2x2x1()
        {
            var c = new C { A = new int[2, 2, 1] { { { 1 }, { 2 } }, { { 3 }, { 4 } } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[1], [2]], [[3], [4]]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Test2x2x2()
        {
            var c = new C { A = new int[2, 2, 2] { { { 1, 2 }, { 3, 4 } }, { { 5, 6 }, { 7, 8 } } } };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'A' : [[[1, 2], [3, 4]], [[5, 6], [7, 8]]] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ThreeDimensionalArraySerializer<int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ThreeDimensionalArraySerializer<int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ThreeDimensionalArraySerializer<int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ThreeDimensionalArraySerializer<int>();
            var y = new ThreeDimensionalArraySerializer<int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var itemSerializer1 = new Int32Serializer(BsonType.Int32);
            var itemSerializer2 = new Int32Serializer(BsonType.String);
            var x = new ThreeDimensionalArraySerializer<int>(itemSerializer1);
            var y = new ThreeDimensionalArraySerializer<int>(itemSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ThreeDimensionalArraySerializer<int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }
}

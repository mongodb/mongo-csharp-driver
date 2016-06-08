/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class ClassArrayTests
    {
        private class B
        {
            public int X;
        }

        private class C
        {
            public B[] Array;
        }

        [Fact]
        public void TestSerializeNull()
        {
            C c = new C { Array = null };
            var json = c.ToJson();
            var expected = ("{ 'Array' : null }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeEmpty()
        {
            C c = new C { Array = new B[0] };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize1()
        {
            C c = new C { Array = new B[] { new B { X = 1 } } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [{ 'X' : 1 }] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize1Null()
        {
            C c = new C { Array = new B[] { null } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [null] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2()
        {
            C c = new C { Array = new B[] { new B { X = 1 }, new B { X = 2 } } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [{ 'X' : 1 }, { 'X' : 2 }] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2Null()
        {
            C c = new C { Array = new B[] { null, null } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [null, null] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2Mixed()
        {
            C c = new C { Array = new B[] { new B { X = 1 }, null } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [{ 'X' : 1 }, null] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class EnumArrayTests
    {
        private enum E
        {
            None,
            A,
            B
        }

        private class C
        {
            [BsonRepresentation(BsonType.String)]
            public E[] Array;
        }

        [Fact]
        public void TestSerializeNull()
        {
            C c = new C { Array = null };
            var json = c.ToJson();
            var expected = ("{ 'Array' : null }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeEmpty()
        {
            C c = new C { Array = new E[0] };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [] }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize1()
        {
            C c = new C { Array = new E[] { E.A } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [\"A\"] }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2()
        {
            C c = new C { Array = new E[] { E.A, E.B } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [\"A\", \"B\"] }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class IntArrayTests
    {
        private class C
        {
            public int[] Array;
        }

        [Fact]
        public void TestSerializeNull()
        {
            C c = new C { Array = null };
            var json = c.ToJson();
            var expected = ("{ 'Array' : null }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeEmpty()
        {
            C c = new C { Array = new int[0] };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize1()
        {
            C c = new C { Array = new int[] { 1 } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [1] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2()
        {
            C c = new C { Array = new int[] { 1, 2 } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [1, 2] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class StringArrayTests
    {
        private class C
        {
            public string[] Array;
        }

        [Fact]
        public void TestSerializeNull()
        {
            C c = new C { Array = null };
            var json = c.ToJson();
            var expected = ("{ 'Array' : null }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerializeEmpty()
        {
            C c = new C { Array = new string[0] };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize1()
        {
            C c = new C { Array = new string[] { "a" } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : ['a'] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize1Null()
        {
            C c = new C { Array = new string[] { null } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [null] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2()
        {
            C c = new C { Array = new string[] { "a", "b" } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : ['a', 'b'] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2Null()
        {
            C c = new C { Array = new string[] { null, null } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : [null, null] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestSerialize2Mixed()
        {
            C c = new C { Array = new string[] { "a", null } };
            var json = c.ToJson();
            var expected = ("{ 'Array' : ['a', null] }").Replace("'", "\""); // no discriminator
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}

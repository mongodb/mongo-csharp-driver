/* Copyright 2010-2015 MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class ObjectSerializerTests
    {
        public class C
        {
            public object Obj;
        }

        public class D
        {
            public object[] Array;
        }

        [Test]
        public void TestArray()
        {
            var d = new D
            {
                Array = new object[]
                {
                    1.5,
                    "abc",
                    new object(),
                    true,
                    BsonConstants.UnixEpoch,
                    null,
                    123,
                    123L
                }
            };
            var json = d.ToJson();
            var expected = "{ 'Array' : [#A] }";
            expected = expected.Replace("#A",
                string.Join(", ", new string[]
                    {
                        "1.5",
                        "'abc'",
                        "{ }",
                        "true",
                        "ISODate('1970-01-01T00:00:00Z')",
                        "null",
                        "123",
                        "NumberLong(123)"
                    }));
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = d.ToBson();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestBoolean()
        {
            var c = new C { Obj = true };
            var json = c.ToJson();
            var expected = "{ 'Obj' : true }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestDateTime()
        {
            var c = new C { Obj = BsonConstants.UnixEpoch };
            var json = c.ToJson();
            var expected = "{ 'Obj' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestDouble()
        {
            var c = new C { Obj = 1.5 };
            var json = c.ToJson();
            var expected = "{ 'Obj' : 1.5 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestInt32()
        {
            var c = new C { Obj = 123 };
            var json = c.ToJson();
            var expected = "{ 'Obj' : 123 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestInt64()
        {
            var c = new C { Obj = 123L };
            var json = c.ToJson();
            var expected = "{ 'Obj' : NumberLong(123) }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [TestCase("{ Obj : {  _v : [] } }")]
        public void TestMissingDiscriminator(string json)
        {
            Assert.Throws<FileFormatException>(() => BsonSerializer.Deserialize<C>(json));
        }

        [TestCase("{ Obj : { _t : \"System.Object[]\" } }")]
        public void TestMissingValue(string json)
        {
            Assert.Throws<FileFormatException>(() => BsonSerializer.Deserialize<C>(json));
        }

        [Test]
        public void TestNull()
        {
            var c = new C { Obj = null };
            var json = c.ToJson();
            var expected = "{ 'Obj' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestObject()
        {
            var c = new C { Obj = new object() };
            var json = c.ToJson();
            var expected = "{ 'Obj' : { } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [TestCase("{ Obj : { _t : \"System.Object[]\", _v : [] } }")]
        [TestCase("{ Obj : { _v : [], _t : \"System.Object[]\" } }")]
        public void TestOrderOfElementsDoesNotMatter(string json)
        {
            var result = BsonSerializer.Deserialize<C>(json);

            Assert.IsInstanceOf<object[]>(result.Obj);
        }

        //[TestCase("{ Obj : { _t : \"System.Guid\", _v : \"01020304-0506-0708-090a-0b0c0d0e0f10\" } }", "01020304-0506-0708-090a-0b0c0d0e0f10")]
        //[TestCase("{ Obj : { _v : \"01020304-0506-0708-090a-0b0c0d0e0f10\", _t : \"System.Guid\" } }", "01020304-0506-0708-090a-0b0c0d0e0f10")]
        //public void TestOrderOfElementsDoesNotMatter(string json, string expectedGuid)
        //{
        //    var result = BsonSerializer.Deserialize<C>(json);
        //
        //    Assert.AreEqual(new Guid(expectedGuid), result.Obj);
        //}

        [Test]
        public void TestString()
        {
            var c = new C { Obj = "abc" };
            var json = c.ToJson();
            var expected = "{ 'Obj' : 'abc' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}

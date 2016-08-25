/* Copyright 2010-2016 MongoDB Inc.
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
using System.Dynamic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
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

        [Fact]
        public void TestArray()
        {
            var d = new D
            {
                Array = new object[]
                {
                    (Decimal128)1.5M,
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
                        "NumberDecimal('1.5')",
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
            Assert.Equal(expected, json);

            var bson = d.ToBson();
            var rehydrated = BsonSerializer.Deserialize<D>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestBoolean()
        {
            var c = new C { Obj = true };
            var json = c.ToJson();
            var expected = "{ 'Obj' : true }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDateTime()
        {
            var c = new C { Obj = BsonConstants.UnixEpoch };
            var json = c.ToJson();
            var expected = "{ 'Obj' : ISODate('1970-01-01T00:00:00Z') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDecimal128()
        {
            var c = new C { Obj = (Decimal128)1.5M };
            var json = c.ToJson();
            var expected = "{ 'Obj' : NumberDecimal('1.5') }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDouble()
        {
            var c = new C { Obj = 1.5 };
            var json = c.ToJson();
            var expected = "{ 'Obj' : 1.5 }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestInt32()
        {
            var c = new C { Obj = 123 };
            var json = c.ToJson();
            var expected = "{ 'Obj' : 123 }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestInt64()
        {
            var c = new C { Obj = 123L };
            var json = c.ToJson();
            var expected = "{ 'Obj' : NumberLong(123) }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Theory]
        [InlineData("{ Obj : {  _v : \"01020304-0506-0708-090a-0b0c0d0e0f10\" } }")]
        public void TestMissingDiscriminator(string json)
        {
            var result = BsonSerializer.Deserialize<C>(json);

            Assert.IsType<ExpandoObject>(result.Obj);
        }

        [Theory]
        [InlineData("{ Obj : { _t : \"System.Guid\" } }")]
        public void TestMissingValue(string json)
        {
            Assert.Throws<FormatException>(() => BsonSerializer.Deserialize<C>(json));
        }

        [Fact]
        public void TestNull()
        {
            var c = new C { Obj = null };
            var json = c.ToJson();
            var expected = "{ 'Obj' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestObject()
        {
            var c = new C { Obj = new object() };
            var json = c.ToJson();
            var expected = "{ 'Obj' : { } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Theory]
        [InlineData("{ Obj : { _t : \"System.Object[]\", _v : [] } }")]
        [InlineData("{ Obj : { _v : [], _t : \"System.Object[]\" } }")]
        public void TestOrderOfElementsDoesNotMatter(string json)
        {
            var result = BsonSerializer.Deserialize<C>(json);

            Assert.IsType<object[]>(result.Obj);
        }

        [Theory]
        [InlineData("{ Obj : { _t : \"System.Guid\", _v : \"01020304-0506-0708-090a-0b0c0d0e0f10\" } }", "01020304-0506-0708-090a-0b0c0d0e0f10")]
        [InlineData("{ Obj : { _v : \"01020304-0506-0708-090a-0b0c0d0e0f10\", _t : \"System.Guid\" } }", "01020304-0506-0708-090a-0b0c0d0e0f10")]
        public void TestOrderOfElementsDoesNotMatter_with_Guids(string json, string expectedGuid)
        {
            var result = BsonSerializer.Deserialize<C>(json);

            Assert.Equal(Guid.Parse(expectedGuid), result.Obj);
        }

        [Fact]
        public void TestString()
        {
            var c = new C { Obj = "abc" };
            var json = c.ToJson();
            var expected = "{ 'Obj' : 'abc' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestUsesDiscriminatorWhenTypeIsNotABsonPrimitive()
        {
            var c = new C { Obj = new ExpandoObject() };
            var json = c.ToJson(configurator: config => config.IsDynamicType = t => false);
#if NET45
            var discriminator = "System.Dynamic.ExpandoObject";
#else
            var discriminator = typeof(ExpandoObject).AssemblyQualifiedName;
#endif
            var expected = ("{ 'Obj' : { '_t' : '" + discriminator + "', '_v' : { } } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson(configurator: config => config.IsDynamicType = t => false);
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson(configurator: config => config.IsDynamicType = t => false)));
        }

        [Fact]
        public void TestDoesNotUseDiscriminatorForDynamicTypes()
        {
            // explicitly setting the IsDynamicType and DynamicDocumentSerializer properties
            // in case we change the dynamic defaults in BsonDefaults.

            var c = new C { Obj = new ExpandoObject() };
            var json = c.ToJson(configurator: b => b.IsDynamicType = t => t == typeof(ExpandoObject));
            var expected = "{ 'Obj' : { } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson(configurator: b => b.IsDynamicType = t => t == typeof(ExpandoObject));
            var rehydrated = BsonSerializer.Deserialize<C>(bson, b => b.DynamicDocumentSerializer = BsonSerializer.LookupSerializer<ExpandoObject>());
            Assert.True(bson.SequenceEqual(rehydrated.ToBson(configurator: b => b.IsDynamicType = t => t == typeof(ExpandoObject))));
        }
    }
}

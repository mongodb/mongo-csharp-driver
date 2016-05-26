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

using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class NullableTypeSerializerTests
    {
        private class C
        {
            public bool? Boolean { get; set; }
            public DateTime? DateTime { get; set; }
            [BsonDateTimeOptions(DateOnly = true)]
            public DateTime? DateOnly { get; set; }
            public double? Double { get; set; }
            public Guid? Guid { get; set; }
            public int? Int32 { get; set; }
            public long? Int64 { get; set; }
            public ObjectId? ObjectId { get; set; }
            [BsonRepresentation(BsonType.String)]
            public ConsoleColor? Enum { get; set; }
            // public Struct? Struct { get; set; }
        }

        //private struct Struct {
        //    public string StructP { get; set; }
        //}

        private const string _template =
            "{ " +
            "'Boolean' : null, " +
            "'DateTime' : null, " +
            "'DateOnly' : null, " +
            "'Double' : null, " +
            "'Guid' : null, " +
            "'Int32' : null, " +
            "'Int64' : null, " +
            "'ObjectId' : null, " +
            "'Enum' : null" +
            // "'Struct' : null" +
            " }";

        [Fact]
        public void TestAllNulls()
        {
            C c = new C();
            var json = c.ToJson();
            var expected = _template.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestBoolean()
        {
            C c = new C { Boolean = true };
            var json = c.ToJson();
            var expected = _template.Replace("'Boolean' : null", "'Boolean' : true").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDateTime()
        {
            C c = new C { DateTime = BsonConstants.UnixEpoch };
            var json = c.ToJson();
            var expected = _template.Replace("'DateTime' : null", "'DateTime' : ISODate('1970-01-01T00:00:00Z')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDateOnly()
        {
            C c = new C { DateOnly = BsonConstants.UnixEpoch };
            var json = c.ToJson();
            var expected = _template.Replace("'DateOnly' : null", "'DateOnly' : ISODate('1970-01-01T00:00:00Z')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDouble()
        {
            C c = new C { Double = 1.5 };
            var json = c.ToJson();
            var expected = _template.Replace("'Double' : null", "'Double' : 1.5").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEnum()
        {
            var c = new C { Enum = ConsoleColor.Red };
            var json = c.ToJson();
            var expected = _template.Replace("'Enum' : null", "'Enum' : 'Red'").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestGuid()
        {
            C c = new C { Guid = Guid.Empty };
            var json = c.ToJson();
            var expected = _template.Replace("'Guid' : null", "'Guid' : CSUUID('00000000-0000-0000-0000-000000000000')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestInt32()
        {
            C c = new C { Int32 = 1 };
            var json = c.ToJson();
            var expected = _template.Replace("'Int32' : null", "'Int32' : 1").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestInt64()
        {
            C c = new C { Int64 = 2 };
            var json = c.ToJson();
            var expected = _template.Replace("'Int64' : null", "'Int64' : NumberLong(2)").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestObjectId()
        {
            C c = new C { ObjectId = ObjectId.Empty };
            var json = c.ToJson();
            var expected = _template.Replace("'ObjectId' : null", "'ObjectId' : ObjectId('000000000000000000000000')").Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        //[Fact]
        //public void TestStruct() {
        //    C c = new C { Struct = new Struct { StructP = "x" } };
        //    var json = c.ToJson();
        //    var expected = template.Replace("'Struct' : null", "'Struct' : { 'StructP' : 'x' }").Replace("'", "\"");
        //    Assert.Equal(expected, json);

        //    var bson = c.ToBson();
        //    var rehydrated = BsonSerializer.Deserialize<C>(bson);
        //    Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        //}
    }
}

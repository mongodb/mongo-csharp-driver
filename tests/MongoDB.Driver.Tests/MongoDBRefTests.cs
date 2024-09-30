﻿/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoDBRefTests
    {
        public class C
        {
            public ObjectId Id;
            public MongoDBRef DBRef;
        }

        [Fact]
        public void TestNull()
        {
            var id = ObjectId.GenerateNewId();
            var obj = new C { Id = id, DBRef = null };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : null }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDateTimeRefId()
        {
            var id = ObjectId.GenerateNewId();
            var dateTime = BsonConstants.UnixEpoch; ;
            var dbRef = new MongoDBRef("collection", dateTime);
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : ISODate('1970-01-01T00:00:00Z') } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestDocumentRefId()
        {
            var id = ObjectId.GenerateNewId();
            var refId = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var dbRef = new MongoDBRef("collection", refId);
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : { 'x' : 1, 'y' : 2 } } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestEqualsWithDatabase()
        {
            var a1 = new MongoDBRef("d", "c", 1);
            var a2 = new MongoDBRef("d", "c", 1);
            var a3 = a2;
            var b = new MongoDBRef("x", "c", 1);
            var c = new MongoDBRef("d", "x", 1);
            var d = new MongoDBRef("d", "c", 2);
            var null1 = (MongoDBRef)null;
            var null2 = (MongoDBRef)null;

            Assert.NotSame(a1, a2);
            Assert.Same(a2, a3);
            Assert.True(a1.Equals((object)a2));
            Assert.False(a1.Equals((object)null));
            Assert.False(a1.Equals((object)"x"));

            Assert.True(a1 == a2);
            Assert.True(a2 == a3);
            Assert.False(a1 == b);
            Assert.False(a1 == c);
            Assert.False(a1 == d);
            Assert.False(a1 == null1);
            Assert.False(null1 == a1);
            Assert.True(null1 == null2);

            Assert.False(a1 != a2);
            Assert.False(a2 != a3);
            Assert.True(a1 != b);
            Assert.True(a1 != c);
            Assert.True(a1 != d);
            Assert.True(a1 != null1);
            Assert.True(null1 != a1);
            Assert.False(null1 != null2);

            Assert.Equal(a1.GetHashCode(), a2.GetHashCode());
        }

        [Fact]
        public void TestEqualsWithoutDatabase()
        {
            var a1 = new MongoDBRef("c", 1);
            var a2 = new MongoDBRef("c", 1);
            var a3 = a2;
            var b = new MongoDBRef("x", 1);
            var c = new MongoDBRef("c", 2);
            var null1 = (MongoDBRef)null;
            var null2 = (MongoDBRef)null;

            Assert.NotSame(a1, a2);
            Assert.Same(a2, a3);
            Assert.True(a1.Equals((object)a2));
            Assert.False(a1.Equals((object)null));
            Assert.False(a1.Equals((object)"x"));

            Assert.True(a1 == a2);
            Assert.True(a2 == a3);
            Assert.False(a1 == b);
            Assert.False(a1 == c);
            Assert.False(a1 == null1);
            Assert.False(null1 == a1);
            Assert.True(null1 == null2);

            Assert.False(a1 != a2);
            Assert.False(a2 != a3);
            Assert.True(a1 != b);
            Assert.True(a1 != c);
            Assert.True(a1 != null1);
            Assert.True(null1 != a1);
            Assert.False(null1 != null2);

            Assert.Equal(a1.GetHashCode(), a2.GetHashCode());
        }

        [Fact]
        public void TestInt32RefId()
        {
            var id = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("collection", 1);
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : 1 } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestInt64RefId()
        {
            var id = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("collection", 123456789012345L);
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : NumberLong('123456789012345') } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestObjectIdRefId()
        {
            var id = ObjectId.GenerateNewId();
            var refId = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("collection", refId);
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : ObjectId('#ref') } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("#ref", refId.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestStringRefId()
        {
            var id = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("collection", "abc");
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : 'abc' } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestWithDatabase()
        {
            var id = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("database", "collection", ObjectId.GenerateNewId());
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : ObjectId('#ref'), '$db' : 'database' } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("#ref", dbRef.Id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }

    public class MongoDBRefSerializerTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new MongoDBRefSerializer();
            var y = new DerivedFromMongoDBRefSerializer();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new MongoDBRefSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new MongoDBRefSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new MongoDBRefSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new MongoDBRefSerializer();
            var y = new MongoDBRefSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new MongoDBRefSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        internal class DerivedFromMongoDBRefSerializer : MongoDBRefSerializer
        {
        }
    }
}

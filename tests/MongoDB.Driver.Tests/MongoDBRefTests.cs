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
            var json = obj.ToJson();
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
            var json = obj.ToJson();
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
            var json = obj.ToJson();
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

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void TestGuidRefId(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

#pragma warning disable 618
            var guid = Guid.NewGuid();
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 && BsonDefaults.GuidRepresentation == GuidRepresentation.Unspecified ||
                BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V3)
            {
                var exception = Record.Exception(() => new MongoDBRef("collection", guid));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var id = ObjectId.GenerateNewId();
                var dbRef = new MongoDBRef("collection", guid);
                var obj = new C { Id = id, DBRef = dbRef };
                var json = obj.ToJson();
                var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : #uuidConstructorName('#guid') } }";
                expected = expected.Replace("#id", id.ToString());
                string uuidConstructorName;
                switch (BsonDefaults.GuidRepresentation)
                {
                    case GuidRepresentation.CSharpLegacy: uuidConstructorName = "CSUUID"; break;
                    case GuidRepresentation.JavaLegacy: uuidConstructorName = "JUUID"; break;
                    case GuidRepresentation.PythonLegacy: uuidConstructorName = "PYUUID"; break;
                    case GuidRepresentation.Standard: uuidConstructorName = "UUID"; break;
                    default: throw new Exception("Invalid GuidRepresentation.");
                };
                expected = expected.Replace("#uuidConstructorName", uuidConstructorName);
                expected = expected.Replace("#guid", guid.ToString());
                expected = expected.Replace("'", "\"");
                Assert.Equal(expected, json);

                var bson = obj.ToBson();
                var rehydrated = BsonSerializer.Deserialize<C>(bson);
                Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
            }
#pragma warning restore 618
        }

        [Fact]
        public void TestInt32RefId()
        {
            var id = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("collection", 1);
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson();
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
            var json = obj.ToJson();
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
            var json = obj.ToJson();
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
            var json = obj.ToJson();
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
            var json = obj.ToJson();
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
}

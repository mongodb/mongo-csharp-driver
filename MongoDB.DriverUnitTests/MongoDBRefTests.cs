/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoDBRefTests
    {
        public class C
        {
            public ObjectId Id;
            public MongoDBRef DBRef;
        }

        [Test]
        public void TestNull()
        {
            var id = ObjectId.GenerateNewId();
            var obj = new C { Id = id, DBRef = null };
            var json = obj.ToJson();
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : null }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
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
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
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
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
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

            Assert.AreNotSame(a1, a2);
            Assert.AreSame(a2, a3);
            Assert.IsTrue(a1.Equals((object)a2));
            Assert.IsFalse(a1.Equals((object)null));
            Assert.IsFalse(a1.Equals((object)"x"));

            Assert.IsTrue(a1 == a2);
            Assert.IsTrue(a2 == a3);
            Assert.IsFalse(a1 == b);
            Assert.IsFalse(a1 == c);
            Assert.IsFalse(a1 == d);
            Assert.IsFalse(a1 == null1);
            Assert.IsFalse(null1 == a1);
            Assert.IsTrue(null1 == null2);

            Assert.IsFalse(a1 != a2);
            Assert.IsFalse(a2 != a3);
            Assert.IsTrue(a1 != b);
            Assert.IsTrue(a1 != c);
            Assert.IsTrue(a1 != d);
            Assert.IsTrue(a1 != null1);
            Assert.IsTrue(null1 != a1);
            Assert.IsFalse(null1 != null2);

            Assert.AreEqual(a1.GetHashCode(), a2.GetHashCode());
        }

        [Test]
        public void TestEqualsWithoutDatabase()
        {
            var a1 = new MongoDBRef("c", 1);
            var a2 = new MongoDBRef("c", 1);
            var a3 = a2;
            var b = new MongoDBRef("x", 1);
            var c = new MongoDBRef("c", 2);
            var null1 = (MongoDBRef)null;
            var null2 = (MongoDBRef)null;

            Assert.AreNotSame(a1, a2);
            Assert.AreSame(a2, a3);
            Assert.IsTrue(a1.Equals((object)a2));
            Assert.IsFalse(a1.Equals((object)null));
            Assert.IsFalse(a1.Equals((object)"x"));

            Assert.IsTrue(a1 == a2);
            Assert.IsTrue(a2 == a3);
            Assert.IsFalse(a1 == b);
            Assert.IsFalse(a1 == c);
            Assert.IsFalse(a1 == null1);
            Assert.IsFalse(null1 == a1);
            Assert.IsTrue(null1 == null2);

            Assert.IsFalse(a1 != a2);
            Assert.IsFalse(a2 != a3);
            Assert.IsTrue(a1 != b);
            Assert.IsTrue(a1 != c);
            Assert.IsTrue(a1 != null1);
            Assert.IsTrue(null1 != a1);
            Assert.IsFalse(null1 != null2);

            Assert.AreEqual(a1.GetHashCode(), a2.GetHashCode());
        }

        [Test]
        public void TestGuidRefId()
        {
            var id = ObjectId.GenerateNewId();
            var guid = Guid.NewGuid();
            var dbRef = new MongoDBRef("collection", guid);
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson();
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : CSUUID('#guid') } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("#guid", guid.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestInt32RefId()
        {
            var id = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("collection", 1);
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson();
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : 1 } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestInt64RefId()
        {
            var id = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("collection", 123456789012345L);
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson();
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : NumberLong('123456789012345') } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
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
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestStringRefId()
        {
            var id = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("collection", "abc");
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson();
            var expected = "{ '_id' : ObjectId('#id'), 'DBRef' : { '$ref' : 'collection', '$id' : 'abc' } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
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
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}

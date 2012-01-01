/* Copyright 2010-2012 10gen Inc.
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
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

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

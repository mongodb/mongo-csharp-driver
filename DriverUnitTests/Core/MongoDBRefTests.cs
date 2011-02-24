/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.DriverUnitTests {
    [TestFixture]
    public class MongoDBRefTests {
        public class C {
            public ObjectId Id;
            public MongoDBRef DBRef;
        }

        [Test]
        public void TestNull() {
            var id = ObjectId.GenerateNewId();
            var obj = new C { Id = id, DBRef = null };
            var json = obj.ToJson();
            var expected = "{ '_id' : { '$oid' : '#id' }, 'DBRef' : null }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNoDatabase() {
            var id = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("collection", ObjectId.GenerateNewId());
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson();
            var expected = "{ '_id' : { '$oid' : '#id' }, 'DBRef' : { '$ref' : 'collection', '$id' : { '$oid' : '#ref' } } }";
            expected = expected.Replace("#id", id.ToString());
            expected = expected.Replace("#ref", dbRef.Id.ToString());
            expected = expected.Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestWithDatabase() {
            var id = ObjectId.GenerateNewId();
            var dbRef = new MongoDBRef("database", "collection", ObjectId.GenerateNewId());
            var obj = new C { Id = id, DBRef = dbRef };
            var json = obj.ToJson();
            var expected = "{ '_id' : { '$oid' : '#id' }, 'DBRef' : { '$ref' : 'collection', '$id' : { '$oid' : '#ref' }, '$db' : 'database' } }";
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

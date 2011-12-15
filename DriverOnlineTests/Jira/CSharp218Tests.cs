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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverOnlineTests.Jira.CSharp218
{
    [TestFixture]
    public class CSharp218Tests
    {
        public class C
        {
            public ObjectId Id;
            public P P;
        }

        public struct S
        {
            public ObjectId Id;
            public P P;
        }

        public struct P
        {
            public int X;
            public int Y;
        }

        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection<BsonDocument> collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            database = server["onlinetests"];
            collection = database.GetCollection("testcollection");
        }

        [Test]
        public void TestDeserializeClassWithStructPropertyFails()
        {
            collection.RemoveAll();
            var c = new C { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            collection.Insert(c);
            try
            {
                collection.FindOneAs<C>();
                Assert.Fail("Expected an exception to be thrown.");
            }
            catch (Exception ex)
            {
                var expectedMessage = "An error occurred while deserializing the P field of class MongoDB.DriverOnlineTests.Jira.CSharp218.CSharp218Tests+C: Value class MongoDB.DriverOnlineTests.Jira.CSharp218.CSharp218Tests+P cannot be deserialized.";
                Assert.IsInstanceOf<FileFormatException>(ex);
                Assert.IsInstanceOf<BsonSerializationException>(ex.InnerException);
                Assert.AreEqual(expectedMessage, ex.Message);
            }
        }

        [Test]
        public void TestDeserializeStructFails()
        {
            collection.RemoveAll();
            var s = new S { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            collection.Insert(s);
            Assert.Throws<BsonSerializationException>(() => collection.FindOneAs<S>());
        }

        [Test]
        public void TestInsertForClassWithIdSucceeds()
        {
            collection.RemoveAll();
            var c = new C { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            collection.Insert(c);
            Assert.AreEqual(1, collection.Count());
            var r = collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(c.Id, r["_id"].AsObjectId);
            Assert.AreEqual(c.P.X, r["P"].AsBsonDocument["X"].AsInt32);
            Assert.AreEqual(c.P.Y, r["P"].AsBsonDocument["Y"].AsInt32);
        }

        [Test]
        public void TestInsertForClassWithoutIdSucceeds()
        {
            collection.RemoveAll();
            var c = new C { P = new P { X = 1, Y = 2 } };
            collection.Insert(c);
            Assert.AreEqual(1, collection.Count());
            var r = collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(c.Id, r["_id"].AsObjectId);
            Assert.AreEqual(c.P.X, r["P"].AsBsonDocument["X"].AsInt32);
            Assert.AreEqual(c.P.Y, r["P"].AsBsonDocument["Y"].AsInt32);
        }

        [Test]
        public void TestInsertForStructWithIdSucceeds()
        {
            collection.RemoveAll();
            var s = new S { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            collection.Insert(s);
            Assert.AreEqual(1, collection.Count());
            var r = collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(s.Id, r["_id"].AsObjectId);
            Assert.AreEqual(s.P.X, r["P"].AsBsonDocument["X"].AsInt32);
            Assert.AreEqual(s.P.Y, r["P"].AsBsonDocument["Y"].AsInt32);
        }

        [Test]
        public void TestInsertForStructWithoutIdFails()
        {
            collection.RemoveAll();
            var s = new S { P = new P { X = 1, Y = 2 } };
            Assert.Throws<BsonSerializationException>(() => collection.Insert(s));
        }

        [Test]
        public void TestSaveForClassWithIdSucceeds()
        {
            collection.RemoveAll();
            var c = new C { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            collection.Save(c);
            Assert.AreEqual(1, collection.Count());
            var r = collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(c.Id, r["_id"].AsObjectId);
            Assert.AreEqual(c.P.X, r["P"].AsBsonDocument["X"].AsInt32);
            Assert.AreEqual(c.P.Y, r["P"].AsBsonDocument["Y"].AsInt32);
        }

        [Test]
        public void TestSaveForClassWithoutIdSucceeds()
        {
            collection.RemoveAll();
            var c = new C { P = new P { X = 1, Y = 2 } };
            collection.Save(c);
            Assert.AreEqual(1, collection.Count());
            var r = collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(c.Id, r["_id"].AsObjectId);
            Assert.AreEqual(c.P.X, r["P"].AsBsonDocument["X"].AsInt32);
            Assert.AreEqual(c.P.Y, r["P"].AsBsonDocument["Y"].AsInt32);
        }

        [Test]
        public void TestSaveForStructWithIdSucceeds()
        {
            collection.RemoveAll();
            var s = new S { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            collection.Save(s);
            Assert.AreEqual(1, collection.Count());
            var r = collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(s.Id, r["_id"].AsObjectId);
            Assert.AreEqual(s.P.X, r["P"].AsBsonDocument["X"].AsInt32);
            Assert.AreEqual(s.P.Y, r["P"].AsBsonDocument["Y"].AsInt32);
        }

        [Test]
        public void TestSaveForStructWithoutIdFails()
        {
            collection.RemoveAll();
            var s = new S { P = new P { X = 1, Y = 2 } };
            Assert.Throws<BsonSerializationException>(() => collection.Save(s));
        }
    }
}

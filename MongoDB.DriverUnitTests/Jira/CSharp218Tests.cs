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
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp218
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

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.TestCollection;
        }

        [Test]
        public void TestDeserializeClassWithStructPropertyFails()
        {
            _collection.RemoveAll();
            var c = new C { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            _collection.Insert(c);
            try
            {
                _collection.FindOneAs<C>();
                Assert.Fail("Expected an exception to be thrown.");
            }
            catch (Exception ex)
            {
                var expectedMessage = "An error occurred while deserializing the P field of class MongoDB.DriverUnitTests.Jira.CSharp218.CSharp218Tests+C: Value class MongoDB.DriverUnitTests.Jira.CSharp218.CSharp218Tests+P cannot be deserialized.";
                Assert.IsInstanceOf<FileFormatException>(ex);
                Assert.IsInstanceOf<BsonSerializationException>(ex.InnerException);
                Assert.AreEqual(expectedMessage, ex.Message);
            }
        }

        [Test]
        public void TestDeserializeStructFails()
        {
            _collection.RemoveAll();
            var s = new S { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            _collection.Insert(s);
            Assert.Throws<BsonSerializationException>(() => _collection.FindOneAs<S>());
        }

        [Test]
        public void TestInsertForClassWithIdSucceeds()
        {
            _collection.RemoveAll();
            var c = new C { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            _collection.Insert(c);
            Assert.AreEqual(1, _collection.Count());
            var r = _collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(c.Id, r["_id"].AsObjectId);
            Assert.AreEqual(c.P.X, r["P"]["X"].AsInt32);
            Assert.AreEqual(c.P.Y, r["P"]["Y"].AsInt32);
        }

        [Test]
        public void TestInsertForClassWithoutIdSucceeds()
        {
            _collection.RemoveAll();
            var c = new C { P = new P { X = 1, Y = 2 } };
            _collection.Insert(c);
            Assert.AreEqual(1, _collection.Count());
            var r = _collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(c.Id, r["_id"].AsObjectId);
            Assert.AreEqual(c.P.X, r["P"]["X"].AsInt32);
            Assert.AreEqual(c.P.Y, r["P"]["Y"].AsInt32);
        }

        [Test]
        public void TestInsertForStructWithIdSucceeds()
        {
            _collection.RemoveAll();
            var s = new S { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            _collection.Insert(s);
            Assert.AreEqual(1, _collection.Count());
            var r = _collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(s.Id, r["_id"].AsObjectId);
            Assert.AreEqual(s.P.X, r["P"]["X"].AsInt32);
            Assert.AreEqual(s.P.Y, r["P"]["Y"].AsInt32);
        }

        [Test]
        public void TestInsertForStructWithoutIdFails()
        {
            _collection.RemoveAll();
            var s = new S { P = new P { X = 1, Y = 2 } };
            Assert.Throws<BsonSerializationException>(() => _collection.Insert(s));
        }

        [Test]
        public void TestSaveForClassWithIdSucceeds()
        {
            _collection.RemoveAll();
            var c = new C { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            _collection.Save(c);
            Assert.AreEqual(1, _collection.Count());
            var r = _collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(c.Id, r["_id"].AsObjectId);
            Assert.AreEqual(c.P.X, r["P"]["X"].AsInt32);
            Assert.AreEqual(c.P.Y, r["P"]["Y"].AsInt32);
        }

        [Test]
        public void TestSaveForClassWithoutIdSucceeds()
        {
            _collection.RemoveAll();
            var c = new C { P = new P { X = 1, Y = 2 } };
            _collection.Save(c);
            Assert.AreEqual(1, _collection.Count());
            var r = _collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(c.Id, r["_id"].AsObjectId);
            Assert.AreEqual(c.P.X, r["P"]["X"].AsInt32);
            Assert.AreEqual(c.P.Y, r["P"]["Y"].AsInt32);
        }

        [Test]
        public void TestSaveForStructWithIdSucceeds()
        {
            _collection.RemoveAll();
            var s = new S { Id = ObjectId.GenerateNewId(), P = new P { X = 1, Y = 2 } };
            _collection.Save(s);
            Assert.AreEqual(1, _collection.Count());
            var r = _collection.FindOne();
            Assert.AreEqual(2, r.ElementCount);
            Assert.AreEqual(2, r["P"].AsBsonDocument.ElementCount);
            Assert.AreEqual(s.Id, r["_id"].AsObjectId);
            Assert.AreEqual(s.P.X, r["P"]["X"].AsInt32);
            Assert.AreEqual(s.P.Y, r["P"]["Y"].AsInt32);
        }

        [Test]
        public void TestSaveForStructWithoutIdFails()
        {
            _collection.RemoveAll();
            var s = new S { P = new P { X = 1, Y = 2 } };
            Assert.Throws<BsonSerializationException>(() => _collection.Save(s));
        }
    }
}

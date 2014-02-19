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
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp101
{
    [TestFixture]
    public class CSharp101Tests
    {
        private class CNoId
        {
            public int A;
        }

        private class CObjectId
        {
            public ObjectId Id;
            public int A;
        }

        private class CGuid
        {
            public Guid Id;
            public int A;
        }

        private class CInt32Id
        {
            public int Id;
            public int A;
        }

        private class CInt64Id
        {
            public long Id;
            public int A;
        }

        private class CStringId
        {
            public string Id;
            public int A;
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetUpAttribute()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.TestCollection;
        }

        [Test]
        public void TestBsonDocumentNoId()
        {
            _collection.RemoveAll();

            var document = new BsonDocument
            {
                { "A", 1 }
            };
            _collection.Save(document);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("_id", document.GetElement(0).Name);
            Assert.IsInstanceOf<BsonObjectId>(document["_id"]);
            Assert.AreNotEqual(ObjectId.Empty, document["_id"].AsObjectId);
            Assert.AreEqual(1, _collection.Count());

            var id = document["_id"].AsObjectId;
            document["A"] = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document["_id"].AsObjectId);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<BsonDocument>();
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(id, document["_id"].AsObjectId);
            Assert.AreEqual(2, document["A"].AsInt32);
        }

        [Test]
        public void TestBsonDocumentBsonNullId()
        {
            _collection.RemoveAll();

            var document = new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "A", 1 }
            };
            _collection.Save(document);
            Assert.AreEqual(1, _collection.Count());

            document["A"] = 2;
            _collection.Save(document);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<BsonDocument>();
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(BsonNull.Value, document["_id"].AsBsonNull);
            Assert.AreEqual(2, document["A"].AsInt32);
        }

        [Test]
        public void TestBsonDocumentEmptyObjectId()
        {
            _collection.RemoveAll();

            var document = new BsonDocument
            {
                { "_id", ObjectId.Empty },
                { "A", 1 }
            };
            _collection.Save(document);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("_id", document.GetElement(0).Name);
            Assert.IsInstanceOf<BsonObjectId>(document["_id"]);
            Assert.AreNotEqual(ObjectId.Empty, document["_id"].AsObjectId);
            Assert.AreEqual(1, _collection.Count());

            var id = document["_id"].AsObjectId;
            document["A"] = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document["_id"].AsObjectId);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<BsonDocument>();
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(id, document["_id"].AsObjectId);
            Assert.AreEqual(2, document["A"].AsInt32);
        }

        [Test]
        public void TestBsonDocumentGeneratedObjectId()
        {
            _collection.RemoveAll();

            var id = ObjectId.GenerateNewId();
            var document = new BsonDocument
            {
                { "_id", id },
                { "A", 1 }
            };
            _collection.Save(document);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("_id", document.GetElement(0).Name);
            Assert.IsInstanceOf<BsonObjectId>(document["_id"]);
            Assert.AreEqual(id, document["_id"].AsObjectId);
            Assert.AreEqual(1, _collection.Count());

            document["A"] = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document["_id"].AsObjectId);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<BsonDocument>();
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(id, document["_id"].AsObjectId);
            Assert.AreEqual(2, document["A"].AsInt32);
        }

        [Test]
        public void TestBsonDocumentEmptyGuid()
        {
            _collection.RemoveAll();

            var document = new BsonDocument
            {
                { "_id", Guid.Empty },
                { "A", 1 }
            };
            _collection.Save(document);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("_id", document.GetElement(0).Name);
            Assert.IsInstanceOf<BsonBinaryData>(document["_id"]);
            Assert.AreNotEqual(new BsonBinaryData(Guid.Empty), document["_id"]);
            Assert.AreEqual(1, _collection.Count());

            var id = document["_id"].AsGuid;
            document["A"] = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document["_id"].AsGuid);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<BsonDocument>();
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(id, document["_id"].AsGuid);
            Assert.AreEqual(2, document["A"].AsInt32);
        }

        [Test]
        public void TestBsonDocumentGeneratedGuid()
        {
            _collection.RemoveAll();

            var guid = Guid.NewGuid();
            var document = new BsonDocument
            {
                { "_id", guid },
                { "A", 1 }
            };
            _collection.Save(document);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("_id", document.GetElement(0).Name);
            Assert.IsInstanceOf<BsonBinaryData>(document["_id"]);
            Assert.AreEqual(guid, document["_id"].AsGuid);
            Assert.AreEqual(1, _collection.Count());

            var id = document["_id"].AsGuid;
            document["A"] = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document["_id"].AsGuid);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<BsonDocument>();
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(id, document["_id"].AsGuid);
            Assert.AreEqual(2, document["A"].AsInt32);
        }

        [Test]
        public void TestBsonDocumentInt32Id()
        {
            _collection.RemoveAll();

            var id = 123;
            var document = new BsonDocument
            {
                { "_id", id },
                { "A", 1 }
            };
            _collection.Save(document);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("_id", document.GetElement(0).Name);
            Assert.IsInstanceOf<BsonInt32>(document["_id"]);
            Assert.AreEqual(id, document["_id"].AsInt32);
            Assert.AreEqual(1, _collection.Count());

            document["A"] = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document["_id"].AsInt32);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<BsonDocument>();
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(id, document["_id"].AsInt32);
            Assert.AreEqual(2, document["A"].AsInt32);
        }

        [Test]
        public void TestBsonDocumentInt64Id()
        {
            _collection.RemoveAll();

            var id = 123L;
            var document = new BsonDocument
            {
                { "_id", id },
                { "A", 1 }
            };
            _collection.Save(document);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("_id", document.GetElement(0).Name);
            Assert.IsInstanceOf<BsonInt64>(document["_id"]);
            Assert.AreEqual(id, document["_id"].AsInt64);
            Assert.AreEqual(1, _collection.Count());

            document["A"] = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document["_id"].AsInt64);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<BsonDocument>();
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(id, document["_id"].AsInt64);
            Assert.AreEqual(2, document["A"].AsInt32);
        }

        [Test]
        public void TestBsonDocumentStringId()
        {
            _collection.RemoveAll();

            var id = "123";
            var document = new BsonDocument
            {
                { "_id", id },
                { "A", 1 }
            };
            _collection.Save(document);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("_id", document.GetElement(0).Name);
            Assert.IsInstanceOf<BsonString>(document["_id"]);
            Assert.AreEqual(id, document["_id"].AsString);
            Assert.AreEqual(1, _collection.Count());

            document["A"] = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document["_id"].AsString);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<BsonDocument>();
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual(id, document["_id"].AsString);
            Assert.AreEqual(2, document["A"].AsInt32);
        }

        [Test]
        public void TestCNoId()
        {
            _collection.RemoveAll();

            var document = new CNoId { A = 1 };
            Assert.Throws<InvalidOperationException>(() => _collection.Save(document));
        }

        [Test]
        public void TestCObjectIdEmpty()
        {
            _collection.RemoveAll();

            var document = new CObjectId { A = 1 };
            Assert.AreEqual(ObjectId.Empty, document.Id);
            _collection.Save(document);
            Assert.AreNotEqual(ObjectId.Empty, document.Id);
            Assert.AreEqual(1, _collection.Count());

            var id = document.Id;
            document.A = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<CObjectId>();
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(2, document.A);
        }

        [Test]
        public void TestCObjectIdGenerated()
        {
            _collection.RemoveAll();

            var id = ObjectId.GenerateNewId();
            var document = new CObjectId { Id = id, A = 1 };
            _collection.Save(document);
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(1, _collection.Count());

            document.A = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<CObjectId>();
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(2, document.A);
        }

        [Test]
        public void TestCGuidEmpty()
        {
            _collection.RemoveAll();

            var document = new CGuid { A = 1 };
            Assert.AreEqual(Guid.Empty, document.Id);
            _collection.Save(document);
            Assert.AreNotEqual(Guid.Empty, document.Id);
            Assert.AreEqual(1, _collection.Count());

            var id = document.Id;
            document.A = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<CGuid>();
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(2, document.A);
        }

        [Test]
        public void TestCGuidGenerated()
        {
            _collection.RemoveAll();

            var id = Guid.NewGuid();
            var document = new CGuid { Id = id, A = 1 };
            _collection.Save(document);
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(1, _collection.Count());

            document.A = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<CGuid>();
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(2, document.A);
        }

        [Test]
        public void TestCInt32Id()
        {
            _collection.RemoveAll();

            var id = 123;
            var document = new CInt32Id { Id = id, A = 1 };
            _collection.Save(document);
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(1, _collection.Count());

            document.A = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<CInt32Id>();
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(2, document.A);
        }

        [Test]
        public void TestCInt64Id()
        {
            _collection.RemoveAll();

            var id = 123L;
            var document = new CInt64Id { Id = id, A = 1 };
            _collection.Save(document);
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(1, _collection.Count());

            document.A = 2;
            _collection.Save(document);
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<CInt64Id>();
            Assert.AreEqual(id, document.Id);
            Assert.AreEqual(2, document.A);
        }

        [Test]
        public void TestCStringId()
        {
            _collection.RemoveAll();

            var document = new CStringId { Id = null, A = 1 };
            Assert.Throws<InvalidOperationException>(() => _collection.Save(document)); // Id is null

            document = new CStringId { Id = "123", A = 1 };
            _collection.Save(document);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<CStringId>();
            Assert.AreEqual("123", document.Id);
            Assert.AreEqual(1, document.A);

            document.A = 2;
            _collection.Save(document);
            Assert.AreEqual(1, _collection.Count());

            document = _collection.FindOneAs<CStringId>();
            Assert.AreEqual("123", document.Id);
            Assert.AreEqual(2, document.A);
        }
    }
}

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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverUnitTests.Jira.CSharp231
{
    [TestFixture]
    public class CSharp231Tests
    {
        public class ClassWithArrayId
        {
            public int[] Id;
            public int X;
        }

        public class ClassWithBooleanId
        {
            public bool Id;
            public int X;
        }

        public class ClassWithBsonArrayId
        {
            public BsonArray Id;
            public int X;
        }

        public class ClassWithBsonBinaryDataId
        {
            public BsonBinaryData Id;
            public int X;
        }

        public class ClassWithBsonBooleanId
        {
            public BsonBoolean Id;
            public int X;
        }

        public class ClassWithBsonDateTimeId
        {
            public BsonDateTime Id;
            public int X;
        }

        public class ClassWithBsonDocumentId
        {
            public BsonDocument Id;
            public int X;
        }

        public class ClassWithBsonDoubleId
        {
            public BsonDouble Id;
            public int X;
        }

        public class ClassWithBsonInt32Id
        {
            public BsonInt32 Id;
            public int X;
        }

        public class ClassWithBsonInt64Id
        {
            public BsonInt64 Id;
            public int X;
        }

        public class ClassWithBsonMaxKeyId
        {
            public BsonMaxKey Id;
            public int X;
        }

        public class ClassWithBsonMinKeyId
        {
            public BsonMinKey Id;
            public int X;
        }

        public class ClassWithBsonNullId
        {
            public BsonNull Id;
            public int X;
        }

        public class ClassWithBsonObjectId
        {
            public BsonObjectId Id;
            public int X;
        }

        public class ClassWithBsonStringId
        {
            public BsonString Id;
            public int X;
        }

        public class ClassWithBsonTimestampId
        {
            public BsonTimestamp Id;
            public int X;
        }

        public class ClassWithBsonValueId
        {
            public BsonValue Id;
            public int X;
        }

        public class ClassWithDateTimeId
        {
            public DateTime Id;
            public int X;
        }

        public class ClassWithDoubleId
        {
            public double Id;
            public int X;
        }

        public class ClassWithInt32Id
        {
            public int Id;
            public int X;
        }

        public class ClassWithInt64Id
        {
            public long Id;
            public int X;
        }

        public class ClassWithObjectId
        {
            public ObjectId Id;
            public int X;
        }

        public class ClassWithStringId
        {
            public string Id;
            public int X;
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
        public void TestBsonDocumentWithBsonArrayId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonArray() }, { "X", 1 } };
            Assert.Throws<WriteConcernException>(() => { _collection.Insert(doc); });

            doc = new BsonDocument { { "_id", new BsonArray { 1, 2, 3 } }, { "X", 1 } };
            Assert.Throws<WriteConcernException>(() => { _collection.Insert(doc); });
        }

        [Test]
        public void TestBsonDocumentWithBsonBinaryDataId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonBinaryData.Create(new byte[] { }) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", BsonBinaryData.Create(new byte[] { 1, 2, 3 }) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonBooleanId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonBoolean.Create(false) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", BsonBoolean.Create(true) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonDateTimeId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonDateTime.Create(DateTime.MinValue) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", BsonDateTime.Create(DateTime.UtcNow) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", BsonDateTime.Create(DateTime.MaxValue) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonDocumentId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonDocument() }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", new BsonDocument { { "A", 1 }, { "B", 2 } } }, { "X", 3 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonDoubleId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonDouble.Create(0.0) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", BsonDouble.Create(1.0) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonInt32Id()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonInt32.Create(0) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", BsonInt32.Create(1) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonInt64Id()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonInt64.Create(0) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", BsonInt64.Create(1) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonMaxKeyId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonMaxKey.Value }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonMinKeyId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonMinKey.Value }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonNullId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonNull.Value }, { "X", 1 } };
            _collection.Insert(doc);
            Assert.AreEqual(BsonNull.Value, doc["_id"]);
        }

        [Test]
        public void TestBsonDocumentWithBsonObjectId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonNull.Value }, { "X", 1 } };
            _collection.Insert(doc);
            Assert.AreEqual(BsonNull.Value, doc["_id"]);

            doc = new BsonDocument { { "_id", BsonObjectId.Empty }, { "X", 1 } };
            _collection.Insert(doc);
            Assert.AreNotEqual(ObjectId.Empty, doc["_id"].AsObjectId);

            doc = new BsonDocument { { "_id", BsonObjectId.GenerateNewId() }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonStringId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonString.Create("") }, { "X", 1 } };
            _collection.Insert(doc);
            Assert.AreEqual("", doc["_id"].AsString);

            doc = new BsonDocument { { "_id", BsonString.Create("123") }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithBsonTimestampId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonTimestamp.Create(0, 0) }, { "X", 1 } };
            _collection.Insert(doc);
            Assert.AreEqual(BsonTimestamp.Create(0, 0), doc["_id"].AsBsonTimestamp);

            doc = new BsonDocument { { "_id", BsonTimestamp.Create(1, 2) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Test]
        public void TestBsonDocumentWithNoId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "X", 1 } };
            _collection.Insert(doc);
            Assert.IsInstanceOf<BsonObjectId>(doc["_id"]);
            Assert.AreNotEqual(ObjectId.Empty, doc["_id"].AsObjectId);
        }

        [Test]
        public void TestClassWithArrayId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithArrayId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithArrayId { Id = new int[] { }, X = 1 };
            Assert.Throws<WriteConcernException>(() => { _collection.Insert(doc); });

            doc = new ClassWithArrayId { Id = new int[] { 1, 2, 3 }, X = 1 };
            Assert.Throws<WriteConcernException>(() => { _collection.Insert(doc); });
        }

        [Test]
        public void TestClassWithBooleanId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBooleanId { Id = false, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBooleanId { Id = true, X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonArrayId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonArrayId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonArrayId { Id = new BsonArray(), X = 1 };
            Assert.Throws<WriteConcernException>(() => { _collection.Insert(doc); });

            doc = new ClassWithBsonArrayId { Id = new BsonArray { 1, 2, 3 }, X = 1 };
            Assert.Throws<WriteConcernException>(() => { _collection.Insert(doc); });
        }

        [Test]
        public void TestClastWithBsonBinaryDataId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonBinaryDataId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonBinaryDataId { Id = BsonBinaryData.Create(new byte[] { }), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonBinaryDataId { Id = BsonBinaryData.Create(new byte[] { 1, 2, 3 }), X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonBooleanId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonBooleanId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonBooleanId { Id = BsonBoolean.Create(false), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonBooleanId { Id = BsonBoolean.Create(true), X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonDocumentId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonDocumentId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDocumentId { Id = new BsonDocument(), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDocumentId { Id = new BsonDocument { { "A", 1 }, { "B", 2 } }, X = 3 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonDateTimeId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonDateTimeId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDateTimeId { Id = BsonDateTime.Create(DateTime.MinValue), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDateTimeId { Id = BsonDateTime.Create(DateTime.UtcNow), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDateTimeId { Id = BsonDateTime.Create(DateTime.MaxValue), X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonDoubleId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonDoubleId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDoubleId { Id = BsonDouble.Create(0.0), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDoubleId { Id = BsonDouble.Create(1.0), X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonInt32Id()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonInt32Id { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonInt32Id { Id = BsonInt32.Create(0), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonInt32Id { Id = BsonInt32.Create(1), X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonInt64Id()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonInt64Id { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonInt64Id { Id = BsonInt64.Create(0), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonInt64Id { Id = BsonInt64.Create(1), X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonMaxKeyId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonMaxKeyId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.AreEqual(null, doc.Id);

            doc = new ClassWithBsonMaxKeyId { Id = BsonMaxKey.Value, X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonMinKeyId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonMinKeyId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.AreEqual(null, doc.Id);

            doc = new ClassWithBsonMinKeyId { Id = BsonMinKey.Value, X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonNullId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonNullId { Id = null, X = 1 };
            _collection.Insert(doc); // serializes _id as { "_id" : { "_csharpnull" : true }, "X" : 1 }
            Assert.AreEqual(null, doc.Id);

            doc = new ClassWithBsonNullId { Id = BsonNull.Value, X = 1 };
            _collection.Insert(doc); // serializes _id as { "_id" : null, "X" : 1 }
            Assert.AreEqual(BsonNull.Value, doc.Id);
        }

        [Test]
        public void TestClassWithBsonObjectId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonObjectId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.IsNotNull(doc.Id);
            Assert.AreNotEqual(ObjectId.Empty, doc.Id.AsObjectId);

            doc = new ClassWithBsonObjectId { Id = BsonObjectId.Empty, X = 1 };
            _collection.Insert(doc);
            Assert.AreNotEqual(ObjectId.Empty, doc.Id.AsObjectId);

            doc = new ClassWithBsonObjectId { Id = BsonObjectId.GenerateNewId(), X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonStringId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonStringId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.IsNull(doc.Id);

            doc = new ClassWithBsonStringId { Id = "", X = 1 };
            _collection.Insert(doc);
            Assert.AreEqual("", doc.Id.AsString);

            doc = new ClassWithBsonStringId { Id = "123", X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonTimestampId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonTimestampId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.IsNull(doc.Id);

            doc = new ClassWithBsonTimestampId { Id = BsonTimestamp.Create(0, 0), X = 1 };
            _collection.Insert(doc);
            Assert.AreEqual(BsonTimestamp.Create(0, 0), doc.Id);

            doc = new ClassWithBsonTimestampId { Id = BsonTimestamp.Create(1, 2), X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithBsonValueId()
        {
            // repeats all tee TestClassWithBsonXyzId tests using ClassWithBsonValueId
            {
                // same as TestClassWithBonArrayId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonArray(), X = 1 };
                Assert.Throws<WriteConcernException>(() => { _collection.Insert(doc); });

                doc = new ClassWithBsonValueId { Id = new BsonArray { 1, 2, 3 }, X = 1 };
                Assert.Throws<WriteConcernException>(() => { _collection.Insert(doc); });
            }

            {
                // same as TestClastWithBsonBinaryDataId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonBinaryData.Create(new byte[] { }), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonBinaryData.Create(new byte[] { 1, 2, 3 }), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonBooleanId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonBoolean.Create(false), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonBoolean.Create(true), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonDocumentId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonDocument(), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonDocument { { "A", 1 }, { "B", 2 } }, X = 3 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonDateTimeId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonDateTime.Create(DateTime.MinValue), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonDateTime.Create(DateTime.UtcNow), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonDateTime.Create(DateTime.MaxValue), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonDoubleId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonDouble.Create(0.0), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonDouble.Create(1.0), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonInt32Id
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonInt32.Create(0), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonInt32.Create(1), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonInt64Id
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonInt64.Create(0), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonInt64.Create(1), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonMaxKeyId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.AreEqual(null, doc.Id);

                doc = new ClassWithBsonValueId { Id = BsonMaxKey.Value, X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonMinKeyId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.AreEqual(null, doc.Id);

                doc = new ClassWithBsonValueId { Id = BsonMinKey.Value, X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonNullId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.AreEqual(null, doc.Id);

                doc = new ClassWithBsonValueId { Id = BsonNull.Value, X = 1 };
                Assert.Throws<WriteConcernException>(() => { _collection.Insert(doc); });
            }

            {
                // same as TestClassWithBsonObjectId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.IsNull(doc.Id); // BsonObjectIdGenerator is not invoked when nominalType is BsonValue

                doc = new ClassWithBsonValueId { Id = BsonObjectId.Empty, X = 1 };
                _collection.Insert(doc);
                Assert.AreEqual(ObjectId.Empty, doc.Id.AsObjectId); // BsonObjectIdGenerator is not invoked when nominalType is BsonValue

                doc = new ClassWithBsonValueId { Id = BsonObjectId.GenerateNewId(), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonStringId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.IsNull(doc.Id);

                doc = new ClassWithBsonValueId { Id = "", X = 1 };
                _collection.Insert(doc);
                Assert.AreEqual("", doc.Id.AsString);

                doc = new ClassWithBsonValueId { Id = "123", X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonTimestampId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.IsNull(doc.Id);

                doc = new ClassWithBsonValueId { Id = BsonTimestamp.Create(0, 0), X = 1 };
                _collection.Insert(doc);
                Assert.AreEqual(BsonTimestamp.Create(0, 0), doc.Id);

                doc = new ClassWithBsonValueId { Id = BsonTimestamp.Create(1, 2), X = 1 };
                _collection.Insert(doc);
            }
        }

        [Test]
        public void TestClassWithDateTimeId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithDateTimeId { Id = DateTime.MinValue, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithDateTimeId { Id = DateTime.UtcNow, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithDateTimeId { Id = DateTime.MaxValue, X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithDoubleId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithDoubleId { Id = 0.0, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithDoubleId { Id = 1.0, X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithInt32Id()
        {
            _collection.RemoveAll();

            var doc = new ClassWithInt32Id { Id = 0, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithInt32Id { Id = 1, X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithInt64Id()
        {
            _collection.RemoveAll();

            var doc = new ClassWithInt64Id { Id = 0, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithInt64Id { Id = 1, X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithObjectId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithObjectId { Id = ObjectId.Empty, X = 1 };
            _collection.Insert(doc);
            Assert.AreNotEqual(ObjectId.Empty, doc.Id);

            doc = new ClassWithObjectId { Id = ObjectId.GenerateNewId(), X = 1 };
            _collection.Insert(doc);
        }

        [Test]
        public void TestClassWithStringId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithStringId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.IsNull(doc.Id);

            doc = new ClassWithStringId { Id = "", X = 1 };
            _collection.Insert(doc);
            Assert.AreEqual("", doc.Id);

            doc = new ClassWithStringId { Id = "123", X = 1 };
            _collection.Insert(doc);
        }
    }
}

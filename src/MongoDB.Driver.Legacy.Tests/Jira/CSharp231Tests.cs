/* Copyright 2010-2016 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp231
{
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

        private MongoCollection<BsonDocument> _collection;

        public CSharp231Tests()
        {
            _collection = LegacyTestConfiguration.Collection;
        }

        [Fact]
        public void TestBsonDocumentWithBsonArrayId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonArray() }, { "X", 1 } };
            Assert.Throws<MongoWriteConcernException>(() => { _collection.Insert(doc); });

            doc = new BsonDocument { { "_id", new BsonArray { 1, 2, 3 } }, { "X", 1 } };
            Assert.Throws<MongoWriteConcernException>(() => { _collection.Insert(doc); });
        }

        [Fact]
        public void TestBsonDocumentWithBsonBinaryDataId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonBinaryData(new byte[] { }) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", new BsonBinaryData(new byte[] { 1, 2, 3 }) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonBooleanId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonBoolean.False }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", BsonBoolean.True }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonDateTimeId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonDateTime(DateTime.MinValue) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", new BsonDateTime(DateTime.UtcNow) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", new BsonDateTime(DateTime.MaxValue) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonDocumentId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonDocument() }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", new BsonDocument { { "A", 1 }, { "B", 2 } } }, { "X", 3 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonDoubleId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonDouble(0.0) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", new BsonDouble(1.0) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonInt32Id()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonInt32(0) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", new BsonInt32(1) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonInt64Id()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonInt64(0) }, { "X", 1 } };
            _collection.Insert(doc);

            doc = new BsonDocument { { "_id", new BsonInt64(1) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonMaxKeyId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonMaxKey.Value }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonMinKeyId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonMinKey.Value }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonNullId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonNull.Value }, { "X", 1 } };
            _collection.Insert(doc);
            Assert.Equal(BsonNull.Value, doc["_id"]);
        }

        [Fact]
        public void TestBsonDocumentWithBsonObjectId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", BsonNull.Value }, { "X", 1 } };
            _collection.Insert(doc);
            Assert.Equal(BsonNull.Value, doc["_id"]);

            doc = new BsonDocument { { "_id", ObjectId.Empty }, { "X", 1 } };
            _collection.Insert(doc);
            Assert.NotEqual(ObjectId.Empty, doc["_id"].AsObjectId);

            doc = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonStringId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonString("") }, { "X", 1 } };
            _collection.Insert(doc);
            Assert.Equal("", doc["_id"].AsString);

            doc = new BsonDocument { { "_id", new BsonString("123") }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithBsonTimestampId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "_id", new BsonTimestamp(1, 2) }, { "X", 1 } };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestBsonDocumentWithNoId()
        {
            _collection.RemoveAll();

            var doc = new BsonDocument { { "X", 1 } };
            _collection.Insert(doc);
            Assert.IsType<BsonObjectId>(doc["_id"]);
            Assert.NotEqual(ObjectId.Empty, doc["_id"].AsObjectId);
        }

        [Fact]
        public void TestClassWithArrayId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithArrayId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithArrayId { Id = new int[] { }, X = 1 };
            Assert.Throws<MongoWriteConcernException>(() => { _collection.Insert(doc); });

            doc = new ClassWithArrayId { Id = new int[] { 1, 2, 3 }, X = 1 };
            Assert.Throws<MongoWriteConcernException>(() => { _collection.Insert(doc); });
        }

        [Fact]
        public void TestClassWithBooleanId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBooleanId { Id = false, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBooleanId { Id = true, X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonArrayId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonArrayId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonArrayId { Id = new BsonArray(), X = 1 };
            Assert.Throws<MongoWriteConcernException>(() => { _collection.Insert(doc); });

            doc = new ClassWithBsonArrayId { Id = new BsonArray { 1, 2, 3 }, X = 1 };
            Assert.Throws<MongoWriteConcernException>(() => { _collection.Insert(doc); });
        }

        [Fact]
        public void TestClastWithBsonBinaryDataId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonBinaryDataId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonBinaryDataId { Id = new BsonBinaryData(new byte[] { }), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonBinaryDataId { Id = new BsonBinaryData(new byte[] { 1, 2, 3 }), X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonBooleanId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonBooleanId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonBooleanId { Id = BsonBoolean.False, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonBooleanId { Id = BsonBoolean.True, X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
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

        [Fact]
        public void TestClassWithBsonDateTimeId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonDateTimeId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDateTimeId { Id = new BsonDateTime(DateTime.MinValue), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDateTimeId { Id = new BsonDateTime(DateTime.UtcNow), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDateTimeId { Id = new BsonDateTime(DateTime.MaxValue), X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonDoubleId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonDoubleId { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDoubleId { Id = new BsonDouble(0.0), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonDoubleId { Id = new BsonDouble(1.0), X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonInt32Id()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonInt32Id { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonInt32Id { Id = new BsonInt32(0), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonInt32Id { Id = new BsonInt32(1), X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonInt64Id()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonInt64Id { Id = null, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonInt64Id { Id = new BsonInt64(0), X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithBsonInt64Id { Id = new BsonInt64(1), X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonMaxKeyId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonMaxKeyId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.Equal(null, doc.Id);

            doc = new ClassWithBsonMaxKeyId { Id = BsonMaxKey.Value, X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonMinKeyId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonMinKeyId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.Equal(null, doc.Id);

            doc = new ClassWithBsonMinKeyId { Id = BsonMinKey.Value, X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonNullId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonNullId { Id = null, X = 1 };
            _collection.Insert(doc); // serializes _id as { "_id" : { "_csharpnull" : true }, "X" : 1 }
            Assert.Equal(null, doc.Id);

            doc = new ClassWithBsonNullId { Id = BsonNull.Value, X = 1 };
            _collection.Insert(doc); // serializes _id as { "_id" : null, "X" : 1 }
            Assert.Equal(BsonNull.Value, doc.Id);
        }

        [Fact]
        public void TestClassWithBsonObjectId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonObjectId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.NotNull(doc.Id);
            Assert.NotEqual(ObjectId.Empty, doc.Id.AsObjectId);

            doc = new ClassWithBsonObjectId { Id = ObjectId.Empty, X = 1 };
            _collection.Insert(doc);
            Assert.NotEqual(ObjectId.Empty, doc.Id.AsObjectId);

            doc = new ClassWithBsonObjectId { Id = ObjectId.GenerateNewId(), X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonStringId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonStringId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.Null(doc.Id);

            doc = new ClassWithBsonStringId { Id = "", X = 1 };
            _collection.Insert(doc);
            Assert.Equal("", doc.Id.AsString);

            doc = new ClassWithBsonStringId { Id = "123", X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonTimestampId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithBsonTimestampId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.Null(doc.Id);

            doc = new ClassWithBsonTimestampId { Id = new BsonTimestamp(1, 2), X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithBsonValueId()
        {
            // repeats all tee TestClassWithBsonXyzId tests using ClassWithBsonValueId
            {
                // same as TestClassWithBonArrayId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonArray(), X = 1 };
                Assert.Throws<MongoWriteConcernException>(() => { _collection.Insert(doc); });

                doc = new ClassWithBsonValueId { Id = new BsonArray { 1, 2, 3 }, X = 1 };
                Assert.Throws<MongoWriteConcernException>(() => { _collection.Insert(doc); });
            }

            {
                // same as TestClastWithBsonBinaryDataId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonBinaryData(new byte[] { }), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonBinaryData(new byte[] { 1, 2, 3 }), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonBooleanId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonBoolean.False, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = BsonBoolean.True, X = 1 };
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

                doc = new ClassWithBsonValueId { Id = new BsonDateTime(DateTime.MinValue), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonDateTime(DateTime.UtcNow), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonDateTime(DateTime.MaxValue), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonDoubleId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonDouble(0.0), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonDouble(1.0), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonInt32Id
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonInt32(0), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonInt32(1), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonInt64Id
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonInt64(0), X = 1 };
                _collection.Insert(doc);

                doc = new ClassWithBsonValueId { Id = new BsonInt64(1), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonMaxKeyId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.Equal(null, doc.Id);

                doc = new ClassWithBsonValueId { Id = BsonMaxKey.Value, X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonMinKeyId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.Equal(null, doc.Id);

                doc = new ClassWithBsonValueId { Id = BsonMinKey.Value, X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonNullId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.Equal(null, doc.Id);

                doc = new ClassWithBsonValueId { Id = BsonNull.Value, X = 1 };
                _collection.Insert(doc);
                Assert.Equal(BsonNull.Value, doc.Id);
            }

            {
                // same as TestClassWithBsonObjectId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.Null(doc.Id); // BsonObjectIdGenerator is not invoked when nominalType is BsonValue

                doc = new ClassWithBsonValueId { Id = ObjectId.Empty, X = 1 };
                _collection.Insert(doc);
                Assert.Equal(ObjectId.Empty, doc.Id.AsObjectId); // BsonObjectIdGenerator is not invoked when nominalType is BsonValue

                doc = new ClassWithBsonValueId { Id = ObjectId.GenerateNewId(), X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonStringId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.Null(doc.Id);

                doc = new ClassWithBsonValueId { Id = "", X = 1 };
                _collection.Insert(doc);
                Assert.Equal("", doc.Id.AsString);

                doc = new ClassWithBsonValueId { Id = "123", X = 1 };
                _collection.Insert(doc);
            }

            {
                // same as TestClassWithBsonTimestampId
                _collection.RemoveAll();

                var doc = new ClassWithBsonValueId { Id = null, X = 1 };
                _collection.Insert(doc);
                Assert.Null(doc.Id);

                doc = new ClassWithBsonValueId { Id = new BsonTimestamp(1, 2), X = 1 };
                _collection.Insert(doc);
            }
        }

        [Fact]
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

        [Fact]
        public void TestClassWithDoubleId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithDoubleId { Id = 0.0, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithDoubleId { Id = 1.0, X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithInt32Id()
        {
            _collection.RemoveAll();

            var doc = new ClassWithInt32Id { Id = 0, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithInt32Id { Id = 1, X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithInt64Id()
        {
            _collection.RemoveAll();

            var doc = new ClassWithInt64Id { Id = 0, X = 1 };
            _collection.Insert(doc);

            doc = new ClassWithInt64Id { Id = 1, X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithObjectId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithObjectId { Id = ObjectId.Empty, X = 1 };
            _collection.Insert(doc);
            Assert.NotEqual(ObjectId.Empty, doc.Id);

            doc = new ClassWithObjectId { Id = ObjectId.GenerateNewId(), X = 1 };
            _collection.Insert(doc);
        }

        [Fact]
        public void TestClassWithStringId()
        {
            _collection.RemoveAll();

            var doc = new ClassWithStringId { Id = null, X = 1 };
            _collection.Insert(doc);
            Assert.Null(doc.Id);

            doc = new ClassWithStringId { Id = "", X = 1 };
            _collection.Insert(doc);
            Assert.Equal("", doc.Id);

            doc = new ClassWithStringId { Id = "123", X = 1 };
            _collection.Insert(doc);
        }
    }
}

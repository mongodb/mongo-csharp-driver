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
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Operations
{
    [TestFixture]
    public class BulkWriteOperationTests
    {
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _collection = Configuration.TestCollection;
        }

        [Test]
        public void TestDelete()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 3)).RemoveOne();
            bulk.Execute();

            Assert.AreEqual(1, _collection.Count());
            Assert.AreEqual(2, _collection.FindOne()["x"].ToInt32());
        }

        [Test]
        public void TestExecuteTwice()
        {
            _collection.Drop();
            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument());
            bulk.Execute();
            Assert.Throws<InvalidOperationException>(() => bulk.Execute());
        }

        [Test]
        public void TestExecuteWithExplicitWriteConcern()
        {
            _collection.Drop();
            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Execute(WriteConcern.W1);
            Assert.AreEqual(1, _collection.Count());
        }

        [Test]
        public void TestExecuteWithNoRequests()
        {
            var bulk = _collection.InitializeOrderedBulkOperation();
            Assert.Throws<InvalidOperationException>(() => bulk.Execute());
        }

        [Test]
        public void TestFindAfterExecute()
        {
            _collection.Drop();
            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Execute();
            Assert.Throws<InvalidOperationException>(() => bulk.Find(new QueryDocument()));
        }

        [Test]
        public void TestFindWithNullQuery()
        {
            var bulk = _collection.InitializeOrderedBulkOperation();
            Assert.Throws<ArgumentNullException>(() => bulk.Find(null));
        }

        [Test]
        public void TestInsert()
        {
            _collection.Drop();
            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Insert(new BsonDocument("x", 2));
            bulk.Insert(new BsonDocument("x", 3));
            bulk.Execute();

            Assert.AreEqual(3, _collection.Count());
        }

        [Test]
        public void TestInsertAfterExecute()
        {
            _collection.Drop();
            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Execute();
            Assert.Throws<InvalidOperationException>(() => bulk.Insert(new BsonDocument()));
        }

        [Test]
        public void TestMixed()
        {
            _collection.Drop();
            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Insert(new BsonDocument("x", 2));
            bulk.Insert(new BsonDocument("x", 3));
            bulk.Insert(new BsonDocument("x", 4));
            bulk.Find(Query.GT("x", 2)).Update(Update.Inc("x", 10));
            bulk.Find(Query.EQ("x", 13)).RemoveOne();
            bulk.Find(Query.EQ("x", 14)).RemoveOne();
            bulk.Execute();

            Assert.AreEqual(2, _collection.Count());
        }

        [Test]
        public void TestMixedOrdered()
        {
            _collection.Drop();
            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Execute();

            Assert.AreEqual(1, _collection.Count());
        }

        [Test]
        public void TestMixedUnordered()
        {
            _collection.Drop();
            var bulk = _collection.InitializeUnorderedBulkOperation();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Execute();

            Assert.AreEqual(0, _collection.Count());
        }

        [Test]
        public void TestUpdate()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("x", 1));
            _collection.Insert(new BsonDocument("x", 2));
            _collection.Insert(new BsonDocument("x", 3));

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Find(Query.GT("x", 0)).Update(Update.Set("z", 1));
            bulk.Find(Query.EQ("x", 3)).UpdateOne(Update.Set("z", 3));
            bulk.Find(Query.EQ("x", 4)).Upsert().UpdateOne(Update.Set("z", 4));
            bulk.Execute();

            Assert.AreEqual(4, _collection.Count());
            foreach (var document in _collection.FindAll())
            {
                var x = document["x"].ToInt32();
                var z = document["z"].ToInt32();
                var expected = (x == 2) ? 1 : x;
                Assert.AreEqual(expected, z);
            }
        }
    }
}

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
using System.Linq;
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
        [TestCase(false)]
        [TestCase(true)]
        public void TestDeleteMultiple(bool ordered)
        {
            var documents = new BsonDocument[]
            {
                new BsonDocument("_id", 1),
                new BsonDocument("_id", 2),
                new BsonDocument("_id", 3)
            };

            _collection.Drop();
            _collection.Insert(documents[0]);
            _collection.Insert(documents[1]);
            _collection.Insert(documents[2]);

            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            bulk.Find(Query.EQ("_id", 1)).RemoveOne();
            bulk.Find(Query.EQ("_id", 3)).RemoveOne();
            var result = bulk.Execute();

            Assert.AreEqual(2, result.DeletedCount);
            Assert.AreEqual(0, result.InsertedCount);
            Assert.AreEqual(true, result.IsAcknowledged);
            Assert.AreEqual(0, result.MatchedCount);
            Assert.AreEqual(0, result.ModifiedCount);
            Assert.AreEqual(2, result.ProcessedRequests.Count);
            Assert.AreEqual(2, result.RequestCount);
            Assert.AreEqual(0, result.Upserts.Count);

            Assert.AreEqual(1, _collection.Count());
            Assert.AreEqual(documents[1], _collection.FindOne());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestExecuteTwice(bool ordered)
        {
            _collection.Drop();
            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            bulk.Insert(new BsonDocument());
            bulk.Execute();
            Assert.Throws<InvalidOperationException>(() => bulk.Execute());
        }

        [Test]
        [TestCase(false, 0)]
        [TestCase(false, 1)]
        [TestCase(true, 0)]
        [TestCase(true, 1)]
        public void TestExecuteWithExplicitWriteConcern(bool ordered, int w)
        {
            var document = new BsonDocument("_id", 1);

            _collection.Drop();
            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            bulk.Insert(document);
            var result = bulk.Execute(new WriteConcern { W = w });

            var isAcknowledged = (w > 0);
            if (isAcknowledged)
            {
                Assert.AreEqual(0, result.DeletedCount);
                Assert.AreEqual(1, result.InsertedCount);
                Assert.AreEqual(0, result.MatchedCount);
                Assert.AreEqual(0, result.ModifiedCount);
                Assert.AreEqual(0, result.Upserts.Count);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => { var x = result.DeletedCount; });
                Assert.Throws<InvalidOperationException>(() => { var x = result.InsertedCount; });
                Assert.Throws<InvalidOperationException>(() => { var x = result.MatchedCount; });
                Assert.Throws<InvalidOperationException>(() => { var x = result.ModifiedCount; });
                Assert.Throws<InvalidOperationException>(() => { var x = result.Upserts.Count; });
            }
            Assert.AreEqual(isAcknowledged, result.IsAcknowledged);
            Assert.AreEqual(1, result.ProcessedRequests.Count);
            Assert.AreEqual(1, result.RequestCount);

            Assert.AreEqual(1, _collection.Count());
            Assert.AreEqual(document, _collection.FindOne());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestExecuteWithNoRequests(bool ordered)
        {
            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            Assert.Throws<InvalidOperationException>(() => bulk.Execute());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestFindAfterExecute(bool ordered)
        {
            _collection.Drop();
            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Execute();
            Assert.Throws<InvalidOperationException>(() => bulk.Find(new QueryDocument()));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestFindWithNullQuery(bool ordered)
        {
            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            Assert.Throws<ArgumentNullException>(() => bulk.Find(null));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestInsertDoesNotAllowInvalidFieldNames(bool ordered)
        {
            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            bulk.Insert(new BsonDocument("$key", 1));
            Assert.Throws<BsonSerializationException>(() => bulk.Execute());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestInsertMultiple(bool ordered)
        {
            var documents = new BsonDocument[]
            {
                new BsonDocument("_id", 1),
                new BsonDocument("_id", 2),
                new BsonDocument("_id", 3)
            };

            _collection.Drop();
            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            bulk.Insert(documents[0]);
            bulk.Insert(documents[1]);
            bulk.Insert(documents[2]);
            var result = bulk.Execute();

            Assert.AreEqual(0, result.DeletedCount);
            Assert.AreEqual(3, result.InsertedCount);
            Assert.AreEqual(true, result.IsAcknowledged);
            Assert.AreEqual(0, result.MatchedCount);
            Assert.AreEqual(0, result.ModifiedCount);
            Assert.AreEqual(3, result.ProcessedRequests.Count);
            Assert.AreEqual(3, result.RequestCount);
            Assert.AreEqual(0, result.Upserts.Count);

            Assert.AreEqual(3, _collection.Count());
            Assert.IsTrue(documents.SequenceEqual(_collection.FindAll().SetSortOrder("_id")));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestInsertOne(bool ordered)
        {
            var document = new BsonDocument("_id", 1);

            _collection.Drop();
            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            bulk.Insert(document);
            var result = bulk.Execute();

            Assert.AreEqual(0, result.DeletedCount);
            Assert.AreEqual(1, result.InsertedCount);
            Assert.AreEqual(true, result.IsAcknowledged);
            Assert.AreEqual(0, result.MatchedCount);
            Assert.AreEqual(0, result.ModifiedCount);
            Assert.AreEqual(1, result.ProcessedRequests.Count);
            Assert.AreEqual(1, result.RequestCount);
            Assert.AreEqual(0, result.Upserts.Count);

            Assert.AreEqual(1, _collection.Count());
            Assert.AreEqual(document, _collection.FindOne());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestInsertAfterExecute(bool ordered)
        {
            _collection.Drop();
            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Execute();
            Assert.Throws<InvalidOperationException>(() => bulk.Insert(new BsonDocument()));
        }

        [Test]
        public void TestMixedOrdered()
        {
            var documents = new BsonDocument[]
            {
                new BsonDocument { { "_id", 1 }, { "x", 1 } },
                new BsonDocument { { "_id", 2 }, { "x", 2 } },
                new BsonDocument { { "_id", 3 }, { "x", 3 } },
                new BsonDocument { { "_id", 4 }, { "x", 4 } }
            };

            _collection.Drop();
            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(documents[0]);
            bulk.Insert(documents[1]);
            bulk.Insert(documents[2]);
            bulk.Insert(documents[3]);
            bulk.Find(Query.GT("x", 2)).Update(Update.Inc("x", 10));
            bulk.Find(Query.EQ("x", 13)).RemoveOne();
            bulk.Find(Query.EQ("x", 14)).RemoveOne();
            var result = bulk.Execute();

            Assert.AreEqual(2, result.DeletedCount);
            Assert.AreEqual(4, result.InsertedCount);
            Assert.AreEqual(true, result.IsAcknowledged);
            Assert.AreEqual(2, result.MatchedCount);
            Assert.AreEqual(2, result.ModifiedCount);
            Assert.AreEqual(7, result.ProcessedRequests.Count);
            Assert.AreEqual(7, result.RequestCount);
            Assert.AreEqual(0, result.Upserts.Count);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "_id", 1 }, { "x", 1 } },
                new BsonDocument { { "_id", 2 }, { "x", 2 } }
            };

            Assert.AreEqual(2, _collection.Count());
            Assert.IsTrue(expectedDocuments.SequenceEqual(_collection.FindAll().SetSortOrder("_id")));
        }

        [Test]
        public void TestMixedUpsertsOrdered()
        {
            _collection.Drop();
            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Find(Query.EQ("_id", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("_id", 1)).RemoveOne();
            bulk.Find(Query.EQ("_id", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("_id", 1)).RemoveOne();
            bulk.Find(Query.EQ("_id", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            var result = bulk.Execute();

            Assert.AreEqual(2, result.DeletedCount);
            Assert.AreEqual(0, result.InsertedCount);
            Assert.AreEqual(true, result.IsAcknowledged);
            Assert.AreEqual(0, result.MatchedCount);
            Assert.AreEqual(0, result.ModifiedCount);
            Assert.AreEqual(5, result.ProcessedRequests.Count);
            Assert.AreEqual(5, result.RequestCount);
            Assert.AreEqual(3, result.Upserts.Count);

            var expectedDocument = new BsonDocument { { "_id", 1 }, { "y", 1 } };

            Assert.AreEqual(1, _collection.Count());
            Assert.AreEqual(expectedDocument, _collection.FindOne());
        }

        [Test]
        public void TestMixedUpsertsUnordered()
        {
            _collection.Drop();
            var bulk = _collection.InitializeUnorderedBulkOperation();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("x", 1)).RemoveOne();
            bulk.Find(Query.EQ("x", 1)).Upsert().UpdateOne(Update.Set("y", 1));
            var result = bulk.Execute();

            Assert.AreEqual(1, result.DeletedCount);
            Assert.AreEqual(0, result.InsertedCount);
            Assert.AreEqual(true, result.IsAcknowledged);
            Assert.AreEqual(2, result.MatchedCount);
            Assert.AreEqual(0, result.ModifiedCount);
            Assert.AreEqual(5, result.ProcessedRequests.Count);
            Assert.AreEqual(5, result.RequestCount);
            Assert.AreEqual(1, result.Upserts.Count);

            Assert.AreEqual(0, _collection.Count());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpdateDoesNotAllowInvalidFieldNames(bool ordered)
        {
            var bulk = ordered ? _collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
            bulk.Insert(new BsonDocument("$key", 1));
            Assert.Throws<BsonSerializationException>(() => bulk.Execute());
        }

        [Test]
        public void TestUpdateMultipleOrdered()
        {
            var documents = new BsonDocument[]
            {
                new BsonDocument("_id", 1),
                new BsonDocument("_id", 2),
                new BsonDocument("_id", 3)
            };

            _collection.Drop();
            _collection.Insert(documents[0]);
            _collection.Insert(documents[1]);
            _collection.Insert(documents[2]);

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Find(Query.GT("_id", 0)).Update(Update.Set("z", 1));
            bulk.Find(Query.EQ("_id", 3)).UpdateOne(Update.Set("z", 3));
            bulk.Find(Query.EQ("_id", 4)).Upsert().UpdateOne(Update.Set("z", 4));
            var result = bulk.Execute();

            Assert.AreEqual(0, result.DeletedCount);
            Assert.AreEqual(0, result.InsertedCount);
            Assert.AreEqual(true, result.IsAcknowledged);
            Assert.AreEqual(4, result.MatchedCount);
            Assert.AreEqual(4, result.ModifiedCount);
            Assert.AreEqual(3, result.ProcessedRequests.Count);
            Assert.AreEqual(3, result.RequestCount);
            Assert.AreEqual(1, result.Upserts.Count);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "_id", 1 }, { "z", 1 } },
                new BsonDocument { { "_id", 2 }, { "z", 1 } },
                new BsonDocument { { "_id", 3 }, { "z", 3 } },
                new BsonDocument { { "_id", 4 }, { "z", 4 } }
            };

            Assert.AreEqual(4, _collection.Count());
            Assert.IsTrue(expectedDocuments.SequenceEqual(_collection.FindAll().SetSortOrder("_id")));
        }
    }
}

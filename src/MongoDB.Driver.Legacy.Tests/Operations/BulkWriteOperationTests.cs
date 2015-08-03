/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Operations
{
    [TestFixture]
    public class BulkWriteOperationTests
    {
        private MongoServer _server;
        private MongoServerInstance _primary;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _server = LegacyTestConfiguration.Server;
            _primary = _server.Instances.First(x => x.IsPrimary);
            _collection = LegacyTestConfiguration.Collection;
        }

        [Test]
        public void TestBatchSplittingBySizeWithErrorsOrdered()
        {
            _collection.Drop();

            var documents = new BsonDocument[8];
            for (var i = 0; i < 6; i++)
            {
                documents[i] = new BsonDocument { { "_id", i }, { "a", new string('x', 4 * 1024 * 1024) } };
            }
            documents[6] = new BsonDocument("_id", 0); // will fail
            documents[7] = new BsonDocument("_id", 100);

            var bulk = _collection.InitializeOrderedBulkOperation();
            for (var i = 0; i < 8; i++)
            {
                bulk.Insert(documents[i]);
            }
            var exception = Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => { bulk.Execute(); });
            var result = exception.Result;

            Assert.IsNull(exception.WriteConcernError);
            Assert.AreEqual(1, exception.WriteErrors.Count);
            var writeError = exception.WriteErrors[0];
            Assert.AreEqual(6, writeError.Index);
            Assert.AreEqual(11000, writeError.Code);

            var expectedResult = new ExpectedResult
            {
                InsertedCount = 6,
                ProcessedRequestsCount = 7,
                RequestCount = 8
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = documents.Take(6);
            Assert.That(_collection.FindAll(), Is.EqualTo(expectedDocuments));
        }

        [Test]
        public void TestBatchSplittingBySizeWithErrorsUnordered()
        {
            _collection.Drop();

            var documents = new BsonDocument[8];
            for (var i = 0; i < 6; i++)
            {
                documents[i] = new BsonDocument { { "_id", i }, { "a", new string('x', 4 * 1024 * 1024) } };
            }
            documents[6] = new BsonDocument("_id", 0); // will fail
            documents[7] = new BsonDocument("_id", 100);

            var bulk = _collection.InitializeUnorderedBulkOperation();
            for (var i = 0; i < 8; i++)
            {
                bulk.Insert(documents[i]);
            }
            var exception = Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => { bulk.Execute(); });
            var result = exception.Result;

            Assert.IsNull(exception.WriteConcernError);
            Assert.AreEqual(1, exception.WriteErrors.Count);
            var writeError = exception.WriteErrors[0];
            Assert.AreEqual(6, writeError.Index);
            Assert.AreEqual(11000, writeError.Code);

            var expectedResult = new ExpectedResult
            {
                InsertedCount = 7,
                RequestCount = 8
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = Enumerable.Range(0, 8).Where(i => i != 6).Select(i => documents[i]);
            Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        public void TestBatchSplittingDeletesNearMaxWriteBatchCount(int maxBatchCountDelta)
        {
            var count = _primary.MaxBatchCount + maxBatchCountDelta;
            _collection.Drop();
            _collection.InsertBatch(Enumerable.Range(0, count).Select(n => new BsonDocument("n", n)));

            var bulk = _collection.InitializeOrderedBulkOperation();
            for (var n = 0; n < count; n++)
            {
                bulk.Find(Query.EQ("n", n)).RemoveOne();
            }
            var result = bulk.Execute();

            Assert.AreEqual(count, result.DeletedCount);
            Assert.AreEqual(0, _collection.Count());
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        public void TestBatchSplittingInsertsNearMaxWriteBatchCount(int maxBatchCountDelta)
        {
            var count = _primary.MaxBatchCount + maxBatchCountDelta;
            _collection.Drop();

            var bulk = _collection.InitializeOrderedBulkOperation();
            for (var n = 0; n < count; n++)
            {
                bulk.Insert(new BsonDocument("n", n));
            }
            var result = bulk.Execute();

            Assert.AreEqual(count, result.InsertedCount);
            Assert.AreEqual(count, _collection.Count());
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        public void TestBatchSplittingUpdatesNearMaxWriteBatchCount(int maxBatchCountDelta)
        {
            var count = _primary.MaxBatchCount + maxBatchCountDelta;
            _collection.Drop();
            _collection.InsertBatch(Enumerable.Range(0, count).Select(n => new BsonDocument("n", n)));

            var bulk = _collection.InitializeOrderedBulkOperation();
            for (var n = 0; n < count; n++)
            {
                bulk.Find(Query.EQ("n", n)).UpdateOne(Update.Set("n", -1));
            }
            var result = bulk.Execute();

            if (_primary.Supports(FeatureId.WriteCommands))
            {
                Assert.AreEqual(true, result.IsModifiedCountAvailable);
                Assert.AreEqual(count, result.ModifiedCount);
            }
            else
            {
                Assert.AreEqual(false, result.IsModifiedCountAvailable);
            }
            Assert.AreEqual(count, _collection.Count());
            Assert.AreEqual(count, _collection.Count(Query.EQ("n", -1)));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestExecuteTwice(bool ordered)
        {
            _collection.Drop();
            var bulk = InitializeBulkOperation(_collection, ordered);
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
            // use RequestStart because some of the test cases use { w : 0 }
            using (_server.RequestStart())
            {
                _collection.Drop();

                var document = new BsonDocument("_id", 1);
                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Insert(document);
                var result = bulk.Execute(new WriteConcern(w));

                var expectedResult = new ExpectedResult { IsAcknowledged = w > 0, InsertedCount = 1 };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new[] { document };
                Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestExecuteWithNoRequests(bool ordered)
        {
            _collection.Drop();
            var bulk = InitializeBulkOperation(_collection, ordered);
            Assert.Throws<InvalidOperationException>(() => bulk.Execute());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestFindAfterExecute(bool ordered)
        {
            _collection.Drop();
            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Execute();
            Assert.Throws<InvalidOperationException>(() => bulk.Find(new QueryDocument()));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestFindWithNullQuery(bool ordered)
        {
            _collection.Drop();
            var bulk = InitializeBulkOperation(_collection, ordered);
            Assert.Throws<ArgumentNullException>(() => bulk.Find(null));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestInsertAfterExecute(bool ordered)
        {
            _collection.Drop();
            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Insert(new BsonDocument("x", 1));
            bulk.Execute();
            Assert.Throws<InvalidOperationException>(() => bulk.Insert(new BsonDocument()));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestInsertKeyValidation(bool ordered)
        {
            _collection.Drop();
            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Insert(new BsonDocument("$key", 1));
            Assert.Throws<BsonSerializationException>(() => bulk.Execute());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestInsertMultipleDocuments(bool ordered)
        {
            _collection.Drop();

            var documents = new BsonDocument[]
            {
                new BsonDocument("_id", 1),
                new BsonDocument("_id", 2),
                new BsonDocument("_id", 3)
            };

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Insert(documents[0]);
            bulk.Insert(documents[1]);
            bulk.Insert(documents[2]);
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult { InsertedCount = 3, RequestCount = 3 };
            CheckExpectedResult(expectedResult, result);

            Assert.That(_collection.FindAll(), Is.EquivalentTo(documents));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestInsertOneDocument(bool ordered)
        {
            _collection.Drop();

            var document = new BsonDocument("_id", 1);

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Insert(document);
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult { InsertedCount = 1 };
            CheckExpectedResult(expectedResult, result);

            Assert.That(_collection.FindAll(), Is.EquivalentTo(new[] { document }));
        }

        [Test]
        public void TestMixedOperationsOrdered()
        {
            _collection.Drop();

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument("a", 1));
            bulk.Find(Query.EQ("a", 1)).UpdateOne(Update.Set("b", 1));
            bulk.Find(Query.EQ("a", 2)).Upsert().UpdateOne(Update.Set("b", 2));
            bulk.Insert(new BsonDocument("a", 3));
            bulk.Find(Query.EQ("a", 3)).Remove();
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                DeletedCount = 1,
                InsertedCount = 2,
                MatchedCount = 1,
                ModifiedCount = 1,
                RequestCount = 5,
                UpsertsCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var upserts = result.Upserts;
            Assert.AreEqual(1, upserts.Count);
            Assert.IsInstanceOf<BsonObjectId>(upserts[0].Id);
            Assert.AreEqual(2, upserts[0].Index);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "a", 1 }, { "b", 1 } },
                new BsonDocument { { "a", 2 }, { "b", 2 } }
            };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        public void TestMixedOperationsUnordered()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument { { "a", 1 } });
            _collection.Insert(new BsonDocument { { "a", 2 } });

            var bulk = _collection.InitializeUnorderedBulkOperation();
            bulk.Find(Query.EQ("a", 1)).Update(Update.Set("b", 1));
            bulk.Find(Query.EQ("a", 2)).Remove();
            bulk.Insert(new BsonDocument("a", 3));
            bulk.Find(Query.EQ("a", 4)).Upsert().UpdateOne(Update.Set("b", 4));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                DeletedCount = 1,
                InsertedCount = 1,
                MatchedCount = 1,
                ModifiedCount = 1,
                RequestCount = 4,
                UpsertsCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var upserts = result.Upserts;
            Assert.AreEqual(1, upserts.Count);
            Assert.IsInstanceOf<BsonObjectId>(upserts[0].Id);
            Assert.AreEqual(3, upserts[0].Index);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "a", 1 }, { "b", 1 } },
                new BsonDocument { { "a", 3 } },
                new BsonDocument { { "a", 4 }, { "b", 4 } }
            };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        public void TestMixedUpsertsOrdered()
        {
            _collection.Drop();

            var bulk = _collection.InitializeOrderedBulkOperation();
            var id = ObjectId.GenerateNewId();
            bulk.Find(Query.EQ("_id", id)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("_id", id)).RemoveOne();
            bulk.Find(Query.EQ("_id", id)).Upsert().UpdateOne(Update.Set("y", 1));
            bulk.Find(Query.EQ("_id", id)).RemoveOne();
            bulk.Find(Query.EQ("_id", id)).Upsert().UpdateOne(Update.Set("y", 1));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                DeletedCount = 2,
                RequestCount = 5,
                UpsertsCount = 3,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new[] { new BsonDocument { { "_id", id }, { "y", 1 } } };
            Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
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

            var expectedResult = new ExpectedResult
            {
                DeletedCount = 1,
                MatchedCount = 2,
                RequestCount = 5,
                UpsertsCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new BsonDocument[0];
            Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        [RequiresServer(ClusterTypes = ClusterTypes.Standalone)]
        public void TestNonDefaultWriteConcern(bool ordered)
        {
            _collection.Drop();

            var documents = new[]
            {
                new BsonDocument("_id", 1).Add("x", 1)
            };

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Insert(documents[0]);

            var writeConcern = WriteConcern.W2;
            if (_primary.BuildInfo.Version < new Version(2, 6, 0))
            {
                Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => { bulk.Execute(writeConcern); });
                Assert.AreEqual(1, _collection.Count());
            }
            else
            {
                Assert.Throws<MongoCommandException>(() => { bulk.Execute(writeConcern); });
                Assert.AreEqual(0, _collection.Count());
            }
        }

        [Test]
        public void TestOrderedBatchWithErrors()
        {
            _collection.Drop();
            _collection.CreateIndex(IndexKeys.Ascending("a"), IndexOptions.SetUnique(true));

            var bulk = _collection.InitializeOrderedBulkOperation();
            bulk.Insert(new BsonDocument { { "b", 1 }, { "a", 1 } });
            bulk.Find(Query.EQ("b", 2)).Upsert().UpdateOne(Update.Set("a", 1)); // will fail
            bulk.Find(Query.EQ("b", 3)).Upsert().UpdateOne(Update.Set("a", 2));
            bulk.Find(Query.EQ("b", 2)).Upsert().UpdateOne(Update.Set("a", 1));
            bulk.Insert(new BsonDocument { { "b", 4 }, { "a", 3 } });
            bulk.Insert(new BsonDocument { { "b", 5 }, { "a", 1 } });
            var exception = Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => { bulk.Execute(); });
            var result = exception.Result;

            var expectedResult = new ExpectedResult
            {
                InsertedCount = 1,
                ProcessedRequestsCount = 2,
                RequestCount = 6,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var upserts = result.Upserts;
            Assert.AreEqual(0, upserts.Count);

            Assert.IsNull(exception.WriteConcernError);
            Assert.AreEqual(4, exception.UnprocessedRequests.Count);

            var writeErrors = exception.WriteErrors;
            Assert.AreEqual(1, writeErrors.Count);
            Assert.AreEqual(1, writeErrors[0].Index);
            Assert.AreEqual(11000, writeErrors[0].Code);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "b", 1 }, { "a", 1 } }
            };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestRemoveMultiple(bool ordered)
        {
            _collection.Drop();

            var documents = new BsonDocument[]
            {
                new BsonDocument("_id", 1),
                new BsonDocument("_id", 2),
                new BsonDocument("_id", 3)
            };
            _collection.Insert(documents[0]);
            _collection.Insert(documents[1]);
            _collection.Insert(documents[2]);

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(Query.EQ("_id", 1)).RemoveOne();
            bulk.Find(Query.EQ("_id", 3)).RemoveOne();
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult { DeletedCount = 2, RequestCount = 2 };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new[] { documents[1] };
            Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestRemoveOneOnlyRemovesOneDocument(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("key", 1));
            _collection.Insert(new BsonDocument("key", 1));

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(new QueryDocument()).RemoveOne();
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult { DeletedCount = 1 };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new[] { new BsonDocument("key", 1) };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestRemoveWithEmptyQueryRemovesAllDocuments(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("key", 1));
            _collection.Insert(new BsonDocument("key", 1));

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(new QueryDocument()).Remove();
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult { DeletedCount = 2 };
            CheckExpectedResult(expectedResult, result);

            Assert.AreEqual(0, _collection.Count());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestRemoveWithQueryRemovesOnlyMatchingDocuments(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("key", 1));
            _collection.Insert(new BsonDocument("key", 2));

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(Query.EQ("key", 1)).Remove();
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult { DeletedCount = 1 };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new[] { new BsonDocument("key", 2) };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestReplaceOneKeyValidation(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("_id", 1));

            var bulk = InitializeBulkOperation(_collection, ordered);
            var query = Query.EQ("_id", 1);
            var replacement = new BsonDocument { { "_id", 1 }, { "$key", 1 } };
            bulk.Find(query).ReplaceOne(replacement);
            Assert.Throws<BsonSerializationException>(() => bulk.Execute());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestReplaceOneWithMultipleMatchingDocuments(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("key", 1));
            _collection.Insert(new BsonDocument("key", 1));

            var bulk = InitializeBulkOperation(_collection, ordered);
            var query = Query.EQ("key", 1);
            bulk.Find(query).ReplaceOne(new BsonDocument("key", 3));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                MatchedCount = 1,
                ModifiedCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument("key", 1),
                new BsonDocument("key", 3)
            };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        public void TestUnorderedBatchWithErrors()
        {
            _collection.Drop();
            _collection.CreateIndex(IndexKeys.Ascending("a"), IndexOptions.SetUnique(true));

            var bulk = _collection.InitializeUnorderedBulkOperation();
            bulk.Insert(new BsonDocument { { "b", 1 }, { "a", 1 } });
            bulk.Find(Query.EQ("b", 2)).Upsert().UpdateOne(Update.Set("a", 1));
            bulk.Find(Query.EQ("b", 3)).Upsert().UpdateOne(Update.Set("a", 2));
            bulk.Find(Query.EQ("b", 2)).Upsert().UpdateOne(Update.Set("a", 1));
            bulk.Insert(new BsonDocument { { "b", 4 }, { "a", 3 } });
            bulk.Insert(new BsonDocument { { "b", 5 }, { "a", 1 } });
            var exception = Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => { bulk.Execute(); });
            var result = exception.Result;

            var expectedResult = new ExpectedResult
            {
                InsertedCount = 2,
                RequestCount = 6,
                UpsertsCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var upserts = result.Upserts;
            Assert.AreEqual(1, upserts.Count);
            Assert.IsInstanceOf<BsonObjectId>(upserts[0].Id);
            Assert.AreEqual(2, upserts[0].Index);

            Assert.IsNull(exception.WriteConcernError);
            Assert.AreEqual(0, exception.UnprocessedRequests.Count);

            var writeErrors = exception.WriteErrors;
            Assert.AreEqual(3, writeErrors.Count);
            Assert.AreEqual(1, writeErrors[0].Index);
            Assert.AreEqual(3, writeErrors[1].Index);
            Assert.AreEqual(5, writeErrors[2].Index);
            Assert.IsTrue(writeErrors.All(e => e.Code == 11000));

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "b", 1 }, { "a", 1 } },                   
                _primary.BuildInfo.Version < new Version(2, 6, 0) ?
                    new BsonDocument { { "a", 2 }, { "b", 3 } } : // servers prior to 2.6 rewrite field order on update
                    new BsonDocument { { "b", 3 }, { "a", 2 } },
                new BsonDocument { { "b", 4 }, { "a", 3 } }
            };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpdateChecksThatAllTopLevelFieldNamesAreOperators(bool ordered)
        {
            _collection.Drop();
            var bulk = InitializeBulkOperation(_collection, ordered);
            var query = Query.EQ("_id", 1);
            var update = new UpdateDocument { { "key", 1 } };
            bulk.Find(query).Update(update);
            Assert.Throws<BsonSerializationException>(() => bulk.Execute());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpdateOneBasic(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("key", 1));
            _collection.Insert(new BsonDocument("key", 1));

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(new QueryDocument()).UpdateOne(Update.Set("key", 3));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                MatchedCount = 1,
                ModifiedCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "key", 1 } },
                new BsonDocument { { "key", 3 } }
            };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpdateOneKeyValidation(bool ordered)
        {
            var updates = new IMongoUpdate[]
            {
                new UpdateDocument { { "key", 1 } },
                new UpdateDocument { { "key", 1 }, { "$key", 1 } }
            };

            foreach (var update in updates)
            {
                _collection.Drop();
                var bulk = InitializeBulkOperation(_collection, ordered);
                var query = Query.EQ("_id", 1);
                bulk.Find(query).UpdateOne(update);
                Assert.Throws<BsonSerializationException>(() => bulk.Execute());
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpdateOnlyAffectsDocumentsThatMatch(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("key", 1));
            _collection.Insert(new BsonDocument("key", 2));

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(Query.EQ("key", 1)).Update(Update.Set("x", 1));
            bulk.Find(Query.EQ("key", 2)).Update(Update.Set("x", 2));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                MatchedCount = 2,
                ModifiedCount = 2,
                RequestCount = 2,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);


            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "key", 1 }, { "x", 1 } },
                new BsonDocument { { "key", 2 }, { "x", 2 } }
            };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpdateUpdatesAllMatchingDocuments(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("key", 1));
            _collection.Insert(new BsonDocument("key", 2));

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(new QueryDocument()).Update(Update.Set("x", 3));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                MatchedCount = 2,
                ModifiedCount = 2,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "key", 1 }, { "x", 3 } },
                new BsonDocument { { "key", 2 }, { "x", 3 } }
            };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertOneVeryLargeDocument(bool ordered)
        {
            if (_primary.BuildInfo.Version >= new Version(2, 6, 0))
            {
                _collection.Drop();

                var bulk = InitializeBulkOperation(_collection, ordered);
                var bigString = new string('x', 16 * 1024 * 1024 - 22);
                bulk.Find(Query.EQ("_id", 1)).Upsert().Update(Update.Set("x", bigString)); // resulting document will be exactly 16MiB
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    UpsertsCount = 1,
                    IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument { { "_id", 1 }, { "x", bigString } }
                };
                Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertReplaceOneDoesNotAffectNonUpsertsInTheSameOperation(bool ordered)
        {
            _collection.Drop();

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(Query.EQ("key", 1)).ReplaceOne(new BsonDocument("x", 1)); // not an upsert
            bulk.Find(Query.EQ("key", 2)).Upsert().ReplaceOne(new BsonDocument("x", 2));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                RequestCount = 2,
                UpsertsCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new[] { new BsonDocument { { "x", 2 } } };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertReplaceOneOnlyReplacesOneMatchingDocument(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("key", 1));
            _collection.Insert(new BsonDocument("key", 1));

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(Query.EQ("key", 1)).Upsert().ReplaceOne(new BsonDocument("x", 1));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                MatchedCount = 1,
                ModifiedCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new[]
            {
                new BsonDocument { { "x", 1 } },
                new BsonDocument { { "key", 1 } }
            };
            Assert.AreEqual(2, _collection.Count());
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertUpdateOneDoesNotAffectNonUpsertsInTheSameOperation(bool ordered)
        {
            _collection.Drop();

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(Query.EQ("key", 1)).UpdateOne(Update.Set("x", 1)); // not an upsert
            bulk.Find(Query.EQ("key", 2)).Upsert().UpdateOne(Update.Set("x", 2));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                RequestCount = 2,
                UpsertsCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new[] { new BsonDocument { { "key", 2 }, { "x", 2 } } };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));

            // repeat the same operation with the current collection contents
            var bulk2 = InitializeBulkOperation(_collection, ordered);
            bulk2.Find(Query.EQ("key", 1)).UpdateOne(Update.Set("x", 1)); // not an upsert
            bulk2.Find(Query.EQ("key", 2)).Upsert().UpdateOne(Update.Set("x", 2));
            var result2 = bulk2.Execute();

            var expectedResult2 = new ExpectedResult
            {
                MatchedCount = 1,
                RequestCount = 2,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult2, result2);
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertUpdateOneOnlyAffectsOneMatchingDocument(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument("key", 1));
            _collection.Insert(new BsonDocument("key", 1));

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(Query.EQ("key", 1)).Upsert().UpdateOne(Update.Set("x", 1));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                MatchedCount = 1,
                ModifiedCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "key", 1 }, { "x", 1 } },
                new BsonDocument { { "key", 1 } }
            };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertUpdateUpsertsAndDoesNotAffectNonUpsertsInTheSameOperation(bool ordered)
        {
            _collection.Drop();

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(Query.EQ("key", 1)).Update(Update.Set("x", 1)); // not an upsert
            bulk.Find(Query.EQ("key", 2)).Upsert().Update(Update.Set("x", 2));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                RequestCount = 2,
                UpsertsCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new[] { new BsonDocument { { "key", 2 }, { "x", 2 } } };
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));

            // repeat the same batch with the current collection contents
            var bulk2 = InitializeBulkOperation(_collection, ordered);
            bulk2.Find(Query.EQ("key", 1)).Update(Update.Set("x", 1)); // not an upsert
            bulk2.Find(Query.EQ("key", 2)).Upsert().Update(Update.Set("x", 2));
            var result2 = bulk2.Execute();

            var expectedResult2 = new ExpectedResult
            {
                MatchedCount = 1,
                RequestCount = 2,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult2, result2);
            Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertWithMultipleMatchingDocuments(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 1 } });
            _collection.Insert(new BsonDocument { { "_id", 2 }, { "x", 1 } });

            var bulk = InitializeBulkOperation(_collection, ordered);
            var query = Query.EQ("x", 1);
            var update = Update.Set("x", 2);
            bulk.Find(query).Upsert().Update(update);
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                MatchedCount = 2,
                ModifiedCount = 2,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "_id", 1 }, { "x", 2 } },
                new BsonDocument { { "_id", 2 }, { "x", 2 } }
            };
            Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertWithNoMatchingDocument(bool ordered)
        {
            _collection.Drop();

            // use non-ObjectIds to ensure functionality against < 2.6 servers
            var id1 = 1;
            var id2 = 2;
            _collection.Insert(new BsonDocument { { "_id", id2 }, { "x", 2 } });

            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Find(Query.EQ("_id", id1)).Upsert().Update(Update.Set("x", 1));
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                UpsertsCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "_id", id1 }, { "x", 1 } },
                new BsonDocument { { "_id", id2 }, { "x", 2 } }
            };
            Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertWithOneMatchingDocument(bool ordered)
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 1 } });
            _collection.Insert(new BsonDocument { { "_id", 2 }, { "x", 2 } });

            var bulk = InitializeBulkOperation(_collection, ordered);
            var query = Query.EQ("_id", 1);
            var update = Update.Set("x", 3);
            bulk.Find(query).Upsert().Update(update);
            var result = bulk.Execute();

            var expectedResult = new ExpectedResult
            {
                MatchedCount = 1,
                ModifiedCount = 1,
                IsModifiedCountAvailable = _primary.Supports(FeatureId.WriteCommands)
            };
            CheckExpectedResult(expectedResult, result);

            var expectedDocuments = new BsonDocument[]
            {
                new BsonDocument { { "_id", 1 }, { "x", 3 } },
                new BsonDocument { { "_id", 2 }, { "x", 2 } }
            };
            Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestW0DoesNotReportErrors(bool ordered)
        {
            // use a request so we can read our own writes even with older servers
            using (_server.RequestStart())
            {
                _collection.Drop();

                var documents = new[]
                {
                    new BsonDocument("_id", 1),
                    new BsonDocument("_id", 1)
                };

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Insert(documents[0]);
                bulk.Insert(documents[1]);
                var result = bulk.Execute(WriteConcern.Unacknowledged);

                var expectedResult = new ExpectedResult { IsAcknowledged = false, RequestCount = 2 };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new[] { documents[0] };
                Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestW2AgainstStandalone(bool ordered)
        {
            if (_primary.InstanceType == MongoServerInstanceType.StandAlone)
            {
                _collection.Drop();

                var documents = new[] { new BsonDocument("x", 1) };

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Insert(documents[0]);

                if (_primary.Supports(FeatureId.WriteCommands))
                {
                    Assert.Throws<MongoCommandException>(() => { bulk.Execute(WriteConcern.W2); });
                    Assert.AreEqual(0, _collection.Count());
                }
                else
                {
                    var exception = Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => { bulk.Execute(WriteConcern.W2); });
                    var result = exception.Result;

                    var expectedResult = new ExpectedResult { InsertedCount = 1, RequestCount = 1 };
                    CheckExpectedResult(expectedResult, result);

                    Assert.AreEqual(0, exception.UnprocessedRequests.Count);
                    Assert.AreEqual(0, exception.WriteErrors.Count);

                    var writeConcernError = exception.WriteConcernError;
                    Assert.IsNotNull(writeConcernError);

                    Assert.That(_collection.FindAll(), Is.EquivalentTo(documents));
                }
            }
        }

        [Test]
        [Explicit]
        [RequiresServer(MinimumVersion = "2.4.0", ClusterTypes = ClusterTypes.ReplicaSet)]
        public void TestWTimeoutPlusDuplicateKeyError()
        {
            _collection.Drop();

            var secondary = LegacyTestConfiguration.Server.Secondaries.First();
            using (LegacyTestConfiguration.StopReplication(secondary))
            {
                var bulk = _collection.InitializeUnorderedBulkOperation();
                bulk.Insert(new BsonDocument("_id", 1));
                bulk.Insert(new BsonDocument("_id", 1));
                var all = LegacyTestConfiguration.Server.Secondaries.Length + 1;
                var exception = Assert.Throws<MongoBulkWriteException<BsonDocument>>(() => bulk.Execute(new WriteConcern(w: all, wTimeout: TimeSpan.FromMilliseconds(1))));
                var result = exception.Result;

                var expectedResult = new ExpectedResult { InsertedCount = 1, RequestCount = 2 };
                CheckExpectedResult(expectedResult, result);

                var writeErrors = exception.WriteErrors;
                Assert.AreEqual(1, writeErrors.Count);
                Assert.AreEqual(11000, writeErrors[0].Code);
                Assert.AreEqual(1, writeErrors[0].Index);

                var writeConcernError = exception.WriteConcernError;
                Assert.AreEqual(64, writeConcernError.Code);
            }
        }

        // private methods
        private void CheckExpectedResult(ExpectedResult expectedResult, BulkWriteResult<BsonDocument> result)
        {
            Assert.AreEqual(expectedResult.IsAcknowledged ?? true, result.IsAcknowledged);
            Assert.AreEqual(expectedResult.ProcessedRequestsCount ?? expectedResult.RequestCount ?? 1, result.ProcessedRequests.Count);
            Assert.AreEqual(expectedResult.RequestCount ?? 1, result.RequestCount);

            if (result.IsAcknowledged)
            {
                Assert.AreEqual(expectedResult.DeletedCount ?? 0, result.DeletedCount);
                Assert.AreEqual(expectedResult.InsertedCount ?? 0, result.InsertedCount);
                Assert.AreEqual(expectedResult.MatchedCount ?? 0, result.MatchedCount);
                Assert.AreEqual(expectedResult.IsModifiedCountAvailable ?? true, result.IsModifiedCountAvailable);
                if (result.IsModifiedCountAvailable)
                {
                    Assert.AreEqual(expectedResult.ModifiedCount ?? 0, result.ModifiedCount);
                }
                else
                {
                    Assert.Throws<NotSupportedException>(() => { var _ = result.ModifiedCount; });
                }
                Assert.AreEqual(expectedResult.UpsertsCount ?? 0, result.Upserts.Count);
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => { var x = result.DeletedCount; });
                Assert.Throws<NotSupportedException>(() => { var x = result.InsertedCount; });
                Assert.Throws<NotSupportedException>(() => { var x = result.MatchedCount; });
                Assert.Throws<NotSupportedException>(() => { var x = result.ModifiedCount; });
                Assert.Throws<NotSupportedException>(() => { var x = result.Upserts; });
            }
        }

        private BulkWriteOperation<T> InitializeBulkOperation<T>(MongoCollection<T> collection, bool ordered)
        {
            return ordered ? collection.InitializeOrderedBulkOperation() : collection.InitializeUnorderedBulkOperation();
        }

        // nested classes
        private class ExpectedResult
        {
            // private fields
            private int? _deletedCount;
            private int? _insertedCount;
            private bool? _isAcknowledged;
            private int? _matchedCount;
            private int? _modifiedCount;
            private bool? _isModifiedCountAvailable;
            private int? _processedRequestsCount;
            private int? _requestCount;
            private int? _upsertsCount;

            // public properties
            public int? DeletedCount
            {
                get { return _deletedCount; }
                set { _deletedCount = value; }
            }

            public int? InsertedCount
            {
                get { return _insertedCount; }
                set { _insertedCount = value; }
            }

            public bool? IsAcknowledged
            {
                get { return _isAcknowledged; }
                set { _isAcknowledged = value; }
            }

            public bool? IsModifiedCountAvailable
            {
                get { return _isModifiedCountAvailable; }
                set { _isModifiedCountAvailable = value; }
            }

            public int? MatchedCount
            {
                get { return _matchedCount; }
                set { _matchedCount = value; }
            }

            public int? ModifiedCount
            {
                get { return _modifiedCount; }
                set { _modifiedCount = value; }
            }

            public int? ProcessedRequestsCount
            {
                get { return _processedRequestsCount; }
                set { _processedRequestsCount = value; }
            }

            public int? RequestCount
            {
                get { return _requestCount; }
                set { _requestCount = value; }
            }

            public int? UpsertsCount
            {
                get { return _upsertsCount; }
                set { _upsertsCount = value; }
            }
        }
    }
}

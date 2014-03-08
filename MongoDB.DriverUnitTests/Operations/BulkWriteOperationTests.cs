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
using System.Collections.Generic;
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
        private MongoServer _server;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _server = Configuration.TestServer;
            _collection = Configuration.TestCollection;
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
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                var document = new BsonDocument("_id", 1);

                _collection.Drop();
                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Insert(document);
                var result = bulk.Execute(new WriteConcern { W = w });

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
            var bulk = InitializeBulkOperation(_collection, ordered);
            bulk.Insert(new BsonDocument("$key", 1));
            Assert.Throws<BsonSerializationException>(() => bulk.Execute());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestInsertMultipleDocuments(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                var documents = new BsonDocument[]
                {
                    new BsonDocument("_id", 1),
                    new BsonDocument("_id", 2),
                    new BsonDocument("_id", 3)
                };

                _collection.Drop();
                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Insert(documents[0]);
                bulk.Insert(documents[1]);
                bulk.Insert(documents[2]);
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult { InsertedCount = 3, RequestCount = 3 };
                CheckExpectedResult(expectedResult, result);

                Assert.That(_collection.FindAll(), Is.EquivalentTo(documents));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestInsertOneDocument(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                var document = new BsonDocument("_id", 1);

                _collection.Drop();
                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Insert(document);
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult { InsertedCount = 1 };
                CheckExpectedResult(expectedResult, result);

                Assert.That(_collection.FindAll(), Is.EquivalentTo(new[] { document }));
            }
        }

        [Test]
        public void TestMixedOrdered()
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

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

                var expectedResult = new ExpectedResult
                {
                    DeletedCount = 2, InsertedCount = 4, MatchedCount = 2, ModifiedCount = 2, RequestCount = 7,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument { { "_id", 1 }, { "x", 1 } },
                    new BsonDocument { { "_id", 2 }, { "x", 2 } }
                };
                Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        public void TestMixedUpsertsOrdered()
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

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
                    DeletedCount = 2, RequestCount = 5, UpsertsCount = 3,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new[] { new BsonDocument { { "_id", id }, { "y", 1 } } };
                Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        public void TestMixedUpsertsUnordered()
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

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
                    DeletedCount = 1, MatchedCount = 2, RequestCount = 5, UpsertsCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[0];
                Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestRemoveMultiple(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

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

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(Query.EQ("_id", 1)).RemoveOne();
                bulk.Find(Query.EQ("_id", 3)).RemoveOne();
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult { DeletedCount = 2, RequestCount = 2 };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new[] { documents[1] };
                Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestRemoveWithEmptyQueryRemovesAllDocuments(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                var documents = new BsonDocument[]
                {
                    new BsonDocument("key", 1),
                    new BsonDocument("key", 1)
                };

                _collection.Drop();
                _collection.Insert(documents[0]);
                _collection.Insert(documents[1]);

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(new QueryDocument()).Remove();
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult { DeletedCount = 2 };
                CheckExpectedResult(expectedResult, result);

                Assert.AreEqual(0, _collection.Count());
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestRemoveWithQueryRemovesOnlyMatchingDocuments(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                var documents = new BsonDocument[]
                {
                    new BsonDocument("key", 1),
                    new BsonDocument("key", 2)
                };

                _collection.Drop();
                _collection.Insert(documents[0]);
                _collection.Insert(documents[1]);

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(Query.EQ("key", 1)).Remove();
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult { DeletedCount = 1 };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new[] { new BsonDocument("key", 2) };
                Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Ignore]
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
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                _collection.Drop();
                _collection.Insert(new BsonDocument("key", 1));
                _collection.Insert(new BsonDocument("key", 1));

                var bulk = InitializeBulkOperation(_collection, ordered);
                var query = Query.EQ("key", 1);
                var replacement = Update.Replace(new BsonDocument("key", 3));
                bulk.Find(query).ReplaceOne(replacement);
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    MatchedCount = 1, ModifiedCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument("key", 1),
                    new BsonDocument("key", 3)
                };
                Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Ignore]
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpdateChecksThatAllTopLevelFieldNamesAreOperators(bool ordered)
        {
            var bulk = InitializeBulkOperation(_collection, ordered);
            var query = Query.EQ("_id", 1);
            var update = new UpdateDocument { { "key", 1 } };
            bulk.Find(query).Update(update);
            Assert.Throws<BsonSerializationException>(() => bulk.Execute());
        }

        [Ignore]
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpdateKeyValidation(bool ordered)
        {
            var bulk = InitializeBulkOperation(_collection, ordered);
            var query = Query.EQ("_id", 1);
            var update = Update.Set("$key", 1);
            bulk.Find(query).Update(update);
            Assert.Throws<BsonSerializationException>(() => bulk.Execute());
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpdateOneBasic(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                var documents = new BsonDocument[]
                {
                    new BsonDocument("key", 1),
                    new BsonDocument("key", 1)
                };

                _collection.Drop();
                _collection.Insert(documents[0]);
                _collection.Insert(documents[1]);

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(new QueryDocument()).UpdateOne(Update.Set("key", 3));
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    MatchedCount = 1, ModifiedCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument { { "key", 1 } },
                    new BsonDocument { { "key", 3 } }
                };
                Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Ignore]
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
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                var documents = new BsonDocument[]
                {
                    new BsonDocument("key", 1),
                    new BsonDocument("key", 2)
                };

                _collection.Drop();
                _collection.Insert(documents[0]);
                _collection.Insert(documents[1]);

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(Query.EQ("key", 1)).Update(Update.Set("x", 1));
                bulk.Find(Query.EQ("key", 2)).Update(Update.Set("x", 2));
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    MatchedCount = 2, ModifiedCount = 2, RequestCount = 2,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);


                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument { { "key", 1 }, { "x", 1 } },
                    new BsonDocument { { "key", 2 }, { "x", 2 } }
                };
                Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpdateUpdatesAllMatchingDocuments(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                var documents = new BsonDocument[]
                {
                    new BsonDocument("key", 1),
                    new BsonDocument("key", 2)
                };

                _collection.Drop();
                _collection.Insert(documents[0]);
                _collection.Insert(documents[1]);

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(new QueryDocument()).Update(Update.Set("x", 3));
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    MatchedCount = 2, ModifiedCount = 2,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument { { "key", 1 }, { "x", 3 } },
                    new BsonDocument { { "key", 2 }, { "x", 3 } }
                };
                Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertOneVeryLargeDocument(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                _collection.Drop();
                var bigString = new string('x', 16 * 1024 * 1024 - 30);

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(Query.EQ("key", 1)).Upsert().Update(Update.Set("x", bigString));
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    UpsertsCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument { { "key", 1 }, { "x", bigString } }
                };
                Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertReplaceOneDoesNotAffectNonUpsertsInTheSameOperation(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                _collection.Drop();

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(Query.EQ("key", 1)).ReplaceOne(new BsonDocument("x", 1)); // not an upsert
                bulk.Find(Query.EQ("key", 2)).Upsert().ReplaceOne(new BsonDocument("x", 2));
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    RequestCount = 2, UpsertsCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new[] { new BsonDocument { { "x", 2 } } };
                Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertReplaceOneOnlyReplacesOneMatchingDocument(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                var documents = new BsonDocument[]
                {
                    new BsonDocument("key", 1),
                    new BsonDocument("key", 1)
                };

                _collection.Drop();
                _collection.Insert(documents[0]);
                _collection.Insert(documents[1]);

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(Query.EQ("key", 1)).Upsert().ReplaceOne(new BsonDocument("x", 1));
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    MatchedCount = 1, ModifiedCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
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
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertUpdateOneDoesNotAffectNonUpsertsInTheSameOperation(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                _collection.Drop();

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(Query.EQ("key", 1)).UpdateOne(Update.Set("x", 1)); // not an upsert
                bulk.Find(Query.EQ("key", 2)).Upsert().UpdateOne(Update.Set("x", 2));
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    RequestCount = 2, UpsertsCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
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
                    MatchedCount = 1, RequestCount = 2,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult2, result2);
                Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertUpdateOneOnlyAffectsOneMatchingDocument(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                var documents = new BsonDocument[]
                {
                    new BsonDocument("key", 1),
                    new BsonDocument("key", 1)
                };

                _collection.Drop();
                _collection.Insert(documents[0]);
                _collection.Insert(documents[1]);

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(Query.EQ("key", 1)).Upsert().UpdateOne(Update.Set("x", 1));
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    MatchedCount = 1, ModifiedCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument { { "key", 1 }, { "x", 1 } },
                    new BsonDocument { { "key", 1 } }
                };
                Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertUpdateUpsertsAndDoesNotAffectNonUpsertsInTheSameOperation(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                _collection.Drop();

                var bulk = InitializeBulkOperation(_collection, ordered);
                bulk.Find(Query.EQ("key", 1)).Update(Update.Set("x", 1)); // not an upsert
                bulk.Find(Query.EQ("key", 2)).Upsert().Update(Update.Set("x", 2));
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    RequestCount = 2, UpsertsCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
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
                    MatchedCount = 1, RequestCount = 2,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult2, result2);
                Assert.That(_collection.FindAll().SetFields(Fields.Exclude("_id")), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertWithMultipleMatchingDocuments(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

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
                    MatchedCount = 2, ModifiedCount = 2,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument { { "_id", 1 }, { "x", 2 } },
                    new BsonDocument { { "_id", 2 }, { "x", 2 } }
                };
                Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertWithNoMatchingDocument(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

                _collection.Drop();
                var id1 = ObjectId.GenerateNewId();
                var id2 = ObjectId.GenerateNewId();
                _collection.Insert(new BsonDocument { { "_id", id2 }, { "x", 2 } });

                var bulk = InitializeBulkOperation(_collection, ordered);
                var query = Query.EQ("_id", id1);
                var update = Update.Set("x", 1);
                bulk.Find(query).Upsert().Update(update);
                var result = bulk.Execute();

                var expectedResult = new ExpectedResult
                {
                    UpsertsCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument { { "_id", id1 }, { "x", 1 } },
                    new BsonDocument { { "_id", id2 }, { "x", 2 } }
                };
                Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
            }
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestUpsertWithOneMatchingDocument(bool ordered)
        {
            using (_server.RequestStart(null, ReadPreference.Primary))
            {
                var serverInstance = _server.RequestConnection.ServerInstance;

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
                    MatchedCount = 1, ModifiedCount = 1,
                    IsModifiedCountAvailable = serverInstance.Supports(FeatureId.WriteCommands)
                };
                CheckExpectedResult(expectedResult, result);

                var expectedDocuments = new BsonDocument[]
                {
                    new BsonDocument { { "_id", 1 }, { "x", 3 } },
                    new BsonDocument { { "_id", 2 }, { "x", 2 } }
                };
                Assert.That(_collection.FindAll(), Is.EquivalentTo(expectedDocuments));
            }
        }

        // private methods
        private void CheckExpectedResult(ExpectedResult expectedResult, BulkWriteResult result)
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

        private BulkWriteOperation InitializeBulkOperation(MongoCollection collection, bool ordered)
        {
            return ordered ? collection.InitializeOrderedBulkOperation() : _collection.InitializeUnorderedBulkOperation();
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

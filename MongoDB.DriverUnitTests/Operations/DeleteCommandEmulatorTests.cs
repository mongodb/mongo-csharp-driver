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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Operations;
using MongoDB.Driver.Support;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Operations
{
    [TestFixture]
    public class DeleteCommandEmulatorTests
    {
        private MongoCollection<BsonDocument> _collection;
        private string _collectionName;
        private MongoDatabase _database;
        private string _databaseName;
        private MongoServerInstance _primary;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _primary = Configuration.TestServer.Primary;
            _collection = Configuration.TestCollection;
            _collectionName = Configuration.TestCollection.Name;
            _database = Configuration.TestDatabase;
            _databaseName = Configuration.TestDatabase.Name;
        }

        [Test]
        public void TestFirstDeleteFailed()
        {
            var connection = _primary.AcquireConnection();
            try
            {
                _collection.Drop();
                _collection.Insert(new BsonDocument("_id", 1));
                _collection.Insert(new BsonDocument("_id", 2));
                _collection.Insert(new BsonDocument("_id", 3));

                var deletes = new[]
                {
                    new DeleteRequest(Query.And(Query.EQ("_id", 1), Query.Where("x"))),
                    new DeleteRequest(Query.EQ("_id", 2))
                };
                var operation = CreateOperation(connection, _collection, deletes);
                var exception = Assert.Throws<BulkWriteException>(() => operation.Execute(connection));

                var result = exception.Result;
                Assert.AreEqual(0, result.DeletedCount);
                Assert.AreEqual(0, result.Upserts.Count);

                var processedRequests = result.ProcessedRequests;
                Assert.AreEqual(1, processedRequests.Count);
                Assert.AreSame(deletes[0], processedRequests[0]);

                var unprocessedRequests = exception.UnprocessedRequests;
                Assert.AreEqual(1, unprocessedRequests.Count);
                Assert.AreSame(deletes[1], unprocessedRequests[0]);

                var errors = exception.WriteErrors;
                Assert.AreEqual(1, errors.Count);
                Assert.IsTrue(errors[0].Code > 0);
                Assert.IsTrue(errors[0].Message != null);
                Assert.AreEqual(0, errors[0].Index);

                Assert.AreEqual(3, _collection.Count());
            }
            finally
            {
                _primary.ReleaseConnection(connection);
            }
        }

        [Test]
        public void TestOneDelete()
        {
            var connection = _primary.AcquireConnection();
            try
            {
                _collection.Drop();
                _collection.Insert(new BsonDocument("_id", 1));
                _collection.Insert(new BsonDocument("_id", 2));

                var deletes = new[] { new DeleteRequest(Query.Null) }; // defaults to Limit=1
                var operation = CreateOperation(connection, _collection, deletes);
                var result = operation.Execute(connection);

                Assert.AreEqual(1, result.DeletedCount);
                Assert.AreEqual(0, result.Upserts.Count);

                var processedRequests = result.ProcessedRequests;
                Assert.AreEqual(1, processedRequests.Count);
                Assert.AreSame(deletes[0], processedRequests[0]);

                Assert.AreEqual(1, _collection.Count());
            }
            finally
            {
                _primary.ReleaseConnection(connection);
            }
        }

        [Test]
        public void TestSecondDeleteFailed()
        {
            var connection = _primary.AcquireConnection();
            try
            {
                _collection.Drop();
                _collection.Insert(new BsonDocument("_id", 1));
                _collection.Insert(new BsonDocument("_id", 2));
                _collection.Insert(new BsonDocument("_id", 3));

                var deletes = new[]
                {
                    new DeleteRequest(Query.EQ("_id", 1)),
                    new DeleteRequest(Query.And(Query.EQ("_id", 2), Query.Where("x")))
                };
                var operation = CreateOperation(connection, _collection, deletes);
                var exception = Assert.Throws<BulkWriteException>(() => operation.Execute(connection));

                var result = exception.Result;
                Assert.AreEqual(1, result.DeletedCount);
                Assert.AreEqual(0, result.Upserts.Count);

                var processedRequests = result.ProcessedRequests;
                Assert.AreEqual(2, processedRequests.Count);
                Assert.AreSame(deletes[0], processedRequests[0]);
                Assert.AreSame(deletes[1], processedRequests[1]);

                var unprocessedRequests = exception.UnprocessedRequests;
                Assert.AreEqual(0, unprocessedRequests.Count);

                var errors = exception.WriteErrors;
                Assert.AreEqual(1, errors.Count);
                Assert.IsTrue(errors[0].Code > 0);
                Assert.IsTrue(errors[0].Message != null);
                Assert.AreEqual(1, errors[0].Index);

                Assert.AreEqual(2, _collection.Count());
            }
            finally
            {
                _primary.ReleaseConnection(connection);
            }
        }

        [Test]
        public void TestTwoDeletes()
        {
            var connection = _primary.AcquireConnection();
            try
            {
                _collection.Drop();
                _collection.Insert(new BsonDocument("_id", 1));
                _collection.Insert(new BsonDocument("_id", 2));
                _collection.Insert(new BsonDocument("_id", 3));

                var deletes = new[] { new DeleteRequest(Query.EQ("_id", 1)), new DeleteRequest(Query.EQ("_id", 2)) };
                var operation = CreateOperation(connection, _collection, deletes);
                var result = operation.Execute(connection);

                Assert.AreEqual(2, result.DeletedCount);
                Assert.AreEqual(0, result.Upserts.Count);

                var processedRequests = result.ProcessedRequests;
                Assert.AreEqual(2, processedRequests.Count);
                Assert.AreSame(deletes[0], processedRequests[0]);
                Assert.AreSame(deletes[1], processedRequests[1]);

                Assert.AreEqual(1, _collection.Count());
            }
            finally
            {
                _primary.ReleaseConnection(connection);
            }
        }

        [Test]
        public void TestZeroDeletes()
        {
            var connection = _primary.AcquireConnection();
            try
            {
                var deletes = new DeleteRequest[0];
                var operation = CreateOperation(connection, _collection, deletes);
                var result = operation.Execute(connection);

                Assert.AreEqual(0, result.DeletedCount);
                Assert.AreEqual(0, result.ProcessedRequests.Count);
                Assert.AreEqual(0, result.Upserts.Count);
            }
            finally
            {
                _primary.ReleaseConnection(connection);
            }
        }

        private BulkDeleteOperationEmulator CreateOperation(MongoConnection connection, MongoCollection collection, IEnumerable<DeleteRequest> deletes)
        {
            var serverInstance = connection.ServerInstance;

            return new BulkDeleteOperationEmulator(new BulkDeleteOperationArgs(
                collection.Name,
                collection.Database.Name,
                1, // maxBatchCount
                serverInstance.MaxMessageLength, // maxBatchLength
                serverInstance.MaxDocumentSize,
                serverInstance.MaxWireDocumentSize,
                true, // isOrdered
                BsonBinaryReaderSettings.Defaults,
                deletes,
                WriteConcern.Acknowledged,
                BsonBinaryWriterSettings.Defaults));
        }
    }
}

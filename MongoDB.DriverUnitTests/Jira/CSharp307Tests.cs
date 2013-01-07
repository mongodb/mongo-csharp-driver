/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.DriverUnitTests.Jira
{
    [TestFixture]
    public class CSharp307Tests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.TestCollection;
            _collection.Drop();
        }

        [Test]
        public void TestInsertNullDocument()
        {
            BsonDocument document = null;
            var ex = Assert.Catch<ArgumentNullException>(() => _collection.Insert(document));
            Assert.AreEqual("document", ex.ParamName);
        }

        [Test]
        public void TestInsertNullBatch()
        {
            BsonDocument[] batch = null;
            var ex = Assert.Catch<ArgumentNullException>(() => _collection.InsertBatch(batch));
            Assert.AreEqual("documents", ex.ParamName);
        }

        [Test]
        public void TestInsertBatchWithNullDocument()
        {
            BsonDocument[] batch = new BsonDocument[] { null };
            Assert.Throws<ArgumentException>(() => _collection.InsertBatch(batch), "Batch contains one or more null documents.");
        }

        [Test]
        public void TestSaveNullDocument()
        {
            BsonDocument document = null;
            var ex = Assert.Catch<ArgumentNullException>(() => _collection.Save(document));
            Assert.AreEqual("document", ex.ParamName);
        }
    }
}

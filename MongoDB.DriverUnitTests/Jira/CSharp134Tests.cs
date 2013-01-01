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

using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp134
{
    [TestFixture]
    public class CSharp134Tests
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public ObjectId Id;
            public MongoDBRef DbRef;
        }
#pragma warning restore

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<C> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<C>();
        }

        [Test]
        public void TestDeserializeMongoDBRef()
        {
            var dbRef = new MongoDBRef("test", ObjectId.GenerateNewId());
            var c = new C { DbRef = dbRef };
            _collection.RemoveAll();
            _collection.Insert(c);

            var rehydrated = _collection.FindOne();
            Assert.IsNull(rehydrated.DbRef.DatabaseName);
            Assert.AreEqual(dbRef.CollectionName, rehydrated.DbRef.CollectionName);
            Assert.AreEqual(dbRef.Id, rehydrated.DbRef.Id);
        }
    }
}

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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp215
{
    [TestFixture]
    public class CSharp215Tests
    {
        public class C
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id;
            public int X;
        }

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
        public void TestSave()
        {
            _collection.RemoveAll();

            var doc = new C { X = 1 };
            _collection.Save(doc);
            var id = doc.Id;

            Assert.AreEqual(1, _collection.Count());
            var fetched = _collection.FindOne();
            Assert.AreEqual(id, fetched.Id);
            Assert.AreEqual(1, fetched.X);

            doc.X = 2;
            _collection.Save(doc);

            Assert.AreEqual(1, _collection.Count());
            fetched = _collection.FindOne();
            Assert.AreEqual(id, fetched.Id);
            Assert.AreEqual(2, fetched.X);
        }
    }
}

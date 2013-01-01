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

namespace MongoDB.DriverUnitTests.Jira.CSharp172
{
    [TestFixture]
    public class CSharp172Tests
    {
        public class C
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id;
            public int N;
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
        public void TestRoundtrip()
        {
            var obj1 = new C { N = 1 };
            Assert.IsNullOrEmpty(obj1.Id);
            _collection.RemoveAll();
            _collection.Insert(obj1);
            Assert.IsNotNullOrEmpty(obj1.Id);

            var obj2 = _collection.FindOne();
            Assert.AreEqual(obj1.Id, obj2.Id);
            Assert.AreEqual(obj1.N, obj2.N);
        }
    }
}

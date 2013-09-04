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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp714
{
    [TestFixture]
    public class CSharp714Tests
    {
        public class C
        {
            public int Id { get; set; }
            public Guid Guid { get; set; }
        }

        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<C> _collection;
        private IIdGenerator _generator = new AscendingGuidGenerator();
        private static int __maxNoOfDocuments = 100;
        
        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            var collectionSettings = new MongoCollectionSettings() { GuidRepresentation = GuidRepresentation.Standard };
            _collection = _database.GetCollection<C>("csharp714", collectionSettings);
            _collection.Drop();
        }
        
        [Test]
        public void TestGuidsAreAscending()
        {
            _collection.RemoveAll();
            CreateTestData();
            // sort descending just so we are not retrieving in insertion order
            var cursor = _collection.FindAll().SetSortOrder(SortBy.Descending("Guid"));
            var id = __maxNoOfDocuments - 1;
            foreach (var c in cursor) 
            {
                Assert.AreEqual(id--, c.Id);
            }
        }

        private void CreateTestData()
        {
            for (var i=0; i<__maxNoOfDocuments; i++) 
            {
                _collection.Insert(new C { Id = i, Guid = (Guid)_generator.GenerateId(null, null) });
            }
            _collection.EnsureIndex("Guid");
        }
    }
}

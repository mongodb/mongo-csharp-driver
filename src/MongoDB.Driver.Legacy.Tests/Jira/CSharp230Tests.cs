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

using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Jira.CSharp230
{
    [TestFixture]
    public class CSharp230Tests
    {
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _collection = LegacyTestConfiguration.Collection;
        }

        [Test]
        public void TestCreateIndexAfterDropCollection()
        {
            if (_collection.Exists())
            {
                _collection.Drop();
            }

            Assert.IsFalse(_collection.IndexExists("x"));
            _collection.CreateIndex("x");
            Assert.IsTrue(_collection.IndexExists("x"));

            _collection.Drop();
            Assert.IsFalse(_collection.IndexExists("x"));
            _collection.CreateIndex("x");
            Assert.IsTrue(_collection.IndexExists("x"));
        }
    }
}

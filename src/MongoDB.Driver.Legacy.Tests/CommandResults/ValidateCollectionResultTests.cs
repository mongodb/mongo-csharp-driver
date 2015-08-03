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

namespace MongoDB.Driver.Tests.CommandResults
{
    [TestFixture]
    public class ValidateCollectionResultTests
    {
        private MongoServer _server;
        private MongoCollection<BsonDocument> _collection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _server = LegacyTestConfiguration.Server;
            _collection = LegacyTestConfiguration.Collection;
        }

        [Test]
        public void Test()
        {
            if (_server.Primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                // make sure collection exists and has exactly one document
                _collection.RemoveAll();
                _collection.Insert(new BsonDocument());

                var result = _collection.Validate();
                Assert.IsTrue(result.Ok);
                Assert.AreEqual(_collection.FullName, result.Namespace);
            }
        }
    }
}

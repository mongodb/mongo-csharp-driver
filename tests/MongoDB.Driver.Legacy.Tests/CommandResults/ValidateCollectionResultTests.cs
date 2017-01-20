/* Copyright 2010-2016 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Tests.CommandResults
{
    public class ValidateCollectionResultTests
    {
        private MongoServer _server;
        private MongoCollection<BsonDocument> _collection;

        public ValidateCollectionResultTests()
        {
            _server = LegacyTestConfiguration.Server;
            _collection = LegacyTestConfiguration.Collection;
        }

        [Fact]
        public void Test()
        {
            if (_server.Primary.InstanceType != MongoServerInstanceType.ShardRouter)
            {
                // make sure collection exists and has exactly one document
                _collection.RemoveAll();
                _collection.Insert(new BsonDocument());

                var result = _collection.Validate();
                Assert.True(result.Ok);
                Assert.Equal(_collection.FullName, result.Namespace);
            }
        }
    }
}

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

using System;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp282Tests
    {
        private MongoCollection<BsonDocument> _collection;

        public CSharp282Tests()
        {
            _collection = LegacyTestConfiguration.Collection;
            _collection.Drop();
        }

        [Fact]
        public void TestEmptyUpdateBuilder()
        {
            var document = new BsonDocument("x", 1);
            _collection.Insert(document);

            var query = Query.EQ("_id", document["_id"]);
            var update = new UpdateBuilder();
            Assert.Throws<ArgumentException>(() => _collection.Update(query, update));
        }
    }
}

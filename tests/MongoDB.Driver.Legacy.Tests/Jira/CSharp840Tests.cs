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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp840
    {
        private MongoCollection<BsonDocument> _collection;

        public CSharp840()
        {
            _collection = LegacyTestConfiguration.GetCollection<BsonDocument>();
            if (_collection.Exists()) { _collection.Drop(); }
            _collection.Insert(new BsonDocument { { "x", 1 } });
            _collection.Insert(new BsonDocument { { "x", 2 }, { "Length", BsonNull.Value } });
            _collection.Insert(new BsonDocument { { "x", 3 }, { "Length", 123 } });
        }

        [Fact]
        public void TestNotWhere()
        {
            var query = Query.Not(Query.Where("this.Length == null"));
            var values = ExecuteQuery(query);
            Assert.Equal(1, values.Length);
            Assert.Equal(3, values[0]);
        }

        [Fact]
        public void TestWhere()
        {
            var query = Query.Where("this.Length == null");
            var values = ExecuteQuery(query);
            Assert.Equal(2, values.Length);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
        }

        private int[] ExecuteQuery(IMongoQuery query)
        {
            return _collection.Find(query).SetFields("x").ToList().Select(r => r["x"].ToInt32()).OrderBy(x => x).ToArray();
        }
    }
}
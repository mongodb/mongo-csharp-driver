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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp172
{
    public class CSharp172Tests
    {
        public class C
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id;
            public int N;
        }

        private MongoCollection<C> _collection;

        public CSharp172Tests()
        {
            _collection = LegacyTestConfiguration.GetCollection<C>();
        }

        [Fact]
        public void TestRoundtrip()
        {
            var obj1 = new C { N = 1 };
            Assert.Null(obj1.Id);
            _collection.RemoveAll();
            _collection.Insert(obj1);
            Assert.NotNull(obj1.Id);
            Assert.NotEqual("", obj1.Id);

            var obj2 = _collection.FindOne();
            Assert.Equal(obj1.Id, obj2.Id);
            Assert.Equal(obj1.N, obj2.N);
        }
    }
}

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

namespace MongoDB.Driver.Tests.Jira.CSharp215
{
    public class CSharp215Tests
    {
        public class C
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id;
            public int X;
        }

        private MongoCollection<C> _collection;

        public CSharp215Tests()
        {
            _collection = LegacyTestConfiguration.GetCollection<C>();
        }

        [Fact]
        public void TestSave()
        {
            _collection.RemoveAll();

            var doc = new C { X = 1 };
            _collection.Save(doc);
            var id = doc.Id;

            Assert.Equal(1, _collection.Count());
            var fetched = _collection.FindOne();
            Assert.Equal(id, fetched.Id);
            Assert.Equal(1, fetched.X);

            doc.X = 2;
            _collection.Save(doc);

            Assert.Equal(1, _collection.Count());
            fetched = _collection.FindOne();
            Assert.Equal(id, fetched.Id);
            Assert.Equal(2, fetched.X);
        }
    }
}

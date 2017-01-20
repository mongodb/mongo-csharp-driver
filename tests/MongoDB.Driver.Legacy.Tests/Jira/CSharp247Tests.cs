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

namespace MongoDB.Driver.Tests.Jira.CSharp247
{
    public class CSharp247Tests
    {
        public interface I
        {
            int X { get; set; }
        }

        public class C : I
        {
            public ObjectId Id { get; set; }
            public int X { get; set; }
        }

        private MongoCollection<BsonDocument> _collection;

        public CSharp247Tests()
        {
            _collection = LegacyTestConfiguration.Collection;
        }

        [Fact]
        public void TestDeserializeInterface()
        {
            _collection.RemoveAll();

            var c = new C { X = 1 };
            _collection.Insert<I>(c);
            var id = c.Id;

            var i = _collection.FindOneAs<I>();
            Assert.IsType<C>(i);
            var r = (C)i;
            Assert.Equal(id, r.Id);
            Assert.Equal(1, r.X);
        }
    }
}

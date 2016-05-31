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

namespace MongoDB.Driver.Tests.Jira.CSharp134
{
    public class CSharp134Tests
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public ObjectId Id;
            public MongoDBRef DbRef;
        }
#pragma warning restore

        private MongoCollection<C> _collection;

        public CSharp134Tests()
        {
            _collection = LegacyTestConfiguration.GetCollection<C>();
        }

        [Fact]
        public void TestDeserializeMongoDBRef()
        {
            var dbRef = new MongoDBRef("test", ObjectId.GenerateNewId());
            var c = new C { DbRef = dbRef };
            _collection.RemoveAll();
            _collection.Insert(c);

            var rehydrated = _collection.FindOne();
            Assert.Null(rehydrated.DbRef.DatabaseName);
            Assert.Equal(dbRef.CollectionName, rehydrated.DbRef.CollectionName);
            Assert.Equal(dbRef.Id, rehydrated.DbRef.Id);
        }
    }
}

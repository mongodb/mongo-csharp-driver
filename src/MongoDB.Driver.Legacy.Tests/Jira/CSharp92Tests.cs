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
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp92
{
    public class CSharp92Tests
    {
        private class C
        {
            [BsonId]
            public int Id { get; set; }
            public string P { get; set; }
        }

        [Fact]
        public void TestSaveDocument()
        {
            var server = LegacyTestConfiguration.Server;
            var database = LegacyTestConfiguration.Database;
            var collection = LegacyTestConfiguration.Collection;

            var document = new BsonDocument { { "_id", -1 }, { "P", "x" } };
            collection.RemoveAll();
            collection.Insert(document);

            var fetched = collection.FindOne();
            Assert.IsType<BsonDocument>(fetched);
            Assert.Equal(-1, fetched["_id"].AsInt32);
            Assert.Equal("x", fetched["P"].AsString);
        }

        [Fact]
        public void TestSaveClass()
        {
            var server = LegacyTestConfiguration.Server;
            var database = LegacyTestConfiguration.Database;
            var collection = LegacyTestConfiguration.GetCollection<C>();

            var document = new C { Id = -1, P = "x" };
            collection.RemoveAll();
            collection.Insert(document);

            var fetched = collection.FindOne();
            Assert.IsType<C>(fetched);
            Assert.Equal(-1, fetched.Id);
            Assert.Equal("x", fetched.P);
        }
    }
}

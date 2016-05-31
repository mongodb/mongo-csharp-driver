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
    public class CSharp281Tests
    {
        private static MongoCollection<BsonDocument> __collection;
        private static Lazy<bool> __lazyOneTimeSetup = new Lazy<bool>(OneTimeSetup);

        public CSharp281Tests()
        {
            var _ = __lazyOneTimeSetup.Value;
        }

        private static bool OneTimeSetup()
        {
            __collection = LegacyTestConfiguration.Collection;
            __collection.Drop();
            return true;
        }

        [Fact]
        public void TestPopFirst()
        {
            var document = new BsonDocument("x", new BsonArray { 1, 2, 3 });
            __collection.RemoveAll();
            __collection.Insert(document);

            var query = Query.EQ("_id", document["_id"]);
            var update = Update.PopFirst("x");
            __collection.Update(query, update);

            document = __collection.FindOne();
            var array = document["x"].AsBsonArray;
            Assert.Equal(2, array.Count);
            Assert.Equal(2, array[0].AsInt32);
            Assert.Equal(3, array[1].AsInt32);
        }

        [Fact]
        public void TestPopLast()
        {
            var document = new BsonDocument("x", new BsonArray { 1, 2, 3 });
            __collection.RemoveAll();
            __collection.Insert(document);

            var query = Query.EQ("_id", document["_id"]);
            var update = Update.PopLast("x");
            __collection.Update(query, update);

            document = __collection.FindOne();
            var array = document["x"].AsBsonArray;
            Assert.Equal(2, array.Count);
            Assert.Equal(1, array[0].AsInt32);
            Assert.Equal(2, array[1].AsInt32);
        }
    }
}

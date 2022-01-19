/* Copyright 2010-present MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp253
{
    public class CSharp253Tests
    {
        public class C
        {
            public ObjectId Id { get; set; }
            public MongoDBRef DBRef { get; set; }
            public BsonNull BsonNull { get; set; }
        }

        private MongoServer _server;
        private MongoCollection<BsonDocument> _collection;

        public CSharp253Tests()
        {
            _server = LegacyTestConfiguration.Server;
            _collection = LegacyTestConfiguration.Database.GetCollection(GetType().Name);
        }

        [Fact]
        public void TestInsertClass()
        {
            var c = new C
            {
                DBRef = new MongoDBRef("database", "collection", ObjectId.GenerateNewId()),
                BsonNull = null
            };
            _collection.Insert(c);
        }

        [Fact]
        public void TestLegacyDollar()
        {
            var document = new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "BsonNull", new BsonDocument("_csharpnull", true) },
                { "DBRef", new BsonDocument
                    {
                        // starting with server version 2.5.2 the order of the fields must be exactly as below
                        { "$ref", "ref" },
                        { "$id", "id" },
                        { "$db", "db" }
                    }
                }
            };

            _collection.Insert(document);
        }

        [Fact]
        public void TestCreateIndexOnNestedElement()
        {
            _collection.CreateIndex("a.b");
        }
    }
}

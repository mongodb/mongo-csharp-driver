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

using System;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp199
{
    public class CSharp199Tests
    {
        [Fact]
        public void TestSingleRename()
        {
            var server = LegacyTestConfiguration.Server;
            server.Connect();
            if (server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var database = LegacyTestConfiguration.Database;
                var collection = LegacyTestConfiguration.Collection;

                collection.RemoveAll();
                collection.Insert(new BsonDocument { { "_id", 1 }, { "a", 2 } });

                var query = Query.EQ("_id", 1);
                var update = Update.Rename("a", "x");
                collection.Update(query, update);
                var document = collection.FindOne();

                var expectedUpdate = "{ '$rename' : { 'a' : 'x' } }".Replace("'", "\"");
                Assert.Equal(expectedUpdate, update.ToJson());
                var expectedDocument = "{ '_id' : 1, 'x' : 2 }".Replace("'", "\"");
                Assert.Equal(expectedDocument, document.ToJson());
            }
        }

        [Fact]
        public void TestMultipleRenames()
        {
            var server = LegacyTestConfiguration.Server;
            server.Connect();
            if (server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var database = LegacyTestConfiguration.Database;
                var collection = LegacyTestConfiguration.Collection;

                collection.RemoveAll();
                collection.Insert(new BsonDocument { { "_id", 1 }, { "a", 2 }, { "b", 3 } });

                var query = Query.EQ("_id", 1);
                var update = Update.Rename("a", "x").Rename("b", "y");
                collection.Update(query, update);
                var document = collection.FindOne();

                var expectedUpdate = "{ '$rename' : { 'a' : 'x', 'b' : 'y' } }".Replace("'", "\"");
                Assert.Equal(expectedUpdate, update.ToJson());
                var expectedDocument = "{ '_id' : 1, 'x' : 2, 'y' : 3 }".Replace("'", "\"");
                Assert.Equal(expectedDocument, document.ToJson());
            }
        }

        [Fact]
        public void TestRenameWithSet()
        {
            var server = LegacyTestConfiguration.Server;
            server.Connect();
            if (server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var database = LegacyTestConfiguration.Database;
                var collection = LegacyTestConfiguration.Collection;

                collection.RemoveAll();
                collection.Insert(new BsonDocument { { "_id", 1 }, { "a", 2 }, { "b", 3 } });

                var query = Query.EQ("_id", 1);
                var update = Update.Rename("a", "x").Set("b", 4);
                collection.Update(query, update);
                var document = collection.FindOne();

                var expectedUpdate = "{ '$rename' : { 'a' : 'x' }, '$set' : { 'b' : 4 } }".Replace("'", "\"");
                Assert.Equal(expectedUpdate, update.ToJson());
                var expectedDocument = "{ '_id' : 1, 'b' : 4, 'x' : 2 }".Replace("'", "\""); // server rearranges elements
                Assert.Equal(expectedDocument, document.ToJson());
            }
        }
    }
}

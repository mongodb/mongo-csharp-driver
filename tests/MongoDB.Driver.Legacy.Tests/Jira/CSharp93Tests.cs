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
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp93
{
    public class CSharp93Tests
    {
        [Fact]
        public void TestDropAllIndexes()
        {
            var server = LegacyTestConfiguration.Server;
            var database = LegacyTestConfiguration.Database;
            var collection = LegacyTestConfiguration.Collection;

            if (collection.Exists())
            {
                collection.DropAllIndexes();
            }
            else
            {
                collection.Insert(new BsonDocument()); // make sure collection exists
            }

            collection.CreateIndex("x", "y");
            collection.DropIndex("x", "y");

            collection.CreateIndex(IndexKeys.Ascending("x", "y"));
            collection.DropIndex(IndexKeys.Ascending("x", "y"));
        }

        [Fact]
        public void CreateIndex_SetUniqueTrue_Success()
        {
            var server = LegacyTestConfiguration.Server;
            var database = LegacyTestConfiguration.Database;
            var collection = LegacyTestConfiguration.Collection;

            if (collection.Exists())
            {
                collection.Drop();
            }
            collection.Insert(new BsonDocument()); // make sure collection exists

            collection.CreateIndex(IndexKeys.Ascending("x"), IndexOptions.SetUnique(true));
            collection.CreateIndex(IndexKeys.Ascending("y"), IndexOptions.SetUnique(false));
        }
    }
}
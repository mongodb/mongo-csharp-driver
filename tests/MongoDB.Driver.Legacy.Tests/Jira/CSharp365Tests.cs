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

namespace MongoDB.Driver.Tests.Jira.CSharp365
{
    public class CSharp365Tests
    {
        [Fact]
        public void TestExplainWithFieldsAndCoveredIndex()
        {
            var server = LegacyTestConfiguration.Server;
            if (server.BuildInfo.Version >= new Version(1, 8, 0))
            {
                var database = LegacyTestConfiguration.Database;
                var collection = LegacyTestConfiguration.Collection;
                collection.Drop();

                collection.CreateIndex("A", "_id");
                collection.Insert(new BsonDocument { { "_id", 1 }, { "A", 1 } });
                collection.Insert(new BsonDocument { { "_id", 2 }, { "A", 2 } });
                collection.Insert(new BsonDocument { { "_id", 3 }, { "A", 3 } });

                var query = Query.EQ("A", 1);
                var fields = Fields.Include("_id");
                var cursor = collection.Find(query).SetFields(fields).SetHint("A_1__id_1"); // make sure it uses the index
                var plan = cursor.Explain();
                if (server.BuildInfo.Version < new Version(2, 7, 0))
                {
                    Assert.True(plan["indexOnly"].ToBoolean());
                }
                else
                {
                    var winningPlan = plan["queryPlanner"]["winningPlan"].AsBsonDocument;
                    if (winningPlan.Contains("shards"))
                    {
                        winningPlan = winningPlan["shards"][0]["winningPlan"].AsBsonDocument;
                    }
                    var inputStage = winningPlan["inputStage"].AsBsonDocument;
                    var stage = inputStage["stage"].AsString;
                    var keyPattern = inputStage["keyPattern"].AsBsonDocument;
                    Assert.Equal("IXSCAN", stage);
                    Assert.Equal(BsonDocument.Parse("{ A : 1, _id : 1 }"), keyPattern);
                }
            }
        }
    }
}

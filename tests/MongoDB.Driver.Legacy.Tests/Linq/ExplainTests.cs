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

using System;
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
    public class ExplainTests
    {
        private class C
        {
            public ObjectId Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        private MongoCollection _collection;

        public ExplainTests()
        {
            _collection = LegacyTestConfiguration.Collection;
        }

        [SkippableFact]
        public void TestExplainFromLinqQueryEqualsExplainFromCursor()
        {
            RequireServer.Check().Supports(Feature.LegacyWireProtocol);

            EnsureCollectionExists();
            var linqExplain = _collection.AsQueryable<C>().Where(c => c.X == 2 && c.Y == 1).Take(1).Explain();
            var queryExplain = _collection.FindAs<C>(Query.And(Query.EQ("X", 2), Query.EQ("Y", 1))).SetLimit(1).Explain();

            // not all versions and/or topologies of the server return a queryPlanner.parsedQuery element in the explain result
            if (linqExplain.Contains("queryPlanner"))
            {
                if (linqExplain["queryPlanner"].AsBsonDocument.Contains("parsedQuery"))
                {
                    var linqQuery = linqExplain["queryPlanner"]["parsedQuery"];
                    var findQuery = queryExplain["queryPlanner"]["parsedQuery"];

                    Assert.Equal(linqQuery, findQuery);
                }
            }
        }

        [SkippableFact]
        public void TestVerboseExplainFromLinqQueryEqualsVerboseExplainFromCursor()
        {
            RequireServer.Check().Supports(Feature.LegacyWireProtocol);

            EnsureCollectionExists();
            var linqExplain = _collection.AsQueryable<C>().Where(c => c.X == 2 && c.Y == 1).Take(1).Explain(true);
            var queryExplain = _collection.FindAs<C>(Query.And(Query.EQ("X", 2), Query.EQ("Y", 1))).SetLimit(1).Explain(true);

            // not all versions and/or topologies of the server return a queryPlanner.parsedQuery element in the explain result
            if (linqExplain.Contains("queryPlanner"))
            {
                if (linqExplain["queryPlanner"].AsBsonDocument.Contains("parsedQuery"))
                {
                    var linqQuery = linqExplain["queryPlanner"]["parsedQuery"];
                    var findQuery = queryExplain["queryPlanner"]["parsedQuery"];

                    Assert.Equal(linqQuery, findQuery);
                }
            }
        }

        [Fact]
        public void TestDistinctQueryCannotBeExplained()
        {
            EnsureCollectionExists();
            Assert.Throws<NotSupportedException>(() => _collection.AsQueryable<C>().Select(c => c.X).Distinct().Explain());
        }

        [Fact]
        public void TestTakeZeroQueriesCannotBeExplained()
        {
            Assert.Throws<NotSupportedException>(() => _collection.AsQueryable<C>().Take(0).Explain());
        }

        // private methods
        private void EnsureCollectionExists()
        {
            var document = new BsonDocument("x", 1);
            _collection.Insert(document);
        }

        private void RemoveMatchingElements(BsonValue value, Regex regex)
        {
            if (value.BsonType == BsonType.Document)
            {
                var document = value.AsBsonDocument;
                foreach (var name in document.Names.ToList())
                {
                    if (regex.IsMatch(name))
                    {
                        document.Remove(name);
                    }
                    else
                    {
                        RemoveMatchingElements(document[name], regex);
                    }
                }
            }
            else if (value.BsonType == BsonType.Array)
            {
                foreach (var item in value.AsBsonArray)
                {
                    RemoveMatchingElements(item, regex);
                }
            }
        }
    }
}

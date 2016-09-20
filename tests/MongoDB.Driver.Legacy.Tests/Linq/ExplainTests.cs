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
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
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

        [Fact]
        public void TestExplainFromLinqQueryEqualsExplainFromCursor()
        {
            var linqExplain = _collection.AsQueryable<C>().Where(c => c.X == 2 && c.Y == 1).Take(1).Explain();
            var queryExplain = _collection.FindAs<C>(Query.And(Query.EQ("X", 2), Query.EQ("Y", 1))).SetLimit(1).Explain();

            // millis could be different, so we'll ignore that difference.
            RemoveMatchingElements(linqExplain, new Regex("millis", RegexOptions.IgnoreCase));
            RemoveMatchingElements(queryExplain, new Regex("millis", RegexOptions.IgnoreCase));

            Assert.Equal(linqExplain, queryExplain);
        }

        [Fact]
        public void TestVerboseExplainFromLinqQueryEqualsVerboseExplainFromCursor()
        {
            var linqExplain = _collection.AsQueryable<C>().Where(c => c.X == 2 && c.Y == 1).Take(1).Explain(true);
            var queryExplain = _collection.FindAs<C>(Query.And(Query.EQ("X", 2), Query.EQ("Y", 1))).SetLimit(1).Explain(true);

            // millis could be different, so we'll ignore that difference.
            RemoveMatchingElements(linqExplain, new Regex("millis", RegexOptions.IgnoreCase));
            RemoveMatchingElements(queryExplain, new Regex("millis", RegexOptions.IgnoreCase));

            Assert.Equal(linqExplain, queryExplain);
        }

        [Fact]
        public void TestDistinctQueryCannotBeExplained()
        {
            Assert.Throws<NotSupportedException>(()=> _collection.AsQueryable<C>().Select(c=>c.X).Distinct().Explain());
        }

        [Fact]
        public void TestTakeZeroQueriesCannotBeExplained()
        {
            Assert.Throws<NotSupportedException>(() => _collection.AsQueryable<C>().Take(0).Explain());
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

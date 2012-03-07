﻿/* Copyright 2010-2012 10gen Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;

namespace MongoDB.DriverUnitTests.Jira.CSharp321
{
    [TestFixture]
    public class CSharp321Tests
    {
        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestNoArgs()
        {
            var query = Query.And();
        }

        [Test]
        public void TestOneNestedAnd()
        {
            var query = Query.And(
                new QueryDocument("$and", new BsonArray { new BsonDocument("x", 1), new BsonDocument("y", 2) })
            );
            var expected = "{ 'x' : 1, 'y' : 2 }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneClause()
        {
            var query = Query.And(
                Query.EQ("x", 1)
            );
            var expected = "{ 'x' : 1 }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoClauses()
        {
            var query = Query.And(
                Query.EQ("x", 1),
                Query.EQ("y", 2)
            );
            var expected = "{ 'x' : 1, 'y' : 2 }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestCombineAndWithOneClause()
        {
            var query = Query.And(
                new QueryDocument("$and", new BsonArray { new BsonDocument("x", 1), new BsonDocument("y", 2) }),
                Query.EQ("z", 3)
            );
            var expected = "{ 'x' : 1, 'y' : 2, 'z' : 3 }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestCombineAndWithAnd()
        {
            var query = Query.And(
                new QueryDocument("$and", new BsonArray { new BsonDocument("a", 1), new BsonDocument("b", 2) }),
                new QueryDocument("$and", new BsonArray { new BsonDocument("x", 1), new BsonDocument("y", 2) })
            );
            var expected = "{ 'a' : 1, 'b' : 2, 'x' : 1, 'y' : 2 }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestCombineTwoClausesWithAnd()
        {
            var query = Query.And(
                Query.EQ("a", 1),
                Query.EQ("b", 2),
                new QueryDocument("$and", new BsonArray { new BsonDocument("x", 1), new BsonDocument("y", 2) })
            );
            var expected = "{ 'a' : 1, 'b' : 2, 'x' : 1, 'y' : 2 }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestThreeClauses()
        {
            var query = Query.And(
                Query.EQ("x", 1),
                Query.EQ("y", 2),
                Query.EQ("z", 3)
            );
            var expected = "{ 'x' : 1, 'y' : 2, 'z' : 3 }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestCombineTwoCombinableClausesForSameField()
        {
            var query = Query.And(
                Query.GTE("x", 1),
                Query.LTE("x", 2)
            );
            var expected = "{ 'x' : { '$gte' : 1, '$lte' : 2 } }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestCombineTwoEQForSameField()
        {
            // never mind that this query can't match anything
            var query = Query.And(
                Query.EQ("x", 1),
                Query.EQ("x", 2)
            );
            var expected = "{ '$and' : [{ 'x' : 1 }, { 'x' : 2 }] }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestCombineTwoNonCombinableClausesForSameField()
        {
            // never mind that this query is somewhat redundant
            var query = Query.And(
                Query.GT("x", 1),
                Query.GT("x", 2)
            );
            var expected = "{ '$and' : [{ 'x' : { '$gt' : 1 } }, { 'x' : { '$gt' : 2 } }] }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestNestedAndClause()
        {
            var query = Query.And(
                new QueryDocument("$and", new BsonArray { new BsonDocument("x", 1), new BsonDocument("y", 2) }),
                Query.EQ("z", 3)
            );
            var expected = "{ 'x' : 1, 'y' : 2, 'z' : 3 }".Replace("'", "\"");
            var json = query.ToJson();
            Assert.AreEqual(expected, json);
        }
    }
}

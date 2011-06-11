/* Copyright 2010 10gen Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverUnitTests.Jira.CSharp137 {
    [TestFixture]
    public class CSharp137Tests {
        [Test]
        public void TestAndInNotIn() {
            var query = Query.And(
                Query.In("value", new BsonArray(new int[] { 1, 2, 3, 4 })),
                Query.NotIn("value", new BsonArray(new int[] { 11, 12, 13, 14 })));

            Assert.AreEqual(
                new BsonDocument() {
                    {"value", new BsonDocument() {
                        {"$in", new BsonArray(new int[] { 1, 2, 3, 4 })},
                        {"$nin", new BsonArray(new int[] { 11, 12, 13, 14 })}
                    }}
                },
                query.ToBsonDocument());
        }

        [Test]
        public void TestAndGtLt() {
            var query = Query.And(
                Query.NotIn("value", new BsonArray(new int[] {1,2,3})),
                Query.EQ("OtherValue", 1),
                Query.GT("value", 6),
                Query.LT("value", 20));

            Assert.AreEqual(
                new BsonDocument() {
                    {"value", new BsonDocument() {
                        {"$nin", new BsonArray(new int[] {1,2,3})},
                        {"$gt", 6},
                        {"$lt", 20}
                    }},
                    {"OtherValue", 1}
                },
                query.ToBsonDocument());
        }

        [Test]
        [ExpectedException(
            typeof(InvalidOperationException),
            ExpectedMessage = "Query.And does not support combining equality comparisons with other operators (field 'value').")]
        public void TestNoDuplicateEq() {
            var query = Query.And(
                Query.EQ("value", 6),
                Query.EQ("value", 20));
        }

        [Test]
        [ExpectedException(
            typeof(InvalidOperationException),
            ExpectedMessage = "Query.And does not support combining equality comparisons with other operators (field 'value').")]
        public void TestNoEq1() {
            var query = Query.And(
                Query.EQ("value", 6),
                Query.LT("value", 20));
        }

        [Test]
        [ExpectedException(
            typeof(InvalidOperationException),
            ExpectedMessage = "Query.And does not support combining equality comparisons with other operators (field 'value').")]
        public void TestNoEq2() {
            var query = Query.And(
                Query.GT("value", 6),
                Query.EQ("value", 20));
        }

        [Test]
        [ExpectedException(
            typeof(InvalidOperationException),
            ExpectedMessage = "Query.And does not support using the same operator more than once (field 'value', operator '$lte').")]
        public void TestNoDuplicateOperation() {
            var query = Query.And(
                Query.LTE("value", 6),
                Query.LTE("value", 20));
        }
    }
}

/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class FieldsBuilderTypedTests
    {
        public class TestClass
        {
            public int _id;
            public int[] a;
            public SubClass[] a2;
            public int x;
            public string textfield;
            [BsonIgnoreIfDefault]
            public double relevance;
        }

        public class SubClass
        {
            public int b;
        }

        [Test]
        public void TestElemMatch()
        {
            var fields = Fields<TestClass>.ElemMatch(tc => tc.a2, qb => qb.EQ(tc => tc.b, 10));
            string expected = "{ \"a2\" : { \"$elemMatch\" : { \"b\" : 10 } } }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestIncludeElemMatch()
        {
            var fields = Fields<TestClass>.Include(tc => tc.x).ElemMatch(tc => tc.a2, qb => qb.EQ(tc => tc.b, 10));
            string expected = "{ \"x\" : 1, \"a2\" : { \"$elemMatch\" : { \"b\" : 10 } } }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestInclude()
        {
            var fields = Fields<TestClass>.Include(tc => tc.a);
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestExclude()
        {
            var fields = Fields<TestClass>.Exclude(tc => tc.a);
            string expected = "{ \"a\" : 0 }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestSliceNameSize()
        {
            var fields = Fields<TestClass>.Slice(tc => tc.a, 10);
            string expected = "{ \"a\" : { \"$slice\" : 10 } }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestSliceNameSkipLimit()
        {
            var fields = Fields<TestClass>.Slice(tc => tc.a, 10, 20);
            string expected = "{ \"a\" : { \"$slice\" : [10, 20] } }";
            Assert.AreEqual(expected, fields.ToJson());
        }
        [Test]
        public void TestIncludeInclude()
        {
            var fields = Fields<TestClass>.Include(tc => tc.x).Include(tc => tc.a);
            string expected = "{ \"x\" : 1, \"a\" : 1 }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestIncludeExclude()
        {
            var fields = Fields<TestClass>.Include(tc => tc.x).Exclude(tc => tc.a);
            string expected = "{ \"x\" : 1, \"a\" : 0 }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestIncludeSliceNameSize()
        {
            var fields = Fields<TestClass>.Include(tc => tc.x).Slice(tc => tc.a, 10);
            string expected = "{ \"x\" : 1, \"a\" : { \"$slice\" : 10 } }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestIncludeSliceNameSkipLimit()
        {
            var fields = Fields<TestClass>.Include(tc => tc.x).Slice(tc => tc.a, 10, 20);
            string expected = "{ \"x\" : 1, \"a\" : { \"$slice\" : [10, 20] } }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestMetaText()
        {
            var server = Configuration.TestServer;
            var primary = server.Primary;
            if (primary.Supports(FeatureId.TextSearchQuery))
            {
                using (server.RequestStart(null, primary))
                {
                    var collection = Configuration.TestDatabase.GetCollection<TestClass>("test_meta_text");
                    collection.Drop();
                    collection.CreateIndex(IndexKeys<TestClass>.Text(x => x.textfield));
                    collection.Insert(new TestClass
                    {
                        _id = 1,
                        textfield = "The quick brown fox jumped",
                        x = 1
                    });
                    collection.Insert(new TestClass
                    {
                        _id = 2,
                        textfield = "over the lazy brown dog",
                        x = 1
                    });

                    var query = Query.Text("fox");
                    var result = collection.FindOneAs<BsonDocument>(query);
                    Assert.AreEqual(1, result["_id"].AsInt32);
                    Assert.IsFalse(result.Contains("relevance"));
                    Assert.IsTrue(result.Contains("x"));

                    var fields = Fields<TestClass>.MetaTextScore(y => y.relevance).Exclude(y => y.x);
                    result = collection.FindOneAs<BsonDocument>(new FindOneArgs() { Query = query, Fields = fields });
                    Assert.AreEqual(1, result["_id"].AsInt32);
                    Assert.IsTrue(result.Contains("relevance"));
                    Assert.IsFalse(result.Contains("x"));
                }
            }
        }

        [Test]
        public void TestMetaTextGenerate()
        {
            var fields = Fields<TestClass>.MetaTextScore(y => y.relevance);
            string expected = "{ \"relevance\" : { \"$meta\" : \"textScore\" } }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestMetaTextIncludeExcludeGenerate()
        {
            var fields = Fields<TestClass>.MetaTextScore(y => y.relevance).Include(y => y.x).Exclude(y => y._id);
            string expected = "{ \"relevance\" : { \"$meta\" : \"textScore\" }, \"x\" : 1, \"_id\" : 0 }";
            Assert.AreEqual(expected, fields.ToJson());
        }
    }
}
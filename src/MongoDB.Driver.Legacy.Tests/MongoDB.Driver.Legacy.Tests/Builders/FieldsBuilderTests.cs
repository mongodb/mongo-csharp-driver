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
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Builders
{
    [TestFixture]
    public class FieldsBuilderTests
    {
        [Test]
        public void TestMetaText()
        {
            if (LegacyTestConfiguration.Server.Primary.Supports(FeatureId.TextSearchQuery))
            {
                var collection = LegacyTestConfiguration.Database.GetCollection<BsonDocument>("test_meta_text");
                collection.Drop();
                collection.CreateIndex(IndexKeys.Text("textfield"));
                collection.Insert(new BsonDocument
                {
                    { "_id", 1 },
                    { "textfield", "The quick brown fox jumped" }
                });
                collection.Insert(new BsonDocument
                {
                    { "_id", 2 },
                    { "textfield", "over the lazy brown dog" }
                });
                var query = Query.Text("fox");
                var result = collection.FindOneAs<BsonDocument>(query);
                Assert.AreEqual(2, result.ElementCount);
                Assert.IsFalse(result.Contains("relevance"));

                var fields = Fields.MetaTextScore("relevance");
                result = collection.FindOneAs<BsonDocument>(new FindOneArgs { Query = query, Fields = fields });
                Assert.AreEqual(3, result.ElementCount);
                Assert.IsTrue(result.Contains("relevance"));
            }
        }
    }
}
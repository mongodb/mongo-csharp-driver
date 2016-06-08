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
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp307Tests
    {
        private static MongoCollection<BsonDocument> __collection;
        private static Lazy<bool> __lazyOneTimeSetup = new Lazy<bool>(OneTimeSetup);

        public CSharp307Tests()
        {
            var _ = __lazyOneTimeSetup.Value;
        }

        private static bool OneTimeSetup()
        {
            __collection = LegacyTestConfiguration.Collection;
            __collection.Drop();
            return true;
        }

        [Fact]
        public void TestInsertNullDocument()
        {
            BsonDocument document = null;

            var exception = Record.Exception(() => __collection.Insert(document));

            var argumentNullException = Assert.IsType<ArgumentNullException>(exception);
            Assert.Equal("document", argumentNullException.ParamName);
        }

        [Fact]
        public void TestInsertNullBatch()
        {
            BsonDocument[] batch = null;

            var exception = Record.Exception(() => __collection.InsertBatch(batch));

            var argumentNullException = Assert.IsType<ArgumentNullException>(exception);
            Assert.Equal("documents", argumentNullException.ParamName);
        }

        [Fact]
        public void TestInsertBatchWithNullDocument()
        {
            BsonDocument[] batch = new BsonDocument[] { null };

            var exception = Record.Exception(() => __collection.InsertBatch(batch));

            Assert.IsType<ArgumentException>(exception);
            Assert.Equal("Batch contains one or more null documents.", exception.Message);
        }

        [Fact]
        public void TestSaveNullDocument()
        {
            BsonDocument document = null;

            var exception = Record.Exception(() => __collection.Save(document));

            var argumentNullException = Assert.IsType<ArgumentNullException>(exception);
            Assert.Equal("document", argumentNullException.ParamName);
        }
    }
}

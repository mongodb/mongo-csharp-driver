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
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp594
{
    public class CSharp594Tests
    {
        [Fact]
        public void TestTryMapToBsonValueWithBsonValues()
        {
            // test all the BsonValue subclasses because we removed them from the __fromMappings table
            var testValues = new BsonValue[]
            {
                new BsonArray(),
                new BsonBinaryData(new byte[0]),
                BsonBoolean.True,
                new BsonDateTime(DateTime.UtcNow),
                new BsonDocument("x", 1),
                new BsonDouble(1.0),
                new BsonInt32(1),
                new BsonInt64(1),
                new BsonJavaScript("code"),
                new BsonJavaScriptWithScope("code", new BsonDocument("x", 1)),
                BsonMaxKey.Value,
                BsonMinKey.Value,
                BsonNull.Value,
                new BsonObjectId(ObjectId.GenerateNewId()),
                new BsonRegularExpression("pattern"),
                new BsonString("abc"),
                BsonSymbolTable.Lookup("xyz"),
                new BsonTimestamp(0),
                BsonUndefined.Value
            };
            foreach (var testValue in testValues)
            {
                BsonValue bsonValue;
                var ok = BsonTypeMapper.TryMapToBsonValue(testValue, out bsonValue);
                Assert.Equal(true, ok);
                Assert.Same(testValue, bsonValue);
            }
        }

        [Fact]
        public void TestTryMapToBsonValueWithQueryDocument()
        {
            var query = new QueryDocument("x", 1);
            BsonValue bsonValue;
            var ok = BsonTypeMapper.TryMapToBsonValue(query, out bsonValue);
            Assert.Equal(true, ok);
            Assert.Same(query, bsonValue);
        }

        [Fact]
        public void TestMapToBsonValueWithQueryDocument()
        {
            var query = new QueryDocument("x", 1);
            var bsonDocument = BsonTypeMapper.MapToBsonValue(query, BsonType.Document);
            Assert.Same(query, bsonDocument);
        }
    }
}
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
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    // these tests use the BsonDocument object model to create a BSON document in memory
    // and then serialize it back and forth between byte arrays to make sure nothing is lost in serialization/deserialization

    public class BsonRoundTripTests
    {
        [Fact]
        public void TestHelloWorld()
        {
            BsonDocument document = new BsonDocument
            {
                { "hello", "world" }
            };
            byte[] bytes1 = document.ToBson();
            byte[] bytes2 = BsonSerializer.Deserialize<BsonDocument>(bytes1).ToBson();
            Assert.Equal(bytes1, bytes2);
        }

        [Fact]
        public void TestBsonIsAwesome()
        {
            BsonDocument document = new BsonDocument
            {
                { "BSON", new BsonArray { "awesome", 5.05, 1986} }
            };
            byte[] bytes1 = document.ToBson();
            byte[] bytes2 = BsonSerializer.Deserialize<BsonDocument>(bytes1).ToBson();
            Assert.Equal(bytes1, bytes2);
        }

        [Fact]
        public void TestAllTypes()
        {
            BsonDocument document = new BsonDocument
            {
                { "double", 1.23 },
                { "string", "rosebud" },
                { "document", new BsonDocument { { "hello", "world" } } },
                { "array", new BsonArray { 1, 2, 3 } },
                { "binary", new byte[] { 1, 2, 3 } },
                { "objectid", new ObjectId(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2 }) },
                { "boolean1", false },
                { "boolean2", true }
            };
            byte[] bytes1 = document.ToBson();
            byte[] bytes2 = BsonSerializer.Deserialize<BsonDocument>(bytes1).ToBson();
            Assert.Equal(bytes1, bytes2);
        }
    }
}

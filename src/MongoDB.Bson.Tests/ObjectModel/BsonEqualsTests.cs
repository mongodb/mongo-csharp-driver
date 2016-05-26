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
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonEqualsTests
    {
        [Fact]
        public void TestBsonArrayEquals()
        {
            BsonArray lhs = new BsonArray { 1, 2, 3 };
            BsonArray rhs = new BsonArray().Add(1).Add(2).Add(3);
            Assert.NotSame(lhs, rhs);
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
        }

        [Fact]
        public void TestBsonBinaryDataEquals()
        {
            BsonBinaryData lhs = new BsonBinaryData(new byte[] { 1, 2, 3 }, BsonBinarySubType.Binary);
            BsonBinaryData rhs = new BsonBinaryData(new byte[] { 1, 2, 3 }, BsonBinarySubType.Binary);
            Assert.NotSame(lhs, rhs);
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
        }

        [Fact]
        public void TestBsonDocumentEquals()
        {
            BsonDocument lhs = new BsonDocument
            {
                { "Hello", "World" },
                { "Foo", "Bar" }
            };
            BsonDocument rhs = new BsonDocument
            {
                { "Hello", "World" },
                { "Foo", "Bar" }
            };
            Assert.NotSame(lhs, rhs);
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
        }

        [Fact]
        public void TestBsonElementEquals()
        {
            BsonElement lhs = new BsonElement("Hello", "World");
            BsonElement rhs = new BsonElement("Hello", "World");
            Assert.NotSame(lhs, rhs);
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
        }

        [Fact]
        public void TestBsonJavaScriptEquals()
        {
            BsonJavaScript lhs = new BsonJavaScript("n = 1");
            BsonJavaScript rhs = new BsonJavaScript("n = 1");
            Assert.NotSame(lhs, rhs);
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
        }

        [Fact]
        public void TestBsonJavaScriptWithScopeEquals()
        {
            BsonJavaScriptWithScope lhs = new BsonJavaScriptWithScope("n = 1", new BsonDocument { { "x", "2" } });
            BsonJavaScriptWithScope rhs = new BsonJavaScriptWithScope("n = 1", new BsonDocument { { "x", "2" } });
            Assert.NotSame(lhs, rhs);
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
        }

        [Fact]
        public void TestBsonObjectIdEquals()
        {
            BsonObjectId lhs = new ObjectId(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
            BsonObjectId rhs = new ObjectId(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
            Assert.NotSame(lhs, rhs);
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
        }

        [Fact]
        public void TestBsonRegularExpressionEquals()
        {
            BsonRegularExpression lhs = new BsonRegularExpression("pattern", "options");
            BsonRegularExpression rhs = new BsonRegularExpression("pattern", "options");
            Assert.NotSame(lhs, rhs);
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
        }

        [Fact]
        public void TestBsonSymbolEquals()
        {
            BsonSymbol lhs = BsonSymbolTable.Lookup("name");
            BsonSymbol rhs = BsonSymbolTable.Lookup("name");
            Assert.Same(lhs, rhs);
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
        }

        [Fact]
        public void TestBsonTimestampEquals()
        {
            BsonTimestamp lhs = new BsonTimestamp(1L);
            BsonTimestamp rhs = new BsonTimestamp(1L);
            Assert.NotSame(lhs, rhs);
            Assert.Equal(lhs, rhs);
            Assert.Equal(lhs.GetHashCode(), rhs.GetHashCode());
        }
    }
}

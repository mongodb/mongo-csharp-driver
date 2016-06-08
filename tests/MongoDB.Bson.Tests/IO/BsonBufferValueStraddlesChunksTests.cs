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

using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class BsonBufferValueStraddlesChunksTests
    {
        private static int __chunkSize = 16 * 1024; // 16KiB
        private static int __used = 16;
        private static int __filler = __chunkSize - __used;

        [Fact]
        public void TestNameStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "yyyyyyyy", 1 }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestArrayLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonArray { 1, 2, 3, 4 } }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestBinaryLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonBinaryData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestBinaryDataStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 4) },
                { "y", new BsonBinaryData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestDateTimeStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", DateTime.UtcNow }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestDocumentLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestDoubleStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", 1.0 }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestInt32Straddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", 1 }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestInt64Straddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", 1L }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestJavaScriptCodeLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonJavaScript("adsfasdf") }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestJavaScriptCodeValueStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 4) },
                { "y", new BsonJavaScript("adsfasdf") }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestJavaScriptWithScopeLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonJavaScriptWithScope("adsfasdf", new BsonDocument("a", 1)) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestJavaScriptWithScopeCodeLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 4) },
                { "y", new BsonJavaScriptWithScope("adsfasdf", new BsonDocument("a", 1)) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestJavaScriptWithScopeCodeValueStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 8) },
                { "y", new BsonJavaScriptWithScope("adsfasdf", new BsonDocument("a", 1)) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestObjectIdStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new ObjectId(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestStringLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", "yyyyyyyy" }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestStringValueStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 4) },
                { "y", "yyyyyyyy" }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestSymbolLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", "yyyyyyyy" }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestSymbolValueStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 4) },
                { "y", "yyyyyyyy" }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }

        [Fact]
        public void TestTimestampStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonTimestamp(123456789012345678L) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.Equal(bson, rehydrated.ToBson());
        }
    }
}

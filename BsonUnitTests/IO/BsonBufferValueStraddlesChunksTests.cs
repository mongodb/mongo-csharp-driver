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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.IO
{
    [TestFixture]
    public class BsonBufferValueStraddlesChunksTests
    {
        private static int __chunkSize = 16 * 1024; // 16KiB
        private static int __used = 16;
        private static int __filler = __chunkSize - __used;

        [Test]
        public void TestNameStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "yyyyyyyy", 1 }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestArrayLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonArray { 1, 2, 3, 4 } }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestBinaryLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonBinaryData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestBinaryDataStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 4) },
                { "y", new BsonBinaryData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestDateTimeStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", DateTime.UtcNow }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestDocumentLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestDoubleStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", 1.0 }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestInt32Straddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", 1 }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestInt64Straddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", 1L }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestJavaScriptCodeLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonJavaScript("adsfasdf") }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestJavaScriptCodeValueStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 4) },
                { "y", new BsonJavaScript("adsfasdf") }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestJavaScriptWithScopeLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonJavaScriptWithScope("adsfasdf", new BsonDocument("a", 1)) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestJavaScriptWithScopeCodeLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 4) },
                { "y", new BsonJavaScriptWithScope("adsfasdf", new BsonDocument("a", 1)) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestJavaScriptWithScopeCodeValueStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 8) },
                { "y", new BsonJavaScriptWithScope("adsfasdf", new BsonDocument("a", 1)) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestObjectIdStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new ObjectId(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestStringLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", "yyyyyyyy" }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestStringValueStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 4) },
                { "y", "yyyyyyyy" }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestSymbolLengthStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", "yyyyyyyy" }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestSymbolValueStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler - 4) },
                { "y", "yyyyyyyy" }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }

        [Test]
        public void TestTimestampStraddles()
        {
            var document = new BsonDocument
            {
                { "x", new string('x', __filler) },
                { "y", new BsonTimestamp(123456789012345678L) }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.AreEqual(bson, rehydrated.ToBson());
        }
    }
}

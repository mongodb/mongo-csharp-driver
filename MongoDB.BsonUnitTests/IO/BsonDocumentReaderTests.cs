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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.IO
{
    [TestFixture]
    public class BsonDocumentReaderTests
    {
        [Test]
        public void TestEmptyDocument()
        {
            BsonDocument document = new BsonDocument();
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestSingleString()
        {
            BsonDocument document = new BsonDocument() { { "abc", "xyz" } };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestEmbeddedDocument()
        {
            BsonDocument document = new BsonDocument
            {
                { "doc", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestArray()
        {
            BsonDocument document = new BsonDocument
            {
                { "array", new BsonArray { 1, 2, 3 } }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestDateTime()
        {
            DateTime jan_1_2010 = DateTime.SpecifyKind(new DateTime(2010, 1, 1), DateTimeKind.Utc);
            BsonDocument document = new BsonDocument
            {
                { "date", jan_1_2010 }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestBinary()
        {
            var document = new BsonDocument
            {
                { "bin", new BsonBinaryData(new byte[] { 1, 2, 3 }) }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestJavaScript()
        {
            var document = new BsonDocument
            {
                { "f", new BsonJavaScript("function f() { return 1; }") }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestJavaScriptWithScope()
        {
            var document = new BsonDocument
            {
                { "f", new BsonJavaScriptWithScope("function f() { return n; }", new BsonDocument("n", 1)) }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestGuid()
        {
            var document = new BsonDocument
            {
                { "guid", new Guid("B5F21E0C2A0D42d6AD03D827008D8AB6") }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestMaxKey()
        {
            var document = new BsonDocument
            {
                { "maxkey", BsonMaxKey.Value }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestMinKey()
        {
            var document = new BsonDocument
            {
                { "minkey", BsonMinKey.Value }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestNull()
        {
            var document = new BsonDocument
            {
                { "maxkey", BsonNull.Value }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestSymbol()
        {
            var document = new BsonDocument
            {
                { "symbol", BsonSymbolTable.Lookup("name") }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }

        [Test]
        public void TestTimestamp()
        {
            var document = new BsonDocument
            {
                { "timestamp", new BsonTimestamp(1234567890) }
            };
            using (var bsonReader = BsonReader.Create(document))
            {
                var rehydrated = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                Assert.IsTrue(document.Equals(rehydrated));
            }
        }
    }
}

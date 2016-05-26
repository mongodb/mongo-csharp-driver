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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class BsonDocumentReaderTests
    {
        [Fact]
        public void TestEmptyDocument()
        {
            BsonDocument document = new BsonDocument();
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestSingleString()
        {
            BsonDocument document = new BsonDocument() { { "abc", "xyz" } };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestEmbeddedDocument()
        {
            BsonDocument document = new BsonDocument
            {
                { "doc", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestArray()
        {
            BsonDocument document = new BsonDocument
            {
                { "array", new BsonArray { 1, 2, 3 } }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestDateTime()
        {
            DateTime jan_1_2010 = DateTime.SpecifyKind(new DateTime(2010, 1, 1), DateTimeKind.Utc);
            BsonDocument document = new BsonDocument
            {
                { "date", jan_1_2010 }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestBinary()
        {
            var document = new BsonDocument
            {
                { "bin", new BsonBinaryData(new byte[] { 1, 2, 3 }) }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestJavaScript()
        {
            var document = new BsonDocument
            {
                { "f", new BsonJavaScript("function f() { return 1; }") }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestJavaScriptWithScope()
        {
            var document = new BsonDocument
            {
                { "f", new BsonJavaScriptWithScope("function f() { return n; }", new BsonDocument("n", 1)) }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestGuid()
        {
            var document = new BsonDocument
            {
                { "guid", new Guid("B5F21E0C2A0D42d6AD03D827008D8AB6") }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestIsAtEndOfFile()
        {
            var expected = new BsonDocument("x", 1);

            using (var reader = new BsonDocumentReader(expected))
            {
                var count = 0;
                while (!reader.IsAtEndOfFile())
                {
                    var document = BsonSerializer.Deserialize<BsonDocument>(reader);
                    Assert.Equal(expected, document);
                    count++;
                }
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void TestMaxKey()
        {
            var document = new BsonDocument
            {
                { "maxkey", BsonMaxKey.Value }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestMinKey()
        {
            var document = new BsonDocument
            {
                { "minkey", BsonMinKey.Value }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestNull()
        {
            var document = new BsonDocument
            {
                { "maxkey", BsonNull.Value }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestSymbol()
        {
            var document = new BsonDocument
            {
                { "symbol", BsonSymbolTable.Lookup("name") }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        [Fact]
        public void TestTimestamp()
        {
            var document = new BsonDocument
            {
                { "timestamp", new BsonTimestamp(1234567890) }
            };
            using (var bsonReader = new BsonDocumentReader(document))
            {
                var rehydrated = DeserializeBsonDocument(bsonReader);
                Assert.True(document.Equals(rehydrated));
            }
        }

        private BsonDocument DeserializeBsonDocument(IBsonReader bsonReader)
        {
            var context = BsonDeserializationContext.CreateRoot(bsonReader);
            return BsonDocumentSerializer.Instance.Deserialize(context);
        }
    }
}

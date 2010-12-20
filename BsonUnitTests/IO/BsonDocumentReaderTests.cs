﻿/* Copyright 2010 10gen Inc.
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
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.DefaultSerializer;

namespace MongoDB.BsonUnitTests.IO {
    [TestFixture]
    public class BsonJDocumentReaderTests {
        [Test]
        public void TestEmptyDocument() {
            BsonDocument document = new BsonDocument();
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestSingleString() {
            BsonDocument document = new BsonDocument() { { "abc", "xyz" } };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestEmbeddedDocument() {
            BsonDocument document = new BsonDocument() {
                { "doc", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestArray() {
            BsonDocument document = new BsonDocument() {
                { "array", new BsonArray { 1, 2, 3 } }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestDateTime() {
            DateTime jan_1_2010 = new DateTime(2010, 1, 1);
            BsonDocument document = new BsonDocument() {
                { "date", jan_1_2010 }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestBinary() {
            var document = new BsonDocument {
                { "bin", new BsonBinaryData(new byte[] { 1, 2, 3 }) }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestJavaScript() {
            var document = new BsonDocument {
                { "f", new BsonJavaScript("function f() { return 1; }") }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestJavaScriptWithScope() {
            var document = new BsonDocument {
                { "f", new BsonJavaScriptWithScope("function f() { return n; }", new BsonDocument("n", 1)) }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestGuid() {
            var document = new BsonDocument {
                { "guid", new Guid("B5F21E0C2A0D42d6AD03D827008D8AB6") }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestMaxKey() {
            var document = new BsonDocument {
                { "maxkey", BsonMaxKey.Value }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestMinKey() {
            var document = new BsonDocument {
                { "minkey", BsonMinKey.Value }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestNull() {
            var document = new BsonDocument {
                { "maxkey", BsonNull.Value }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestSymbol() {
            var document = new BsonDocument {
                { "symbol", BsonSymbol.Create("name") }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestTimestamp() {
            var document = new BsonDocument {
                { "timestamp", new BsonTimestamp(1234567890) }
            };
            var rehydrated = BsonDocument.ReadFrom(BsonReader.Create(document));
            Assert.IsTrue(document.Equals(rehydrated));
        }

        [Test]
        public void TestSerializeDeserializeCustomTypes()
        {
            A value = new B(){Num=3, Str="test"};
            A dehydrated = SerializeDeserialize(value);
            Assert.AreEqual(value.Num, dehydrated.Num);
            Assert.AreEqual(value.Str, dehydrated.Str);
        }
        [Test]
        public void TestSerializeDeserializeInterface()
        {
            BsonSerializer.RegisterSerializer(typeof(INum), BsonClassMapSerializer.Singleton);
            INum value = new B() { Num = 3, Str = "test" };
            INum dehydrated = SerializeDeserialize<INum>(value);
            Assert.AreEqual(value.Num, dehydrated.Num);
            Assert.AreEqual(((B)value).Str, ((B)dehydrated).Str);
        }

        private static T SerializeDeserialize<T>(T value)
        {
            BsonDocumentWriter writer = new BsonDocumentWriter();
            BsonSerializer.Serialize(writer, value);

            Console.WriteLine(writer.WrittenValue.ToJson());

            T dehydrated = (T)BsonSerializer.Deserialize(writer.WrittenValue.AsBsonDocument, typeof(T));

            return dehydrated;
        }
        private class A : INum
        {
            public int Num { get; set; }
            public string Str { get; set; }
            public override bool Equals(object obj)
            {
                A other = obj as A;
                return Num.Equals(other.Num) && Str.Equals(other.Str);
            }
            public override int GetHashCode()
            {
                return Num.GetHashCode() + Str.GetHashCode();
            }
        }
        private class B : A { }
        private interface INum
        {
            int Num { get; set; }
        }

    }
}

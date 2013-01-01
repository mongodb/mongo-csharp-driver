/* Copyright 2010-2013 10gen Inc.
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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Options;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonExtensionMethodsTests
    {
        private class C
        {
            public int N;
            public ObjectId Id; // deliberately not the first element
        }

        [Test]
        public void TestToBsonEmptyDocument()
        {
            var document = new BsonDocument();
            var bson = document.ToBson();
            var expected = new byte[] { 5, 0, 0, 0, 0 };
            Assert.IsTrue(expected.SequenceEqual(bson));
        }

        [Test]
        public void TestToBson()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var bson = c.ToBson();
            var expected = new byte[] { 29, 0, 0, 0, 16, 78, 0, 1, 0, 0, 0, 7, 95, 105, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Assert.IsTrue(expected.SequenceEqual(bson));
        }

        [Test]
        public void TestToBsonIdFirst()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var bson = c.ToBson(DocumentSerializationOptions.SerializeIdFirstInstance);
            var expected = new byte[] { 29, 0, 0, 0, 7, 95, 105, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 78, 0, 1, 0, 0, 0, 0 };
            Assert.IsTrue(expected.SequenceEqual(bson));
        }

        [Test]
        public void TestToBsonDocumentEmptyDocument()
        {
            var empty = new BsonDocument();
            var document = empty.ToBsonDocument();
            Assert.AreEqual(0, document.ElementCount);
        }

        [Test]
        public void TestToBsonDocument()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var document = c.ToBsonDocument();
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("N", document.GetElement(0).Name);
            Assert.AreEqual("_id", document.GetElement(1).Name);
            Assert.IsInstanceOf<BsonInt32>(document[0]);
            Assert.IsInstanceOf<BsonObjectId>(document[1]);
            Assert.AreEqual(1, document[0].AsInt32);
            Assert.AreEqual(ObjectId.Empty, document[1].AsObjectId);
        }

        [Test]
        public void TestToBsonDocumentIdFirst()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var document = c.ToBsonDocument(DocumentSerializationOptions.SerializeIdFirstInstance);
            Assert.AreEqual(2, document.ElementCount);
            Assert.AreEqual("_id", document.GetElement(0).Name);
            Assert.AreEqual("N", document.GetElement(1).Name);
            Assert.IsInstanceOf<BsonObjectId>(document[0]);
            Assert.IsInstanceOf<BsonInt32>(document[1]);
            Assert.AreEqual(ObjectId.Empty, document[0].AsObjectId);
            Assert.AreEqual(1, document[1].AsInt32);
        }

        [Test]
        public void TestToJsonEmptyDocument()
        {
            var document = new BsonDocument();
            var json = document.ToJson();
            var expected = "{ }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestToJson()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var json = c.ToJson();
            var expected = "{ 'N' : 1, '_id' : ObjectId('000000000000000000000000') }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestToJsonIdFirst()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var json = c.ToJson(DocumentSerializationOptions.SerializeIdFirstInstance);
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), 'N' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }
    }
}

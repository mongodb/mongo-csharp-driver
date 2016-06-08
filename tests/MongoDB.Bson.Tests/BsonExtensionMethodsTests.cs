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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonExtensionMethodsTests
    {
        private class C
        {
            public int N;
            public ObjectId Id; // deliberately not the first element
        }

        [Fact]
        public void TestToBsonEmptyDocument()
        {
            var document = new BsonDocument();
            var bson = document.ToBson();
            var expected = new byte[] { 5, 0, 0, 0, 0 };
            Assert.True(expected.SequenceEqual(bson));
        }

        [Fact]
        public void TestToBson()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var bson = c.ToBson();
            var expected = new byte[] { 29, 0, 0, 0, 16, 78, 0, 1, 0, 0, 0, 7, 95, 105, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Assert.True(expected.SequenceEqual(bson));
        }

        [Fact]
        public void TestToBsonIdFirst()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var bson = c.ToBson(args: new BsonSerializationArgs { SerializeIdFirst = true });
            var expected = new byte[] { 29, 0, 0, 0, 7, 95, 105, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 78, 0, 1, 0, 0, 0, 0 };
            Assert.True(expected.SequenceEqual(bson));
        }

        [Fact]
        public void TestToBsonDocumentEmptyDocument()
        {
            var empty = new BsonDocument();
            var document = empty.ToBsonDocument();
            Assert.Equal(0, document.ElementCount);
        }

        [Fact]
        public void TestToBsonDocument()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var document = c.ToBsonDocument();
            Assert.Equal(2, document.ElementCount);
            Assert.Equal("N", document.GetElement(0).Name);
            Assert.Equal("_id", document.GetElement(1).Name);
            Assert.IsType<BsonInt32>(document[0]);
            Assert.IsType<BsonObjectId>(document[1]);
            Assert.Equal(1, document[0].AsInt32);
            Assert.Equal(ObjectId.Empty, document[1].AsObjectId);
        }

        [Fact]
        public void TestToBsonDocumentIdFirst()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var document = c.ToBsonDocument(args: new BsonSerializationArgs { SerializeIdFirst = true });
            Assert.Equal(2, document.ElementCount);
            Assert.Equal("_id", document.GetElement(0).Name);
            Assert.Equal("N", document.GetElement(1).Name);
            Assert.IsType<BsonObjectId>(document[0]);
            Assert.IsType<BsonInt32>(document[1]);
            Assert.Equal(ObjectId.Empty, document[0].AsObjectId);
            Assert.Equal(1, document[1].AsInt32);
        }

        [Fact]
        public void TestToJsonEmptyDocument()
        {
            var document = new BsonDocument();
            var json = document.ToJson();
            var expected = "{ }";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestToJson()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var json = c.ToJson();
            var expected = "{ 'N' : 1, '_id' : ObjectId('000000000000000000000000') }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestToJsonIdFirst()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var json = c.ToJson(args: new BsonSerializationArgs { SerializeIdFirst = true });
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), 'N' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }
    }
}

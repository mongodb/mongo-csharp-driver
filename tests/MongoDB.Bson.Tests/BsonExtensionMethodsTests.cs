/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.TestHelpers.XunitExtensions;
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
        public void TestToBsonWithBadEstimatedBsonSizeShouldThrowException()
        {
            var document = new BsonDocument();
            var exception = Record.Exception(() => document.ToBson(estimatedBsonSize: -1));
            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("estimatedBsonSize");
            e.Message.Should().StartWith("Value cannot be negative");
        }

        [Theory]
        [InlineData(13, new byte[] {}, new byte[] { 13, 0, 0, 0, 5, 118, 0, 0, 0, 0, 0, 0, 0 })]
        [InlineData(18, new byte[] { 1, 2, 3, 4, 5}, new byte[] { 18, 0, 0, 0, 5, 118, 0, 5, 0, 0, 0, 0, 1, 2, 3, 4, 5, 0 })]
        [InlineData(32, new byte[] { 1, 2, 3, 4, 5}, new byte[] { 18, 0, 0, 0, 5, 118, 0, 5, 0, 0, 0, 0, 1, 2, 3, 4, 5, 0 })]
        [InlineData(12, new byte[] { 1, 2, 3, 4, 5}, new byte[] { 18, 0, 0, 0, 5, 118, 0, 5, 0, 0, 0, 0, 1, 2, 3, 4, 5, 0 })]
        [InlineData(0, new byte[] { 1, 2, 3, 4, 5}, new byte[] { 18, 0, 0, 0, 5, 118, 0, 5, 0, 0, 0, 0, 1, 2, 3, 4, 5, 0 })]
        public void TestToBsonWithNonZeroEstimatedBsonSize(int estimatedBsonSize, byte[] data, byte[] expectedBson)
        {
            var document = new BsonDocument("v", new BsonBinaryData(data));
            var bson = document.ToBson(estimatedBsonSize: estimatedBsonSize);
            Assert.True(bson.SequenceEqual(expectedBson));
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
            var json = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ }";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestToJson()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'N' : 1, '_id' : ObjectId('000000000000000000000000') }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestToJsonIdFirst()
        {
            var c = new C { N = 1, Id = ObjectId.Empty };
            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell }, args: new BsonSerializationArgs { SerializeIdFirst = true });
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), 'N' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }
    }
}

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

using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class CircularReferencesTests
    {
        public class C
        {
            public int X { get; set; }
            public C NestedDocument { get; set; }
            public BsonArray BsonArray { get; set; }
        }

        [Fact]
        public void TestCircularBsonArray()
        {
            // note: setting a breakpoint in this method will crash the debugger if the locals window is open
            // because it tries to display the value of array (presumably it's getting an internal stack overflow)
            var array = new BsonArray();
            array.Add(array);
            var c1 = new C { X = 1, BsonArray = array };
            Assert.Throws<BsonSerializationException>(() => c1.ToBson());
            Assert.Throws<BsonSerializationException>(() => c1.ToBsonDocument());
            Assert.Throws<BsonSerializationException>(() => c1.ToJson());
        }

        [Fact]
        public void TestCircularDocument()
        {
            var c1 = new C { X = 1 };
            c1.NestedDocument = c1;
            Assert.Throws<BsonSerializationException>(() => c1.ToBson());
            Assert.Throws<BsonSerializationException>(() => c1.ToBsonDocument());
            Assert.Throws<BsonSerializationException>(() => c1.ToJson());
        }

        [Fact]
        public void TestNoCircularReference()
        {
            var c2 = new C { X = 2 };
            var c1 = new C { X = 1, NestedDocument = c2 };

            var json = c1.ToJson();
            var expected = "{ 'X' : 1, 'NestedDocument' : { 'X' : 2, 'NestedDocument' : null, 'BsonArray' : { '_csharpnull' : true } }, 'BsonArray' : { '_csharpnull' : true } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var memoryStream = new MemoryStream();
            using (var writer = new BsonBinaryWriter(memoryStream))
            {
                BsonSerializer.Serialize(writer, c1);
                Assert.Equal(0, writer.SerializationDepth);
            }

            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                BsonSerializer.Serialize(writer, c1);
                Assert.Equal(0, writer.SerializationDepth);
            }

            var stringWriter = new StringWriter();
            using (var writer = new JsonWriter(stringWriter))
            {
                BsonSerializer.Serialize(writer, c1);
                Assert.Equal(0, writer.SerializationDepth);
            }
        }
    }
}

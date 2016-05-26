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
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp270
{
    public class C
    {
        public ObjectId Id;
        [BsonRequired]
        [BsonElement("field")]
        public int Field;
        [BsonRequired]
        [BsonElement("property")]
        public int Property { get; set; }
    }

    public class CSharp270Tests
    {
        [Fact]
        public void TestBogusElement()
        {
            var document = new BsonDocument("bogus", 0);
            var message = "Element 'bogus' does not match any field or property of class MongoDB.Bson.Tests.Jira.CSharp270.C.";
            var ex = Assert.Throws<FormatException>(() => { BsonSerializer.Deserialize<C>(document); });
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void TestMissingElementForField()
        {
            var document = new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "property", 0 }
            };
            var message = "Required element 'field' for field 'Field' of class MongoDB.Bson.Tests.Jira.CSharp270.C is missing.";
            var ex = Assert.Throws<FormatException>(() => { BsonSerializer.Deserialize<C>(document); });
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void TestMissingElementForProperty()
        {
            var document = new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "field", 0 }
            };
            var message = "Required element 'property' for property 'Property' of class MongoDB.Bson.Tests.Jira.CSharp270.C is missing.";
            var ex = Assert.Throws<FormatException>(() => { BsonSerializer.Deserialize<C>(document); });
            Assert.Equal(message, ex.Message);
        }
    }
}

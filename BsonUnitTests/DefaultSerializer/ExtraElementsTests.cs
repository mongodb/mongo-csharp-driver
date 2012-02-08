/* Copyright 2010-2012 10gen Inc.
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
using System.Reflection;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.BsonUnitTests.Serialization
{
    [TestFixture]
    public class BsonExtraElementsTests
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public int Id;
            public int A;
            public int B;
            [BsonExtraElements]
            public BsonDocument X;
        }
#pragma warning restore

        [Test]
        public void TestNoExtraElements()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraInt32Element()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 4 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraInt32ElementNamedX()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'X' : 4 }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraStringElement()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 'xyz' }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestOneExtraDocumentElement()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : { 'D' : 4, 'E' : 'xyz' } }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }

        [Test]
        public void TestTwoExtraElements()
        {
            var json = "{ '_id' : 1, 'A' : 2, 'B' : 3, 'C' : 4, 'D' : 'xyz' }".Replace("'", "\"");
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.AreEqual(json, c.ToJson());
        }
    }
}

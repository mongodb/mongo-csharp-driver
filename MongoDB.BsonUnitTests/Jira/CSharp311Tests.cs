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

using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    public class C1
    {
        public Dictionary<string, object> D;
    }

    public class C2
    {
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<string, object> D;
    }

    public class C3
    {
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, object> D;
    }

    public class C4
    {
        [BsonRepresentation(BsonType.Array)]
        public Dictionary<string, object> D;
    }

    public class D1
    {
        public Hashtable H;
    }

    public class D2
    {
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Hashtable H;
    }

    public class D3
    {
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Hashtable H;
    }

    public class D4
    {
        [BsonRepresentation(BsonType.Array)]
        public Hashtable H;
    }

    [TestFixture]
    public class CSharp311Tests
    {
        [Test]
        public void TestDictionarySerializedAsDocument()
        {
            var c = new C1 { D = new Dictionary<string, object> { { "x", 1 } } };
            var json = c.ToJson();
            var expected = "{ 'D' : { 'x' : 1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C1>(json);
            Assert.AreEqual(1, r.D.Count);
            Assert.AreEqual(1, r.D["x"]);
        }

        [Test]
        public void TestDictionarySerializedAsArrayOfArrays()
        {
            var c = new C2 { D = new Dictionary<string, object> { { "x", 1 } } };
            var json = c.ToJson();
            var expected = "{ 'D' : [['x', 1]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C2>(json);
            Assert.AreEqual(1, r.D.Count);
            Assert.AreEqual(1, r.D["x"]);
        }

        [Test]
        public void TestDictionarySerializedAsArrayOfDocuments()
        {
            var c = new C3 { D = new Dictionary<string, object> { { "x", 1 } } };
            var json = c.ToJson();
            var expected = "{ 'D' : [{ 'k' : 'x', 'v' : 1 }] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C3>(json);
            Assert.AreEqual(1, r.D.Count);
            Assert.AreEqual(1, r.D["x"]);
        }

        [Test]
        public void TestDictionarySerializedAsBsonArray()
        {
            var c = new C4 { D = new Dictionary<string, object> { { "x", 1 } } };
            var json = c.ToJson();
            var expected = "{ 'D' : [['x', 1]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<C4>(json);
            Assert.AreEqual(1, r.D.Count);
            Assert.AreEqual(1, r.D["x"]);
        }

        [Test]
        public void TestHashtableSerializedAsDocument()
        {
            var d = new D1 { H = new Hashtable { { "x", 1 } } };
            var json = d.ToJson();
            var expected = "{ 'H' : { 'x' : 1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<D1>(json);
            Assert.AreEqual(1, r.H.Count);
            Assert.AreEqual(1, r.H["x"]);
        }

        [Test]
        public void TestHashtableSerializedAsArrayOfArrays()
        {
            var d = new D2 { H = new Hashtable { { "x", 1 } } };
            var json = d.ToJson();
            var expected = "{ 'H' : [['x', 1]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<D2>(json);
            Assert.AreEqual(1, r.H.Count);
            Assert.AreEqual(1, r.H["x"]);
        }

        [Test]
        public void TestHashtableSerializedAsArrayOfDocuments()
        {
            var d = new D3 { H = new Hashtable { { "x", 1 } } };
            var json = d.ToJson();
            var expected = "{ 'H' : [{ 'k' : 'x', 'v' : 1 }] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<D3>(json);
            Assert.AreEqual(1, r.H.Count);
            Assert.AreEqual(1, r.H["x"]);
        }

        [Test]
        public void TestHashtableSerializedAsBsonArray()
        {
            var d = new D4 { H = new Hashtable { { "x", 1 } } };
            var json = d.ToJson();
            var expected = "{ 'H' : [['x', 1]] }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var r = BsonSerializer.Deserialize<D4>(json);
            Assert.AreEqual(1, r.H.Count);
            Assert.AreEqual(1, r.H["x"]);
        }
    }
}

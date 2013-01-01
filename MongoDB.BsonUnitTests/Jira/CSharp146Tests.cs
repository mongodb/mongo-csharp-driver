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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp146
{
    public class Doc
    {
        public Hashtable Values { get; set; }
    }

    public enum E
    {
        A,
        B
    }

    public class C
    {
        public E E;
        public object O;
    }

    [TestFixture]
    public class CSharp146Tests
    {
        [Test]
        public void TestClass()
        {
            var c = new C { E = E.B, O = E.B };
            var json = c.ToJson();
            var expected = "{ 'E' : 1, 'O' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var rehydrated = BsonSerializer.Deserialize<C>(json);
            Assert.IsInstanceOf<E>(rehydrated.E);
            Assert.IsInstanceOf<int>(rehydrated.O); // this might be considered a bug that it's not an instance of E
            Assert.AreEqual(E.B, rehydrated.E);
            Assert.AreEqual(1, rehydrated.O);
        }

        [Test]
        public void TestDoc()
        {
            var table = new Hashtable();
            table["Text"] = "hello";
            table["Enum"] = E.B;
            var doc = new Doc { Values = table };

            var json = doc.ToJson();
            // var expected = "{ 'Values' : { 'Text' : 'hello', 'Enum' : 1 } }".Replace("'", "\"");
            // Assert.AreEqual(expected, json);
            var rehydrated = BsonSerializer.Deserialize<Doc>(json);
            Assert.IsNotNull(rehydrated.Values);
            Assert.AreEqual(doc.Values.Count, rehydrated.Values.Count);
            Assert.AreEqual(doc.Values["Text"], rehydrated.Values["Text"]);
            Assert.AreEqual((int)doc.Values["Enum"], rehydrated.Values["Enum"]);
        }
    }
}

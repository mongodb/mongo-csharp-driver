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

using System.Collections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp146
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

    public class CSharp146Tests
    {
        [Fact]
        public void TestClass()
        {
            var c = new C { E = E.B, O = E.B };
            var json = c.ToJson();
            var expected = "{ 'E' : 1, 'O' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var rehydrated = BsonSerializer.Deserialize<C>(json);
            Assert.IsType<E>(rehydrated.E);
            Assert.IsType<int>(rehydrated.O); // this might be considered a bug that it's not an instance of E
            Assert.Equal(E.B, rehydrated.E);
            Assert.Equal(1, rehydrated.O);
        }

        [Fact]
        public void TestDoc()
        {
            var table = new Hashtable();
            table["Text"] = "hello";
            table["Enum"] = E.B;
            var doc = new Doc { Values = table };

            var json = doc.ToJson();
            // var expected = "{ 'Values' : { 'Text' : 'hello', 'Enum' : 1 } }".Replace("'", "\"");
            // Assert.Equal(expected, json);
            var rehydrated = BsonSerializer.Deserialize<Doc>(json);
            Assert.NotNull(rehydrated.Values);
            Assert.Equal(doc.Values.Count, rehydrated.Values.Count);
            Assert.Equal(doc.Values["Text"], rehydrated.Values["Text"]);
            Assert.Equal((int)doc.Values["Enum"], rehydrated.Values["Enum"]);
        }
    }
}

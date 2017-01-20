/* Copyright 2010-2016 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp301Tests
    {
        public class C
        {
            public int Id;
            public object Obj;
        }

        [Fact]
        public void TestDictionaryEmpty()
        {
            var c = new C { Id = 1, Obj = new Dictionary<string, int> { } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Collections.Generic.Dictionary`2[System.String,System.Int32]', '_v' : { } } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestDictionaryOneElement()
        {
            var c = new C { Id = 1, Obj = new Dictionary<string, int> { { "x", 1 } } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Collections.Generic.Dictionary`2[System.String,System.Int32]', '_v' : { 'x' : 1 } } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestHashtableEmpty()
        {
            var c = new C { Id = 1, Obj = new Hashtable { } };
            var json = c.ToJson();
#if NET45
            var discriminator = "System.Collections.Hashtable";
#else
            var discriminator = typeof(Hashtable).AssemblyQualifiedName;
#endif
            var expected = ("{ '_id' : 1, 'Obj' : { '_t' : '" + discriminator + "', '_v' : { } } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestHashtableOneElement()
        {
            var c = new C { Id = 1, Obj = new Hashtable { { "x", 1 } } };
            var json = c.ToJson();
#if NET45
            var discriminator = "System.Collections.Hashtable";
#else
            var discriminator = typeof(Hashtable).AssemblyQualifiedName;
#endif
            var expected = ("{ '_id' : 1, 'Obj' : { '_t' : '" + discriminator + "', '_v' : { 'x' : 1 } } }").Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }
    }
}

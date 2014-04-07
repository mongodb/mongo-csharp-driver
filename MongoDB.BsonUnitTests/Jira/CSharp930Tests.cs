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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp930
{
    [TestFixture]
    public class CSharp930Tests
    {
        private class C
        {
            public Dictionary<string, int> G;
        }

        [Test]
        [TestCase("\u0000\"><script>alert(309)</script>")]
        public void DictionarySerialization(string key)
        {
            var c = new C { G = new Dictionary<string, int> { { key, 1 } } };
            var json = c.ToJson();
            var expected = "{ 'G' : { '#' : 1 } }".Replace("#", key).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.AreEqual(1, rehydrated.G.Count);
            Assert.AreEqual(key, rehydrated.G.Keys.First());
            Assert.AreEqual(1, rehydrated.G[key]);
        }
    }
}

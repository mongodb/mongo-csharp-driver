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
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Jira.CSharp624
{
    [TestFixture]
    public class CSharp624Tests
    {
        private class C
        {
            public int Id;
            public Hashtable D;
            public Dictionary<object, int> G;
        }

        [Test]
        [TestCase("x")]
        [TestCase("x$")]
        public void TestValidKeys(string key)
        {
            var c = new C { Id = 1, D = new Hashtable { { key, 2 } }, G = new Dictionary<object, int> { { key, 3 } } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'D' : { '#' : 2 }, 'G' : { '#' : 3 } }".Replace("#", key).Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.AreEqual(1, rehydrated.Id);
            Assert.AreEqual(1, rehydrated.D.Count);
            Assert.AreEqual(key, rehydrated.D.Keys.Cast<object>().First());
            Assert.AreEqual(2, rehydrated.D[key]);
            Assert.AreEqual(1, rehydrated.G.Count);
            Assert.AreEqual(key, rehydrated.G.Keys.First());
            Assert.AreEqual(3, rehydrated.G[key]);
        }

        [Test]
        [TestCase("")]
        [TestCase("$")]
        [TestCase("$x")]
        [TestCase(".")]
        [TestCase("x.")]
        [TestCase(".y")]
        [TestCase("x.y")]
        public void TestInvalidKeys(string key)
        {
            var c = new C { Id = 1, D = new Hashtable { { key, 2 } }, G = new Dictionary<object, int> { { key, 3 } } };

            using (var stream = new MemoryStream())
            using (var bsonWriter = new BsonBinaryWriter(stream, BsonBinaryWriterSettings.Defaults))
            {
                bsonWriter.PushElementNameValidator(CollectionElementNameValidator.Instance);
                Assert.Throws<BsonSerializationException>(() => BsonSerializer.Serialize(bsonWriter, c));
            }
        }

        [Test]
        [TestCase(1, "1")]
        [TestCase(1.5, "1.5")]
        public void TestKeyIsNotAString(object key, string keyAsString)
        {
            var c = new C { Id = 1, D = new Hashtable { { key, 2 } }, G = new Dictionary<object, int> { { key, 3 } } };
            Assert.Throws<BsonSerializationException>(() => c.ToBson());
        }
    }
}

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
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp624
{
    public class CSharp624Tests
    {
        private class C
        {
            public int Id;
            public Hashtable D;
            public Dictionary<object, int> G;
        }

        [Theory]
        [InlineData("x")]
        [InlineData("x$")]
        public void TestValidKeys(string key)
        {
            var c = new C { Id = 1, D = new Hashtable { { key, 2 } }, G = new Dictionary<object, int> { { key, 3 } } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'D' : { '#' : 2 }, 'G' : { '#' : 3 } }".Replace("#", key).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(1, rehydrated.Id);
            Assert.Equal(1, rehydrated.D.Count);
            Assert.Equal(key, rehydrated.D.Keys.Cast<object>().First());
            Assert.Equal(2, rehydrated.D[key]);
            Assert.Equal(1, rehydrated.G.Count);
            Assert.Equal(key, rehydrated.G.Keys.First());
            Assert.Equal(3, rehydrated.G[key]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("$")]
        [InlineData("$x")]
        [InlineData(".")]
        [InlineData("x.")]
        [InlineData(".y")]
        [InlineData("x.y")]
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

        [Theory]
        [InlineData(1, "1")]
        [InlineData(1.5, "1.5")]
        public void TestKeyIsNotAString(object key, string keyAsString)
        {
            var c = new C { Id = 1, D = new Hashtable { { key, 2 } }, G = new Dictionary<object, int> { { key, 3 } } };
            Assert.Throws<BsonSerializationException>(() => c.ToBson());
        }
    }
}

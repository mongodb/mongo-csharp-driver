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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.DefaultSerializer.Serializers
{
    public class KeyValuePairSerializerTests
    {
        [Fact]
        public void TestNullKey()
        {
            var kvp = new KeyValuePair<string, object>(null, "value");
            var json = kvp.ToJson();
            var expected = "{ 'k' : null, 'v' : 'value' }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = kvp.ToBson();
            var rehydrated = BsonSerializer.Deserialize<KeyValuePair<string, object>>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNullValue()
        {
            var kvp = new KeyValuePair<string, object>("key", null);
            var json = kvp.ToJson();
            var expected = "{ 'k' : 'key', 'v' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = kvp.ToBson();
            var rehydrated = BsonSerializer.Deserialize<KeyValuePair<string, object>>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}

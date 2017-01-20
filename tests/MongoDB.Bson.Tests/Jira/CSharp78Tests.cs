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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp78Tests
    {
        private class C
        {
            public short S { get; set; }
            public object O { get; set; }
        }

        [Fact]
        public void TestShortSerialization()
        {
            var c = new C { S = 1, O = (short)2 };
            var json = c.ToJson();
            var expected = "{ 'S' : 1, 'O' : 2 }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsType<C>(rehydrated);
            Assert.IsType<short>(rehydrated.S);
            Assert.IsType<int>(rehydrated.O); // the short became an int after deserialization
            Assert.Equal(1, rehydrated.S);
            Assert.Equal(2, rehydrated.O);
        }
    }
}

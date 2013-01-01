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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp78Tests
    {
        private class C
        {
            public short S { get; set; }
            public object O { get; set; }
        }

        [Test]
        public void TestShortSerialization()
        {
            var c = new C { S = 1, O = (short)2 };
            var json = c.ToJson();
            var expected = "{ 'S' : 1, 'O' : 2 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsInstanceOf<short>(rehydrated.S);
            Assert.IsInstanceOf<int>(rehydrated.O); // the short became an int after deserialization
            Assert.AreEqual(1, rehydrated.S);
            Assert.AreEqual(2, rehydrated.O);
        }
    }
}

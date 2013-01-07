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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp122
{
    public abstract class B
    {
        [BsonElement("B.N")]
        public int N { get; set; }
        public abstract int A { get; set; }
        public virtual int V { get; set; }
    }

    public class C : B
    {
        [BsonElement("C.N")]
        public new int N { get; set; }
        public override int A { get; set; }
        public override int V { get; set; }
    }

    [TestFixture]
    public class CSharp122Tests
    {
        [Test]
        public void TestTwoPropertiesWithSameName()
        {
            var c = new C { N = 4, A = 2, V = 3 };
            ((B)c).N = 1;
            var json = c.ToJson();
            var expected = "{ 'B.N' : 1, 'A' : 2, 'V' : 3, 'C.N' : 4 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}

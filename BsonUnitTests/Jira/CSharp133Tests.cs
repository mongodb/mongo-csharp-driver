/* Copyright 2010-2011 10gen Inc.
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.BsonUnitTests.Jira.CSharp133
{
    public class C
    {
        public string S;
        [BsonIgnoreIfNull]
        public string I;
        [BsonDefaultValue(null, SerializeDefaultValue = false)] // works the same as [BsonIgnoreIfNull]
        public string D;
    }

    [TestFixture]
    public class CSharp133Tests
    {
        [Test]
        public void TestNull()
        {
            var c = new C { S = null, I = null, D = null };
            var json = c.ToJson();
            var expected = "{ 'S' : null }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.IsNull(rehydrated.S);
            Assert.IsNull(rehydrated.I);
            Assert.IsNull(rehydrated.D);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestNotNull()
        {
            var c = new C { S = "xyz", I = "xyz", D = "xyz" };
            var json = c.ToJson();
            var expected = "{ 'S' : 'xyz', 'I' : 'xyz', 'D' : 'xyz' }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsInstanceOf<C>(rehydrated);
            Assert.AreEqual("xyz", rehydrated.S);
            Assert.AreEqual("xyz", rehydrated.I);
            Assert.AreEqual("xyz", rehydrated.D);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}

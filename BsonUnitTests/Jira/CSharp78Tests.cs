/* Copyright 2010 10gen Inc.
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
using System.Xml;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.Jira {
    [TestFixture]
    public class CSharp78Tests {
        private class C {
            public short S { get; set; }
            public object O { get; set; }
        }

        [Test]
        [Ignore] // TODO: this test is failing right now (Int16 is an unknown discriminator)
        public void TestShortSerialization() {
            var c = new C { S = 1, O = (short) 1 };
            var json = c.ToJson();
            var expected = "{ 'S' : 1, 'O' : { '_t' : 'Int16', '_v' : 1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}

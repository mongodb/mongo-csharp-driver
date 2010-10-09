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
using System.Text;
using NUnit.Framework;

using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary.UnitTests.Serialization {
    [TestFixture]
    public class SerializeInterfaceTests {
        private interface IX {
            string FX { get; set; }
        }

        private class A : IX {
            public static readonly string TypeName = "MongoDB.BsonLibrary.UnitTests.Serialization.SerializeInterfaceTests+A";
            public string FX { get; set; }
        }

        private class B : IX {
            public static readonly string TypeName = "MongoDB.BsonLibrary.UnitTests.Serialization.SerializeInterfaceTests+B";
            public string FX { get; set; }
        }

        [Test]
        public void TestSerializeA() {
            IX a = new A { FX = "a" };
            var json = a.ToJson();
            var expected = ("{ '_t' : '" + A.TypeName + "', 'FX' : 'a' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeB() {
            IX b = new B { FX = "b" };
            var json = b.ToJson();
            var expected = ("{ '_t' : '" + B.TypeName + "', 'FX' : 'b' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }
    }
}

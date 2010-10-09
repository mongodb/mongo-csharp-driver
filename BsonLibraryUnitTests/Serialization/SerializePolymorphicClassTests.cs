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
    public class SerializePolymorphicClassTests {
        private abstract class A {
            public string FA { get; set; }
        }

        private abstract class B : A {
            public string FB { get; set; }
        }

        private class C : A {
            public static readonly string TypeName = "MongoDB.BsonLibrary.UnitTests.Serialization.SerializePolymorphicClassTests+C";
            public string FC { get; set; }
        }

        private class D : B {
            public string FD { get; set; }
        }

        private class E : B {
            public string FE { get; set; }
        }

        private class T {
            public A FT { get; set; }
        }

        [Test]
        public void TestSerializeA() {
            A a = new C { FA = "a", FC = "c" };
            var json = a.ToJson();
            var expected = ("{ '_t' : '" + C.TypeName + "', 'FA' : 'a', 'FC' : 'c' }").Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }
    }
}

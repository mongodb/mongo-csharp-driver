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
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;

namespace MongoDB.BsonUnitTests {
    [TestFixture]
    public class BsonValueTests {
        [Test]
        public void TestBsonValueEqualsFalse() {
            BsonValue a = false;
            Assert.IsTrue(a == false);
            Assert.IsFalse(a != false);
            Assert.IsFalse(a == true);
            Assert.IsTrue(a != true);
        }

        [Test]
        public void TestBsonValueEqualsTrue() {
            BsonValue a = true;
            Assert.IsTrue(a == true);
            Assert.IsFalse(a != true);
            Assert.IsFalse(a == false);
            Assert.IsTrue(a != false);
        }

        [Test]
        public void TestBsonValueEqualsDouble() {
            BsonValue a = 1;
            Assert.IsTrue(a == 1.0);
            Assert.IsFalse(a != 1.0);
            Assert.IsFalse(a == 2.0);
            Assert.IsTrue(a != 2.0);
        }

        [Test]
        public void TestBsonValueEqualsInt32() {
            BsonValue a = 1;
            Assert.IsTrue(a == 1);
            Assert.IsFalse(a != 1);
            Assert.IsFalse(a == 2);
            Assert.IsTrue(a != 2);
        }

        [Test]
        public void TestBsonValueEqualsInt64() {
            BsonValue a = 1;
            Assert.IsTrue(a == 1);
            Assert.IsFalse(a != 1);
            Assert.IsFalse(a == 2);
            Assert.IsTrue(a != 2);
        }
    }
}

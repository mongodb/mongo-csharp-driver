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
using System.Text.RegularExpressions;
using NUnit.Framework;

using MongoDB.Bson;

namespace MongoDB.BsonUnitTests {
    [TestFixture]
    public class BsonValueTests {
        [Test]
        public void TestAsNullableBoolean() {
            BsonValue v = true;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(true, v.AsNullableBoolean);
            Assert.AreEqual(null, n.AsNullableBoolean);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableBoolean; });
        }

        [Test]
        public void TestAsNullableDateTime() {
            var utcNow = DateTime.UtcNow;
            BsonValue v = utcNow;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(utcNow, v.AsNullableDateTime);
            Assert.AreEqual(null, n.AsNullableDateTime);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableDateTime; });
        }

        [Test]
        public void TestAsNullableDouble() {
            BsonValue v = 1.5;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(1.5, v.AsNullableDouble);
            Assert.AreEqual(null, n.AsNullableDouble);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableDouble; });
        }

        [Test]
        public void TestAsNullableGuid() {
            Guid guid = Guid.NewGuid();
            BsonValue v = guid;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(guid, v.AsNullableGuid);
            Assert.AreEqual(null, n.AsNullableGuid);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableGuid; });
        }

        [Test]
        public void TestAsNullableInt32() {
            BsonValue v = 1;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(1, v.AsNullableInt32);
            Assert.AreEqual(null, n.AsNullableInt32);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableInt32; });
        }

        [Test]
        public void TestAsNullableInt64() {
            BsonValue v = 1L;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(1L, v.AsNullableInt64);
            Assert.AreEqual(null, n.AsNullableInt64);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableInt64; });
        }

        [Test]
        public void TestAsNullableObjectId() {
            var objectId = ObjectId.GenerateNewId();
            BsonValue v = objectId;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(objectId, v.AsNullableObjectId);
            Assert.AreEqual(null, n.AsNullableObjectId);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableObjectId; });
        }

        [Test]
        public void TestBsonRegularExpressionConstructors() {
            var regex = BsonRegularExpression.Create("pattern");
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern", regex.Pattern);
            Assert.AreEqual("", regex.Options);

            regex = BsonRegularExpression.Create("/pattern/i");
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern", regex.Pattern);
            Assert.AreEqual("i", regex.Options);

            regex = BsonRegularExpression.Create(@"/pattern\/withslash/i");
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual(@"pattern\/withslash", regex.Pattern);
            Assert.AreEqual("i", regex.Options);

            regex = BsonRegularExpression.Create("pattern", "i");
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern", regex.Pattern);
            Assert.AreEqual("i", regex.Options);

            regex = BsonRegularExpression.Create(new Regex("pattern"));
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern", regex.Pattern);
            Assert.AreEqual("", regex.Options);

            regex = BsonRegularExpression.Create(new Regex("pattern", RegexOptions.IgnoreCase));
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern", regex.Pattern);
            Assert.AreEqual("i", regex.Options);
        }

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

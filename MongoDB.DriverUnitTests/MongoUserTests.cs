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

using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoUserTests
    {
        private MongoUser a = new MongoUser(new MongoCredentials("a", "p"), false);
        private MongoUser b = new MongoUser(new MongoCredentials("a", "p"), false);
        private MongoUser c = new MongoUser(new MongoCredentials("c", "p"), false);
        private MongoUser q = new MongoUser(new MongoCredentials("a", "q"), false);
        private MongoUser r = new MongoUser(new MongoCredentials("a", "p"), true);

        [Test]
        public void TestConstructor1()
        {
            var u = new MongoUser(new MongoCredentials("u", "p"), true);
            var ph = MongoUser.HashPassword("u", "p");
            Assert.AreEqual("u", u.Username);
            Assert.AreEqual(ph, u.PasswordHash);
            Assert.AreEqual(true, u.IsReadOnly);
        }

        [Test]
        public void TestConstructor2()
        {
            var h = MongoUser.HashPassword("u", "p");
            var u = new MongoUser("u", h, true);
            Assert.AreEqual("u", u.Username);
            Assert.AreEqual(h, u.PasswordHash);
            Assert.AreEqual(true, u.IsReadOnly);
        }

        [Test]
        public void TestEquals()
        {
            var a1 = new MongoUser("u", "h", false);
            var a2 = new MongoUser("u", "h", false);
            var a3 = a2;
            var b = new MongoUser("x", "h", false);
            var c = new MongoUser("u", "x", false);
            var d = new MongoUser("u", "h", true);
            var null1 = (MongoUser)null;
            var null2 = (MongoUser)null;

            Assert.AreNotSame(a1, a2);
            Assert.AreSame(a2, a3);
            Assert.IsTrue(a1.Equals((object)a2));
            Assert.IsFalse(a1.Equals((object)null));
            Assert.IsFalse(a1.Equals((object)"x"));

            Assert.IsTrue(a1 == a2);
            Assert.IsTrue(a2 == a3);
            Assert.IsFalse(a1 == b);
            Assert.IsFalse(a1 == c);
            Assert.IsFalse(a1 == d);
            Assert.IsFalse(a1 == null1);
            Assert.IsFalse(null1 == a1);
            Assert.IsTrue(null1 == null2);

            Assert.IsFalse(a1 != a2);
            Assert.IsFalse(a2 != a3);
            Assert.IsTrue(a1 != b);
            Assert.IsTrue(a1 != c);
            Assert.IsTrue(a1 != d);
            Assert.IsTrue(a1 != null1);
            Assert.IsTrue(null1 != a1);
            Assert.IsFalse(null1 != null2);

            Assert.AreEqual(a1.GetHashCode(), a2.GetHashCode());
        }
    }
}

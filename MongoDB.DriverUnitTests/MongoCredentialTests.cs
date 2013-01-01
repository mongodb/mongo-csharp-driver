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
    public class MongoCredentialsTests
    {
        [Test]
        public void TestCreateAdminCredentials()
        {
            var credentials = MongoCredentials.Create("username(admin)", "password");
            Assert.AreEqual("username", credentials.Username);
            Assert.AreEqual("password", credentials.Password);
            Assert.IsTrue(credentials.Admin);

            credentials = new MongoCredentials("username(admin)", "password");
            Assert.AreEqual("username", credentials.Username);
            Assert.AreEqual("password", credentials.Password);
            Assert.IsTrue(credentials.Admin);

            credentials = new MongoCredentials("username", "password", true);
            Assert.AreEqual("username", credentials.Username);
            Assert.AreEqual("password", credentials.Password);
            Assert.IsTrue(credentials.Admin);
        }

        [Test]
        public void TestCreateCredentials()
        {
            var credentials = new MongoCredentials("username", "password");
            Assert.AreEqual("username", credentials.Username);
            Assert.AreEqual("password", credentials.Password);
            Assert.IsFalse(credentials.Admin);

            credentials = new MongoCredentials("username", "password", false);
            Assert.AreEqual("username", credentials.Username);
            Assert.AreEqual("password", credentials.Password);
            Assert.IsFalse(credentials.Admin);
        }

        [Test]
        public void TestEquals()
        {
            var a = new MongoCredentials("user1", "password");
            var b = new MongoCredentials("user1", "password");
            var c = new MongoCredentials("user2", "password");
            var n = (MongoCredentials)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }
    }
}

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

using System;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoServerAddressTests
    {
        [Test]
        [TestCase("host")]
        [TestCase("192.168.0.1")]
        [TestCase("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]")]
        public void TestConstructor(string host)
        {
            var address = new MongoServerAddress(host);
            Assert.AreEqual(host, address.Host);
            Assert.AreEqual(27017, address.Port);
        }

        [Test]
        [TestCase("host", 27017)]
        [TestCase("host", 27018)]
        [TestCase("192.168.0.1", 27017)]
        [TestCase("192.168.0.1", 27018)]
        [TestCase("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]", 27017)]
        [TestCase("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]", 27018)]
        public void TestConstructor(string host, int port)
        {
            var address = new MongoServerAddress(host, port);
            Assert.AreEqual(host, address.Host);
            Assert.AreEqual(port, address.Port);
        }

        [Test]
        public void TestEquals()
        {
            var a = new MongoServerAddress("host1");
            var b = new MongoServerAddress("host1");
            var c = new MongoServerAddress("host2");
            var n = (MongoServerAddress)null;

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

        [Test]
        [TestCase("host", 27017, "host")]
        [TestCase("host", 27017, "host:27017")]
        [TestCase("host", 27018, "host:27018")]
        [TestCase("192.168.0.1", 27017, "192.168.0.1")]
        [TestCase("192.168.0.1", 27017, "192.168.0.1:27017")]
        [TestCase("192.168.0.1", 27018, "192.168.0.1:27018")]
        [TestCase("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]", 27017, "[2001:0db8:85a3:0042:0000:8a2e:0370:7334]")]
        [TestCase("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]", 27017, "[2001:0db8:85a3:0042:0000:8a2e:0370:7334]:27017")]
        [TestCase("[2001:0db8:85a3:0042:0000:8a2e:0370:7334]", 27018, "[2001:0db8:85a3:0042:0000:8a2e:0370:7334]:27018")]
        public void TestParse(string host, int port, string value)
        {
            var address = MongoServerAddress.Parse(value);
            Assert.AreEqual(host, address.Host);
            Assert.AreEqual(port, address.Port);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("abc:def")]
        [TestCase("abc:123:456")]
        [TestCase("[]")]
        [TestCase("a[]")]
        [TestCase("[]b")]
        [TestCase("a[]b")]
        public void TestParse_InvalidValue(string value)
        {
            var message = string.Format("'{0}' is not a valid server address.", value);
            Assert.Throws<FormatException>(() => { var address = MongoServerAddress.Parse(value); }, message);
        }
    }
}

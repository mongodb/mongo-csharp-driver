/* Copyright 2010-2012 10gen Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoServerAddressTests
    {
        [Test]
        public void TestCreateWithHost()
        {
            var credentials = new MongoServerAddress("host");
            Assert.AreEqual("host", credentials.Host);
            Assert.AreEqual(27017, credentials.Port);
        }

        [Test]
        public void TestCreateWithHostAndPort()
        {
            var credentials = new MongoServerAddress("host", 123);
            Assert.AreEqual("host", credentials.Host);
            Assert.AreEqual(123, credentials.Port);
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
        public void TestParseWithHost()
        {
            var credentials = MongoServerAddress.Parse("host");
            Assert.AreEqual("host", credentials.Host);
            Assert.AreEqual(27017, credentials.Port);
        }

        [Test]
        public void TestParseWithHostAndPort()
        {
            var credentials = MongoServerAddress.Parse("host:123");
            Assert.AreEqual("host", credentials.Host);
            Assert.AreEqual(123, credentials.Port);
        }
    }
}

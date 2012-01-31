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
    public class MongoSafeModeTests
    {
        [Test]
        public void TestCreateWithEnabled()
        {
            var safeMode = new SafeMode(true);
            Assert.AreEqual(true, safeMode.Enabled);
            Assert.AreEqual(false, safeMode.FSync);
            Assert.AreEqual(false, safeMode.J);
            Assert.AreEqual(0, safeMode.W);
            Assert.AreEqual(null, safeMode.WMode);
            Assert.AreEqual(TimeSpan.Zero, safeMode.WTimeout);
        }

        [Test]
        public void TestCreateWithEnabledAndFSync()
        {
            var safeMode = new SafeMode(true, true);
            Assert.AreEqual(true, safeMode.Enabled);
            Assert.AreEqual(true, safeMode.FSync);
            Assert.AreEqual(false, safeMode.J);
            Assert.AreEqual(0, safeMode.W);
            Assert.AreEqual(null, safeMode.WMode);
            Assert.AreEqual(TimeSpan.Zero, safeMode.WTimeout);
        }

        [Test]
        public void TestCreateWithEnabledAndFSyncAndW()
        {
            var safeMode = new SafeMode(true, true, 2);
            Assert.AreEqual(true, safeMode.Enabled);
            Assert.AreEqual(true, safeMode.FSync);
            Assert.AreEqual(false, safeMode.J);
            Assert.AreEqual(2, safeMode.W);
            Assert.AreEqual(null, safeMode.WMode);
            Assert.AreEqual(TimeSpan.Zero, safeMode.WTimeout);
        }

        [Test]
        public void TestCreateWithEnabledAndFSyncAndWAndWTimeout()
        {
            var safeMode = new SafeMode(true, true, 2, TimeSpan.FromSeconds(30));
            Assert.AreEqual(true, safeMode.Enabled);
            Assert.AreEqual(true, safeMode.FSync);
            Assert.AreEqual(false, safeMode.J);
            Assert.AreEqual(2, safeMode.W);
            Assert.AreEqual(null, safeMode.WMode);
            Assert.AreEqual(TimeSpan.FromSeconds(30), safeMode.WTimeout);
        }

        [Test]
        public void TestCreateWithW()
        {
            var safeMode = new SafeMode(2);
            Assert.AreEqual(true, safeMode.Enabled);
            Assert.AreEqual(false, safeMode.FSync);
            Assert.AreEqual(false, safeMode.J);
            Assert.AreEqual(2, safeMode.W);
            Assert.AreEqual(null, safeMode.WMode);
            Assert.AreEqual(TimeSpan.Zero, safeMode.WTimeout);
        }

        [Test]
        public void TestCreateWithWAndTimeout()
        {
            var safeMode = new SafeMode(2, TimeSpan.FromSeconds(30));
            Assert.AreEqual(true, safeMode.Enabled);
            Assert.AreEqual(false, safeMode.FSync);
            Assert.AreEqual(false, safeMode.J);
            Assert.AreEqual(2, safeMode.W);
            Assert.AreEqual(null, safeMode.WMode);
            Assert.AreEqual(TimeSpan.FromSeconds(30), safeMode.WTimeout);
        }

        [Test]
        public void TestCreateWithOther()
        {
            var safeMode = new SafeMode(SafeMode.W2);
            Assert.AreEqual(SafeMode.W2, safeMode);
        }

        [Test]
        public void TestCreateWithWMode()
        {
            var safeMode = new SafeMode(true) { WMode = "majority" };
            Assert.AreEqual(true, safeMode.Enabled);
            Assert.AreEqual(false, safeMode.FSync);
            Assert.AreEqual(false, safeMode.J);
            Assert.AreEqual(0, safeMode.W);
            Assert.AreEqual("majority", safeMode.WMode);
            Assert.AreEqual(TimeSpan.Zero, safeMode.WTimeout);
        }

        [Test]
        public void TestEquals()
        {
            var a = new SafeMode(false);
            var b = new SafeMode(false);
            var c = new SafeMode(true);
            var n = (SafeMode)null;

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

        // CSHARP-386
        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "SafeMode has been frozen and no further changes are allowed.")]
        public void TestSafeModeFalseIsFrozen()
        {
            var s = SafeMode.False;
            s.Enabled = true;
        }

        // CSHARP-386
        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "SafeMode has been frozen and no further changes are allowed.")]
        public void TestSafeModeFSyncTrueIsFrozen()
        {
            var s = SafeMode.FSyncTrue;
            s.Enabled = true;
        }

        // CSHARP-386
        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "SafeMode has been frozen and no further changes are allowed.")]
        public void TestSafeModeTrueIsFrozen()
        {
            var s = SafeMode.True;
            s.Enabled = true;
        }

        // CSHARP-386
        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "SafeMode has been frozen and no further changes are allowed.")]
        public void TestSafeModeW2IsFrozen()
        {
            var s = SafeMode.W2;
            s.Enabled = true;
        }

        // CSHARP-386
        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "SafeMode has been frozen and no further changes are allowed.")]
        public void TestSafeModeW3IsFrozen()
        {
            var s = SafeMode.W3;
            s.Enabled = true;
        }

        // CSHARP-386
        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "SafeMode has been frozen and no further changes are allowed.")]
        public void TestSafeModeW4IsFrozen()
        {
            var s = SafeMode.W4;
            s.Enabled = true;
        }

    }
}

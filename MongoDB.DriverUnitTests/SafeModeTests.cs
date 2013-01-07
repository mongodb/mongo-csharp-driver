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
    public class MongoSafeModeTests
    {
#pragma warning disable 618
        [Test]
        public void TestCreateWithEnabled()
        {
            var safeMode = new SafeMode(true);
            Assert.AreEqual(true, safeMode.Enabled);
            Assert.AreEqual(false, safeMode.FSync);
            Assert.AreEqual(false, safeMode.Journal);
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
            Assert.AreEqual(false, safeMode.Journal);
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
            Assert.AreEqual(false, safeMode.Journal);
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
            Assert.AreEqual(false, safeMode.Journal);
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
            Assert.AreEqual(false, safeMode.Journal);
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
            Assert.AreEqual(false, safeMode.Journal);
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
            Assert.AreEqual(false, safeMode.Journal);
            Assert.AreEqual(0, safeMode.W);
            Assert.AreEqual("majority", safeMode.WMode);
            Assert.AreEqual(TimeSpan.Zero, safeMode.WTimeout);
        }

        [Test]
        public void TestEquals()
        {
            var a1 = new SafeMode(false);
            var a2 = new SafeMode(true) { Enabled = false };
            var a3 = a2;
            var b = new SafeMode(false) { Enabled = true };
            var c = new SafeMode(false) { FSync = true };
            var d = new SafeMode(false) { Journal = true };
            var e = new SafeMode(false) { W = 2 };
            var f = new SafeMode(false) { WMode = "mode" };
            var g = new SafeMode(false) { W = 2, WTimeout = TimeSpan.FromMinutes(1) };
            var null1 = (SafeMode)null;
            var null2 = (SafeMode)null;

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
            Assert.IsFalse(a1 == e);
            Assert.IsFalse(a1 == f);
            Assert.IsFalse(a1 == g);
            Assert.IsFalse(a1 == null1);
            Assert.IsFalse(null1 == a1);
            Assert.IsTrue(null1 == null2);

            Assert.IsFalse(a1 != a2);
            Assert.IsFalse(a2 != a3);
            Assert.IsTrue(a1 != b);
            Assert.IsTrue(a1 != c);
            Assert.IsTrue(a1 != d);
            Assert.IsTrue(a1 != e);
            Assert.IsTrue(a1 != f);
            Assert.IsTrue(a1 != g);
            Assert.IsTrue(a1 != null1);
            Assert.IsTrue(null1 != a1);
            Assert.IsFalse(null1 != null2);

            var hash = a1.GetHashCode();
            Assert.AreEqual(hash, a2.GetHashCode());

            // check that all tests still pass after objects are Frozen
            a1.Freeze();
            a2.Freeze();
            a3.Freeze();
            b.Freeze();
            c.Freeze();
            d.Freeze();
            e.Freeze();
            f.Freeze();
            g.Freeze();

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
            Assert.IsFalse(a1 == e);
            Assert.IsFalse(a1 == f);
            Assert.IsFalse(a1 == g);
            Assert.IsFalse(a1 == null1);
            Assert.IsFalse(null1 == a1);
            Assert.IsTrue(null1 == null2);

            Assert.IsFalse(a1 != a2);
            Assert.IsFalse(a2 != a3);
            Assert.IsTrue(a1 != b);
            Assert.IsTrue(a1 != c);
            Assert.IsTrue(a1 != d);
            Assert.IsTrue(a1 != e);
            Assert.IsTrue(a1 != f);
            Assert.IsTrue(a1 != g);
            Assert.IsTrue(a1 != null1);
            Assert.IsTrue(null1 != a1);
            Assert.IsFalse(null1 != null2);

            Assert.AreEqual(hash, a1.GetHashCode());
            Assert.AreEqual(hash, a2.GetHashCode());
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
#pragma warning restore
    }
}

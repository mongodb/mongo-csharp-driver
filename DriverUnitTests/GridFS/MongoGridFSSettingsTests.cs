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
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace MongoDB.DriverUnitTests.GridFS
{
    [TestFixture]
    public class MongoGridFSSettingsTests
    {
        [Test]
        public void TestDefaults()
        {
            var settings = new MongoGridFSSettings();
            Assert.IsFalse(settings.IsFrozen);
            Assert.AreEqual("fs.chunks", settings.ChunksCollectionName);
            Assert.AreEqual(256 * 1024, settings.ChunkSize);
            Assert.AreEqual("fs.files", settings.FilesCollectionName);
            Assert.AreEqual("fs", settings.Root);
            Assert.AreEqual(SafeMode.False, settings.SafeMode);
        }

        [Test]
        public void TestCreation()
        {
            var settings = new MongoGridFSSettings
            {
                ChunkSize = 64 * 1024,
                Root = "root",
                SafeMode = SafeMode.True
            };
            Assert.IsFalse(settings.IsFrozen);
            Assert.AreEqual("root.chunks", settings.ChunksCollectionName);
            Assert.AreEqual(64 * 1024, settings.ChunkSize);
            Assert.AreEqual("root.files", settings.FilesCollectionName);
            Assert.AreEqual("root", settings.Root);
            Assert.AreEqual(SafeMode.True, settings.SafeMode);
        }

        [Test]
        public void TestCloneAndEquals()
        {
            var settings = new MongoGridFSSettings
            {
                ChunkSize = 64 * 1024,
                Root = "root",
                SafeMode = SafeMode.True
            };
            var clone = settings.Clone();
            Assert.IsTrue(settings == clone);
            Assert.AreEqual(settings, clone);
        }

        [Test]
        public void TestEquals()
        {
            var a = new MongoGridFSSettings(123, "root", SafeMode.True);
            var b = new MongoGridFSSettings(123, "root", SafeMode.True);
            var c = new MongoGridFSSettings(345, "root", SafeMode.True);
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

        [Test]
        public void TestFreeze()
        {
            var settings = new MongoGridFSSettings();
            Assert.IsFalse(settings.IsFrozen);
            settings.Freeze();
            Assert.IsTrue(settings.IsFrozen);
            settings.Freeze(); // test that it's OK to call Freeze more than once
            Assert.IsTrue(settings.IsFrozen);
            Assert.Throws<InvalidOperationException>(() => settings.ChunkSize = 64 * 1024);
            Assert.Throws<InvalidOperationException>(() => settings.Root = "root");
            Assert.Throws<InvalidOperationException>(() => settings.SafeMode = SafeMode.True);
        }
    }
}

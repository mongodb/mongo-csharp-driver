/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver.GridFS;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.GridFS
{
    [TestFixture]
    public class MongoGridFSSettingsTests
    {
        private MongoDatabase _database;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _database = Configuration.TestDatabase;
        }

        [Test]
        public void TestDefaults()
        {
            var settings = MongoGridFSSettings.Defaults;
            Assert.AreEqual(256 * 1024, settings.ChunkSize);
            Assert.AreEqual("fs", settings.Root);
            Assert.AreEqual(true, settings.UpdateMD5);
            Assert.AreEqual(true, settings.VerifyMD5);
            Assert.AreEqual(true, settings.IsFrozen);
            Assert.AreEqual(null, settings.WriteConcern);
        }

        [Test]
        public void TestDefaultsObsolete()
        {
#pragma warning disable 618
            var settings = new MongoGridFSSettings();
            Assert.IsFalse(settings.IsFrozen);
            Assert.AreEqual(0, settings.ChunkSize);
            Assert.AreEqual(null, settings.Root);
            Assert.AreEqual(null, settings.WriteConcern);
#pragma warning restore
        }

        public void TestCreation()
        {
            var settings = new MongoGridFSSettings()
            {
                ChunkSize = 64 * 1024,
                Root = "root",
                UpdateMD5 = true,
                VerifyMD5 = true,
                WriteConcern = WriteConcern.Acknowledged
            };
            Assert.AreEqual(64 * 1024, settings.ChunkSize);
            Assert.AreEqual("root", settings.Root);
            Assert.AreEqual(true, settings.UpdateMD5);
            Assert.AreEqual(true, settings.VerifyMD5);
            Assert.AreEqual(WriteConcern.Acknowledged, settings.WriteConcern);
            Assert.AreEqual(false, settings.IsFrozen);
        }

        [Test]
        public void TestCreationEmpty()
        {
            var settings = new MongoGridFSSettings();
            Assert.AreEqual(0, settings.ChunkSize);
            Assert.AreEqual(null, settings.Root);
            Assert.AreEqual(false, settings.UpdateMD5);
            Assert.AreEqual(false, settings.VerifyMD5);
            Assert.AreEqual(false, settings.IsFrozen);
            Assert.AreEqual(null, settings.WriteConcern);
        }

        [Test]
        public void TestCreationObsolete()
        {
#pragma warning disable 618
            var settings = new MongoGridFSSettings
            {
                ChunkSize = 64 * 1024,
                Root = "root",
                WriteConcern = WriteConcern.Acknowledged
            };
            Assert.IsFalse(settings.IsFrozen);
            Assert.AreEqual("root.chunks", settings.ChunksCollectionName);
            Assert.AreEqual(64 * 1024, settings.ChunkSize);
            Assert.AreEqual("root.files", settings.FilesCollectionName);
            Assert.AreEqual("root", settings.Root);
            Assert.AreEqual(WriteConcern.Acknowledged, settings.WriteConcern);
#pragma warning restore
        }

        [Test]
        public void TestCloneAndEquals()
        {
            var settings = new MongoGridFSSettings()
            {
                ChunkSize = 64 * 1024,
                Root = "root",
                UpdateMD5 = false,
                VerifyMD5 = false,
                WriteConcern = WriteConcern.Acknowledged
            };
            var clone = settings.Clone();
            Assert.IsTrue(settings == clone);
            Assert.AreEqual(settings, clone);
        }

        [Test]
        public void TestEquals()
        {
            var a = new MongoGridFSSettings() { ChunkSize = 123 };
            var b = new MongoGridFSSettings() { ChunkSize = 123 };
            var c = new MongoGridFSSettings() { ChunkSize = 345 };
            var n = (WriteConcern)null;

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
            Assert.Throws<InvalidOperationException>(() => settings.UpdateMD5 = true);
            Assert.Throws<InvalidOperationException>(() => settings.VerifyMD5 = true);
            Assert.Throws<InvalidOperationException>(() => settings.WriteConcern = WriteConcern.Acknowledged);
        }
    }
}

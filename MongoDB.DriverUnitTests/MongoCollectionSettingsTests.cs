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
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoCollectionSettingsTests
    {
        private MongoClient _client;
        private MongoServer _server;
        private MongoDatabase _database;

        [TestFixtureSetUp()]
        public void TestFixtureSetUp()
        {
            _client = new MongoClient();
            _server = _client.GetServer();
            _database = _server.GetDatabase("database");
        }

        [Test]
        public void TestAll()
        {
            var settings = new MongoCollectionSettings
            {
                AssignIdOnInsert = true,
                GuidRepresentation = GuidRepresentation.PythonLegacy,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.Acknowledged
            };

            Assert.AreEqual(true, settings.AssignIdOnInsert);
            Assert.AreEqual(GuidRepresentation.PythonLegacy, settings.GuidRepresentation);
            Assert.AreSame(ReadPreference.Primary, settings.ReadPreference);
            Assert.AreSame(WriteConcern.Acknowledged, settings.WriteConcern);
        }

        [Test]
        public void TestAllObsolete()
        {
#pragma warning disable 618
            var settings = new MongoCollectionSettings<BsonDocument>(_database, "collection")
            {
                AssignIdOnInsert = true,
                GuidRepresentation = GuidRepresentation.PythonLegacy,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.Acknowledged
            };

            Assert.AreEqual("collection", settings.CollectionName);
            Assert.AreEqual(typeof(BsonDocument), settings.DefaultDocumentType);
            Assert.AreEqual(true, settings.AssignIdOnInsert);
            Assert.AreEqual(GuidRepresentation.PythonLegacy, settings.GuidRepresentation);
            Assert.AreSame(ReadPreference.Primary, settings.ReadPreference);
            Assert.AreSame(WriteConcern.Acknowledged, settings.WriteConcern);
#pragma warning restore
        }

        [Test]
        public void TestAssignIdOnInsert()
        {
            var settings = new MongoCollectionSettings();
            Assert.AreEqual(false, settings.AssignIdOnInsert);

            var assignIdOnInsert = !settings.AssignIdOnInsert;
            settings.AssignIdOnInsert = assignIdOnInsert;
            Assert.AreEqual(assignIdOnInsert, settings.AssignIdOnInsert);

            settings.Freeze();
            Assert.AreEqual(assignIdOnInsert, settings.AssignIdOnInsert);
            Assert.Throws<InvalidOperationException>(() => { settings.AssignIdOnInsert = assignIdOnInsert; });
        }

        [Test]
        public void TestClone()
        {
            // set everything to non default values to test that all settings are cloned
            var settings = new MongoCollectionSettings
            {
                AssignIdOnInsert = !MongoDefaults.AssignIdOnInsert,
                GuidRepresentation = GuidRepresentation.PythonLegacy,
                ReadPreference = ReadPreference.Secondary,
                WriteConcern = WriteConcern.W2
            };
            var clone = settings.Clone();
            Assert.IsTrue(clone.Equals(settings));
        }

        [Test]
        public void TestCollectionNameObsolete()
        {
#pragma warning disable 618
            var settings = new MongoCollectionSettings<BsonDocument>(_database, "collection");
            Assert.AreEqual("collection", settings.CollectionName);

            settings.Freeze();
            Assert.AreEqual("collection", settings.CollectionName);
#pragma warning restore
        }

        [Test]
        public void TestConstructor()
        {
            var settings = new MongoCollectionSettings();
            Assert.AreEqual(false, settings.AssignIdOnInsert);
            Assert.AreEqual(GuidRepresentation.Unspecified, settings.GuidRepresentation);
            Assert.AreEqual(null, settings.ReadPreference);
            Assert.AreEqual(null, settings.WriteConcern);
        }

        [Test]
        public void TestConstructorObsolete()
        {
#pragma warning disable 618
            var settings = new MongoCollectionSettings<BsonDocument>(_database, "collection");
            Assert.AreEqual("collection", settings.CollectionName);
            Assert.AreEqual(typeof(BsonDocument), settings.DefaultDocumentType);
            Assert.AreEqual(MongoDefaults.AssignIdOnInsert, settings.AssignIdOnInsert);
            Assert.AreEqual(_database.Settings.GuidRepresentation, settings.GuidRepresentation);
            Assert.AreEqual(_database.Settings.ReadPreference, settings.ReadPreference);
            Assert.AreEqual(_database.Settings.WriteConcern, settings.WriteConcern);
#pragma warning restore
        }

        [Test]
        public void TestDefaultDocumentTypeObsolete()
        {
#pragma warning disable 618
            var settings = new MongoCollectionSettings<BsonDocument>(_database, "collection");
            Assert.AreEqual(typeof(BsonDocument), settings.DefaultDocumentType);

            settings.Freeze();
            Assert.AreEqual(typeof(BsonDocument), settings.DefaultDocumentType);
#pragma warning restore
        }

        [Test]
        public void TestEquals()
        {
            var settings = new MongoCollectionSettings();
            var clone = settings.Clone();
            Assert.IsTrue(clone.Equals(settings));

            settings.Freeze();
            clone.Freeze();
            Assert.IsTrue(clone.Equals(settings));

            clone = settings.Clone();
            clone.AssignIdOnInsert = !clone.AssignIdOnInsert;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.GuidRepresentation = GuidRepresentation.PythonLegacy;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.ReadPreference = ReadPreference.Secondary;
            Assert.IsFalse(clone.Equals(settings));

#pragma warning disable 618
            clone = settings.Clone();
            clone.SafeMode = SafeMode.W2;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.SlaveOk = !clone.SlaveOk;
            Assert.IsFalse(clone.Equals(settings));
#pragma warning restore

            clone = settings.Clone();
            clone.WriteConcern = WriteConcern.W2;
            Assert.IsFalse(clone.Equals(settings));
        }

        [Test]
        public void TestFeeze()
        {
            var settings = new MongoCollectionSettings
            {
                ReadPreference = new ReadPreference(),
                WriteConcern = new WriteConcern()
            };
            Assert.IsFalse(settings.IsFrozen);
            Assert.IsFalse(settings.ReadPreference.IsFrozen);
            Assert.IsFalse(settings.WriteConcern.IsFrozen);
            var hashCode = settings.GetHashCode();
            var stringRepresentation = settings.ToString();

            settings.Freeze();
            Assert.IsTrue(settings.IsFrozen);
            Assert.IsTrue(settings.ReadPreference.IsFrozen);
            Assert.IsTrue(settings.WriteConcern.IsFrozen);
            Assert.AreEqual(hashCode, settings.GetHashCode());
            Assert.AreEqual(stringRepresentation, settings.ToString());
        }

        [Test]
        public void TestFrozenCopy()
        {
            var settings = new MongoCollectionSettings();
            Assert.IsFalse(settings.IsFrozen);

            var frozenCopy = settings.FrozenCopy();
            Assert.IsFalse(settings.IsFrozen);
            Assert.IsTrue(frozenCopy.IsFrozen);
            Assert.AreNotSame(settings, frozenCopy);

            var secondFrozenCopy = frozenCopy.FrozenCopy();
            Assert.IsTrue(secondFrozenCopy.IsFrozen);
            Assert.AreSame(frozenCopy, secondFrozenCopy);
        }

        [Test]
        public void TestGuidRepresentation()
        {
            var settings = new MongoCollectionSettings();
            Assert.AreEqual(GuidRepresentation.Unspecified, settings.GuidRepresentation);

            var guidRepresentation = GuidRepresentation.PythonLegacy;
            settings.GuidRepresentation = guidRepresentation;
            Assert.AreEqual(guidRepresentation, settings.GuidRepresentation);

            settings.Freeze();
            Assert.AreEqual(guidRepresentation, settings.GuidRepresentation);
            Assert.Throws<InvalidOperationException>(() => { settings.GuidRepresentation = guidRepresentation; });
        }

        [Test]
        public void TestReadPreference()
        {
            var settings = new MongoCollectionSettings();
            Assert.AreEqual(null, settings.ReadPreference);

            var readPreference = ReadPreference.Secondary;
            settings.ReadPreference = readPreference;
            Assert.AreEqual(readPreference, settings.ReadPreference);

            settings.Freeze();
            Assert.AreEqual(readPreference, settings.ReadPreference);
            Assert.Throws<InvalidOperationException>(() => { settings.ReadPreference = readPreference; });
        }

        [Test]
        public void TestSafeMode()
        {
#pragma warning disable 618
            var settings = new MongoCollectionSettings();
            Assert.AreEqual(null, settings.SafeMode);

            var safeMode = SafeMode.W2;
            settings.SafeMode = safeMode;
            Assert.AreEqual(safeMode, settings.SafeMode);

            settings.Freeze();
            Assert.AreEqual(safeMode, settings.SafeMode);
            Assert.Throws<InvalidOperationException>(() => { settings.SafeMode = safeMode; });
#pragma warning restore
        }

        [Test]
        public void TestSlaveOk()
        {
#pragma warning disable 618
            var settings = new MongoCollectionSettings<BsonDocument>(_database, "collection");
            Assert.AreEqual(false, settings.SlaveOk);

            var slaveOk = true;
            settings.SlaveOk = slaveOk;
            Assert.AreEqual(slaveOk, settings.SlaveOk);

            settings.Freeze();
            Assert.AreEqual(slaveOk, settings.SlaveOk);
            Assert.Throws<InvalidOperationException>(() => { settings.SlaveOk = slaveOk; });
#pragma warning restore
        }

        [Test]
        public void TestWriteConcern()
        {
            var settings = new MongoCollectionSettings();
            Assert.AreEqual(null, settings.WriteConcern);

            var writeConcern = WriteConcern.W2;
            settings.WriteConcern = writeConcern;
            Assert.AreEqual(writeConcern, settings.WriteConcern);

            settings.Freeze();
            Assert.AreEqual(writeConcern, settings.WriteConcern);
            Assert.Throws<InvalidOperationException>(() => { settings.WriteConcern = writeConcern; });
        }
    }
}

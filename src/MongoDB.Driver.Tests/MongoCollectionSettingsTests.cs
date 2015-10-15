/* Copyright 2010-2015 MongoDB Inc.
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

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class MongoCollectionSettingsTests
    {
        [Test]
        public void TestAll()
        {
            var settings = new MongoCollectionSettings
            {
                AssignIdOnInsert = true,
                GuidRepresentation = GuidRepresentation.PythonLegacy,
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.Acknowledged
            };

            Assert.AreEqual(true, settings.AssignIdOnInsert);
            Assert.AreEqual(GuidRepresentation.PythonLegacy, settings.GuidRepresentation);
            Assert.AreEqual(ReadConcern.Majority, settings.ReadConcern);
            Assert.AreSame(ReadPreference.Primary, settings.ReadPreference);
            Assert.AreSame(WriteConcern.Acknowledged, settings.WriteConcern);
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
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Secondary,
                WriteConcern = WriteConcern.W2
            };
            var clone = settings.Clone();
            Assert.IsTrue(clone.Equals(settings));
        }

        [Test]
        public void TestConstructor()
        {
            var settings = new MongoCollectionSettings();
            Assert.AreEqual(false, settings.AssignIdOnInsert);
            Assert.AreEqual(GuidRepresentation.Unspecified, settings.GuidRepresentation);
            Assert.AreEqual(null, settings.ReadConcern);
            Assert.AreEqual(null, settings.ReadPreference);
            Assert.AreEqual(null, settings.WriteConcern);
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
            clone.ReadConcern = ReadConcern.Majority;
            Assert.IsFalse(clone.Equals(settings));
            clone = settings.Clone();
            clone.ReadPreference = ReadPreference.Secondary;
            Assert.IsFalse(clone.Equals(settings));

            clone = settings.Clone();
            clone.WriteConcern = WriteConcern.W2;
            Assert.IsFalse(clone.Equals(settings));
        }

        [Test]
        public void TestFeeze()
        {
            var settings = new MongoCollectionSettings();
            Assert.IsFalse(settings.IsFrozen);
            var hashCode = settings.GetHashCode();
            var stringRepresentation = settings.ToString();

            settings.Freeze();
            Assert.IsTrue(settings.IsFrozen);
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
        public void TestReadConcern()
        {
            var settings = new MongoCollectionSettings();
            Assert.AreEqual(null, settings.ReadConcern);

            var readConcern = ReadConcern.Majority;
            settings.ReadConcern = readConcern;
            Assert.AreEqual(readConcern, settings.ReadConcern);

            settings.Freeze();
            Assert.AreEqual(readConcern, settings.ReadConcern);
            Assert.Throws<InvalidOperationException>(() => { settings.ReadConcern = readConcern; });
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

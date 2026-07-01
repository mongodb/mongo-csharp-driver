/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoDatabaseSettingsTests
    {
        [Fact]
        public void TestAll()
        {
            var settings = new MongoDatabaseSettings
            {
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.Acknowledged
            };

            Assert.Equal(ReadConcern.Majority, settings.ReadConcern);
            Assert.Same(ReadPreference.Primary, settings.ReadPreference);
            Assert.Same(WriteConcern.Acknowledged, settings.WriteConcern);
        }

        [Fact]
        public void TestApplyDefaultValues()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("FromClient");
            var clientSettings = new MongoClientSettings { SerializationDomain = domain };

            var settings = new MongoDatabaseSettings();
            settings.ApplyDefaultValues(clientSettings);
            Assert.Same(domain, settings.SerializationDomain);

            var overrideDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Database");
            var settingsWithOverride = new MongoDatabaseSettings { SerializationDomain = overrideDomain };
            settingsWithOverride.ApplyDefaultValues(clientSettings);
            Assert.Same(overrideDomain, settingsWithOverride.SerializationDomain);
        }

        [Fact]
        public void TestClone()
        {
            // set everything to non default values to test that all settings are cloned
            var settings = new MongoDatabaseSettings
            {
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Secondary,
                SerializationDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Clone"),
                WriteConcern = WriteConcern.W2
            };
            var clone = settings.Clone();
            Assert.True(clone.Equals(settings));
        }

        [Fact]
        public void TestConstructor()
        {
            var settings = new MongoDatabaseSettings();
            Assert.Equal(null, settings.ReadConcern);
            Assert.Equal(null, settings.ReadPreference);
            Assert.Equal(null, settings.WriteConcern);
        }

        [Fact]
        public void TestEquals()
        {
            var settings = new MongoDatabaseSettings();
            var clone = settings.Clone();
            Assert.True(clone.Equals(settings));

            settings.Freeze();
            clone.Freeze();
            Assert.True(clone.Equals(settings));

            clone = settings.Clone();
            clone.ReadConcern = ReadConcern.Majority;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.ReadPreference = ReadPreference.Secondary;
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.SerializationDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Other");
            Assert.False(clone.Equals(settings));

            clone = settings.Clone();
            clone.WriteConcern = WriteConcern.W2;
            Assert.False(clone.Equals(settings));
        }

        [Fact]
        public void TestFreeze()
        {
            var settings = new MongoDatabaseSettings
            {
                WriteConcern = new WriteConcern()
            };
            Assert.False(settings.IsFrozen);
            var hashCode = settings.GetHashCode();
            var stringRepresentation = settings.ToString();

            settings.Freeze();
            Assert.True(settings.IsFrozen);
            Assert.Equal(hashCode, settings.GetHashCode());
            Assert.Equal(stringRepresentation, settings.ToString());
        }

        [Fact]
        public void TestFrozenCopy()
        {
            var settings = new MongoDatabaseSettings();
            Assert.False(settings.IsFrozen);

            var frozenCopy = settings.FrozenCopy();
            Assert.False(settings.IsFrozen);
            Assert.True(frozenCopy.IsFrozen);
            Assert.NotSame(settings, frozenCopy);

            var secondFrozenCopy = frozenCopy.FrozenCopy();
            Assert.True(secondFrozenCopy.IsFrozen);
            Assert.Same(frozenCopy, secondFrozenCopy);
        }

        [Fact]
        public void TestReadConcern()
        {
            var settings = new MongoDatabaseSettings();
            Assert.Equal(null, settings.ReadConcern);

            var readConcern = ReadConcern.Majority;
            settings.ReadConcern = readConcern;
            Assert.Equal(readConcern, settings.ReadConcern);

            settings.Freeze();
            Assert.Equal(readConcern, settings.ReadConcern);
            Assert.Throws<InvalidOperationException>(() => { settings.ReadConcern = readConcern; });
        }

        [Fact]
        public void TestReadPreference()
        {
            var settings = new MongoDatabaseSettings();
            Assert.Equal(null, settings.ReadPreference);

            var readPreference = ReadPreference.Secondary;
            settings.ReadPreference = readPreference;
            Assert.Equal(readPreference, settings.ReadPreference);

            settings.Freeze();
            Assert.Equal(readPreference, settings.ReadPreference);
            Assert.Throws<InvalidOperationException>(() => { settings.ReadPreference = readPreference; });
        }

        [Fact]
        public void TestSerializationDomain()
        {
            var settings = new MongoDatabaseSettings();
            Assert.Same(BsonSerializer.DefaultSerializationDomain, settings.SerializationDomain);

            var serializationDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            settings.SerializationDomain = serializationDomain;
            Assert.Same(serializationDomain, settings.SerializationDomain);

            settings.Freeze();
            Assert.Same(serializationDomain, settings.SerializationDomain);
            Assert.Throws<InvalidOperationException>(() => { settings.SerializationDomain = serializationDomain; });
        }

        [Fact]
        public void TestWriteConcern()
        {
            var settings = new MongoDatabaseSettings();
            Assert.Equal(null, settings.WriteConcern);

            var writeConcern = WriteConcern.W2;
            settings.WriteConcern = writeConcern;
            Assert.Equal(writeConcern, settings.WriteConcern);

            settings.Freeze();
            Assert.Equal(writeConcern, settings.WriteConcern);
            Assert.Throws<InvalidOperationException>(() => { settings.WriteConcern = writeConcern; });
        }
    }
}

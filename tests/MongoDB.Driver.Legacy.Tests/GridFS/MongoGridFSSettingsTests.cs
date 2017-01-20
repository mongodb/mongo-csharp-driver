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
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Xunit;

namespace MongoDB.Driver.Tests.GridFS
{
    public class MongoGridFSSettingsTests
    {
        [Fact]
        public void TestDefaults()
        {
            var settings = MongoGridFSSettings.Defaults;
            Assert.Equal(255 * 1024, settings.ChunkSize);
            Assert.Equal("fs", settings.Root);
            Assert.Equal(true, settings.UpdateMD5);
            Assert.Equal(true, settings.VerifyMD5);
            Assert.Equal(true, settings.IsFrozen);
            Assert.Equal(null, settings.WriteConcern);
        }

        [Fact]
        public void TestDefaultsObsolete()
        {
#pragma warning disable 618
            var settings = new MongoGridFSSettings();
            Assert.False(settings.IsFrozen);
            Assert.Equal(0, settings.ChunkSize);
            Assert.Equal(null, settings.Root);
            Assert.Equal(null, settings.WriteConcern);
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
            Assert.Equal(64 * 1024, settings.ChunkSize);
            Assert.Equal("root", settings.Root);
            Assert.Equal(true, settings.UpdateMD5);
            Assert.Equal(true, settings.VerifyMD5);
            Assert.Equal(WriteConcern.Acknowledged, settings.WriteConcern);
            Assert.Equal(false, settings.IsFrozen);
        }

        [Fact]
        public void TestCreationEmpty()
        {
            var settings = new MongoGridFSSettings();
            Assert.Equal(0, settings.ChunkSize);
            Assert.Equal(null, settings.Root);
            Assert.Equal(false, settings.UpdateMD5);
            Assert.Equal(false, settings.VerifyMD5);
            Assert.Equal(false, settings.IsFrozen);
            Assert.Equal(null, settings.WriteConcern);
        }

        [Fact]
        public void TestCreationObsolete()
        {
#pragma warning disable 618
            var settings = new MongoGridFSSettings
            {
                ChunkSize = 64 * 1024,
                Root = "root",
                WriteConcern = WriteConcern.Acknowledged
            };
            Assert.False(settings.IsFrozen);
            Assert.Equal("root.chunks", settings.ChunksCollectionName);
            Assert.Equal(64 * 1024, settings.ChunkSize);
            Assert.Equal("root.files", settings.FilesCollectionName);
            Assert.Equal("root", settings.Root);
            Assert.Equal(WriteConcern.Acknowledged, settings.WriteConcern);
#pragma warning restore
        }

        [Fact]
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
            Assert.True(settings == clone);
            Assert.Equal(settings, clone);
        }

        [Fact]
        public void TestEquals()
        {
            var a = new MongoGridFSSettings() { ChunkSize = 123 };
            var b = new MongoGridFSSettings() { ChunkSize = 123 };
            var c = new MongoGridFSSettings() { ChunkSize = 345 };
            var n = (WriteConcern)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestFreeze()
        {
            var settings = new MongoGridFSSettings();
            Assert.False(settings.IsFrozen);
            settings.Freeze();
            Assert.True(settings.IsFrozen);
            settings.Freeze(); // test that it's OK to call Freeze more than once
            Assert.True(settings.IsFrozen);
            Assert.Throws<InvalidOperationException>(() => settings.ChunkSize = 64 * 1024);
            Assert.Throws<InvalidOperationException>(() => settings.Root = "root");
            Assert.Throws<InvalidOperationException>(() => settings.UpdateMD5 = true);
            Assert.Throws<InvalidOperationException>(() => settings.VerifyMD5 = true);
            Assert.Throws<InvalidOperationException>(() => settings.WriteConcern = WriteConcern.Acknowledged);
        }
    }
}

/* Copyright 2010-2016 MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Xunit;

namespace MongoDB.Driver.Tests.GridFS
{
    public class MongoGridFSFileInfoTests
    {
        private MongoServer _server;
        private MongoDatabase _database;

        public MongoGridFSFileInfoTests()
        {
            _server = LegacyTestConfiguration.Server;
            _database = LegacyTestConfiguration.Database;
        }

        [Fact]
        public void TestCreateWithRemoteFileNameAndCreateOptions()
        {
            var aliases = new string[] { "a", "b" };
            var uploadDate = new DateTime(2011, 11, 10, 19, 57, 0, DateTimeKind.Utc);
            var metadata = new BsonDocument("x", 1);
            var createOptions = new MongoGridFSCreateOptions()
            {
                Aliases = aliases,
                ChunkSize = 123,
                ContentType = "content",
                Id = 1,
                Metadata = metadata,
                UploadDate = uploadDate
            };
            var settings = new MongoGridFSSettings();
            var info = new MongoGridFSFileInfo(_server, _server.Primary, _database.Name, settings, "filename", createOptions);
            Assert.True(aliases.SequenceEqual(info.Aliases));
            Assert.Equal(123, info.ChunkSize);
            Assert.Equal("content", info.ContentType);
            Assert.Equal(1, info.Id.AsInt32);
            Assert.Equal(0, info.Length);
            Assert.Equal(null, info.MD5);
            Assert.Equal(metadata, info.Metadata);
            Assert.Equal("filename", info.Name);
            Assert.Equal(uploadDate, info.UploadDate);
        }

        [Fact]
        public void TestEquals()
        {
            var settings = new MongoGridFSSettings();
            var createOptions = new MongoGridFSCreateOptions { ChunkSize = 123 };
            var a1 = new MongoGridFSFileInfo(_server, _server.Primary, _database.Name, settings, "f", createOptions);
            var a2 = new MongoGridFSFileInfo(_server, _server.Primary, _database.Name, settings, "f", createOptions);
            var a3 = a2;
            var b = new MongoGridFSFileInfo(_server, _server.Primary, _database.Name, settings, "g", createOptions);
            var null1 = (MongoGridFSFileInfo)null;
            var null2 = (MongoGridFSFileInfo)null;

            Assert.NotSame(a1, a2);
            Assert.Same(a2, a3);
            Assert.True(a1.Equals((object)a2));
            Assert.False(a1.Equals((object)null));
            Assert.False(a1.Equals((object)"x"));

            Assert.True(a1 == a2);
            Assert.True(a2 == a3);
            Assert.False(a1 == b);
            Assert.False(a1 == null1);
            Assert.False(null1 == a1);
            Assert.True(null1 == null2);

            Assert.False(a1 != a2);
            Assert.False(a2 != a3);
            Assert.True(a1 != b);
            Assert.True(a1 != null1);
            Assert.True(null1 != a1);
            Assert.False(null1 != null2);

            Assert.Equal(a1.GetHashCode(), a2.GetHashCode());
        }
    }
}

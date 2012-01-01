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
    public class MongoGridFSFileInfoTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoGridFS _gridFS;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _server = MongoServer.Create();
            _database = _server["test"];
            _gridFS = _database.GridFS;
        }

        [Test]
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
            var info = new MongoGridFSFileInfo(_gridFS, "filename", createOptions);
            Assert.IsTrue(aliases.SequenceEqual(info.Aliases));
            Assert.AreEqual(123, info.ChunkSize);
            Assert.AreEqual("content", info.ContentType);
            Assert.AreEqual(_gridFS, info.GridFS);
            Assert.AreEqual(1, info.Id.AsInt32);
            Assert.AreEqual(0, info.Length);
            Assert.AreEqual(null, info.MD5);
            Assert.AreEqual(metadata, info.Metadata);
            Assert.AreEqual("filename", info.Name);
            Assert.AreEqual(uploadDate, info.UploadDate);
        }

        [Test]
        public void TestEquals()
        {
            var createOptions = new MongoGridFSCreateOptions { ChunkSize = 123 };
            var a = new MongoGridFSFileInfo(_gridFS, "f", createOptions);
            var b = new MongoGridFSFileInfo(_gridFS, "f", createOptions);
            var c = new MongoGridFSFileInfo(_gridFS, "g", createOptions);
            var n = (MongoCredentials)null;

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
    }
}

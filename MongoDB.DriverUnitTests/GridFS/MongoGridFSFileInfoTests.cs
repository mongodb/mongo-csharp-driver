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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.GridFS
{
    [TestFixture]
    public class MongoGridFSFileInfoTests
    {
        private MongoDatabase _database;
        private MongoGridFS _gridFS;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _database = Configuration.TestDatabase;
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
            var a1 = new MongoGridFSFileInfo(_gridFS, "f", createOptions);
            var a2 = new MongoGridFSFileInfo(_gridFS, "f", createOptions);
            var a3 = a2;
            var b = new MongoGridFSFileInfo(_gridFS, "g", createOptions);
            var null1 = (MongoGridFSFileInfo)null;
            var null2 = (MongoGridFSFileInfo)null;

            Assert.AreNotSame(a1, a2);
            Assert.AreSame(a2, a3);
            Assert.IsTrue(a1.Equals((object)a2));
            Assert.IsFalse(a1.Equals((object)null));
            Assert.IsFalse(a1.Equals((object)"x"));

            Assert.IsTrue(a1 == a2);
            Assert.IsTrue(a2 == a3);
            Assert.IsFalse(a1 == b);
            Assert.IsFalse(a1 == null1);
            Assert.IsFalse(null1 == a1);
            Assert.IsTrue(null1 == null2);

            Assert.IsFalse(a1 != a2);
            Assert.IsFalse(a2 != a3);
            Assert.IsTrue(a1 != b);
            Assert.IsTrue(a1 != null1);
            Assert.IsTrue(null1 != a1);
            Assert.IsFalse(null1 != null2);

            Assert.AreEqual(a1.GetHashCode(), a2.GetHashCode());
        }
    }
}

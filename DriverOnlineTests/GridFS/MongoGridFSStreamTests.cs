/* Copyright 2010 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;

namespace MongoDB.DriverOnlineTests.GridFS {
    [TestFixture]
    public class MongoGridFSStreamTests {
        private MongoServer server;
        private MongoDatabase database;
        private MongoGridFS gridFS;

        [TestFixtureSetUp]
        public void TestFixtureSetup() {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            database = server["onlinetests"];
            var settings = new MongoGridFSSettings {
                DefaultChunkSize = 256,
                SafeMode = SafeMode.True
            };
            gridFS = database.GetGridFS(settings);
        }

        [Test]
        public void TestCreate1ByteFiles() {
            gridFS.Files.RemoveAll();
            gridFS.Chunks.RemoveAll();
            gridFS.Chunks.ResetIndexCache();

            var fileInfo = gridFS.FindOne("test");
            Assert.IsNull(fileInfo);

            using (var stream = gridFS.Create("test")) {
                fileInfo = gridFS.FindOne("test");
                Assert.IsTrue(fileInfo.Exists);
                Assert.IsNull(fileInfo.Aliases);
                Assert.AreEqual("test", fileInfo.Name);
                Assert.AreEqual(gridFS.Settings.DefaultChunkSize, fileInfo.ChunkSize);
                Assert.IsNull(fileInfo.ContentType);
                Assert.AreEqual(0, fileInfo.Length);
                Assert.IsNull(fileInfo.MD5);
                Assert.IsNull(fileInfo.Metadata);

                stream.WriteByte(1);
            }

            fileInfo = gridFS.FindOne("test");
            Assert.IsTrue(fileInfo.Exists);
            Assert.AreEqual(1, fileInfo.Length);
            Assert.IsNotNull(fileInfo.MD5);
        }

        [Test]
        public void TestCreate3ChunkFiles() {
            gridFS.Files.RemoveAll();
            gridFS.Chunks.RemoveAll();
            gridFS.Chunks.ResetIndexCache();

            var fileInfo = gridFS.FindOne("test");
            Assert.IsNull(fileInfo);

            using (var stream = gridFS.Create("test")) {
                fileInfo = gridFS.FindOne("test");
                Assert.IsTrue(fileInfo.Exists);
                Assert.IsNull(fileInfo.Aliases);
                Assert.AreEqual("test", fileInfo.Name);
                Assert.AreEqual(gridFS.Settings.DefaultChunkSize, fileInfo.ChunkSize);
                Assert.IsNull(fileInfo.ContentType);
                Assert.AreEqual(0, fileInfo.Length);
                Assert.IsNull(fileInfo.MD5);
                Assert.IsNull(fileInfo.Metadata);

                var bytes = new byte[fileInfo.ChunkSize * 3];
                stream.Write(bytes, 0, bytes.Length);
            }

            fileInfo = gridFS.FindOne("test");
            Assert.IsTrue(fileInfo.Exists);
            Assert.AreEqual(fileInfo.ChunkSize * 3, fileInfo.Length);
            Assert.IsNotNull(fileInfo.MD5);
        }
    }
}

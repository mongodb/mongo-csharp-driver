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
    public class MongoGridFSTests {
        private MongoServer server;
        private MongoDatabase database;
        private MongoGridFS gridFS;

        [TestFixtureSetUp]
        public void TestFixtureSetup() {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            database = server["onlinetests"];
            gridFS = database.GridFS;
            gridFS.Chunks.RemoveAll();
            gridFS.Chunks.ResetIndexCache();
            gridFS.Files.RemoveAll();
        }

        [Test]
        public void TestConstructorFeezesSettings() {
            var settings = new MongoGridFSSettings();
            Assert.IsFalse(settings.IsFrozen);
            var gridFs = new MongoGridFS(database, settings);
            Assert.IsTrue(settings.IsFrozen);
        }

        [Test]
        public void TestAppendText() {
            Assert.IsFalse(gridFS.Exists("HelloWorld.txt"));
            using (var writer = gridFS.AppendText("HelloWorld.txt")) {
                Assert.IsFalse(writer.BaseStream.CanRead);
                Assert.IsTrue(writer.BaseStream.CanSeek);
                Assert.IsTrue(writer.BaseStream.CanWrite);
                writer.Write("Hello");
            }
            Assert.IsTrue(gridFS.Exists("HelloWorld.txt"));
            using (var writer = gridFS.AppendText("HelloWorld.txt")) {
                writer.Write(" World");
            }
            var memoryStream = new MemoryStream();
            gridFS.Download(memoryStream, "HelloWorld.txt");
            var bytes = memoryStream.ToArray();
            Assert.AreEqual(0xEF, bytes[0]); // the BOM
            Assert.AreEqual(0xBB, bytes[1]);
            Assert.AreEqual(0xBF, bytes[2]);
            var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            Assert.AreEqual("Hello World", text);
        }

        [Test]
        public void TestDeleteByFileId() {
            gridFS.Delete(Query.Null);
            Assert.AreEqual(0, gridFS.Chunks.Count());
            Assert.AreEqual(0, gridFS.Files.Count());

            var fileInfo = UploadHelloWord();
            Assert.AreEqual(1, gridFS.Chunks.Count());
            Assert.AreEqual(1, gridFS.Files.Count());

            gridFS.DeleteById(fileInfo.Id);
            Assert.AreEqual(0, gridFS.Chunks.Count());
            Assert.AreEqual(0, gridFS.Files.Count());
        }

        [Test]
        public void TestDeleteByFileName() {
            gridFS.Delete(Query.Null);
            Assert.AreEqual(0, gridFS.Chunks.Count());
            Assert.AreEqual(0, gridFS.Files.Count());

            UploadHelloWord();
            Assert.AreEqual(1, gridFS.Chunks.Count());
            Assert.AreEqual(1, gridFS.Files.Count());

            gridFS.Delete("HelloWorld.txt");
            Assert.AreEqual(0, gridFS.Chunks.Count());
            Assert.AreEqual(0, gridFS.Files.Count());
        }

        [Test]
        public void TestDeleteAll() {
            gridFS.Delete(Query.Null);
            Assert.AreEqual(0, gridFS.Chunks.Count());
            Assert.AreEqual(0, gridFS.Files.Count());
        }

        [Test]
        public void TestDownload() {
            gridFS.Delete(Query.Null);
            var fileInfo = UploadHelloWord();

            var downloadStream = new MemoryStream();
            gridFS.Download(downloadStream, fileInfo);
            var downloadedBytes = downloadStream.ToArray();
            var downloadedContents = Encoding.UTF8.GetString(downloadedBytes);
            Assert.AreEqual("Hello World", downloadedContents);
        }

        [Test]
        public void TestDownloadTwoChunks() {
            gridFS.Delete(Query.Null);
            var contents = new string('x', 256 * 1024) + new string('y', 256 * 1024);
            var fileInfo = UploadString(contents, "TwoChunks.txt");
            Assert.AreEqual(2 * fileInfo.ChunkSize, fileInfo.Length);
            Assert.AreEqual(2, gridFS.Chunks.Count());
            Assert.AreEqual(1, gridFS.Files.Count());

            var downloadStream = new MemoryStream();
            gridFS.Download(downloadStream, fileInfo);
            var downloadedBytes = downloadStream.ToArray();
            var downloadedContents = Encoding.UTF8.GetString(downloadedBytes);
            Assert.AreEqual(contents, downloadedContents);
        }

        [Test]
        public void TestExists() {
            gridFS.Delete(Query.Null);
            Assert.IsFalse(gridFS.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWord();
            Assert.IsTrue(gridFS.Exists("HelloWorld.txt"));
            Assert.IsTrue(gridFS.ExistsById(fileInfo.Id));
        }

        [Test]
        public void TestFindAll() {
            gridFS.Delete(Query.Null);
            Assert.IsFalse(gridFS.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWord();
            foreach (var foundInfo in gridFS.FindAll()) {
                Assert.AreEqual(fileInfo, foundInfo);
            }
        }

        [Test]
        public void TestFindByName() {
            gridFS.Delete(Query.Null);
            Assert.IsFalse(gridFS.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWord();
            foreach (var foundInfo in gridFS.Find("HelloWorld.txt")) {
                Assert.AreEqual(fileInfo, foundInfo);
            }
        }

        [Test]
        public void TestFindOneById() {
            gridFS.Delete(Query.Null);
            Assert.IsFalse(gridFS.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWord();
            var foundInfo = gridFS.FindOneById(fileInfo.Id);
            Assert.AreEqual(fileInfo, foundInfo);
        }

        [Test]
        public void TestFindOneByName() {
            gridFS.Delete(Query.Null);
            Assert.IsFalse(gridFS.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWord();
            var foundInfo = gridFS.FindOne("HelloWorld.txt");
            Assert.AreEqual(fileInfo, foundInfo);
        }

        [Test]
        public void TestFindOneNewest() {
            gridFS.Delete(Query.Null);
            Assert.IsFalse(gridFS.Exists("HelloWorld.txt"));

            var fileInfo1 = UploadHelloWord();
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            var fileInfo2 = UploadHelloWord();
            var foundInfo = gridFS.FindOne("HelloWorld.txt", -1);
            Assert.AreEqual(fileInfo2, foundInfo);
        }

        [Test]
        public void TestFindOneOldest() {
            gridFS.Delete(Query.Null);
            Assert.IsFalse(gridFS.Exists("HelloWorld.txt"));

            var fileInfo1 = UploadHelloWord();
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            var fileInfo2 = UploadHelloWord();
            var foundInfo = gridFS.FindOne("HelloWorld.txt", 1);
            Assert.AreEqual(fileInfo1, foundInfo);
        }

        [Test]
        public void TestUpload() {
            gridFS.Delete(Query.Null);
            Assert.AreEqual(0, gridFS.Chunks.Count());
            Assert.AreEqual(0, gridFS.Files.Count());

            var contents = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(contents);
            var uploadStream = new MemoryStream(bytes);
            var fileInfo = gridFS.Upload(uploadStream, "HelloWorld.txt");
            Assert.AreEqual("HelloWorld.txt", fileInfo.Name);
            Assert.AreEqual(1, gridFS.Chunks.Count());
            Assert.AreEqual(1, gridFS.Files.Count());
        }

        private MongoGridFSFileInfo UploadHelloWord() {
            return UploadString("Hello World", "HelloWorld.txt");
        }

        private MongoGridFSFileInfo UploadString(
            string value,
            string remoteFileName
        ) {
            var bytes = Encoding.UTF8.GetBytes(value);
            var stream = new MemoryStream(bytes);
            return gridFS.Upload(stream, remoteFileName);
        }
    }
}

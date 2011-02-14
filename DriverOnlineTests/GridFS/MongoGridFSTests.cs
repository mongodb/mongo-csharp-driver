/* Copyright 2010-2011 10gen Inc.
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
        private MongoServer serverMaster;
        private MongoServer serverSlave;
        private MongoDatabase databaseMaster;
        private MongoDatabase databaseSlave;
        private MongoGridFS gridFSMaster;
        private MongoGridFS gridFSSlave;

        [TestFixtureSetUp]
        public void TestFixtureSetup() {
            serverMaster = MongoServer.Create("mongodb://localhost/?safe=true");
            databaseMaster = serverMaster["onlinetests"];
            gridFSMaster = databaseMaster.GetGridFS(new MongoGridFSSettings { SafeMode = SafeMode.W2 });
            gridFSMaster.Chunks.RemoveAll();
            gridFSMaster.Chunks.ResetIndexCache();
            gridFSMaster.Files.RemoveAll();

            /* Master / Slave started using cmd file containing:
             *   start bin\mongod --master
             *   start bin\mongod --slave --source localhost:27017 --dbpath slave --port 27018
             */
            serverSlave = MongoServer.Create("mongodb://localhost:27018/?safe=true&slaveOk=true");
            databaseSlave = serverSlave["onlinetests"];
            gridFSSlave = databaseSlave.GridFS;
        }

        [Test]
        public void TestConstructorFeezesSettings() {
            var settings = new MongoGridFSSettings();
            Assert.IsFalse(settings.IsFrozen);
            var gridFs = new MongoGridFS(databaseMaster, settings);
            Assert.IsTrue(settings.IsFrozen);
        }

        [Test]
        public void TestCopyTo() {
            gridFSMaster.Delete(Query.Null);
            Assert.AreEqual(0, gridFSMaster.Chunks.Count());
            Assert.AreEqual(0, gridFSMaster.Files.Count());

            var contents = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(contents);
            var uploadStream = new MemoryStream(bytes);
            var createOptions = new MongoGridFSCreateOptions {
                Aliases = new[] { "HelloWorld", "HelloUniverse" },
                ChunkSize = gridFSMaster.Settings.DefaultChunkSize,
                ContentType = "text/plain",
                Id = ObjectId.GenerateNewId(),
                Metadata = new BsonDocument { { "a", 1 }, { "b", 2 } },
                UploadDate = DateTime.UtcNow
            };
            var fileInfo = gridFSMaster.Upload(uploadStream, "HelloWorld.txt", createOptions);
            var copyInfo = fileInfo.CopyTo("HelloWorld2.txt");
            Assert.AreEqual(2, gridFSMaster.Chunks.Count());
            Assert.AreEqual(2, gridFSMaster.Files.Count());
            Assert.IsNull(copyInfo.Aliases);
            Assert.AreEqual(fileInfo.ChunkSize, copyInfo.ChunkSize);
            Assert.AreEqual(fileInfo.ContentType, copyInfo.ContentType);
            Assert.AreNotEqual(fileInfo.Id, copyInfo.Id);
            Assert.AreEqual(fileInfo.Length, copyInfo.Length);
            Assert.AreEqual(fileInfo.MD5, copyInfo.MD5);
            Assert.AreEqual(fileInfo.Metadata, copyInfo.Metadata);
            Assert.AreEqual("HelloWorld2.txt", copyInfo.Name);
            Assert.AreEqual(fileInfo.UploadDate, copyInfo.UploadDate);
        }

        [Test]
        public void TestAppendText() {
            Assert.IsFalse(gridFSMaster.Exists("HelloWorld.txt"));
            using (var writer = gridFSMaster.AppendText("HelloWorld.txt")) {
                Assert.IsFalse(writer.BaseStream.CanRead);
                Assert.IsTrue(writer.BaseStream.CanSeek);
                Assert.IsTrue(writer.BaseStream.CanWrite);
                writer.Write("Hello");
            }
            Assert.IsTrue(gridFSMaster.Exists("HelloWorld.txt"));
            using (var writer = gridFSMaster.AppendText("HelloWorld.txt")) {
                writer.Write(" World");
            }
            var memoryStream = new MemoryStream();
            gridFSMaster.Download(memoryStream, "HelloWorld.txt");
            var bytes = memoryStream.ToArray();
            Assert.AreEqual(0xEF, bytes[0]); // the BOM
            Assert.AreEqual(0xBB, bytes[1]);
            Assert.AreEqual(0xBF, bytes[2]);
            var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            Assert.AreEqual("Hello World", text);
        }

        [Test]
        public void TestDeleteByFileId() {
            gridFSMaster.Delete(Query.Null);
            Assert.AreEqual(0, gridFSMaster.Chunks.Count());
            Assert.AreEqual(0, gridFSMaster.Files.Count());

            var fileInfo = UploadHelloWord();
            Assert.AreEqual(1, gridFSMaster.Chunks.Count());
            Assert.AreEqual(1, gridFSMaster.Files.Count());

            gridFSMaster.DeleteById(fileInfo.Id);
            Assert.AreEqual(0, gridFSMaster.Chunks.Count());
            Assert.AreEqual(0, gridFSMaster.Files.Count());
        }

        [Test]
        public void TestDeleteByFileName() {
            gridFSMaster.Delete(Query.Null);
            Assert.AreEqual(0, gridFSMaster.Chunks.Count());
            Assert.AreEqual(0, gridFSMaster.Files.Count());

            UploadHelloWord();
            Assert.AreEqual(1, gridFSMaster.Chunks.Count());
            Assert.AreEqual(1, gridFSMaster.Files.Count());

            gridFSMaster.Delete("HelloWorld.txt");
            Assert.AreEqual(0, gridFSMaster.Chunks.Count());
            Assert.AreEqual(0, gridFSMaster.Files.Count());
        }

        [Test]
        public void TestDeleteAll() {
            gridFSMaster.Delete(Query.Null);
            Assert.AreEqual(0, gridFSMaster.Chunks.Count());
            Assert.AreEqual(0, gridFSMaster.Files.Count());
        }

        [Test]
        public void TestDownload() {
            gridFSMaster.Delete(Query.Null);
            var fileInfo = UploadHelloWord();

            using (var downloadStream = new MemoryStream())
            {
                gridFSMaster.Download(downloadStream, fileInfo);
                var downloadedBytes = downloadStream.ToArray();
                var downloadedContents = Encoding.UTF8.GetString(downloadedBytes);
                Assert.AreEqual("Hello World", downloadedContents);
            }

            using (var downloadStream = new MemoryStream())
            {
                gridFSSlave.Download(downloadStream, fileInfo);
                var downloadedBytes = downloadStream.ToArray();
                var downloadedContents = Encoding.UTF8.GetString(downloadedBytes);
                Assert.AreEqual("Hello World", downloadedContents);
            }
        }

        [Test]
        public void TestDownloadTwoChunks() {
            gridFSMaster.Delete(Query.Null);
            var contents = new string('x', 256 * 1024) + new string('y', 256 * 1024);
            var bytes = Encoding.UTF8.GetBytes(contents);
            var stream = new MemoryStream(bytes);
            var fileInfo = gridFSMaster.Upload(stream, "TwoChunks.txt");
            Assert.AreEqual(2 * fileInfo.ChunkSize, fileInfo.Length);
            Assert.AreEqual(2, gridFSMaster.Chunks.Count());
            Assert.AreEqual(1, gridFSMaster.Files.Count());

            using (var downloadStream = new MemoryStream())
            {
                gridFSMaster.Download(downloadStream, fileInfo);
                var downloadedBytes = downloadStream.ToArray();
                var downloadedContents = Encoding.UTF8.GetString(downloadedBytes);
                Assert.AreEqual(contents, downloadedContents);
            }

            using (var downloadStream = new MemoryStream())
            {
                gridFSSlave.Download(downloadStream, fileInfo);
                var downloadedBytes = downloadStream.ToArray();
                var downloadedContents = Encoding.UTF8.GetString(downloadedBytes);
                Assert.AreEqual(contents, downloadedContents);
            }
        }

        [Test]
        public void TestExists() {
            gridFSMaster.Delete(Query.Null);
            Assert.IsFalse(gridFSMaster.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWord();
            Assert.IsTrue(gridFSMaster.Exists("HelloWorld.txt"));
            Assert.IsTrue(gridFSMaster.ExistsById(fileInfo.Id));
            Assert.IsTrue(gridFSSlave.Exists("HelloWorld.txt"));
            Assert.IsTrue(gridFSSlave.ExistsById(fileInfo.Id));
        }

        [Test]
        public void TestFindAll() {
            gridFSMaster.Delete(Query.Null);
            Assert.IsFalse(gridFSMaster.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWord();
            foreach (var foundInfo in gridFSMaster.FindAll()) {
                Assert.AreEqual(fileInfo, foundInfo);
            }
            foreach (var foundInfo in gridFSSlave.FindAll()) {
                Assert.AreEqual(fileInfo, foundInfo);
            }
        }

        [Test]
        public void TestFindByName() {
            gridFSMaster.Delete(Query.Null);
            Assert.IsFalse(gridFSMaster.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWord();
            foreach (var foundInfo in gridFSMaster.Find("HelloWorld.txt")) {
                Assert.AreEqual(fileInfo, foundInfo);
            }
            foreach (var foundInfo in gridFSSlave.Find("HelloWorld.txt")) {
                Assert.AreEqual(fileInfo, foundInfo);
            }
        }

        [Test]
        public void TestFindOneById() {
            gridFSMaster.Delete(Query.Null);
            Assert.IsFalse(gridFSMaster.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWord();
            var foundInfo = gridFSMaster.FindOneById(fileInfo.Id);
            Assert.AreEqual(fileInfo, foundInfo);
            foundInfo = gridFSSlave.FindOneById(fileInfo.Id);
            Assert.AreEqual(fileInfo, foundInfo);
        }

        [Test]
        public void TestFindOneByName() {
            gridFSMaster.Delete(Query.Null);
            Assert.IsFalse(gridFSMaster.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWord();
            var foundInfo = gridFSMaster.FindOne("HelloWorld.txt");
            Assert.AreEqual(fileInfo, foundInfo);
            foundInfo = gridFSSlave.FindOne("HelloWorld.txt");
            Assert.AreEqual(fileInfo, foundInfo);
        }

        [Test]
        public void TestFindOneNewest() {
            gridFSMaster.Delete(Query.Null);
            Assert.IsFalse(gridFSMaster.Exists("HelloWorld.txt"));

            var fileInfo1 = UploadHelloWord();
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            var fileInfo2 = UploadHelloWord();
            var foundInfo = gridFSMaster.FindOne("HelloWorld.txt", -1);
            Assert.AreEqual(fileInfo2, foundInfo);
            foundInfo = gridFSSlave.FindOne("HelloWorld.txt", -1);
            Assert.AreEqual(fileInfo2, foundInfo);
        }

        [Test]
        public void TestFindOneOldest() {
            gridFSMaster.Delete(Query.Null);
            Assert.IsFalse(gridFSMaster.Exists("HelloWorld.txt"));

            var fileInfo1 = UploadHelloWord();
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            var fileInfo2 = UploadHelloWord();
            var foundInfo = gridFSMaster.FindOne("HelloWorld.txt", 1);
            Assert.AreEqual(fileInfo1, foundInfo);
            foundInfo = gridFSSlave.FindOne("HelloWorld.txt", 1);
            Assert.AreEqual(fileInfo1, foundInfo);
        }

        [Test]
        public void TestMoveTo() {
            gridFSMaster.Delete(Query.Null);
            Assert.AreEqual(0, gridFSMaster.Chunks.Count());
            Assert.AreEqual(0, gridFSMaster.Files.Count());

            var contents = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(contents);
            var uploadStream = new MemoryStream(bytes);
            var fileInfo = gridFSMaster.Upload(uploadStream, "HelloWorld.txt");
            Assert.AreEqual(1, gridFSMaster.Chunks.Count());
            Assert.AreEqual(1, gridFSMaster.Files.Count());

            gridFSMaster.MoveTo("HelloWorld.txt", "HelloWorld2.txt");
            Assert.AreEqual(1, gridFSMaster.Chunks.Count());
            Assert.AreEqual(1, gridFSMaster.Files.Count());
            var movedInfo = gridFSMaster.FindOne("HelloWorld2.txt");
            Assert.AreEqual("HelloWorld2.txt", movedInfo.Name);
            Assert.AreEqual(fileInfo.Id, movedInfo.Id);
        }

        [Test]
        public void TestSetAliases() {
            var fileInfo = UploadHelloWord();
            Assert.IsNull(fileInfo.Aliases);

            var aliases = new string[] { "a", "b" };
            gridFSMaster.SetAliases(fileInfo, aliases);
            fileInfo.Refresh();
            Assert.IsTrue(aliases.SequenceEqual(fileInfo.Aliases));

            gridFSMaster.SetAliases(fileInfo, null);
            fileInfo.Refresh();
            Assert.IsNull(fileInfo.Aliases);
        }

        [Test]
        public void TestSetContentType() {
            var fileInfo = UploadHelloWord();
            Assert.IsNull(fileInfo.ContentType);

            gridFSMaster.SetContentType(fileInfo, "text/plain");
            fileInfo.Refresh();
            Assert.AreEqual("text/plain", fileInfo.ContentType);

            gridFSMaster.SetContentType(fileInfo, null);
            fileInfo.Refresh();
            Assert.IsNull(fileInfo.ContentType);
        }

        [Test]
        public void TestSetMetadata() {
            var fileInfo = UploadHelloWord();
            Assert.IsNull(fileInfo.Metadata);

            var metadata = new BsonDocument { { "a", 1 }, { "b", 2 } };
            gridFSMaster.SetMetadata(fileInfo, metadata);
            fileInfo.Refresh();
            Assert.AreEqual(metadata, fileInfo.Metadata);

            gridFSMaster.SetMetadata(fileInfo, null);
            fileInfo.Refresh();
            Assert.IsNull(fileInfo.Metadata);
        }

        [Test]
        public void TestUpload() {
            gridFSMaster.Delete(Query.Null);
            Assert.AreEqual(0, gridFSMaster.Chunks.Count());
            Assert.AreEqual(0, gridFSMaster.Files.Count());

            var contents = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(contents);
            var uploadStream = new MemoryStream(bytes);
            var createOptions = new MongoGridFSCreateOptions {
                Aliases = new[] { "HelloWorld", "HelloUniverse" },
                ChunkSize = gridFSMaster.Settings.DefaultChunkSize,
                ContentType = "text/plain",
                Id = ObjectId.GenerateNewId(),
                Metadata = new BsonDocument { { "a", 1 }, { "b", 2 } },
                UploadDate = DateTime.UtcNow
            };
            var fileInfo = gridFSMaster.Upload(uploadStream, "HelloWorld.txt", createOptions);
            Assert.AreEqual(1, gridFSMaster.Chunks.Count());
            Assert.AreEqual(1, gridFSMaster.Files.Count());
            Assert.IsTrue(createOptions.Aliases.SequenceEqual(fileInfo.Aliases));
            Assert.AreEqual(createOptions.ChunkSize, fileInfo.ChunkSize);
            Assert.AreEqual(createOptions.ContentType, fileInfo.ContentType);
            Assert.AreEqual(createOptions.Id, fileInfo.Id);
            Assert.AreEqual(11, fileInfo.Length);
            Assert.IsTrue(!string.IsNullOrEmpty(fileInfo.MD5));
            Assert.AreEqual(createOptions.Metadata, fileInfo.Metadata);
            Assert.AreEqual("HelloWorld.txt", fileInfo.Name);
            Assert.AreEqual(createOptions.UploadDate.AddTicks(-(createOptions.UploadDate.Ticks % 10000)), fileInfo.UploadDate);
        }

        private MongoGridFSFileInfo UploadHelloWord() {
            var bytes = Encoding.UTF8.GetBytes("Hello World");
            var stream = new MemoryStream(bytes);
            return gridFSMaster.Upload(stream, "HelloWorld.txt");
        }
    }
}

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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using Xunit;

namespace MongoDB.Driver.Tests.GridFS
{
    public class MongoGridFSTests
    {
        private MongoServer _server;
        private MongoDatabase _database;
        private MongoGridFS _gridFS;

        public MongoGridFSTests()
        {
            _server = LegacyTestConfiguration.Server;
            _database = LegacyTestConfiguration.Database;
            _gridFS = _database.GridFS;
            _gridFS.Chunks.RemoveAll();
            _gridFS.Files.RemoveAll();
        }

        [Fact]
        public void TestConstructorFreezesSettings()
        {
            var settings = new MongoGridFSSettings();
            Assert.False(settings.IsFrozen);
            var gridFS = new MongoGridFS(_server, _database.Name, settings);
            Assert.True(gridFS.Settings.IsFrozen);
        }

        [Fact]
        public void TestCopyTo()
        {
            _gridFS.Delete(Query.Null);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());

            var contents = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(contents);
            var uploadStream = new MemoryStream(bytes);
            var createOptions = new MongoGridFSCreateOptions
            {
                Aliases = new[] { "HelloWorld", "HelloUniverse" },
                ChunkSize = _gridFS.Settings.ChunkSize,
                ContentType = "text/plain",
                Id = ObjectId.GenerateNewId(),
                Metadata = new BsonDocument { { "a", 1 }, { "b", 2 } },
                UploadDate = DateTime.UtcNow
            };
            var fileInfo = _gridFS.Upload(uploadStream, "HelloWorld.txt", createOptions);
            var copyInfo = fileInfo.CopyTo("HelloWorld2.txt");
            Assert.Equal(2, _gridFS.Chunks.Count());
            Assert.Equal(2, _gridFS.Files.Count());
            Assert.Null(copyInfo.Aliases);
            Assert.Equal(fileInfo.ChunkSize, copyInfo.ChunkSize);
            Assert.Equal(fileInfo.ContentType, copyInfo.ContentType);
            Assert.NotEqual(fileInfo.Id, copyInfo.Id);
            Assert.Equal(fileInfo.Length, copyInfo.Length);
            Assert.Equal(fileInfo.MD5, copyInfo.MD5);
            Assert.Equal(fileInfo.Metadata, copyInfo.Metadata);
            Assert.Equal("HelloWorld2.txt", copyInfo.Name);
            Assert.Equal(fileInfo.UploadDate, copyInfo.UploadDate);
        }

        [Fact]
        public void TestAppendText()
        {
            _gridFS.Delete(Query.Null);
            Assert.False(_gridFS.Exists("HelloWorld.txt"));
            using (var writer = _gridFS.AppendText("HelloWorld.txt"))
            {
                Assert.False(writer.BaseStream.CanRead);
                Assert.True(writer.BaseStream.CanSeek);
                Assert.True(writer.BaseStream.CanWrite);
                writer.Write("Hello");
            }
            Assert.True(_gridFS.Exists("HelloWorld.txt"));
            using (var writer = _gridFS.AppendText("HelloWorld.txt"))
            {
                writer.Write(" World");
            }
            var memoryStream = new MemoryStream();
            _gridFS.Download(memoryStream, "HelloWorld.txt");
            var bytes = memoryStream.ToArray();
            Assert.Equal(0xEF, bytes[0]); // the BOM
            Assert.Equal(0xBB, bytes[1]);
            Assert.Equal(0xBF, bytes[2]);
            var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            Assert.Equal("Hello World", text);
        }

        [Fact]
        public void TestDeleteByFileId()
        {
            _gridFS.Delete(Query.Null);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());

            var fileInfo = UploadHelloWorld();
            Assert.Equal(1, _gridFS.Chunks.Count());
            Assert.Equal(1, _gridFS.Files.Count());

            _gridFS.DeleteById(fileInfo.Id);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());
        }

        [Fact]
        public void TestDeleteByFileName()
        {
            _gridFS.Delete(Query.Null);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());

            UploadHelloWorld();
            Assert.Equal(1, _gridFS.Chunks.Count());
            Assert.Equal(1, _gridFS.Files.Count());

            _gridFS.Delete("HelloWorld.txt");
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());
        }

        [Fact]
        public void TestDeleteAll()
        {
            _gridFS.Delete(Query.Null);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());
        }

        [Fact]
        public void TestDownload()
        {
            _gridFS.Delete(Query.Null);
            var fileInfo = UploadHelloWorld();

            var downloadStream = new MemoryStream();
            _gridFS.Download(downloadStream, fileInfo);
            var downloadedBytes = downloadStream.ToArray();
            var downloadedContents = Encoding.UTF8.GetString(downloadedBytes);
            Assert.Equal("Hello World", downloadedContents);
        }

        [Fact]
        public void TestDownloadDontVerifyMD5()
        {
            _gridFS.Delete(Query.Null);
            var fileInfo = UploadHelloWorld(false);

            var settings = new MongoGridFSSettings() { VerifyMD5 = false };
            var gridFS = _database.GetGridFS(settings);
            var downloadStream = new MemoryStream();
            gridFS.Download(downloadStream, fileInfo);
            var downloadedBytes = downloadStream.ToArray();
            var downloadedContents = Encoding.UTF8.GetString(downloadedBytes);
            Assert.Equal("Hello World", downloadedContents);
        }

        [Fact]
        public void TestDownloadTwoChunks()
        {
            _gridFS.Delete(Query.Null);
            var contents = new string('x', 255 * 1024) + new string('y', 255 * 1024);
            var bytes = Encoding.UTF8.GetBytes(contents);
            var stream = new MemoryStream(bytes);
            var fileInfo = _gridFS.Upload(stream, "TwoChunks.txt");
            Assert.Equal(2 * fileInfo.ChunkSize, fileInfo.Length);
            Assert.Equal(2, _gridFS.Chunks.Count());
            Assert.Equal(1, _gridFS.Files.Count());

            var downloadStream = new MemoryStream();
            _gridFS.Download(downloadStream, fileInfo);
            var downloadedBytes = downloadStream.ToArray();
            var downloadedContents = Encoding.UTF8.GetString(downloadedBytes);
            Assert.Equal(contents, downloadedContents);
        }

        [Fact]
        public void TestExists()
        {
            _gridFS.Delete(Query.Null);
            Assert.False(_gridFS.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWorld();
            Assert.True(_gridFS.Exists("HelloWorld.txt"));
            Assert.True(_gridFS.ExistsById(fileInfo.Id));
        }

        [Fact]
        public void TestFindAll()
        {
            _gridFS.Delete(Query.Null);
            Assert.False(_gridFS.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWorld();
            foreach (var foundInfo in _gridFS.FindAll())
            {
                Assert.Equal(fileInfo, foundInfo);
            }
        }

        [Fact]
        public void TestFindByName()
        {
            _gridFS.Delete(Query.Null);
            Assert.False(_gridFS.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWorld();
            foreach (var foundInfo in _gridFS.Find("HelloWorld.txt"))
            {
                Assert.Equal(fileInfo, foundInfo);
            }
        }

        [Fact]
        public void TestFindOneById()
        {
            _gridFS.Delete(Query.Null);
            Assert.False(_gridFS.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWorld();
            var foundInfo = _gridFS.FindOneById(fileInfo.Id);
            Assert.Equal(fileInfo, foundInfo);
        }

        [Fact]
        public void TestFindOneByName()
        {
            _gridFS.Delete(Query.Null);
            Assert.False(_gridFS.Exists("HelloWorld.txt"));

            var fileInfo = UploadHelloWorld();
            var foundInfo = _gridFS.FindOne("HelloWorld.txt");
            Assert.Equal(fileInfo, foundInfo);
        }

        [Fact]
        public void TestFindOneNewest()
        {
            _gridFS.Delete(Query.Null);
            Assert.False(_gridFS.Exists("HelloWorld.txt"));

            UploadHelloWorld();
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            var fileInfo2 = UploadHelloWorld();
            var foundInfo = _gridFS.FindOne("HelloWorld.txt", -1);
            Assert.Equal(fileInfo2, foundInfo);
        }

        [Fact]
        public void TestFindOneOldest()
        {
            _gridFS.Delete(Query.Null);
            Assert.False(_gridFS.Exists("HelloWorld.txt"));

            var fileInfo1 = UploadHelloWorld();
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
            UploadHelloWorld();
            var foundInfo = _gridFS.FindOne("HelloWorld.txt", 1);
            Assert.Equal(fileInfo1, foundInfo);
        }

        [Fact]
        public void TestMoveTo()
        {
            _gridFS.Delete(Query.Null);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());

            var contents = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(contents);
            var uploadStream = new MemoryStream(bytes);
            var fileInfo = _gridFS.Upload(uploadStream, "HelloWorld.txt");
            Assert.Equal(1, _gridFS.Chunks.Count());
            Assert.Equal(1, _gridFS.Files.Count());

            _gridFS.MoveTo("HelloWorld.txt", "HelloWorld2.txt");
            Assert.Equal(1, _gridFS.Chunks.Count());
            Assert.Equal(1, _gridFS.Files.Count());
            var movedInfo = _gridFS.FindOne("HelloWorld2.txt");
            Assert.Equal("HelloWorld2.txt", movedInfo.Name);
            Assert.Equal(fileInfo.Id, movedInfo.Id);
        }

        [Fact]
        public void TestSetAliases()
        {
            var fileInfo = UploadHelloWorld();
            Assert.Null(fileInfo.Aliases);

            var aliases = new string[] { "a", "b" };
            _gridFS.SetAliases(fileInfo, aliases);
            fileInfo.Refresh();
            Assert.True(aliases.SequenceEqual(fileInfo.Aliases));

            _gridFS.SetAliases(fileInfo, null);
            fileInfo.Refresh();
            Assert.Null(fileInfo.Aliases);
        }

        [Fact]
        public void TestSetContentType()
        {
            var fileInfo = UploadHelloWorld();
            Assert.Null(fileInfo.ContentType);

            _gridFS.SetContentType(fileInfo, "text/plain");
            fileInfo.Refresh();
            Assert.Equal("text/plain", fileInfo.ContentType);

            _gridFS.SetContentType(fileInfo, null);
            fileInfo.Refresh();
            Assert.Null(fileInfo.ContentType);
        }

        [Fact]
        public void TestSetMetadata()
        {
            var fileInfo = UploadHelloWorld();
            Assert.Null(fileInfo.Metadata);

            var metadata = new BsonDocument { { "a", 1 }, { "b", 2 } };
            _gridFS.SetMetadata(fileInfo, metadata);
            fileInfo.Refresh();
            Assert.Equal(metadata, fileInfo.Metadata);

            _gridFS.SetMetadata(fileInfo, null);
            fileInfo.Refresh();
            Assert.Null(fileInfo.Metadata);
        }

        [Fact]
        public void TestUpload()
        {
            _gridFS.Delete(Query.Null);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());

            var contents = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(contents);
            var uploadStream = new MemoryStream(bytes);
            var createOptions = new MongoGridFSCreateOptions
            {
                Aliases = new[] { "HelloWorld", "HelloUniverse" },
                ChunkSize = _gridFS.Settings.ChunkSize,
                ContentType = "text/plain",
                Id = ObjectId.GenerateNewId(),
                Metadata = new BsonDocument { { "a", 1 }, { "b", 2 } },
                UploadDate = DateTime.UtcNow
            };
            var fileInfo = _gridFS.Upload(uploadStream, "HelloWorld.txt", createOptions);
            Assert.Equal(1, _gridFS.Chunks.Count());
            Assert.Equal(1, _gridFS.Files.Count());
            Assert.True(createOptions.Aliases.SequenceEqual(fileInfo.Aliases));
            Assert.Equal(createOptions.ChunkSize, fileInfo.ChunkSize);
            Assert.Equal(createOptions.ContentType, fileInfo.ContentType);
            Assert.Equal(createOptions.Id, fileInfo.Id);
            Assert.Equal(11, fileInfo.Length);
            Assert.True(!string.IsNullOrEmpty(fileInfo.MD5));
            Assert.Equal(createOptions.Metadata, fileInfo.Metadata);
            Assert.Equal("HelloWorld.txt", fileInfo.Name);
            Assert.Equal(createOptions.UploadDate.AddTicks(-(createOptions.UploadDate.Ticks % 10000)), fileInfo.UploadDate);
        }

        [Fact]
        public void TestDeleteByFileNameWithSecondaryReadPreference()
        {
            var fileName = "HelloWorld.txt";
            _gridFS.Delete(Query.Null);
            UploadHelloWorld(fileName);
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            gridFS.Delete(fileName);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());
        }

        [Fact]
        public void TestDeleteByFileIdWithSecondaryReadPreference()
        {
            _gridFS.Delete(Query.Null);
            var fileInfo = UploadHelloWorld();
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            gridFS.DeleteById(fileInfo.Id);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());
        }

        [Fact]
        public void TestDeleteAllWithSecondaryReadPreference()
        {
            _gridFS.Delete(Query.Null);
            UploadHelloWorld("HelloWorld1.txt");
            UploadHelloWorld("HelloWorld2.txt");
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            gridFS.Delete(Query.Null);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());
        }

        [Fact]
        public void TestCopyToWithSecondaryReadPreference()
        {
            _gridFS.Delete(Query.Null);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());

            var contents = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(contents);
            var uploadStream = new MemoryStream(bytes);
            var createOptions = new MongoGridFSCreateOptions
            {
                Aliases = new[] { "HelloWorld", "HelloUniverse" },
                ChunkSize = _gridFS.Settings.ChunkSize,
                ContentType = "text/plain",
                Id = ObjectId.GenerateNewId(),
                Metadata = new BsonDocument { { "a", 1 }, { "b", 2 } },
                UploadDate = DateTime.UtcNow
            };
            var fileInfo = _gridFS.Upload(uploadStream, "HelloWorld.txt", createOptions);
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            var copyInfo = gridFS.CopyTo("HelloWorld.txt", "HelloWorld2.txt");
            Assert.Equal(2, _gridFS.Chunks.Count());
            Assert.Equal(2, _gridFS.Files.Count());
            Assert.Null(copyInfo.Aliases);
            Assert.Equal(fileInfo.ChunkSize, copyInfo.ChunkSize);
            Assert.Equal(fileInfo.ContentType, copyInfo.ContentType);
            Assert.NotEqual(fileInfo.Id, copyInfo.Id);
            Assert.Equal(fileInfo.Length, copyInfo.Length);
            Assert.Equal(fileInfo.MD5, copyInfo.MD5);
            Assert.Equal(fileInfo.Metadata, copyInfo.Metadata);
            Assert.Equal("HelloWorld2.txt", copyInfo.Name);
            Assert.Equal(fileInfo.UploadDate, copyInfo.UploadDate);
        }

        [Fact]
        public void TestCopyToWithSecondaryReadPreferenceAndSettings()
        {
            _gridFS.Delete(Query.Null);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());

            var contents = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(contents);
            var uploadStream = new MemoryStream(bytes);
            var createOptions = new MongoGridFSCreateOptions
            {
                Aliases = new[] { "HelloWorld", "HelloUniverse" },
                ChunkSize = _gridFS.Settings.ChunkSize,
                ContentType = "text/plain",
                Id = ObjectId.GenerateNewId(),
                Metadata = new BsonDocument { { "a", 1 }, { "b", 2 } },
                UploadDate = DateTime.UtcNow
            };
            var fileInfo = _gridFS.Upload(uploadStream, "HelloWorld.txt", createOptions);
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            createOptions.Metadata = new BsonDocument("c", 3);
            createOptions.Id = ObjectId.GenerateNewId();
            var copyInfo = gridFS.CopyTo("HelloWorld.txt", "HelloWorld2.txt", createOptions);
            Assert.Equal(2, _gridFS.Chunks.Count());
            Assert.Equal(2, _gridFS.Files.Count());
            Assert.Equal(createOptions.Aliases, copyInfo.Aliases);
            Assert.Equal(fileInfo.ChunkSize, copyInfo.ChunkSize);
            Assert.Equal(fileInfo.ContentType, copyInfo.ContentType);
            Assert.Equal(createOptions.Id, copyInfo.Id);
            Assert.Equal(fileInfo.Length, copyInfo.Length);
            Assert.Equal(fileInfo.MD5, copyInfo.MD5);
            Assert.Equal(createOptions.Metadata, copyInfo.Metadata);
            Assert.Equal("HelloWorld2.txt", copyInfo.Name);
            Assert.Equal(fileInfo.UploadDate, copyInfo.UploadDate);
        }

        [Fact]
        public void TestMoveToWithSecondaryReadPreference()
        {
            _gridFS.Delete(Query.Null);
            Assert.Equal(0, _gridFS.Chunks.Count());
            Assert.Equal(0, _gridFS.Files.Count());

            var contents = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(contents);
            var uploadStream = new MemoryStream(bytes);
            var fileInfo = _gridFS.Upload(uploadStream, "HelloWorld.txt");
            Assert.Equal(1, _gridFS.Chunks.Count());
            Assert.Equal(1, _gridFS.Files.Count());

            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            gridFS.MoveTo("HelloWorld.txt", "HelloWorld2.txt");
            Assert.Equal(1, _gridFS.Chunks.Count());
            Assert.Equal(1, _gridFS.Files.Count());
            var movedInfo = _gridFS.FindOne("HelloWorld2.txt");
            Assert.Equal("HelloWorld2.txt", movedInfo.Name);
            Assert.Equal(fileInfo.Id, movedInfo.Id);
        }

        [Fact]
        public void TestAppendTextWithSecondaryReadPreference()
        {
            _gridFS.Delete(Query.Null);
            Assert.False(_gridFS.Exists("HelloWorld.txt"));
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            using (var writer = gridFS.AppendText("HelloWorld.txt"))
            {
                Assert.False(writer.BaseStream.CanRead);
                Assert.True(writer.BaseStream.CanSeek);
                Assert.True(writer.BaseStream.CanWrite);
                writer.Write("Hello");
            }
            Assert.True(_gridFS.Exists("HelloWorld.txt"));
            using (var writer = gridFS.AppendText("HelloWorld.txt"))
            {
                writer.Write(" World");
            }
            var memoryStream = new MemoryStream();
            _gridFS.Download(memoryStream, "HelloWorld.txt");
            var bytes = memoryStream.ToArray();
            Assert.Equal(0xEF, bytes[0]); // the BOM
            Assert.Equal(0xBB, bytes[1]);
            Assert.Equal(0xBF, bytes[2]);
            var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            Assert.Equal("Hello World", text);
        }

        [Fact]
        public void TestCreateWithSecondaryReadPreference()
        {
            _gridFS.Delete(Query.Null);
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            var fileName = "test.txt";
            using (var stream = gridFS.Create(fileName))
            {
                stream.WriteByte(1);
            }
            var fileInfo = _gridFS.FindOne("test.txt");
            Assert.NotNull(fileInfo);
        }

        [Fact]
        public void TestCreateWithSecondaryReadPreferenceAndOptions()
        {
            _gridFS.Delete(Query.Null);
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            var fileName = "test.txt";
            var createOptions = new MongoGridFSCreateOptions
            {
                Aliases = new[] { "test" },
                Id = ObjectId.GenerateNewId(),
                Metadata = new BsonDocument { { "a", 1 } }
            };
            using (var stream = gridFS.Create(fileName, createOptions))
            {
                stream.WriteByte(1);
            }
            var fileInfo = _gridFS.FindOne("test.txt");
            Assert.NotNull(fileInfo);
            Assert.Equal(createOptions.Aliases, fileInfo.Aliases);
            Assert.Equal(createOptions.Id, fileInfo.Id);
            Assert.Equal(createOptions.Metadata, fileInfo.Metadata);
        }

        [Fact]
        public void TestCreateTextWithSecondaryReadPreference()
        {
            _gridFS.Delete(Query.Null);
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            var fileName = "test.txt";
            using (var stream = gridFS.CreateText(fileName))
            {
                stream.Write("1");
            }
            var fileInfo = _gridFS.FindOne("test.txt");
            Assert.NotNull(fileInfo);
        }

        [Fact]
        public void TestCreateTextWithSecondaryReadPreferenceAndOptions()
        {
            _gridFS.Delete(Query.Null);
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            var fileName = "test.txt";
            var createOptions = new MongoGridFSCreateOptions
            {
                Aliases = new[] { "test" },
                Id = ObjectId.GenerateNewId(),
                Metadata = new BsonDocument { { "a", 1 } }
            };
            using (var stream = gridFS.CreateText(fileName, createOptions))
            {
                stream.Write("1");
            }
            var fileInfo = _gridFS.FindOne("test.txt");
            Assert.NotNull(fileInfo);
            Assert.Equal(createOptions.Aliases, fileInfo.Aliases);
            Assert.Equal(createOptions.Id, fileInfo.Id);
            Assert.Equal(createOptions.Metadata, fileInfo.Metadata);
        }

        [Fact]
        public void TestOpenWriteWithSecondaryReadPreference()
        {
            _gridFS.Delete(Query.Null);
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            var fileName = "test.txt";
            using (var stream = gridFS.OpenWrite(fileName))
            {
                stream.WriteByte(1);
            }
            var fileInfo = _gridFS.FindOne("test.txt");
            Assert.NotNull(fileInfo);
        }

        [Fact]
        public void TestOpenWriteWithSecondaryReadPreferenceAndOptions()
        {
            _gridFS.Delete(Query.Null);
            var settings = new MongoGridFSSettings() { ReadPreference = ReadPreference.Secondary };
            var gridFS = _database.GetGridFS(settings);
            var fileName = "test.txt";
            var createOptions = new MongoGridFSCreateOptions
            {
                Aliases = new[] { "test" },
                Id = ObjectId.GenerateNewId(),
                Metadata = new BsonDocument { { "a", 1 } }
            };
            using (var stream = gridFS.OpenWrite(fileName, createOptions))
            {
                stream.WriteByte(1);
            }
            var fileInfo = _gridFS.FindOne("test.txt");
            Assert.NotNull(fileInfo);
            Assert.Equal(createOptions.Aliases, fileInfo.Aliases);
            Assert.Equal(createOptions.Id, fileInfo.Id);
            Assert.Equal(createOptions.Metadata, fileInfo.Metadata);
        }

        [Fact]
        public void TestUploadWithSecondaryReadPreference()
        {
            _gridFS.Delete(Query.Null);
            var settings = new MongoGridFSSettings 
            {
                ReadPreference = ReadPreference.Secondary,
                VerifyMD5 = true 
            };
            var gridFS = _database.GetGridFS(settings);
            var bytes = Encoding.UTF8.GetBytes("Hello World");
            var stream = new MemoryStream(bytes);
            var fileName = "HelloWorld.txt";
            var fileInfo = gridFS.Upload(stream, fileName);
            Assert.NotNull(fileInfo);
            Assert.Equal(fileName, fileInfo.Name);
        }

        private MongoGridFSFileInfo UploadHelloWorld()
        {
            return UploadHelloWorld(true, "HelloWorld.txt");
        }

        private MongoGridFSFileInfo UploadHelloWorld(string fileName)
        {
            return UploadHelloWorld(true, fileName);
        }

        private MongoGridFSFileInfo UploadHelloWorld(bool verifyMD5)
        {
            return UploadHelloWorld(verifyMD5, "HelloWorld.txt");
        }

        private MongoGridFSFileInfo UploadHelloWorld(bool verifyMD5, string fileName)
        {
            var settings = new MongoGridFSSettings() { VerifyMD5 = verifyMD5 };
            var gridFS = _database.GetGridFS(settings);
            var bytes = Encoding.UTF8.GetBytes("Hello World");
            var stream = new MemoryStream(bytes);
            return gridFS.Upload(stream, fileName);
        }
    }
}

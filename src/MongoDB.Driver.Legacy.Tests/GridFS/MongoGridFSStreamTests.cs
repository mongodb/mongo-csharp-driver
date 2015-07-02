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

using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.GridFS
{
    [TestFixture]
    public class MongoGridFSStreamTests
    {
        private MongoDatabase _database;
        private MongoGridFS _gridFS;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _database = LegacyTestConfiguration.Database;
            var settings = new MongoGridFSSettings
            {
                ChunkSize = 16,
                WriteConcern = WriteConcern.Acknowledged
            };
            _gridFS = _database.GetGridFS(settings);
        }

        [Test]
        public void TestCreateZeroLengthFile()
        {
            _gridFS.Files.RemoveAll();
            _gridFS.Chunks.RemoveAll();

            var fileInfo = _gridFS.FindOne("test");
            Assert.IsNull(fileInfo);

            using (var stream = _gridFS.Create("test"))
            {
                Assert.IsTrue(stream.CanRead);
                Assert.IsTrue(stream.CanSeek);
                Assert.IsFalse(stream.CanTimeout);
                Assert.IsTrue(stream.CanWrite);
                Assert.AreEqual(0, stream.Length);
                Assert.AreEqual(0, stream.Position);

                fileInfo = _gridFS.FindOne("test");
                Assert.IsTrue(fileInfo.Exists);
                Assert.IsNull(fileInfo.Aliases);
                Assert.AreEqual("test", fileInfo.Name);
                Assert.AreEqual(_gridFS.Settings.ChunkSize, fileInfo.ChunkSize);
                Assert.IsNull(fileInfo.ContentType);
                Assert.AreEqual(0, fileInfo.Length);
                Assert.IsNull(fileInfo.MD5);
                Assert.IsNull(fileInfo.Metadata);
            }

            fileInfo = _gridFS.FindOne("test");
            Assert.IsTrue(fileInfo.Exists);
            Assert.AreEqual(0, fileInfo.Length);
            Assert.IsNotNull(fileInfo.MD5);
        }

        [Test]
        public void TestCreate1ByteFile()
        {
            _gridFS.Files.RemoveAll();
            _gridFS.Chunks.RemoveAll();

            var fileInfo = _gridFS.FindOne("test");
            Assert.IsNull(fileInfo);

            using (var stream = _gridFS.Create("test"))
            {
                stream.WriteByte(1);
            }

            fileInfo = _gridFS.FindOne("test");
            Assert.IsTrue(fileInfo.Exists);
            Assert.AreEqual(1, fileInfo.Length);
            Assert.IsNotNull(fileInfo.MD5);

            using (var stream = _gridFS.OpenRead("test"))
            {
                var b = stream.ReadByte();
                Assert.AreEqual(1, b);
                b = stream.ReadByte();
                Assert.AreEqual(-1, b); // EOF
            }
        }

        [Test]
        public void TestCreate3ChunkFile()
        {
            _gridFS.Files.RemoveAll();
            _gridFS.Chunks.RemoveAll();

            var fileInfo = _gridFS.FindOne("test");
            Assert.IsNull(fileInfo);

            using (var stream = _gridFS.Create("test"))
            {
                fileInfo = _gridFS.FindOne("test");
                var bytes = new byte[fileInfo.ChunkSize * 3];
                stream.Write(bytes, 0, bytes.Length);
            }

            fileInfo = _gridFS.FindOne("test");
            Assert.IsTrue(fileInfo.Exists);
            Assert.AreEqual(fileInfo.ChunkSize * 3, fileInfo.Length);
            Assert.IsNotNull(fileInfo.MD5);

            using (var stream = _gridFS.OpenRead("test"))
            {
                var bytes = new byte[fileInfo.ChunkSize * 3];
                var bytesRead = stream.Read(bytes, 0, fileInfo.ChunkSize * 3);
                Assert.AreEqual(bytesRead, fileInfo.ChunkSize * 3);
                Assert.IsTrue(bytes.All(b => b == 0));

                bytesRead = stream.Read(bytes, 0, 1);
                Assert.AreEqual(0, bytesRead); // EOF
            }
        }

        [Test]
        public void TestCreate3ChunkFile1ByteAtATime()
        {
            _gridFS.Files.RemoveAll();
            _gridFS.Chunks.RemoveAll();

            var fileInfo = _gridFS.FindOne("test");
            Assert.IsNull(fileInfo);

            using (var stream = _gridFS.Create("test"))
            {
                fileInfo = _gridFS.FindOne("test");

                for (int i = 0; i < fileInfo.ChunkSize * 3; i++)
                {
                    stream.WriteByte((byte)i);
                }
            }

            fileInfo = _gridFS.FindOne("test");
            Assert.IsTrue(fileInfo.Exists);
            Assert.AreEqual(fileInfo.ChunkSize * 3, fileInfo.Length);
            Assert.IsNotNull(fileInfo.MD5);

            using (var stream = _gridFS.OpenRead("test"))
            {
                for (int i = 0; i < fileInfo.ChunkSize * 3; i++)
                {
                    var b = stream.ReadByte();
                    Assert.AreEqual((byte)i, b);
                }
                var eof = stream.ReadByte();
                Assert.AreEqual(-1, eof);
            }
        }

        [Test]
        public void TestCreate3ChunkFile14BytesAtATime()
        {
            _gridFS.Files.RemoveAll();
            _gridFS.Chunks.RemoveAll();

            var fileInfo = _gridFS.FindOne("test");
            Assert.IsNull(fileInfo);

            using (var stream = _gridFS.Create("test"))
            {
                fileInfo = _gridFS.FindOne("test");

                var bytes = new byte[] { 1, 2, 3, 4 };
                for (int i = 0; i < fileInfo.ChunkSize * 3; i += 4)
                {
                    stream.Write(bytes, 0, 4);
                }
            }

            fileInfo = _gridFS.FindOne("test");
            Assert.IsTrue(fileInfo.Exists);
            Assert.AreEqual(fileInfo.ChunkSize * 3, fileInfo.Length);
            Assert.IsNotNull(fileInfo.MD5);

            using (var stream = _gridFS.OpenRead("test"))
            {
                var expected = new byte[] { 1, 2, 3, 4 };
                var bytes = new byte[4];
                for (int i = 0; i < fileInfo.ChunkSize * 3; i += 4)
                {
                    var bytesRead = stream.Read(bytes, 0, 4);
                    Assert.AreEqual(4, bytesRead);
                    Assert.IsTrue(expected.SequenceEqual(bytes));
                }
                var eof = stream.Read(bytes, 0, 1);
                Assert.AreEqual(0, eof);
            }
        }

        [Test]
        public void TestOpenCreateWithId()
        {
            _gridFS.Files.RemoveAll();
            _gridFS.Chunks.RemoveAll();

            var createOptions = new MongoGridFSCreateOptions
            {
                Id = 1
            };
            using (var stream = _gridFS.Create("test", createOptions))
            {
                var bytes = new byte[] { 1, 2, 3, 4 };
                stream.Write(bytes, 0, 4);
            }

            var fileInfo = _gridFS.FindOne("test");
            Assert.AreEqual(new BsonInt32(1), fileInfo.Id);
        }

        [Test]
        public void TestOpenCreateWithMetadata()
        {
            _gridFS.Files.RemoveAll();
            _gridFS.Chunks.RemoveAll();

            var metadata = new BsonDocument("author", "John Doe");
            var createOptions = new MongoGridFSCreateOptions
            {
                Metadata = metadata
            };
            using (var stream = _gridFS.Create("test", createOptions))
            {
                var bytes = new byte[] { 1, 2, 3, 4 };
                stream.Write(bytes, 0, 4);
            }

            var fileInfo = _gridFS.FindOne("test");
            Assert.AreEqual(metadata, fileInfo.Metadata);
        }

        [Test]
        public void TestUpdateMD5()
        {
            _gridFS.Files.RemoveAll();
            _gridFS.Chunks.RemoveAll();

            var fileInfo = _gridFS.FindOne("test");
            Assert.IsNull(fileInfo);

            var settings = new MongoGridFSSettings()
            {
                ChunkSize = 16,
                UpdateMD5 = false,
                WriteConcern = WriteConcern.Acknowledged
            };
            var gridFS = _database.GetGridFS(settings);

            using (var stream = gridFS.Create("test"))
            {
                var bytes = new byte[] { 1, 2, 3, 4 };
                stream.Write(bytes, 0, 4);
            }

            fileInfo = _gridFS.FindOne("test");
            Assert.IsTrue(fileInfo.Exists);
            Assert.AreEqual(4, fileInfo.Length);
            Assert.IsNull(fileInfo.MD5);

            settings = new MongoGridFSSettings()
            {
                ChunkSize = 16,
                UpdateMD5 = true,
                WriteConcern = WriteConcern.Acknowledged
            };
            gridFS = _database.GetGridFS(settings);

            using (var stream = gridFS.Open("test", FileMode.Append, FileAccess.Write))
            {
                var bytes = new byte[] { 1, 2, 3, 4 };
                stream.Write(bytes, 0, 4);
            }

            fileInfo = _gridFS.FindOne("test");
            Assert.IsTrue(fileInfo.Exists);
            Assert.AreEqual(8, fileInfo.Length);
            Assert.IsNotNull(fileInfo.MD5);
        }
    }
}

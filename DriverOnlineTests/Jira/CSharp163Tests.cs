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
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverOnlineTests.Jira.CSharp163
{
    [TestFixture]
    public class CSharp163Tests
    {
        private MongoServer server;
        private MongoDatabase database;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            database = server["onlinetests"];
        }

        [Test]
        public void TestNullAliasesAndContentType()
        {
            database.GridFS.Files.RemoveAll();
            database.GridFS.Chunks.RemoveAll();

            var text = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(text);
            var stream = new MemoryStream(bytes);
            var fileInfo = database.GridFS.Upload(stream, null); // test no filename!
            Assert.IsNull(fileInfo.Aliases);
            Assert.IsNull(fileInfo.ContentType);
            Assert.IsNull(fileInfo.Metadata);
            Assert.IsNull(fileInfo.Name);

            var query = Query.EQ("_id", fileInfo.Id);
            var files = database.GridFS.Files.FindOne(query);
            Assert.IsFalse(files.Contains("aliases"));
            Assert.IsFalse(files.Contains("contentType"));
            Assert.IsFalse(files.Contains("filename"));
            Assert.IsFalse(files.Contains("metadata"));

            // simulate null values as stored by other drivers
            var update = Update
                .Set("aliases", BsonNull.Value)
                .Set("contentType", BsonNull.Value)
                .Set("filename", BsonNull.Value)
                .Set("metadata", BsonNull.Value);
            database.GridFS.Files.Update(query, update);

            var fileInfo2 = database.GridFS.FindOne(query);
            Assert.IsNull(fileInfo2.Aliases);
            Assert.IsNull(fileInfo2.ContentType);
            Assert.IsNull(fileInfo2.Metadata);
            Assert.IsNull(fileInfo2.Name);

            // test that non-null values still work
            var aliases = new[] { "a", "b", "c" };
            var contentType = "text/plain";
            var metadata = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var name = "HelloWorld.txt";
            database.GridFS.SetAliases(fileInfo, aliases);
            database.GridFS.SetContentType(fileInfo, contentType);
            database.GridFS.SetMetadata(fileInfo, metadata);
            fileInfo.MoveTo(name);

            var fileInfo3 = database.GridFS.FindOne(query);
            Assert.IsTrue(aliases.SequenceEqual(fileInfo3.Aliases));
            Assert.AreEqual(contentType, fileInfo3.ContentType);
            Assert.AreEqual(metadata, fileInfo3.Metadata);
            Assert.AreEqual(name, fileInfo3.Name);
        }
    }
}

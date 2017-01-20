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

using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp163
{
    public class CSharp163Tests
    {
        private MongoDatabase _database;

        public CSharp163Tests()
        {
            _database = LegacyTestConfiguration.Database;
        }

        [Fact]
        public void TestNullAliasesAndContentType()
        {
            _database.GridFS.Files.RemoveAll();
            _database.GridFS.Chunks.RemoveAll();

            var text = "Hello World";
            var bytes = Encoding.UTF8.GetBytes(text);
            var stream = new MemoryStream(bytes);
            var fileInfo = _database.GridFS.Upload(stream, null); // test no filename!
            Assert.Null(fileInfo.Aliases);
            Assert.Null(fileInfo.ContentType);
            Assert.Null(fileInfo.Metadata);
            Assert.Null(fileInfo.Name);

            var query = Query.EQ("_id", fileInfo.Id);
            var files = _database.GridFS.Files.FindOne(query);
            Assert.False(files.Contains("aliases"));
            Assert.False(files.Contains("contentType"));
            Assert.False(files.Contains("filename"));
            Assert.False(files.Contains("metadata"));

            // simulate null values as stored by other drivers
            var update = Update
                .Set("aliases", BsonNull.Value)
                .Set("contentType", BsonNull.Value)
                .Set("filename", BsonNull.Value)
                .Set("metadata", BsonNull.Value);
            _database.GridFS.Files.Update(query, update);

            var fileInfo2 = _database.GridFS.FindOne(query);
            Assert.Null(fileInfo2.Aliases);
            Assert.Null(fileInfo2.ContentType);
            Assert.Null(fileInfo2.Metadata);
            Assert.Null(fileInfo2.Name);

            // test that non-null values still work
            var aliases = new[] { "a", "b", "c" };
            var contentType = "text/plain";
            var metadata = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var name = "HelloWorld.txt";
            _database.GridFS.SetAliases(fileInfo, aliases);
            _database.GridFS.SetContentType(fileInfo, contentType);
            _database.GridFS.SetMetadata(fileInfo, metadata);
            fileInfo.MoveTo(name);

            var fileInfo3 = _database.GridFS.FindOne(query);
            Assert.True(aliases.SequenceEqual(fileInfo3.Aliases));
            Assert.Equal(contentType, fileInfo3.ContentType);
            Assert.Equal(metadata, fileInfo3.Metadata);
            Assert.Equal(name, fileInfo3.Name);
        }
    }
}

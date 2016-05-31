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
using System.Text;
using System.Threading;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Xunit;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver.Tests.Jira.CSharp269
{
    public class CSharp269Tests
    {
        private MongoServer _server;
        private MongoDatabase _database;

        public CSharp269Tests()
        {
            var clientSettings = DriverTestConfiguration.Client.Settings.Clone();
            clientSettings.ReadPreference = ReadPreference.SecondaryPreferred;
            var client = new MongoClient(clientSettings); // ReadPreference=SecondaryPreferred
#pragma warning disable 618
            _server = client.GetServer();
#pragma warning restore
            _database = _server.GetDatabase(LegacyTestConfiguration.Database.Name);
            _database.GridFS.Files.Drop();
            _database.GridFS.Chunks.Drop();
        }

        [Fact]
        public void TestUploadAndDownload()
        {
            MongoGridFSFileInfo uploadedFileInfo;

            var text = "HelloWorld";
            var bytes = Encoding.UTF8.GetBytes(text);
            using (var stream = new MemoryStream(bytes))
            {
                uploadedFileInfo = _database.GridFS.Upload(stream, "HelloWorld.txt");
            }

            // use RequestStart so that if we are running this test against a replica set we will bind to a specific secondary
            using (_server.RequestStart(ReadPreference.SecondaryPreferred))
            {
                // wait for the GridFS file to be replicated before trying to Download it
                var timeoutAt = DateTime.UtcNow.AddSeconds(30);
                while (!_database.GridFS.Exists(Query.EQ("_id", uploadedFileInfo.Id)))
                {
                    if (DateTime.UtcNow >= timeoutAt)
                    {
                        throw new TimeoutException("HelloWorld.txt failed to propagate to secondary");
                    }
                    Thread.Sleep(TimeSpan.FromMilliseconds(1));
                }

                using (var stream = new MemoryStream())
                {
                    _database.GridFS.Download(stream, "HelloWorld.txt");
                    var downloadedBytes = stream.ToArray();
                    var downloadedText = Encoding.UTF8.GetString(downloadedBytes);
                    Assert.Equal("HelloWorld", downloadedText);
                }
            }
        }
    }
}

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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverOnlineTests.Jira.CSharp269
{
    [TestFixture]
    public class CSharp269Tests
    {
        private MongoServer _server;
        private MongoDatabase _database;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            var serverSettings = Configuration.TestServer.Settings.Clone();
            serverSettings.SlaveOk = true;
            _server = MongoServer.Create(serverSettings); // slaveOk=true
            _database = Configuration.TestDatabase;
            _database.GridFS.Files.Drop();
            _database.GridFS.Chunks.Drop();
        }

        [Test]
        public void TestUploadAndDownload()
        {
            var text = "HelloWorld";
            var bytes = Encoding.UTF8.GetBytes(text);
            using (var stream = new MemoryStream(bytes))
            {
                _database.GridFS.Upload(stream, "HelloWorld.txt");
            }

            using (var stream = new MemoryStream())
            {
                _database.GridFS.Download(stream, "HelloWorld.txt");
                var downloadedBytes = stream.ToArray();
                var downloadedText = Encoding.UTF8.GetString(downloadedBytes);
                Assert.AreEqual("HelloWorld", downloadedText);
            }
        }
    }
}

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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp358
{
    [TestFixture]
    public class CSharp358Tests
    {
        [Test]
        public void TestInsertUpdateAndSaveWithElementNameStartingWithDollarSign()
        {
            // starting with version 2.5.2 the server got stricter about dollars in element names
            // so this test should only be run when testing against older servers
            var server = Configuration.TestServer;
            if (server.BuildInfo.Version < new Version(2, 5, 2))
            {
                var database = Configuration.TestDatabase;
                var collection = Configuration.TestCollection;
                collection.Drop();

                var document = new BsonDocument
                {
                    { "_id", 1 },
                    { "v", new BsonDocument("$x", 1) } // server doesn't allow "$" at top level
                };
                var insertOptions = new MongoInsertOptions { CheckElementNames = false };
                collection.Insert(document, insertOptions);
                document = collection.FindOne();
                Assert.AreEqual(1, document["v"]["$x"].AsInt32);

                document["v"]["$x"] = 2;
                var query = Query.EQ("_id", 1);
                var update = Update.Replace(document);
                var updateOptions = new MongoUpdateOptions { CheckElementNames = false };
                collection.Update(query, update, updateOptions);
                document = collection.FindOne();
                Assert.AreEqual(2, document["v"]["$x"].AsInt32);

                document["v"]["$x"] = 3;
                collection.Save(document, insertOptions);
                document = collection.FindOne();
                Assert.AreEqual(3, document["v"]["$x"].AsInt32);
            }
        }
    }
}

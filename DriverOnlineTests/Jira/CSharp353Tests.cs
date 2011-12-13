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

namespace MongoDB.DriverOnlineTests.Jira.CSharp353
{
    [TestFixture]
    public class CSharp353Tests
    {
        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection<BsonDocument> collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            server = MongoServer.Create("mongodb://localhost/?safe=true;slaveOk=true");
            database = server["onlinetests"];
            collection = database["test"];
        }

        [Test]
        public void TestDropDatabaseClearsIndexCache()
        {
            server.IndexCache.Reset();
            collection.EnsureIndex("x");
            Assert.IsTrue(server.IndexCache.Contains(collection, "x_1"));
            database.Drop();
            Assert.IsFalse(server.IndexCache.Contains(collection, "x_1"));
        }
    }
}

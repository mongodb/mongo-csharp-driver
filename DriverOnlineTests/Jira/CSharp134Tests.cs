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

namespace MongoDB.DriverOnlineTests.Jira.CSharp134
{
    [TestFixture]
    public class CSharp134Tests
    {
#pragma warning disable 649 // never assigned to
        private class C
        {
            public ObjectId Id;
            public MongoDBRef DbRef;
        }
#pragma warning restore

        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection<C> collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            database = server["onlinetests"];
            collection = database.GetCollection<C>("csharp134");
        }

        [Test]
        public void TestDeserializeMongoDBRef()
        {
            var dbRef = new MongoDBRef("test", ObjectId.GenerateNewId());
            var c = new C { DbRef = dbRef };
            collection.RemoveAll();
            collection.Insert(c);

            var rehydrated = collection.FindOne();
            Assert.IsNull(rehydrated.DbRef.DatabaseName);
            Assert.AreEqual(dbRef.CollectionName, rehydrated.DbRef.CollectionName);
            Assert.AreEqual(dbRef.Id, rehydrated.DbRef.Id);
        }
    }
}

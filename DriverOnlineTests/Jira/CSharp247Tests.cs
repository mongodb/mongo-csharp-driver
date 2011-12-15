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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverOnlineTests.Jira.CSharp247
{
    [TestFixture]
    public class CSharp247Tests
    {
        public interface I
        {
            int X { get; set; }
        }

        public class C : I
        {
            public ObjectId Id { get; set; }
            public int X { get; set; }
        }

        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection<BsonDocument> collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            database = server["onlinetests"];
            collection = database.GetCollection("testcollection");
        }

        [Test]
        public void TestDeserializeInterface()
        {
            collection.RemoveAll();

            var c = new C { X = 1 };
            collection.Insert<I>(c);
            var id = c.Id;

            var i = collection.FindOneAs<I>();
            Assert.IsInstanceOf<C>(i);
            var r = (C)i;
            Assert.AreEqual(id, r.Id);
            Assert.AreEqual(1, r.X);
        }
    }
}

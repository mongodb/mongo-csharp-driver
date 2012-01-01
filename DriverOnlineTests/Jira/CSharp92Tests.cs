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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace MongoDB.DriverOnlineTests.Jira.CSharp92
{
    [TestFixture]
    public class CSharp92Tests
    {
        private class C
        {
            [BsonId]
            public int Id { get; set; }
            public string P { get; set; }
        }

        [Test]
        public void TestSaveDocument()
        {
            var server = Configuration.TestServer;
            var database = Configuration.TestDatabase;
            var collection = Configuration.TestCollection;

            var document = new BsonDocument { { "_id", -1 }, { "P", "x" } };
            collection.RemoveAll();
            collection.Insert(document);

            var fetched = collection.FindOne();
            Assert.IsInstanceOf<BsonDocument>(fetched);
            Assert.AreEqual(-1, fetched["_id"].AsInt32);
            Assert.AreEqual("x", fetched["P"].AsString);
        }

        [Test]
        public void TestSaveClass()
        {
            var server = Configuration.TestServer;
            var database = Configuration.TestDatabase;
            var collection = Configuration.GetTestCollection<C>();

            var document = new C { Id = -1, P = "x" };
            collection.RemoveAll();
            collection.Insert(document);

            var fetched = collection.FindOne();
            Assert.IsInstanceOf<C>(fetched);
            Assert.AreEqual(-1, fetched.Id);
            Assert.AreEqual("x", fetched.P);
        }
    }
}

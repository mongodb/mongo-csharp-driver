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
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace MongoDB.DriverOnlineTests.Linq {
    [TestFixture]
    public class MongoLinqFindQueryTests {
        private class C {
            public ObjectId Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection<C> collection;

        [TestFixtureSetUp]
        public void Setup() {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            server.Connect();
            database = server["onlinetests"];
            collection = database.GetCollection<C>("linqtests");

            collection.Drop();
            collection.Insert(new C { X = 1, Y = 11 });
            collection.Insert(new C { X = 2, Y = 12 });
            collection.Insert(new C { X = 3, Y = 13 });
            collection.Insert(new C { X = 4, Y = 14 });
            collection.Insert(new C { X = 5, Y = 15 });
        }

        [Test]
        public void TestQueryXEquals1() {
            var query = from c in collection.AsQueryable<C>()
                        where c.X == 1
                        select c;
            var count = 0;
            foreach (var c in query) {
                Assert.AreEqual(1, c.X);
                count++;
            }
            Assert.AreEqual(1, count);
        }
   }
}

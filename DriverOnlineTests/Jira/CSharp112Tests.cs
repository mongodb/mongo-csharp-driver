/* Copyright 2010 10gen Inc.
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
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverOnlineTests.Jira.CSharp112 {
    [TestFixture]
    public class CSharp112Tests {
        private class C {
            public int Id;
            public int N;
        }

        [Test]
        public void TestDeserializeInt32() {
            var server = MongoServer.Create("mongodb://localhost/?safe=true");
            var database = server["onlinetests"];
            var collection = database.GetCollection<C>("csharp112");

            // test with valid values
            collection.RemoveAll();
            var values = new[] { 0.0, 0L, -1.0, -1L, 1.0, 1L, (double) Int32.MinValue, (double) Int32.MaxValue, (long) Int32.MinValue, (long) Int32.MaxValue };
            for (int i = 0; i < values.Length; i++) {
                var document = new BsonDocument {
                    { "_id", i + 1 },
                    { "N", values[i] }
                };
                collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++) {
                var query = Query.EQ("_id", i + 1);
                var c = collection.Find(query).Single();
                Assert.AreEqual((int) values[i], c.N);
            }

            // test with values that cause data loss
            collection.RemoveAll();
            values = new[] { -1.5, 1.5, ((long) Int32.MinValue - 1), ((long) Int32.MaxValue + 1) , Int64.MinValue, Int64.MaxValue };
            for (int i = 0; i < values.Length; i++) {
                var document = new BsonDocument {
                    { "_id", i + 1 },
                    { "N", values[i] }
                };
                collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++) {
                var query = Query.EQ("_id", i + 1);
                Assert.Throws<FileFormatException>(() => collection.Find(query).Single());
            }
        }
    }
}

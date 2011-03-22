﻿/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.DriverOnlineTests.Jira.CSharp112 {
    [TestFixture]
    public class CSharp112Tests {
#pragma warning disable 649 // never assigned to
        private class D {
            public int Id;
            public double N;
        }

        private class I {
            public int Id;
            public int N;
        }

        private class L {
            public int Id;
            public long N;
        }
#pragma warning restore

        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup() {
            server = MongoServer.Create("mongodb://localhost/?safe=true");
            database = server["onlinetests"];
            collection = database["csharp112"];
        }

        [Test]
        public void TestDeserializeDouble() {
            // test with valid values
            collection.RemoveAll();
            var values = new object[] {
                0,
                0L,
                0.0,
                -1,
                -1L,
                -1.0,
                1,
                1L,
                1.0,
                Int32.MinValue,
                Int32.MaxValue
            };
            for (int i = 0; i < values.Length; i++) {
                var document = new BsonDocument {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++) {
                var query = Query.EQ("_id", i + 1);
                var document = collection.FindOneAs<D>(query);
                Assert.AreEqual(BsonValue.Create(values[i]).ToDouble(), document.N);
            }

            // test with values that cause data loss
            collection.RemoveAll();
            values = new object[] {
                0x7eeeeeeeeeeeeeee,
                0xfeeeeeeeeeeeeeee,
                Int64.MinValue + 1, // need some low order bits to see data loss
                Int64.MaxValue
            };
            for (int i = 0; i < values.Length; i++) {
                var document = new BsonDocument {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++) {
                var query = Query.EQ("_id", i + 1);
                Assert.Throws<TruncationException>(() => collection.FindOneAs<D>(query));
            }
        }

        [Test]
        public void TestDeserializeInt32() {
            // test with valid values
            collection.RemoveAll();
            var values = new object[] {
                0L,
                0.0,
                -1L,
                -1.0,
                1L,
                1.0,
                (double) Int32.MinValue,
                (double) Int32.MaxValue,
                (long) Int32.MinValue,
                (long) Int32.MaxValue
            };
            for (int i = 0; i < values.Length; i++) {
                var document = new BsonDocument {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++) {
                var query = Query.EQ("_id", i + 1);
                var document = collection.FindOneAs<I>(query);
                Assert.AreEqual(BsonValue.Create(values[i]).ToInt32(), document.N);
            }

            // test with values that cause overflow
            collection.RemoveAll();
            values = new object[] {
                ((long) Int32.MinValue - 1),
                ((long) Int32.MaxValue + 1),
                Int64.MinValue,
                Int64.MaxValue,
                double.MaxValue,
                double.MinValue
            };
            for (int i = 0; i < values.Length; i++) {
                var document = new BsonDocument {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++) {
                var query = Query.EQ("_id", i + 1);
                Assert.Throws<OverflowException>(() => collection.FindOneAs<I>(query));
            }

            // test with values that cause truncation
            collection.RemoveAll();
            values = new object[] {
                -1.5,
                1.5
            };
            for (int i = 0; i < values.Length; i++) {
                var document = new BsonDocument {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++) {
                var query = Query.EQ("_id", i + 1);
                Assert.Throws<TruncationException>(() => collection.FindOneAs<I>(query));
            }
        }

        [Test]
        public void TestDeserializeInt64() {
            // test with valid values
            collection.RemoveAll();
            var values = new object[] {
                0,
                0L,
                0.0,
                -1,
                -1L,
                -1.0,
                1,
                1L,
                1.0,
                (double) Int32.MinValue,
                (double) Int32.MaxValue,
                (long) Int32.MinValue,
                (long) Int32.MaxValue,
                Int64.MinValue,
                Int64.MaxValue
            };
            for (int i = 0; i < values.Length; i++) {
                var document = new BsonDocument {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++) {
                var query = Query.EQ("_id", i + 1);
                var document = collection.FindOneAs<L>(query);
                Assert.AreEqual(BsonValue.Create(values[i]).ToInt64(), document.N);
            }

            // test with values that cause overflow
            collection.RemoveAll();
            values = new object[] {
                double.MaxValue,
                double.MinValue
            };
            for (int i = 0; i < values.Length; i++) {
                var document = new BsonDocument {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++) {
                var query = Query.EQ("_id", i + 1);
                Assert.Throws<OverflowException>(() => collection.FindOneAs<L>(query));
            }

            // test with values that cause data truncation
            collection.RemoveAll();
            values = new object[] {
                -1.5,
                1.5
            };
            for (int i = 0; i < values.Length; i++) {
                var document = new BsonDocument {
                    { "_id", i + 1 },
                    { "N", BsonValue.Create(values[i]) }
                };
                collection.Insert(document);
            }

            for (int i = 0; i < values.Length; i++) {
                var query = Query.EQ("_id", i + 1);
                Assert.Throws<TruncationException>(() => collection.FindOneAs<L>(query));
            }
        }
    }
}

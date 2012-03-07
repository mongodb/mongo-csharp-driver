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
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverOnlineTests.Jira.CSharp378
{
    [TestFixture]
    public class CSharp378Tests
    {
        public class C
        {
            static C()
            {
                BsonClassMap.RegisterClassMap<C>(cm =>
                {
                    cm.AutoMap();
                    cm.GetMemberMap(c => c.Id).SetSerializer(new MyIdSerializer());
                }
                );
            }

            public string Id;
            public int X;
        }

        public class D
        {
            public Guid Id;
            public int X;
        }

        public class MyIdSerializer : IBsonSerializer
        {
            public object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
            {
                var actualType = nominalType;
                return Deserialize(bsonReader, nominalType, actualType, options);
            }

            public object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
            {
                int timestamp, machine, increment;
                short pid;
                bsonReader.ReadObjectId(out timestamp, out machine, out pid, out increment);
                return new ObjectId(timestamp, machine, pid, increment).ToString();
            }

            public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
            {
                throw new NotSupportedException();
            }

            public BsonSerializationInfo GetItemSerializationInfo()
            {
                throw new NotSupportedException();
            }

            public BsonSerializationInfo GetMemberSerializationInfo(string memberName)
            {
                throw new NotSupportedException();
            }

            public void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
            {
                var objectId = ObjectId.Parse((string)value);
                bsonWriter.WriteObjectId(objectId.Timestamp, objectId.Machine, objectId.Pid, objectId.Increment);
            }

            public void SetDocumentId(object document, object id)
            {
                throw new NotSupportedException();
            }
        }


        private MongoServer _server;
        private MongoDatabase _database;
        private MongoCollection<C> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _server = Configuration.TestServer;
            _database = Configuration.TestDatabase;
            _collection = Configuration.GetTestCollection<C>();
        }

        [Test]
        public void TestSaveC()
        {
            _collection.Drop();

            var doc = new C { Id = ObjectId.GenerateNewId().ToString(), X = 1 };
            _collection.Insert(doc);
            var id = doc.Id;

            Assert.AreEqual(1, _collection.Count());
            var fetched = _collection.FindOne();
            Assert.AreEqual(id, fetched.Id);
            Assert.AreEqual(1, fetched.X);

            doc.X = 2;
            _collection.Save(doc);

            Assert.AreEqual(1, _collection.Count());
            fetched = _collection.FindOne();
            Assert.AreEqual(id, fetched.Id);
            Assert.AreEqual(2, fetched.X);
        }

        [Test]
        public void TestSaveD()
        {
            var collectionSettings = new MongoCollectionSettings<D>(_database, "test")
            {
                GuidRepresentation = GuidRepresentation.Standard
            };
            var collection = _database.GetCollection(collectionSettings);
            collection.Drop();

            var id = new Guid("00112233-4455-6677-8899-aabbccddeeff");
            var doc = new D { Id = id, X = 1 };
            collection.Insert(doc);

            Assert.AreEqual(1, collection.Count());
            var fetched = collection.FindOne();
            Assert.AreEqual(id, fetched.Id);
            Assert.AreEqual(1, fetched.X);

            doc.X = 2;
            collection.Save(doc);

            Assert.AreEqual(1, collection.Count());
            fetched = collection.FindOne();
            Assert.AreEqual(id, fetched.Id);
            Assert.AreEqual(2, fetched.X);
        }
    }
}

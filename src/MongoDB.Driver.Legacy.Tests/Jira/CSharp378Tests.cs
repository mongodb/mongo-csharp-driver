/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp378
{
    public class CSharp378Tests
    {
        public class C
        {
            [BsonSerializer(typeof(MyIdSerializer))]
            public string Id;
            public int X;
        }

        public class D
        {
            public Guid Id;
            public int X;
        }

        public class MyIdSerializer : ClassSerializerBase<string>
        {
            public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var bsonReader = context.Reader;
                return bsonReader.ReadObjectId().ToString();
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
            {
                var bsonWriter = context.Writer;
                bsonWriter.WriteObjectId(ObjectId.Parse(value));
            }
        }

        private MongoDatabase _database;
        private MongoCollection<C> _collection;

        public CSharp378Tests()
        {
            _database = LegacyTestConfiguration.Database;
            _collection = LegacyTestConfiguration.GetCollection<C>();
        }

        [Fact]
        public void TestSaveC()
        {
            _collection.Drop();

            var doc = new C { Id = ObjectId.GenerateNewId().ToString(), X = 1 };
            _collection.Insert(doc);
            var id = doc.Id;

            Assert.Equal(1, _collection.Count());
            var fetched = _collection.FindOne();
            Assert.Equal(id, fetched.Id);
            Assert.Equal(1, fetched.X);

            doc.X = 2;
            _collection.Save(doc);

            Assert.Equal(1, _collection.Count());
            fetched = _collection.FindOne();
            Assert.Equal(id, fetched.Id);
            Assert.Equal(2, fetched.X);
        }

        [Fact]
        public void TestSaveD()
        {
            var collectionSettings = new MongoCollectionSettings { GuidRepresentation = GuidRepresentation.Standard };
            var collection = _database.GetCollection<D>("test", collectionSettings);
            collection.Drop();

            var id = new Guid("00112233-4455-6677-8899-aabbccddeeff");
            var doc = new D { Id = id, X = 1 };
            collection.Insert(doc);

            Assert.Equal(1, collection.Count());
            var fetched = collection.FindOne();
            Assert.Equal(id, fetched.Id);
            Assert.Equal(1, fetched.X);

            doc.X = 2;
            collection.Save(doc);

            Assert.Equal(1, collection.Count());
            fetched = collection.FindOne();
            Assert.Equal(id, fetched.Id);
            Assert.Equal(2, fetched.X);
        }
    }
}

/* Copyright 2010-present MongoDB Inc.
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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MultipleRegistriesTests
    {
        [Fact]
        public void TestSerialization()
        {
            {
                var client = DriverTestConfiguration.CreateMongoClient();
                var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                db.DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var collection = db.GetCollection<Person>(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var bsonCollection =
                    db.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var person = new Person { Id = ObjectId.Parse("6797b56bf5495bf53aa3078f"), Name = "Mario", Age = 24 };
                collection.InsertOne(person);

                var retrieved = bsonCollection.FindSync("{}").ToList().Single();
                var toString = retrieved.ToString();

                var expectedVal =
                    """{ "_id" : { "$oid" : "6797b56bf5495bf53aa3078f" }, "Name" : "Mario", "Age" : 24 }""";
                Assert.Equal(expectedVal, toString);
            }

            //The first section demonstrates that the class maps are also separated
            {
                var customDomain = BsonSerializer.CreateSerializationDomain();
                customDomain.RegisterSerializer(new CustomStringSerializer());

                var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = customDomain);
                var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                db.DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var collection = db.GetCollection<Person>(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var bsonCollection =
                    db.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var person = new Person { Id = ObjectId.Parse("6797b56bf5495bf53aa3078f"), Name = "Mario", Age = 24 };
                collection.InsertOne(person);

                var retrievedAsBson = bsonCollection.FindSync("{}").ToList().Single();
                var toString = retrievedAsBson.ToString();

                var expectedVal =
                    """{ "_id" : { "$oid" : "6797b56bf5495bf53aa3078f" }, "Name" : "Mariotest", "Age" : 24 }""";
                Assert.Equal(expectedVal, toString);

                var retrievedTyped = collection.FindSync("{}").ToList().Single();
                Assert.Equal("Mario", retrievedTyped.Name);
            }
        }

        [Fact]
        public void TestDeserialization()
        {
            {
                var client = DriverTestConfiguration.CreateMongoClient();
                var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                db.DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
                var collection = db.GetCollection<Person1>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var person = new Person1 { Id = ObjectId.Parse("6797b56bf5495bf53aa3078f"), Name = "Mariotest", Age = 24 };
                collection.InsertOne(person);
            }

            {
                var customDomain = BsonSerializer.CreateSerializationDomain();
                customDomain.RegisterSerializer(new CustomStringSerializer());

                var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = customDomain);
                var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = db.GetCollection<Person>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var retrievedTyped = collection.FindSync("{}").ToList().Single();
                Assert.Equal("Mario", retrievedTyped.Name);
            }
        }

        [Fact]
        public void TestLinq()
        {
            var customDomain = BsonSerializer.CreateSerializationDomain();
            customDomain.RegisterSerializer(new CustomStringSerializer());

            var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = customDomain);
            var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            db.DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
            var collection = db.GetCollection<Person>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            var untypedCollection = db.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            var person = new Person { Id = ObjectId.Parse("6797b56bf5495bf53aa3078f"), Name = "Mario", Age = 24 };
            collection.InsertOne(person);

            var retrievedAsBson = untypedCollection.FindSync("{}").ToList().Single();
            var toString = retrievedAsBson.ToString();

            var expectedVal =
                """{ "_id" : { "$oid" : "6797b56bf5495bf53aa3078f" }, "Name" : "Mariotest", "Age" : 24 }""";
            Assert.Equal(expectedVal, toString);

            var retrievedTyped = collection.AsQueryable().Where(x => x.Name == "Mario").ToList();  //The string serializer is correctly serializing "Mario" to "Mariotest"
            Assert.NotEmpty(retrievedTyped);
        }

        public class Person
        {
            [BsonId] public ObjectId Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public class Person1
        {
            [BsonId] public ObjectId Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public class CustomStringSerializer : SealedClassSerializerBase<string> //This serializer just adds "test" to any serialised string
        {
            /// <inheritdoc/>
            public override int GetHashCode() => 0;

            protected override string DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var bsonReader = context.Reader;

                var bsonType = bsonReader.GetCurrentBsonType();
                return bsonType switch
                {
                    BsonType.String => bsonReader.ReadString().Replace("test", ""),
                    _ => throw CreateCannotDeserializeFromBsonTypeException(bsonType)
                };
            }

            protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args,
                string value)
            {
                var bsonWriter = context.Writer;
                bsonWriter.WriteString(value + "test");
            }
        }
    }
}
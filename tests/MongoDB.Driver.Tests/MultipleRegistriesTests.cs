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
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    [Trait("Category", "Integration")]
    public class MultipleRegistriesTests
    {
        [Fact]
        public void TestSerialization()
        {
            RequireServer.Check();

            // {
            //     var client = CreateClient();
            //     var collection = GetTypedCollection<Person>(client);
            //     var bsonCollection = GetUntypedCollection(client);
            //
            //     var person = new Person { Id = ObjectId.Parse("6797b56bf5495bf53aa3078f"), Name = "Mario", Age = 24 };
            //     collection.InsertOne(person);
            //
            //     var retrieved = bsonCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
            //     var toString = retrieved.ToString();
            //
            //     var expectedVal =
            //         """{ "_id" : { "$oid" : "6797b56bf5495bf53aa3078f" }, "Name" : "Mario", "Age" : 24 }""";
            //     Assert.Equal(expectedVal, toString);
            // }

            //The first section demonstrates that the class maps are also separated
            {
                var customDomain = BsonSerializer.CreateSerializationDomain();
                customDomain.RegisterSerializer(new CustomStringSerializer());

                var client = CreateClientWithDomain(customDomain);
                var collection = GetTypedCollection<Person>(client);
                var bsonCollection = GetUntypedCollection(client);

                var person = new Person { Id = ObjectId.Parse("6797b56bf5495bf53aa3078f"), Name = "Mario", Age = 24 };
                collection.InsertOne(person);

                var retrievedAsBson = bsonCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
                var toString = retrievedAsBson.ToString();

                var expectedVal =
                    """{ "_id" : { "$oid" : "6797b56bf5495bf53aa3078f" }, "Name" : "Mariotest", "Age" : 24 }""";
                Assert.Equal(expectedVal, toString);

                var retrievedTyped = collection.FindSync(FilterDefinition<Person>.Empty).ToList().Single();
                Assert.Equal("Mario", retrievedTyped.Name);
            }
        }

        [Fact]
        public void TestDeserialization()
        {
            RequireServer.Check();

            {
                var client = CreateClient();
                var collection = GetTypedCollection<Person1>(client);

                var person = new Person1 { Id = ObjectId.Parse("6797b56bf5495bf53aa3078f"), Name = "Mariotest", Age = 24 };
                collection.InsertOne(person);
            }

            {
                var customDomain = BsonSerializer.CreateSerializationDomain();
                customDomain.RegisterSerializer(new CustomStringSerializer());

                var client = CreateClientWithDomain(customDomain, dropCollection: false);
                var collection = GetTypedCollection<Person>(client);

                var retrievedTyped = collection.FindSync(FilterDefinition<Person>.Empty).ToList().Single();
                Assert.Equal("Mario", retrievedTyped.Name);
            }
        }

        [Fact]
        public void TestLinq()
        {
            RequireServer.Check();

            var customDomain = BsonSerializer.CreateSerializationDomain();
            customDomain.RegisterSerializer(new CustomStringSerializer());

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<Person>(client);
            var untypedCollection = GetUntypedCollection(client);

            var person = new Person { Id = ObjectId.Parse("6797b56bf5495bf53aa3078f"), Name = "Mario", Age = 24 };
            collection.InsertOne(person);

            var retrievedAsBson = untypedCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
            var toString = retrievedAsBson.ToString();

            var expectedVal =
                """{ "_id" : { "$oid" : "6797b56bf5495bf53aa3078f" }, "Name" : "Mariotest", "Age" : 24 }""";
            Assert.Equal(expectedVal, toString);

            var retrievedTyped = collection.AsQueryable().Where(x => x.Name == "Mario").ToList();  //The string serializer is correctly serializing "Mario" to "Mariotest"
            Assert.NotEmpty(retrievedTyped);
        }

        [Fact]
        public void TestConventions()
        {
            RequireServer.Check();

            var customDomain = BsonSerializer.CreateSerializationDomain();

            // Register an id generator convention that uses a custom ObjectIdGenerator
            customDomain.RegisterIdGenerator(typeof(ObjectId), new CustomObjectIdGenerator());

            //Register a convention to use lowercase for all fields on the Person class
            var pack = new ConventionPack();
            pack.AddMemberMapConvention(
                "LowerCaseElementName",
                m => m.SetElementName(m.MemberName.ToLower()));
            customDomain.ConventionRegistry.Register("myPack", pack, t => t == typeof(Person));

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<Person>(client);
            var untypedCollection = GetUntypedCollection(client);

            var person = new Person { Name = "Mario", Age = 24 };  //Id is not set, so the custom ObjectIdGenerator should be used
            collection.InsertOne(person);

            var retrievedAsBson = untypedCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
            var toString = retrievedAsBson.ToString();

            var expectedVal =
                """{ "_id" : { "$oid" : "6797b56bf5495bf53aa3078f" }, "name" : "Mario", "age" : 24 }""";
            Assert.Equal(expectedVal, toString);
        }

        [Fact]
        public void TestDiscriminators()
        {
            RequireServer.Check();

            var customDomain = BsonSerializer.CreateSerializationDomain();

            customDomain.BsonClassMap.RegisterClassMap<BasePerson>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });

            customDomain.BsonClassMap.RegisterClassMap<DerivedPerson1>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("dp1");
                cm.MapMember( m => m.ExtraField1).SetSerializer(new CustomStringSerializer());
            });

            customDomain.BsonClassMap.RegisterClassMap<DerivedPerson2>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("dp2");
                cm.MapMember( m => m.ExtraField2).SetSerializer(new CustomStringSerializer());
            });

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<BasePerson>(client);

            var bp1 = new DerivedPerson1 { Name = "Alice", Age = 30, ExtraField1 = "Field1" };
            var bp2 = new DerivedPerson2 { Name = "Bob", Age = 40, ExtraField2 = "Field2" };
            collection.InsertMany(new BasePerson[] { bp1, bp2 });

            //Aggregate with OfType
            var retrievedDerivedPerson1 = collection.Aggregate().OfType<DerivedPerson1>().Single();
            var retrievedDerivedPerson2 = collection.Aggregate().OfType<DerivedPerson2>().Single();

            AssertDerivedPerson1(bp1, retrievedDerivedPerson1);
            AssertDerivedPerson2(bp2, retrievedDerivedPerson2);

            //AppendStage with OfType
            retrievedDerivedPerson1 = collection.AsQueryable().AppendStage(PipelineStageDefinitionBuilder.OfType<BasePerson, DerivedPerson1>()).Single();
            retrievedDerivedPerson2 = collection.AsQueryable().AppendStage(PipelineStageDefinitionBuilder.OfType<BasePerson, DerivedPerson2>()).Single();

            AssertDerivedPerson1(bp1, retrievedDerivedPerson1);
            AssertDerivedPerson2(bp2, retrievedDerivedPerson2);

            //LINQ with OfType
            retrievedDerivedPerson1 = collection.AsQueryable().OfType<DerivedPerson1>().Single();
            retrievedDerivedPerson2 = collection.AsQueryable().OfType<DerivedPerson2>().Single();

            AssertDerivedPerson1(bp1, retrievedDerivedPerson1);
            AssertDerivedPerson2(bp2, retrievedDerivedPerson2);

            //Facet with OfType

            var pipeline1 = PipelineDefinition<BasePerson, DerivedPerson1>.Create( new [] {
                PipelineStageDefinitionBuilder.OfType<BasePerson, DerivedPerson1>() });
            var facet1 = AggregateFacet.Create("facet1", pipeline1);

            var pipeline2 = PipelineDefinition<BasePerson, DerivedPerson2>.Create( new [] {
                PipelineStageDefinitionBuilder.OfType<BasePerson, DerivedPerson2>() });
            var facet2 = AggregateFacet.Create("facet2", pipeline2);

            var result = collection.Aggregate().Facet(facet1, facet2).Single().Facets;
            retrievedDerivedPerson1 = result[0].Output<DerivedPerson1>().Single();
            retrievedDerivedPerson2 = result[1].Output<DerivedPerson2>().Single();

            AssertDerivedPerson1(bp1, retrievedDerivedPerson1);
            AssertDerivedPerson2(bp2, retrievedDerivedPerson2);

            //Find with OfType
            var retrievedBasePerson1 = collection.FindSync(Builders<BasePerson>.Filter.OfType<DerivedPerson1>()).Single();
            var retrievedBasePerson2 = collection.FindSync(Builders<BasePerson>.Filter.OfType<DerivedPerson2>()).Single();

            AssertBasePerson(bp1, retrievedBasePerson1);
            AssertBasePerson(bp2, retrievedBasePerson2);

            void AssertDerivedPerson1(DerivedPerson1 expected, DerivedPerson1 retrieved)
            {
                AssertBasePerson(expected, retrieved);
                Assert.Equal(expected.ExtraField1, retrieved.ExtraField1);
            }

            void AssertDerivedPerson2(DerivedPerson2 expected, DerivedPerson2 retrieved)
            {
                AssertBasePerson(expected, retrieved);
                Assert.Equal(expected.ExtraField2, retrieved.ExtraField2);
            }

            void AssertBasePerson(BasePerson expected, BasePerson retrieved)
            {
                Assert.Equal(expected.Id, retrieved.Id);
                Assert.Equal(expected.Name, retrieved.Name);
                Assert.Equal(expected.Age, retrieved.Age);
            }
        }

        private static IMongoCollection<T> GetTypedCollection<T>(IMongoClient client) =>
            client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .GetCollection<T>(DriverTestConfiguration.CollectionNamespace.CollectionName);

        private static IMongoCollection<BsonDocument> GetUntypedCollection(IMongoClient client) =>
            client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

        private static IMongoClient CreateClientWithDomain(IBsonSerializationDomain domain, bool dropCollection = true)
        {
            var client = DriverTestConfiguration.CreateMongoClient((MongoClientSettings c) => ((IInheritableMongoClientSettings)c).SerializationDomain = domain);
            if (dropCollection)
            {
                var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                db.DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
            }
            return client;
        }

        private static IMongoClient CreateClient()
        {
            var client = DriverTestConfiguration.CreateMongoClient();
            var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            db.DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
            return client;
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

        public class BasePerson
        {
            [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public class DerivedPerson1 : BasePerson
        {
            public string ExtraField1 { get; set; }
        }

        public class DerivedPerson2 : BasePerson
        {
            public string ExtraField2 { get; set; }
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

        public class CustomObjectIdGenerator : IIdGenerator
        {
            public object GenerateId(object container, object document)
            {
                return ObjectId.Parse("6797b56bf5495bf53aa3078f");
            }

            public bool IsEmpty(object id)
            {
                return true;
            }
        }
    }
}
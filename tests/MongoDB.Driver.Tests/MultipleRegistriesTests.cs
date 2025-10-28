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
        private readonly string _defaultObjectIdString = "6797b56bf5495bf53aa3078f";
        private readonly ObjectId _defaultId = ObjectId.Parse("6797b56bf5495bf53aa3078f");

        [Fact]
        public void TestSerialization()
        {
            RequireServer.Check();

            // The first section demonstrates that the class maps are also separated
            {
                var client = CreateClient();
                var collection = GetTypedCollection<Person>(client);
                var bsonCollection = GetUntypedCollection(client);

                var person = new Person { Id = _defaultId, Name = "Mario", Age = 24 };
                collection.InsertOne(person);

                var retrieved = bsonCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
                var toString = retrieved.ToString();

                var expectedVal =
                    $$"""{ "_id" : { "$oid" : "{{_defaultObjectIdString}}" }, "Name" : "Mario", "Age" : 24 }""";
                Assert.Equal(expectedVal, toString);
            }

            {
                var customDomain = BsonSerializer.CreateSerializationDomain();
                customDomain.RegisterSerializer(new CustomStringSerializer("test1"));

                var client = CreateClientWithDomain(customDomain);
                var collection = GetTypedCollection<Person>(client);
                var bsonCollection = GetUntypedCollection(client);

                var person = new Person { Id = _defaultId, Name = "Mario", Age = 24 };
                collection.InsertOne(person);

                var retrievedAsBson = bsonCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
                var toString = retrievedAsBson.ToString();

                var expectedVal =
                    $$"""{ "_id" : { "$oid" : "{{_defaultObjectIdString}}" }, "Name" : "Mariotest1", "Age" : 24 }""";
                Assert.Equal(expectedVal, toString);

                var retrievedTyped = collection.FindSync(FilterDefinition<Person>.Empty).ToList().Single();
                Assert.Equal("Mario", retrievedTyped.Name);
            }

            {
                var customDomain = BsonSerializer.CreateSerializationDomain();
                customDomain.RegisterSerializer(new CustomStringSerializer("test2"));

                var client = CreateClientWithDomain(customDomain);
                var collection = GetTypedCollection<Person>(client);
                var bsonCollection = GetUntypedCollection(client);

                var person = new Person { Id = _defaultId, Name = "Mario", Age = 24 };
                collection.InsertOne(person);

                var retrievedAsBson = bsonCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
                var toString = retrievedAsBson.ToString();

                var expectedVal =
                    $$"""{ "_id" : { "$oid" : "{{_defaultObjectIdString}}" }, "Name" : "Mariotest2", "Age" : 24 }""";
                Assert.Equal(expectedVal, toString);

                var retrievedTyped = collection.FindSync(FilterDefinition<Person>.Empty).ToList().Single();
                Assert.Equal("Mario", retrievedTyped.Name);
            }
        }

        [Fact]
        public void TestMultipleDomainSimultaneously()
        {
            RequireServer.Check();

            var objectId1 = ObjectId.GenerateNewId();
            var objectId2 = ObjectId.GenerateNewId();

            var client = CreateClient();
            var collection = GetTypedCollection<Person>(client);
            var bsonCollection = GetUntypedCollection(client);

            var customDomain = BsonSerializer.CreateSerializationDomain();
            customDomain.RegisterSerializer(new CustomStringSerializer("test1"));
            var client2 = CreateClientWithDomain(customDomain);
            var collection2 = GetTypedCollection<Person>(client2);
            var bsonCollection2 = GetUntypedCollection(client2);

            var person = new Person { Id = objectId1, Name = "Mario", Age = 24 };
            var person2 = new Person { Id = objectId2, Name = "Mario", Age = 24 };
            collection.InsertOne(person);
            collection2.InsertOne(person2);

            var retrieved = bsonCollection.FindSync(Builders<BsonDocument>.Filter.Eq("_id", objectId1)).ToList().Single();
            var expectedVal =
                $$"""{ "_id" : { "$oid" : "{{objectId1.ToString()}}" }, "Name" : "Mario", "Age" : 24 }""";
            Assert.Equal(expectedVal, retrieved.ToString());

            var retrievedAsBson = bsonCollection2.FindSync(Builders<BsonDocument>.Filter.Eq("_id", objectId2)).ToList().Single();
            var expectedVal2 =
                $$"""{ "_id" : { "$oid" : "{{objectId2.ToString()}}" }, "Name" : "Mariotest1", "Age" : 24 }""";
            Assert.Equal(expectedVal2, retrievedAsBson.ToString());

            var retrievedTyped = collection2.FindSync(p => p.Id == objectId2).ToList().Single();
            Assert.Equal("Mario", retrievedTyped.Name);
        }

        [Fact]
        public void TestDeserialization()
        {
            RequireServer.Check();

            {
                var client = CreateClient();
                var collection = GetTypedCollection<Person1>(client);

                var person = new Person1 { Id = _defaultId, Name = "Mariotest", Age = 24 };
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

            var person = new Person { Id = _defaultId, Name = "Mario", Age = 24 };
            collection.InsertOne(person);

            var retrievedAsBson = untypedCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
            var toString = retrievedAsBson.ToString();

            var expectedVal =
                $$"""{ "_id" : { "$oid" : "{{_defaultObjectIdString}}" }, "Name" : "Mariotest", "Age" : 24 }""";
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
                $$"""{ "_id" : { "$oid" : "{{_defaultObjectIdString}}" }, "name" : "Mario", "age" : 24 }""";
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
                cm.SetDiscriminator("bp");
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
            var untypedCollection = GetUntypedCollection(client);

            var bp1 = new DerivedPerson1 { Name = "Alice", Age = 30, ExtraField1 = "Field1" };
            var bp2 = new DerivedPerson2 { Name = "Bob", Age = 40, ExtraField2 = "Field2" };
            collection.InsertMany([bp1, bp2]);

            var retrieved1 = untypedCollection.FindSync(Builders<BsonDocument>.Filter.Eq("_id", bp1.Id)).ToList().Single();
            var retrieved2 = untypedCollection.FindSync(Builders<BsonDocument>.Filter.Eq("_id", bp2.Id)).ToList().Single();

            var expectedDiscriminator1 =
                $"""_t" : ["bp", "dp1"]""";
            var expectedDiscriminator2 =
                $"""_t" : ["bp", "dp2"]""";
            Assert.Contains(expectedDiscriminator1, retrieved1.ToString());
            Assert.Contains(expectedDiscriminator2, retrieved2.ToString());

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

        [Fact]
        public void TestDiscriminatorsWithAttributes()
        {
            RequireServer.Check();

            var customDomain = BsonSerializer.CreateSerializationDomain();
            customDomain.RegisterSerializer(new CustomStringSerializer());

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<BasePersonAttribute>(client);
            var untypedCollection = GetUntypedCollection(client);

            var bp1 = new DerivedPersonAttribute1 { Name = "Alice", Age = 30, ExtraField1 = "Field1" };
            var bp2 = new DerivedPersonAttribute2 { Name = "Bob", Age = 40, ExtraField2 = "Field2" };
            collection.InsertMany([bp1, bp2]);

            var retrieved1 = untypedCollection.FindSync(Builders<BsonDocument>.Filter.Eq("_id", bp1.Id)).ToList().Single();
            var retrieved2 = untypedCollection.FindSync(Builders<BsonDocument>.Filter.Eq("_id", bp2.Id)).ToList().Single();

            var expectedDiscriminator1 =
                $"""_t" : ["bp", "dp1"]""";
            var expectedDiscriminator2 =
                $"""_t" : ["bp", "dp2"]""";
            Assert.Contains(expectedDiscriminator1, retrieved1.ToString());
            Assert.Contains(expectedDiscriminator2, retrieved2.ToString());

            //Aggregate with OfType
            var retrievedDerivedPerson1 = collection.Aggregate().OfType<DerivedPersonAttribute1>().Single();
            var retrievedDerivedPerson2 = collection.Aggregate().OfType<DerivedPersonAttribute2>().Single();

            AssertDerivedPerson1(bp1, retrievedDerivedPerson1);
            AssertDerivedPerson2(bp2, retrievedDerivedPerson2);

            //AppendStage with OfType
            retrievedDerivedPerson1 = collection.AsQueryable().AppendStage(PipelineStageDefinitionBuilder.OfType<BasePersonAttribute, DerivedPersonAttribute1>()).Single();
            retrievedDerivedPerson2 = collection.AsQueryable().AppendStage(PipelineStageDefinitionBuilder.OfType<BasePersonAttribute, DerivedPersonAttribute2>()).Single();

            AssertDerivedPerson1(bp1, retrievedDerivedPerson1);
            AssertDerivedPerson2(bp2, retrievedDerivedPerson2);

            //LINQ with OfType
            retrievedDerivedPerson1 = collection.AsQueryable().OfType<DerivedPersonAttribute1>().Single();
            retrievedDerivedPerson2 = collection.AsQueryable().OfType<DerivedPersonAttribute2>().Single();

            AssertDerivedPerson1(bp1, retrievedDerivedPerson1);
            AssertDerivedPerson2(bp2, retrievedDerivedPerson2);


            void AssertDerivedPerson1(DerivedPersonAttribute1 expected, DerivedPersonAttribute1 retrieved)
            {
                AssertBasePerson(expected, retrieved);
                Assert.Equal(expected.ExtraField1, retrieved.ExtraField1);
            }

            void AssertDerivedPerson2(DerivedPersonAttribute2 expected, DerivedPersonAttribute2 retrieved)
            {
                AssertBasePerson(expected, retrieved);
                Assert.Equal(expected.ExtraField2, retrieved.ExtraField2);
            }

            void AssertBasePerson(BasePersonAttribute expected, BasePersonAttribute retrieved)
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

        [BsonDiscriminator("bp", RootClass = true)]
        public class BasePersonAttribute
        {
            [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [BsonDiscriminator("dp1")]
        public class DerivedPersonAttribute1 : BasePersonAttribute
        {
            public string ExtraField1 { get; set; }
        }

        [BsonDiscriminator("dp2")]
        public class DerivedPersonAttribute2 : BasePersonAttribute
        {
            public string ExtraField2 { get; set; }
        }

        // This serializer adds the _appended variable to any serialised string
        public class CustomStringSerializer(string appended = "test")
            : SealedClassSerializerBase<string>
        {
            /// <inheritdoc/>
            public override int GetHashCode() => 0;

            protected override string DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var bsonReader = context.Reader;

                var bsonType = bsonReader.GetCurrentBsonType();
                return bsonType switch
                {
                    BsonType.String => bsonReader.ReadString().Replace(appended, ""),
                    _ => throw CreateCannotDeserializeFromBsonTypeException(bsonType)
                };
            }

            protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, string value)
            {
                var bsonWriter = context.Writer;
                bsonWriter.WriteString(value + appended);
            }
        }

        public class CustomObjectIdGenerator : IIdGenerator
        {
            public object GenerateId(object container, object document)
            {
                return  ObjectId.Parse("6797b56bf5495bf53aa3078f");
            }

            public bool IsEmpty(object id)
            {
                return true;
            }
        }
    }
}
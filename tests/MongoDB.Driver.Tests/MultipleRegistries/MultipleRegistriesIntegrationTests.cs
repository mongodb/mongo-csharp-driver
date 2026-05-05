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
using System.Text;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    [Trait("Category", "Integration")]
    public class MultipleRegistriesIntegrationTests
    {
        private readonly string _defaultObjectIdString = "6797b56bf5495bf53aa3078f";
        private readonly ObjectId _defaultId = ObjectId.Parse("6797b56bf5495bf53aa3078f");

        [Fact]
        public void Conventions_and_custom_id_generator_apply_per_domain()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            customDomain.RegisterIdGenerator(typeof(ObjectId), new CustomObjectIdGenerator());

            var pack = new ConventionPack(customDomain);
            pack.AddMemberMapConvention(
                "LowerCaseElementName",
                m => m.SetElementName(m.MemberName.ToLower()));
            customDomain.ConventionRegistry.Register("myPack", pack, t => t == typeof(Person));

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<Person>(client);
            var untypedCollection = GetUntypedCollection(client);

            var person = new Person { Name = "Mario", Age = 24 };
            collection.InsertOne(person);

            var retrievedAsBson = untypedCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
            var expectedVal = $$"""{ "_id" : { "$oid" : "{{_defaultObjectIdString}}" }, "name" : "Mario", "age" : 24 }""";
            Assert.Equal(expectedVal, retrievedAsBson.ToString());
        }

        [Fact]
        public void Deserialization_with_custom_domain_strips_suffix()
        {
            RequireServer.Check();

            // Insert with default domain — Name stored as-is.
            {
                var client = CreateClient();
                var collection = GetTypedCollection<Person>(client);
                collection.InsertOne(new Person { Id = _defaultId, Name = "Mariotest", Age = 24 });
            }

            // Read back with a custom domain whose serializer strips the "test" suffix.
            {
                var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
                customDomain.RegisterSerializer(new CustomStringSerializer());

                var client = CreateClientWithDomain(customDomain, dropCollection: false);
                var collection = GetTypedCollection<Person>(client);

                var retrievedTyped = collection.FindSync(FilterDefinition<Person>.Empty).ToList().Single();
                Assert.Equal("Mario", retrievedTyped.Name);
            }
        }

        [Fact]
        public void Discriminators_via_BsonDiscriminator_attributes_use_custom_domain()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            customDomain.RegisterSerializer(new CustomStringSerializer());

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<BasePersonAttribute>(client);
            var untypedCollection = GetUntypedCollection(client);

            var bp1 = new DerivedPersonAttribute1 { Name = "Alice", Age = 30, ExtraField1 = "Field1" };
            var bp2 = new DerivedPersonAttribute2 { Name = "Bob", Age = 40, ExtraField2 = "Field2" };
            collection.InsertMany([bp1, bp2]);

            var retrieved1 = untypedCollection.FindSync(Builders<BsonDocument>.Filter.Eq("_id", bp1.Id)).ToList().Single();
            var retrieved2 = untypedCollection.FindSync(Builders<BsonDocument>.Filter.Eq("_id", bp2.Id)).ToList().Single();

            Assert.Contains("""_t" : ["bp", "dp1"]""", retrieved1.ToString());
            Assert.Contains("""_t" : ["bp", "dp2"]""", retrieved2.ToString());

            // Aggregate with OfType
            AssertDerivedPersonAttribute1(bp1, collection.Aggregate().OfType<DerivedPersonAttribute1>().Single());
            AssertDerivedPersonAttribute2(bp2, collection.Aggregate().OfType<DerivedPersonAttribute2>().Single());

            // AppendStage with OfType
            AssertDerivedPersonAttribute1(bp1, collection.AsQueryable().AppendStage(PipelineStageDefinitionBuilder.OfType<BasePersonAttribute, DerivedPersonAttribute1>()).Single());
            AssertDerivedPersonAttribute2(bp2, collection.AsQueryable().AppendStage(PipelineStageDefinitionBuilder.OfType<BasePersonAttribute, DerivedPersonAttribute2>()).Single());

            // LINQ with OfType
            AssertDerivedPersonAttribute1(bp1, collection.AsQueryable().OfType<DerivedPersonAttribute1>().Single());
            AssertDerivedPersonAttribute2(bp2, collection.AsQueryable().OfType<DerivedPersonAttribute2>().Single());
        }

        [Fact]
        public void Discriminators_via_class_map_registration_use_custom_domain()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

            customDomain.ClassMapRegistry.RegisterClassMap<BasePerson>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("bp");
                cm.SetIsRootClass(true);
            });
            customDomain.ClassMapRegistry.RegisterClassMap<DerivedPerson1>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("dp1");
                cm.MapMember(m => m.ExtraField1).SetSerializer(new CustomStringSerializer());
            });
            customDomain.ClassMapRegistry.RegisterClassMap<DerivedPerson2>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("dp2");
                cm.MapMember(m => m.ExtraField2).SetSerializer(new CustomStringSerializer());
            });

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<BasePerson>(client);
            var untypedCollection = GetUntypedCollection(client);

            var bp1 = new DerivedPerson1 { Name = "Alice", Age = 30, ExtraField1 = "Field1" };
            var bp2 = new DerivedPerson2 { Name = "Bob", Age = 40, ExtraField2 = "Field2" };
            collection.InsertMany([bp1, bp2]);

            var retrieved1 = untypedCollection.FindSync(Builders<BsonDocument>.Filter.Eq("_id", bp1.Id)).ToList().Single();
            var retrieved2 = untypedCollection.FindSync(Builders<BsonDocument>.Filter.Eq("_id", bp2.Id)).ToList().Single();

            Assert.Contains("""_t" : ["bp", "dp1"]""", retrieved1.ToString());
            Assert.Contains("""_t" : ["bp", "dp2"]""", retrieved2.ToString());

            // Aggregate with OfType
            AssertDerivedPerson1(bp1, collection.Aggregate().OfType<DerivedPerson1>().Single());
            AssertDerivedPerson2(bp2, collection.Aggregate().OfType<DerivedPerson2>().Single());

            // AppendStage with OfType
            AssertDerivedPerson1(bp1, collection.AsQueryable().AppendStage(PipelineStageDefinitionBuilder.OfType<BasePerson, DerivedPerson1>()).Single());
            AssertDerivedPerson2(bp2, collection.AsQueryable().AppendStage(PipelineStageDefinitionBuilder.OfType<BasePerson, DerivedPerson2>()).Single());

            // LINQ with OfType
            AssertDerivedPerson1(bp1, collection.AsQueryable().OfType<DerivedPerson1>().Single());
            AssertDerivedPerson2(bp2, collection.AsQueryable().OfType<DerivedPerson2>().Single());

            // Facet with OfType
            var pipeline1 = PipelineDefinition<BasePerson, DerivedPerson1>.Create([PipelineStageDefinitionBuilder.OfType<BasePerson, DerivedPerson1>()]);
            var pipeline2 = PipelineDefinition<BasePerson, DerivedPerson2>.Create([PipelineStageDefinitionBuilder.OfType<BasePerson, DerivedPerson2>()]);
            var facets = collection.Aggregate().Facet(AggregateFacet.Create("facet1", pipeline1), AggregateFacet.Create("facet2", pipeline2)).Single().Facets;
            AssertDerivedPerson1(bp1, facets[0].Output<DerivedPerson1>().Single());
            AssertDerivedPerson2(bp2, facets[1].Output<DerivedPerson2>().Single());

            // Find with OfType
            AssertBasePerson(bp1, collection.FindSync(Builders<BasePerson>.Filter.OfType<DerivedPerson1>()).Single());
            AssertBasePerson(bp2, collection.FindSync(Builders<BasePerson>.Filter.OfType<DerivedPerson2>()).Single());
        }

        [Fact]
        public void GridFS_TFileId_serializer_is_resolved_through_bucket_domain_on_filter_render()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            customDomain.RegisterSerializer(new CustomStringSerializer());

            var client = CreateGridFSClientWithDomain(customDomain);
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var bucket = new GridFSBucket<string>(database);

            bucket.UploadFromBytes("abc", "hello.txt", Encoding.UTF8.GetBytes("hi"));

            // Filter is rendered through the bucket's domain; "abc" -> "abctest" via the custom serializer,
            // matching the stored _id. If the render used the default-domain registry, this would find nothing.
            var hits = bucket.Find(Builders<GridFSFileInfo<string>>.Filter.Eq(f => f.Id, "abc")).ToList();
            hits.Should().HaveCount(1);
            hits.Single().Filename.Should().Be("hello.txt");
        }

        [Fact]
        public void GridFS_TFileId_serializer_is_resolved_through_bucket_domain_on_upload()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            customDomain.RegisterSerializer(new CustomStringSerializer());

            var client = CreateGridFSClientWithDomain(customDomain);
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var bucket = new GridFSBucket<string>(database);

            bucket.UploadFromBytes("abc", "hello.txt", Encoding.UTF8.GetBytes("hi"));

            // Read fs.files raw to confirm the custom serializer ran on _id at upload time.
            // Reading as BsonDocument bypasses any custom string handling on the read side.
            var fileDoc = database.GetCollection<BsonDocument>("fs.files")
                .Find(FilterDefinition<BsonDocument>.Empty).Single();
            fileDoc["_id"].AsString.Should().Be("abctest");
        }

        [Fact]
        public void Linq_AsQueryable_where_filter_uses_custom_domain()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            customDomain.RegisterSerializer(new CustomStringSerializer());

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<Person>(client);
            var untypedCollection = GetUntypedCollection(client);

            var person = new Person { Id = _defaultId, Name = "Mario", Age = 24 };
            collection.InsertOne(person);

            var retrievedAsBson = untypedCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
            var expectedVal = $$"""{ "_id" : { "$oid" : "{{_defaultObjectIdString}}" }, "Name" : "Mariotest", "Age" : 24 }""";
            Assert.Equal(expectedVal, retrievedAsBson.ToString());

            // The string serializer correctly serializes "Mario" to "Mariotest" in the filter too.
            var retrievedTyped = collection.AsQueryable().Where(x => x.Name == "Mario").ToList();
            Assert.NotEmpty(retrievedTyped);
        }

        [Fact]
        public void Linq_OfType_executes_under_custom_domain()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

            customDomain.ClassMapRegistry.RegisterClassMap<BasePerson>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("bp");
                cm.SetIsRootClass(true);
            });
            customDomain.ClassMapRegistry.RegisterClassMap<DerivedPerson1>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("dp1");
            });

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<BasePerson>(client);
            var bp1 = new DerivedPerson1 { Name = "Alice", Age = 30, ExtraField1 = "F1" };
            collection.InsertOne(bp1);

            var ofTypeResult = collection.AsQueryable().OfType<DerivedPerson1>().Single();
            ofTypeResult.ExtraField1.Should().Be("F1");
        }

        [Fact]
        public void Linq_StandardDeviationPopulation_executes_under_custom_domain()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<NumericModel>(client);
            collection.InsertMany([
                new NumericModel { Id = ObjectId.GenerateNewId(), D = 1.0 },
                new NumericModel { Id = ObjectId.GenerateNewId(), D = 2.0 },
                new NumericModel { Id = ObjectId.GenerateNewId(), D = 3.0 }
            ]);

            var stdDev = collection.AsQueryable().StandardDeviationPopulation(m => m.D);
            stdDev.Should().BeApproximately(0.816, 0.01);
        }

        [Theory]
        [InlineData("test1", "Mariotest1")]
        [InlineData("test2", "Mariotest2")]
        public void Serialization_with_custom_domain_appends_suffix(string suffix, string expectedName)
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            customDomain.RegisterSerializer(new CustomStringSerializer(suffix));

            var client = CreateClientWithDomain(customDomain);
            var collection = GetTypedCollection<Person>(client);
            var bsonCollection = GetUntypedCollection(client);

            var person = new Person { Id = _defaultId, Name = "Mario", Age = 24 };
            collection.InsertOne(person);

            var retrievedAsBson = bsonCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
            var expectedVal = $$"""{ "_id" : { "$oid" : "{{_defaultObjectIdString}}" }, "Name" : "{{expectedName}}", "Age" : 24 }""";
            Assert.Equal(expectedVal, retrievedAsBson.ToString());

            var retrievedTyped = collection.FindSync(FilterDefinition<Person>.Empty).ToList().Single();
            Assert.Equal("Mario", retrievedTyped.Name);
        }

        [Fact]
        public void Serialization_with_default_domain_uses_default_serializers()
        {
            RequireServer.Check();

            var client = CreateClient();
            var collection = GetTypedCollection<Person>(client);
            var bsonCollection = GetUntypedCollection(client);

            var person = new Person { Id = _defaultId, Name = "Mario", Age = 24 };
            collection.InsertOne(person);

            var retrieved = bsonCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
            var expectedVal = $$"""{ "_id" : { "$oid" : "{{_defaultObjectIdString}}" }, "Name" : "Mario", "Age" : 24 }""";
            Assert.Equal(expectedVal, retrieved.ToString());
        }

        [Fact]
        public void Two_domains_serialize_and_deserialize_independently()
        {
            RequireServer.Check();

            var objectId1 = ObjectId.GenerateNewId();
            var objectId2 = ObjectId.GenerateNewId();

            var client = CreateClient();
            var collection = GetTypedCollection<Person>(client);
            var bsonCollection = GetUntypedCollection(client);

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            customDomain.RegisterSerializer(new CustomStringSerializer("test1"));
            var client2 = CreateClientWithDomain(customDomain);
            var collection2 = GetTypedCollection<Person>(client2);
            var bsonCollection2 = GetUntypedCollection(client2);

            collection.InsertOne(new Person { Id = objectId1, Name = "Mario", Age = 24 });
            collection2.InsertOne(new Person { Id = objectId2, Name = "Mario", Age = 24 });

            var retrieved = bsonCollection.FindSync(Builders<BsonDocument>.Filter.Eq("_id", objectId1)).ToList().Single();
            var expectedVal = $$"""{ "_id" : { "$oid" : "{{objectId1}}" }, "Name" : "Mario", "Age" : 24 }""";
            Assert.Equal(expectedVal, retrieved.ToString());

            var retrievedAsBson = bsonCollection2.FindSync(Builders<BsonDocument>.Filter.Eq("_id", objectId2)).ToList().Single();
            var expectedVal2 = $$"""{ "_id" : { "$oid" : "{{objectId2}}" }, "Name" : "Mariotest1", "Age" : 24 }""";
            Assert.Equal(expectedVal2, retrievedAsBson.ToString());

            var retrievedTyped = collection2.FindSync(p => p.Id == objectId2).ToList().Single();
            Assert.Equal("Mario", retrievedTyped.Name);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static IMongoCollection<T> GetTypedCollection<T>(IMongoClient client) =>
            client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .GetCollection<T>(DriverTestConfiguration.CollectionNamespace.CollectionName);

        private static IMongoCollection<BsonDocument> GetUntypedCollection(IMongoClient client) =>
            client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

        private static IMongoClient CreateClientWithDomain(IBsonSerializationDomain domain, bool dropCollection = true)
        {
            var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = domain);
            if (dropCollection)
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
            }
            return client;
        }

        private static IMongoClient CreateClient()
        {
            var client = DriverTestConfiguration.CreateMongoClient();
            client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
            return client;
        }

        private static IMongoClient CreateGridFSClientWithDomain(IBsonSerializationDomain domain)
        {
            var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = domain);
            var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            db.DropCollection("fs.files");
            db.DropCollection("fs.chunks");
            return client;
        }

        // ── Assertion helpers ─────────────────────────────────────────────────

        private static void AssertBasePerson(BasePerson expected, BasePerson retrieved)
        {
            Assert.Equal(expected.Id, retrieved.Id);
            Assert.Equal(expected.Name, retrieved.Name);
            Assert.Equal(expected.Age, retrieved.Age);
        }

        private static void AssertDerivedPerson1(DerivedPerson1 expected, DerivedPerson1 retrieved)
        {
            AssertBasePerson(expected, retrieved);
            Assert.Equal(expected.ExtraField1, retrieved.ExtraField1);
        }

        private static void AssertDerivedPerson2(DerivedPerson2 expected, DerivedPerson2 retrieved)
        {
            AssertBasePerson(expected, retrieved);
            Assert.Equal(expected.ExtraField2, retrieved.ExtraField2);
        }

        private static void AssertBasePersonAttribute(BasePersonAttribute expected, BasePersonAttribute retrieved)
        {
            Assert.Equal(expected.Id, retrieved.Id);
            Assert.Equal(expected.Name, retrieved.Name);
            Assert.Equal(expected.Age, retrieved.Age);
        }

        private static void AssertDerivedPersonAttribute1(DerivedPersonAttribute1 expected, DerivedPersonAttribute1 retrieved)
        {
            AssertBasePersonAttribute(expected, retrieved);
            Assert.Equal(expected.ExtraField1, retrieved.ExtraField1);
        }

        private static void AssertDerivedPersonAttribute2(DerivedPersonAttribute2 expected, DerivedPersonAttribute2 retrieved)
        {
            AssertBasePersonAttribute(expected, retrieved);
            Assert.Equal(expected.ExtraField2, retrieved.ExtraField2);
        }
    }
}

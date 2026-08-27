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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using Xunit;
using static MongoDB.Driver.Tests.MultipleRegistriesTestHelpers;

namespace MongoDB.Driver.Tests;

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
        retrievedAsBson.ToString().Should().Be(expectedVal);
    }

    [Fact]
    public void Deserialization_with_custom_domain_strips_suffix()
    {
        RequireServer.Check();

        // Insert with default domain — Name stored as-is.
        {
            var client = CreateDefaultClient();
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
            retrievedTyped.Name.Should().Be("Mario");
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

        retrieved1.ToString().Should().Contain("""_t" : ["bp", "dp1"]""");
        retrieved2.ToString().Should().Contain("""_t" : ["bp", "dp2"]""");

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

        retrieved1.ToString().Should().Contain("""_t" : ["bp", "dp1"]""");
        retrieved2.ToString().Should().Contain("""_t" : ["bp", "dp2"]""");

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
        retrievedAsBson.ToString().Should().Be(expectedVal);

        // The string serializer correctly serializes "Mario" to "Mariotest" in the filter too,
        // and on the read path the deserializer strips the suffix back to "Mario".
        var retrievedTyped = collection.AsQueryable().Where(x => x.Name == "Mario").ToList();
        retrievedTyped.Should().ContainSingle();
        retrievedTyped.Single().Name.Should().Be("Mario");
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

        // Contrast: same query against a default-domain client misses the doc — the "dp1" discriminator
        // was registered only on the custom domain, so the default domain's OfType filter looks for
        // the auto-mapped discriminator "DerivedPerson1" instead and finds nothing.
        var defaultClient = DriverTestConfiguration.CreateMongoClient();
        var defaultCollection = GetTypedCollection<BasePerson>(defaultClient);
        defaultCollection.AsQueryable().OfType<DerivedPerson1>().ToList().Should().BeEmpty();
    }

    [Fact]
    public void Linq_StandardDeviationPopulation_executes_under_custom_domain()
    {
        RequireServer.Check();

        // Register a renaming convention only on the custom domain. If both write and LINQ paths
        // consult this domain, docs land with field "d" (lowercase) and StdDev finds them.
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var pack = new ConventionPack(customDomain);
        pack.AddMemberMapConvention("LowerCaseElementName", m => m.SetElementName(m.MemberName.ToLower()));
        customDomain.ConventionRegistry.Register("lowercase", pack, t => t == typeof(NumericModel));

        var client = CreateClientWithDomain(customDomain);
        var collection = GetTypedCollection<NumericModel>(client);
        collection.InsertMany([
            new NumericModel { Id = ObjectId.GenerateNewId(), D = 1.0 },
            new NumericModel { Id = ObjectId.GenerateNewId(), D = 2.0 },
            new NumericModel { Id = ObjectId.GenerateNewId(), D = 3.0 }
        ]);

        // Raw read confirms the custom domain's convention was on the write path.
        var rawDoc = GetUntypedCollection(client).FindSync(FilterDefinition<BsonDocument>.Empty).ToList().First();
        rawDoc.Contains("d").Should().BeTrue();
        rawDoc.Contains("D").Should().BeFalse();

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
        retrievedAsBson.ToString().Should().Be(expectedVal);

        var retrievedTyped = collection.FindSync(FilterDefinition<Person>.Empty).ToList().Single();
        retrievedTyped.Name.Should().Be("Mario");
    }

    [Fact]
    public void Serialization_with_default_domain_uses_default_serializers()
    {
        RequireServer.Check();

        var client = CreateDefaultClient();
        var collection = GetTypedCollection<Person>(client);
        var bsonCollection = GetUntypedCollection(client);

        var person = new Person { Id = _defaultId, Name = "Mario", Age = 24 };
        collection.InsertOne(person);

        var retrieved = bsonCollection.FindSync(FilterDefinition<BsonDocument>.Empty).ToList().Single();
        var expectedVal = $$"""{ "_id" : { "$oid" : "{{_defaultObjectIdString}}" }, "Name" : "Mario", "Age" : 24 }""";
        retrieved.ToString().Should().Be(expectedVal);
    }

    [Fact]
    public void Two_domains_serialize_and_deserialize_independently()
    {
        RequireServer.Check();

        var objectId1 = ObjectId.GenerateNewId();
        var objectId2 = ObjectId.GenerateNewId();

        var client = CreateDefaultClient();
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
        retrieved.ToString().Should().Be(expectedVal);

        var retrievedAsBson = bsonCollection2.FindSync(Builders<BsonDocument>.Filter.Eq("_id", objectId2)).ToList().Single();
        var expectedVal2 = $$"""{ "_id" : { "$oid" : "{{objectId2}}" }, "Name" : "Mariotest1", "Age" : 24 }""";
        retrievedAsBson.ToString().Should().Be(expectedVal2);

        var retrievedTyped = collection2.FindSync(p => p.Id == objectId2).ToList().Single();
        retrievedTyped.Name.Should().Be("Mario");
    }

    // Helpers

    private static IMongoCollection<BsonDocument> GetUntypedCollection(IMongoClient client) =>
        client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
            .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

    private static IMongoClient CreateDefaultClient(bool dropCollection = true)
    {
        var client = DriverTestConfiguration.CreateMongoClient();
        if (dropCollection)
        {
            client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
        }
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

    private static void AssertBasePerson(BasePerson expected, BasePerson retrieved)
    {
        retrieved.Id.Should().Be(expected.Id);
        retrieved.Name.Should().Be(expected.Name);
        retrieved.Age.Should().Be(expected.Age);
    }

    private static void AssertDerivedPerson1(DerivedPerson1 expected, DerivedPerson1 retrieved)
    {
        AssertBasePerson(expected, retrieved);
        retrieved.ExtraField1.Should().Be(expected.ExtraField1);
    }

    private static void AssertDerivedPerson2(DerivedPerson2 expected, DerivedPerson2 retrieved)
    {
        AssertBasePerson(expected, retrieved);
        retrieved.ExtraField2.Should().Be(expected.ExtraField2);
    }

    private static void AssertBasePersonAttribute(BasePersonAttribute expected, BasePersonAttribute retrieved)
    {
        retrieved.Id.Should().Be(expected.Id);
        retrieved.Name.Should().Be(expected.Name);
        retrieved.Age.Should().Be(expected.Age);
    }

    private static void AssertDerivedPersonAttribute1(DerivedPersonAttribute1 expected, DerivedPersonAttribute1 retrieved)
    {
        AssertBasePersonAttribute(expected, retrieved);
        retrieved.ExtraField1.Should().Be(expected.ExtraField1);
    }

    private static void AssertDerivedPersonAttribute2(DerivedPersonAttribute2 expected, DerivedPersonAttribute2 retrieved)
    {
        AssertBasePersonAttribute(expected, retrieved);
        retrieved.ExtraField2.Should().Be(expected.ExtraField2);
    }
}

// Discriminated hierarchy (class-map registration) — derived types are integration-only.
internal class DerivedPerson1 : BasePerson
{
    public string ExtraField1 { get; set; }
}

internal class DerivedPerson2 : BasePerson
{
    public string ExtraField2 { get; set; }
}

// Discriminated hierarchy (attribute-based) — integration-only.
[BsonDiscriminator("bp", RootClass = true)]
internal class BasePersonAttribute
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public int Age { get; set; }
}

[BsonDiscriminator("dp1")]
internal class DerivedPersonAttribute1 : BasePersonAttribute
{
    public string ExtraField1 { get; set; }
}

[BsonDiscriminator("dp2")]
internal class DerivedPersonAttribute2 : BasePersonAttribute
{
    public string ExtraField2 { get; set; }
}

internal class CustomObjectIdGenerator : IIdGenerator
{
    public object GenerateId(object container, object document)
        => ObjectId.Parse("6797b56bf5495bf53aa3078f");

    public bool IsEmpty(object id) => true;
}

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

using System;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Xunit;
using static MongoDB.Driver.Tests.MultipleRegistriesTestHelpers;

namespace MongoDB.Driver.Tests;

[Trait("Category", "Integration")]
public class MultipleRegistriesLinqTests
{
    [Fact]
    public void Linq_Concat_across_different_MongoClients_throws()
    {
        var outerDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("outer");
        var innerDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("inner");

        var databaseName = DriverTestConfiguration.DatabaseNamespace.DatabaseName;
        var outerColl = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = outerDomain)
            .GetDatabase(databaseName).GetCollection<Person>("concat_outer");
        var innerColl = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = innerDomain)
            .GetDatabase(databaseName).GetCollection<Person>("concat_inner");

        var queryable = outerColl.AsQueryable().Concat(innerColl.AsQueryable());

        var ex = Assert.Throws<ExpressionNotSupportedException>(() => Linq3TestHelpers.Translate<Person, Person>(queryable));
        ex.Message.Should().EndWith("because Concat is not supported on queryables from different MongoClients (current domain 'outer', other queryable's domain 'inner').");
    }

    [Fact]
    public void Linq_Inject_translates_filter_under_outer_domain()
    {
        RequireServer.Check();

        // Custom domain's CustomStringSerializer rewrites strings with a "test" suffix; the filter value
        // must be serialized through the domain's registry when the Inject path is translated.
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        customDomain.RegisterSerializer(new CustomStringSerializer());

        var client = CreateClientWithDomain(customDomain, dropCollection: false);
        var collection = GetTypedCollection<Person>(client);

        var injectedFilter = Builders<Person>.Filter.Eq(p => p.Name, "Mario");
        var queryable = collection.AsQueryable().Where(p => injectedFilter.Inject());

        var stages = Linq3TestHelpers.Translate<Person, Person>(queryable);
        var match = stages.Single(s => s.Contains("$match"));
        match.ToJson().Should().Contain("Mariotest");
    }

    [Fact]
    public void Linq_math_methods_translate_under_custom_domain()
    {
        RequireServer.Check();

        // Register a renaming convention only on the custom domain so the rendered $project's
        // field reference proves the Ceiling/Floor translators consulted this domain's class map.
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        RegisterLowerCaseElementNameConvention(customDomain, typeof(NumericModel));

        var client = CreateClientWithDomain(customDomain, dropCollection: false);
        var collection = GetTypedCollection<NumericModel>(client);

        var queryable = collection
            .AsQueryable()
            .Select(m => new { Ceil = System.Math.Ceiling(m.D), Flr = System.Math.Floor(m.D) });

        var stages = Linq3TestHelpers.Translate<NumericModel, object>(queryable);
        var project = stages.Single(s => s.Contains("$project"));
        var projectJson = project.ToJson();
        projectJson.Should().Contain("$ceil");
        projectJson.Should().Contain("$floor");
        // Lowercase 'd' (not 'D') only appears if NumericModel's class map was looked up via the custom domain.
        projectJson.Should().Contain("$d");
        projectJson.Should().NotContain("$D");
    }

    [Fact]
    public void Linq_Pick_with_SortBy_runs_under_custom_domain()
    {
        RequireServer.Check();

        // Register a renaming convention only on the custom domain so the rendered sortBy's
        // field name proves Pick's SortDefinition render path consulted this domain.
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        RegisterLowerCaseElementNameConvention(customDomain, typeof(Person));

        var client = CreateClientWithDomain(customDomain, dropCollection: false);
        var collection = GetTypedCollection<Person>(client);

        var queryable = collection
            .AsQueryable()
            .GroupBy(p => p.Name)
            .Select(g => new { Name = g.Key, YoungestAge = g.BottomN(Builders<Person>.Sort.Descending(p => p.Age), p => p.Age, 1) });

        var stages = Linq3TestHelpers.Translate<Person, object>(queryable);
        var group = stages.Single(s => s.Contains("$group"));
        var groupJson = group.ToJson();
        groupJson.Should().Contain("$bottomN");
        groupJson.Should().Contain("sortBy");
        // Lowercase 'age' (not 'Age') only appears if Person's class map was looked up via the custom domain.
        groupJson.Should().Contain("\"age\" : -1");
        groupJson.Should().NotContain("\"Age\"");
    }

    [Fact]
    public void Mql_Constant_translates_under_custom_domain()
    {
        RequireServer.Check();

        // Register a renaming convention only on the custom domain so the rendered $project's
        // field reference proves Mql.Constant's translator consulted this domain's class map.
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        RegisterLowerCaseElementNameConvention(customDomain, typeof(EnumModel));

        var client = CreateClientWithDomain(customDomain, dropCollection: false);
        var collection = GetTypedCollection<EnumModel>(client);

        var queryable = collection
            .AsQueryable()
            .Select(d => new { d.Id, V = d.ApprovalState == ApprovalState.Active ? Mql.Constant(ApprovalState.Active, BsonType.String) : ApprovalState.Inactive });

        var stages = Linq3TestHelpers.Translate<EnumModel, object>(queryable);
        var project = stages.Single(s => s.Contains("$project"));
        var projectJson = project.ToJson();
        projectJson.Should().Contain("Active");
        // Lowercase '$approvalstate' (not '$ApprovalState') only appears if EnumModel's class map
        // was looked up via the custom domain.
        projectJson.Should().Contain("$approvalstate");
        projectJson.Should().NotContain("$ApprovalState");
    }

    [Fact]
    public void Linq_Where_serializes_constant_through_custom_domain_serializer()
    {
        RequireServer.Check();

        // CustomStringSerializer appends a "test" suffix; the Where constant must be serialized through
        // the domain's registry (the SerializerFinderVisitConstant path), not the global Default.
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        customDomain.RegisterSerializer(new CustomStringSerializer());

        var client = CreateClientWithDomain(customDomain, dropCollection: false);
        var collection = GetTypedCollection<Person>(client);

        var queryable = collection.AsQueryable().Where(p => p.Name == "Mario");

        var stages = Linq3TestHelpers.Translate<Person, Person>(queryable);
        var match = stages.Single(s => s.Contains("$match"));
        match.ToJson().Should().Contain("Mariotest");
    }

    [Fact]
    public void Linq_GroupBy_resolves_key_field_name_under_custom_domain()
    {
        RequireServer.Check();

        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        RegisterLowerCaseElementNameConvention(customDomain, typeof(Person));

        var client = CreateClientWithDomain(customDomain, dropCollection: false);
        var collection = GetTypedCollection<Person>(client);

        var queryable = collection.AsQueryable().GroupBy(p => p.Name).Select(g => new { g.Key, Count = g.Count() });

        var stages = Linq3TestHelpers.Translate<Person, object>(queryable);
        var group = stages.Single(s => s.Contains("$group"));
        var groupJson = group.ToJson();
        // Lowercase '$name' only appears if Person's class map was looked up via the custom domain.
        groupJson.Should().Contain("$name");
        groupJson.Should().NotContain("$Name");
    }

    [Fact]
    public void Linq_OrderBy_resolves_field_name_under_custom_domain()
    {
        RequireServer.Check();

        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        RegisterLowerCaseElementNameConvention(customDomain, typeof(Person));

        var client = CreateClientWithDomain(customDomain, dropCollection: false);
        var collection = GetTypedCollection<Person>(client);

        var queryable = collection.AsQueryable().OrderBy(p => p.Age);

        var stages = Linq3TestHelpers.Translate<Person, Person>(queryable);
        var sort = stages.Single(s => s.Contains("$sort"));
        var sortJson = sort.ToJson();
        // Lowercase 'age' only appears if Person's class map was looked up via the custom domain.
        sortJson.Should().Contain("age");
        sortJson.Should().NotContain("Age");
    }

    [Fact]
    public void Linq_nested_member_resolves_field_name_under_custom_domain()
    {
        RequireServer.Check();

        // Lowercase both levels so a lowercased nested path proves both class maps were looked up via the custom domain.
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var pack = new ConventionPack(customDomain);
        pack.AddMemberMapConvention("LowerCaseElementName", m => m.SetElementName(m.MemberName.ToLower()));
        customDomain.ConventionRegistry.Register("lowercase", pack, t => t == typeof(Order) || t == typeof(Customer));

        var client = CreateClientWithDomain(customDomain, dropCollection: false);
        var collection = GetTypedCollection<Order>(client);

        var queryable = collection.AsQueryable().Where(o => o.Customer.Name == "Mario");

        var stages = Linq3TestHelpers.Translate<Order, Order>(queryable);
        var match = stages.Single(s => s.Contains("$match"));
        var matchJson = match.ToJson();
        matchJson.Should().Contain("customer.name");
        matchJson.Should().NotContain("Customer");
    }

    [Fact]
    public void Linq_OfType_resolves_discriminator_under_custom_domain()
    {
        RequireServer.Check();

        // A custom '_type' discriminator (not the default '_t') proves OfType consulted the domain's convention.
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        customDomain.RegisterDiscriminatorConvention(typeof(Shape), new ScalarDiscriminatorConvention(customDomain, "_type"));
        customDomain.RegisterDiscriminator(typeof(Circle), "Circle");

        var client = CreateClientWithDomain(customDomain, dropCollection: false);
        var collection = GetTypedCollection<Shape>(client);

        var queryable = collection.AsQueryable().OfType<Circle>();

        var stages = Linq3TestHelpers.Translate<Shape, Circle>(queryable);
        var match = stages.Single(s => s.Contains("$match"));
        var matchJson = match.ToJson();
        matchJson.Should().Contain("_type");
        matchJson.Should().Contain("Circle");
        matchJson.Should().NotContain("\"_t\"");
    }

    private static void RegisterLowerCaseElementNameConvention(IBsonSerializationDomain domain, Type targetType)
    {
        var pack = new ConventionPack(domain);
        pack.AddMemberMapConvention("LowerCaseElementName", m => m.SetElementName(m.MemberName.ToLower()));
        domain.ConventionRegistry.Register("lowercase", pack, t => t == targetType);
    }
}

internal enum ApprovalState { Active = 1, Inactive = 2 }

internal class EnumModel
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonRepresentation(BsonType.String)]
    public ApprovalState ApprovalState { get; set; }
}

internal class Order
{
    [BsonId] public ObjectId Id { get; set; }
    public Customer Customer { get; set; }
}

internal class Customer
{
    public string Name { get; set; }
}

internal class Shape
{
    [BsonId] public ObjectId Id { get; set; }
}

internal class Circle : Shape
{
    public double Radius { get; set; }
}

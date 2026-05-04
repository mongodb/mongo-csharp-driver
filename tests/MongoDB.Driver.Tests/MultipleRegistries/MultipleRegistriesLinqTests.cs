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
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Xunit;

namespace MongoDB.Driver.Tests
{
    [Trait("Category", "Integration")]
    public class MultipleRegistriesLinqTests
    {
        [Fact]
        public void TestLinqConcatAcrossDifferentMongoClientsThrows()
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
            ex.Message.Should().Contain("different MongoClients");
        }

        [Fact]
        public void TestLinqInjectTranslatesFilterUnderOuterDomain()
        {
            RequireServer.Check();

            // Custom domain's CustomStringSerializer rewrites strings with a "test" suffix; the filter value
            // must be serialized through the domain's registry when the Inject path is translated.
            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
            customDomain.RegisterSerializer(new CustomStringSerializer());

            var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = customDomain);
            var collection = client
                .GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .GetCollection<Person>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            var injectedFilter = Builders<Person>.Filter.Eq(p => p.Name, "Mario");
            var queryable = collection.AsQueryable().Where(p => injectedFilter.Inject());

            var stages = Linq3TestHelpers.Translate<Person, Person>(queryable);
            var match = stages.Single(s => s.Contains("$match"));
            match.ToJson().Should().Contain("Mariotest");
        }

        [Fact]
        public void TestLinqPickWithSortByRunsUnderCustomDomain()
        {
            RequireServer.Check();

            // Smoke test: Pick's SortDefinition render path now receives the custom SerializationDomain.
            // The assertion is that translation completes without reaching the global static registry.
            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

            var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = customDomain);
            var collection = client
                .GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .GetCollection<Person>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            var queryable = collection
                .AsQueryable()
                .GroupBy(p => p.Name)
                .Select(g => new { Name = g.Key, YoungestAge = g.BottomN(Builders<Person>.Sort.Descending(p => p.Age), p => p.Age, 1) });

            var stages = Linq3TestHelpers.Translate<Person, object>(queryable);
            var group = stages.Single(s => s.Contains("$group"));
            group.ToJson().Should().Contain("$bottomN");
            group.ToJson().Should().Contain("sortBy");
        }

        [Fact]
        public void TestLinqMathMethodsUnderCustomDomain()
        {
            RequireServer.Check();

            // Smoke test: aggregation-expression translators that previously called BsonSerializer.LookupSerializer
            // (Ceiling, Floor) now route through context.SerializationDomain.
            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

            var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = customDomain);
            var collection = client
                .GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .GetCollection<NumericModel>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            var queryable = collection
                .AsQueryable()
                .Select(m => new { Ceil = System.Math.Ceiling(m.D), Flr = System.Math.Floor(m.D) });

            var stages = Linq3TestHelpers.Translate<NumericModel, object>(queryable);
            var project = stages.Single(s => s.Contains("$project"));
            project.ToJson().Should().Contain("$ceil");
            project.ToJson().Should().Contain("$floor");
        }

        [Fact]
        public void TestMqlConstantUnderCustomDomain()
        {
            RequireServer.Check();

            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

            var client = DriverTestConfiguration.CreateMongoClient(c => c.SerializationDomain = customDomain);
            var collection = client
                .GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .GetCollection<EnumModel>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            var queryable = collection
                .AsQueryable()
                .Select(d => new { d.Id, V = d.ApprovalState == ApprovalState.Active ? Mql.Constant(ApprovalState.Active, BsonType.String) : ApprovalState.Inactive });

            var stages = Linq3TestHelpers.Translate<EnumModel, object>(queryable);
            var project = stages.Single(s => s.Contains("$project"));
            project.ToJson().Should().Contain("Active");
        }
    }
}

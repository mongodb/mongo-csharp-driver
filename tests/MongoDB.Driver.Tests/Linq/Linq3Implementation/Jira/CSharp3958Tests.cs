﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3958Tests : LinqIntegrationTest<CSharp3958Tests.ClassFixture>
    {
        public CSharp3958Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Sort_on_a_field_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var queryable = collection
                .AsQueryable()
                .Select(x => new { Result = x.Team.OrderBy(m => m.Name) });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Result : { $sortArray : { input : '$Team', sortBy : { Name : 1 } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Result.Select(m => m.Name).Should().Equal("Charlie", "Dallas", "Pat");
        }

        [Fact]
        public void Sort_on_a_subfield_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var queryable = collection
                .AsQueryable()
                .Select(x => new { Result = x.Team.OrderByDescending(m => m.Address.City) });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Result : { $sortArray : { input : '$Team', sortBy : { 'Address.City' : -1 } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Result.Select(m => m.Address.City).Should().Equal("Palo Alto", "New Brunswick", "London");
        }

        [Fact]
        public void Sort_on_multiple_fields_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var queryable = collection
                .AsQueryable()
                .Select(x => new { Result = x.Team.OrderByDescending(m => m.Age).ThenBy(m => m.Name) });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Result : { $sortArray : { input : '$Team', sortBy : { 'Age' : -1, Name : 1 } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Result.Select(m => m.Age).Should().Equal(42, 30, 30);
            result.Result.Select(m => m.Name).Should().Equal("Pat", "Charlie", "Dallas");
        }

        [Fact]
        public void Sort_an_array_of_integers_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var queryable = collection
                .AsQueryable()
                .Select(x => new { Result = new[] { 1, 4, 1, 6, 12, 5 }.OrderBy(v => v) });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Result : { $sortArray : { input : [1, 4, 1, 6, 12, 5] sortBy : 1 } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Result.Should().Equal(1, 1, 4, 5, 6, 12);
        }

        [Fact]
        public void Sort_on_mixed_type_fields_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var queryable = collection
                .AsQueryable()
                .Select(
                    x => new
                    {
                        Result = new BsonValue[] {
                            20,
                            4,
                            new BsonDocument("a", "Free"),
                            6,
                            21,
                            5,
                            "Gratis",
                            new BsonDocument("a", BsonNull.Value),
                            new BsonDocument("a", new BsonDocument { { "sale", true }, { "price", 19 } }),
                            10.23M,
                            new BsonDocument("a", "On sale")
                        }
                        .OrderBy(v => v)
                    });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Result : { $sortArray : { input : [20, 4, { a : 'Free' }, 6, 21, 5, 'Gratis', { a : null }, { a : { sale : true, price : 19 } }, NumberDecimal('10.23'), { a : 'On sale' }] sortBy : 1 } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Result.Should().Equal(
                4,
                5,
                6,
                10.23M,
                20,
                21,
                "Gratis",
                new BsonDocument("a", BsonNull.Value),
                new BsonDocument("a", "Free"),
                new BsonDocument("a", "On sale"),
                new BsonDocument("a", new BsonDocument { { "sale", true }, { "price", 19 } }));
        }

        [Theory]
        [ParameterAttributeData]
        public void OrderBy_on_entire_object_followed_by_ThenBy_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection
                .AsQueryable(translationOptions)
                .Select(x => new { Result = x.Team.OrderBy(m => m).ThenBy(m => m.Name) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, "{ $project : { _snippets : ['$Team'], _id : 0 } }");
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var result = queryable.Single();
                result.Result.Select(m => m.Age).Should().Equal(30, 30, 42);
                result.Result.Select(m => m.Name).Should().Equal("Charlie", "Dallas", "Pat");
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection,queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("ThenBy and ThenByDescending cannot be used when OrderBy or OrderByDescending is sorting on the entire object");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void OrderByDescending_on_entire_object_followed_by_ThenBy_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection
                .AsQueryable(translationOptions)
                .Select(x => new { Result = x.Team.OrderBy(m => m).ThenBy(m => m.Name) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, "{ $project : { _snippets : ['$Team'], _id : 0 } }");
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var result = queryable.Single();
                result.Result.Select(m => m.Age).Should().Equal(30, 30, 42);
                result.Result.Select(m => m.Name).Should().Equal("Charlie", "Dallas", "Pat");
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("ThenBy and ThenByDescending cannot be used when OrderBy or OrderByDescending is sorting on the entire object");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ThenBy_on_entire_object_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection
                .AsQueryable(translationOptions)
                .Select(x => new { Result = x.Team.OrderBy(m => m.Name).ThenBy(m => m) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, "{ $project : { _snippets : ['$Team'], _id : 0 } }");
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var result = queryable.Single();
                result.Result.Select(tm => tm.Name).Should().Equal("Charlie", "Dallas", "Pat");
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("ThenBy and ThenByDescending cannot be used to sort on the entire object");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void ThenByDescending_on_entire_object_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection
                .AsQueryable(translationOptions)
                .Select(x => new { Result = x.Team.OrderBy(m => m.Name).ThenByDescending(m => m) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, "{ $project : { _snippets : ['$Team'], _id : 0 } }");
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var result = queryable.Single();
                result.Result.Select(tm => tm.Name).Should().Equal("Charlie", "Dallas", "Pat");
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("ThenBy and ThenByDescending cannot be used to sort on the entire object");
            }
        }

        [Fact]
        public void Client_side_ThenBy_should_throw()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var queryable = collection
                .AsQueryable()
                .Select(x => new { Result = x.Team.OrderBy(m => m.Name) });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Result : { $sortArray : { input : '$Team', sortBy : { Name : 1 } } }, _id : 0 } }");

            var exception = Record.Exception(() => queryable.ToList()[0].Result.ThenBy(x => x.Name));
            var invalidOperationException = exception.Should().BeOfType<InvalidOperationException>().Subject;
            invalidOperationException.Message.Should().Be("ThenBy or ThenByDescending cannot be executed client-side and should be moved to the LINQ query.");
        }

        [Fact]
        public void Client_side_ThenByDescending_should_throw()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var queryable = collection
                .AsQueryable()
                .Select(x => new { Result = x.Team.OrderBy(m => m.Name) });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Result : { $sortArray : { input : '$Team', sortBy : { Name : 1 } } }, _id : 0 } }");

            var exception = Record.Exception(() => queryable.ToList()[0].Result.ThenBy(x => x.Name));
            var invalidOperationException = exception.Should().BeOfType<InvalidOperationException>().Subject;
            invalidOperationException.Message.Should().Be("ThenBy or ThenByDescending cannot be executed client-side and should be moved to the LINQ query.");
        }

        [Fact]
        public void IOrderedEnumerableSerializer_Serialize_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = Fixture.Collection;
            var queryable = collection
                .AsQueryable()
                .Select(x => new { Result = x.Team.OrderBy(m => m.Name) });

            var result = queryable.Single();

            var json = result.ToJson();
            json.Should().Be("{ \"Result\" : [{ \"Name\" : \"Charlie\", \"Age\" : 30, \"Address\" : { \"Street\" : \"12 French St\", \"City\" : \"New Brunswick\" } }, { \"Name\" : \"Dallas\", \"Age\" : 30, \"Address\" : { \"Street\" : \"12 Cowper St\", \"City\" : \"Palo Alto\" } }, { \"Name\" : \"Pat\", \"Age\" : 42, \"Address\" : { \"Street\" : \"12 Baker St\", \"City\" : \"London\" } }] }");
        }

        public class Engineers
        {
            public int Id { get; set; }
            public TeamMember[] Team { get; set; }
        }

        public class TeamMember : IComparable<TeamMember>
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public Address Address { get; set; }

            public int CompareTo(TeamMember other) => Age.CompareTo(other.Age);
        }

        public class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Engineers>
        {
            protected override IEnumerable<Engineers> InitialData =>
            [
                new Engineers
                {
                    Id = 1,
                    Team = new[]
                    {
                        new TeamMember { Name = "Pat", Age = 42, Address = new Address { Street = "12 Baker St", City = "London"}},
                        new TeamMember { Name = "Dallas", Age = 30, Address = new Address { Street = "12 Cowper St", City = "Palo Alto"}},
                        new TeamMember { Name = "Charlie", Age = 30, Address = new Address { Street = "12 French St", City = "New Brunswick"}}
                    }
                }
            ];
        }
    }
}

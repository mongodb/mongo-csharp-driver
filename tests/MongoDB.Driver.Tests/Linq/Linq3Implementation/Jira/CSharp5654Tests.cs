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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5654Tests : LinqIntegrationTest<CSharp5654Tests.ClassFixture>
{
    public CSharp5654Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void IndexOf_equal_to_minus_one()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(c => c.City.IndexOf("e") == -1);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $indexOfCP : ['$City', 'e'] }, -1] } } }");

        var results = queryable.ToList();
        results.Select(c => c.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void IndexOf_not_equal_to_minus_one()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(c => c.City.IndexOf("e") != -1);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $ne : [{ $indexOfCP : ['$City', 'e'] }, -1] } } }");

        var results = queryable.ToList();
        results.Select(c => c.Id).Should().Equal(3, 4);
    }

    [Fact]
    public void IndexOf_less_than_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(c => c.City.IndexOf("e") < 0);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $lt : [{ $indexOfCP : ['$City', 'e'] }, 0] } } }");

        var results = queryable.ToList();
        results.Select(c => c.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void IndexOf_greater_than_or_equal_to_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(c => c.City.IndexOf("e") >= 0);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $gte : [{ $indexOfCP : ['$City', 'e'] }, 0] } } }");

        var results = queryable.ToList();
        results.Select(c => c.Id).Should().Equal(3, 4);
    }

    public class Customer
    {
        public int Id { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<Customer>
    {
        protected override IEnumerable<Customer> InitialData =>
        [
            new() { Id = 1, City = "London", Country = "UK" },
            new() { Id = 2, City = "London", Country = "USA" },
            new() { Id = 3, City = "Seattle", Country = "UK" },
            new() { Id = 4, City = "Seattle", Country = "USA" }
        ];
    }
}

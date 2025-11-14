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

using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp4593Tests : LinqIntegrationTest<CSharp4593Tests.ClassFixture>
{
    public CSharp4593Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void First_example_should_work()
    {
        var collection = Fixture.Orders;

        var find = collection
            .Find(o => o.RateBasisHistoryId == "abc")
            .Project(r => r.Id);

        var translatedFilter = TranslateFindFilter(collection, find);
        translatedFilter.Should().Be("{ RateBasisHistoryId : 'abc' }");

        var translatedProjection = TranslateFindProjection(collection, find);
        translatedProjection.Should().Be("{ _id : 1 }");

        var result = find.Single();
        result.Should().Be("a");
    }

    [Fact]
    public void First_example_workaround_should_work()
    {
        var collection = Fixture.Orders;

        var find = collection
            .Find(o => o.RateBasisHistoryId == "abc")
            .Project(Builders<Order>.Projection.Include(o => o.Id));

        var translatedFilter = TranslateFindFilter(collection, find);
        translatedFilter.Should().Be("{ RateBasisHistoryId : 'abc' }");

        var translatedProjection = TranslateFindProjection(collection, find);
        translatedProjection.Should().Be("{ _id : 1 }");

        var result = find.Single();
        result["_id"].AsString.Should().Be("a");
    }

    [Fact]
    public void Second_example_should_work()
    {
        var collection = Fixture.Entities;
        var idsFilter = Builders<Entity>.Filter.Eq(x => x.Id, 1);

        var aggregate = collection.Aggregate()
            .Match(idsFilter)
            .Project(e => new
            {
                _id = e.Id,
                CampaignId = e.CampaignId,
                Accepted = e.Status.Key == "Accepted" ? 1 : 0,
                Rejected = e.Status.Key == "Rejected" ? 1 : 0,
            });

        var stages = Translate(collection, aggregate);
        AssertStages(
            stages,
            "{ $match : { _id : 1 } }",
            """
            { $project :
                {
                    _id : "$_id",
                    CampaignId : "$CampaignId",
                    Accepted : { $cond : { if : { $eq : ["$Status.Key", "Accepted"] }, then : 1, else : 0 } },
                    Rejected : { $cond : { if : { $eq : ["$Status.Key", "Rejected"] }, then : 1, else : 0 } }
                }
            }
            """);

        var results = aggregate.ToList();
        results.Count.Should().Be(1);
        results[0]._id.Should().Be(1);
        results[0].CampaignId.Should().Be(11);
        results[0].Accepted.Should().Be(1);
        results[0].Rejected.Should().Be(0);
    }

    public class Order
    {
        public string Id { get; set; }
        public string RateBasisHistoryId { get; set; }
    }

    public class Entity
    {
        public int Id { get; set; }
        public int CampaignId { get; set; }
        public Status Status { get; set; }
    }

    public class Status
    {
        public string Key { get; set; }
    }

    public sealed class ClassFixture : MongoDatabaseFixture
    {
        public IMongoCollection<Order> Orders { get; private set; }
        public IMongoCollection<Entity> Entities { get; private set; }

        protected override void InitializeFixture()
        {
            Orders = CreateCollection<Order>("orders");
            Orders.InsertMany(
            [
                    new Order { Id = "a", RateBasisHistoryId = "abc" }
                ]);

            Entities = CreateCollection<Entity>("entities");
            Entities.InsertMany(
            [
                new Entity { Id = 1, CampaignId = 11, Status =  new Status { Key = "Accepted" } },
                new Entity { Id = 2, CampaignId = 22, Status =  new Status { Key = "Rejected" } }
            ]);
        }
    }
}

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

public class CSharp4535Tests : LinqIntegrationTest<CSharp4535Tests.ClassFixture>
{
    public CSharp4535Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Empty_query_should_work()
    {
        var mongoEntityCollection = Fixture.Collection;
        var entityQueryable = (IQueryable<Entity>)mongoEntityCollection.AsQueryable();

        var results = entityQueryable.ToArray();

        results.Select(x => x.Id).Should().Equal(1);
    }

    [Fact]
    public void Where_should_work()
    {
        var mongoEntityCollection = Fixture.Collection;
        var entityQueryable = (IQueryable<Entity>)mongoEntityCollection.AsQueryable();

        var queryable = entityQueryable.Where(x => x.Id == 1);

        var results = queryable.ToArray();

        results.Select(x => x.Id).Should().Equal(1);
    }

    public class Entity
    {
        public int Id { get; set; }
    }

    public class MongoEntity : Entity
    {
    }

    public sealed class ClassFixture : MongoCollectionFixture<MongoEntity>
    {
        protected override IEnumerable<MongoEntity> InitialData =>
        [
            new MongoEntity { Id = 1 }
        ];
    }
}

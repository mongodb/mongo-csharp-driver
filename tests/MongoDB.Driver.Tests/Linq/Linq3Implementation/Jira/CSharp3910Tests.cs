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
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3910Tests : LinqIntegrationTest<CSharp3910Tests.ClassFixture>
    {
        public CSharp3910Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Filter_expression_needing_partial_evaluation_should_work()
        {
            var collection = Fixture.Collection;

            var param = "A";
            var result = collection.DeleteMany(mvi => mvi.Description.StartsWith(string.Format("{0}", param)));

            result.DeletedCount.Should().Be(2);
        }

        public class Entity
        {
            public int Id { get; set; }
            public string Description { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Entity>
        {
            protected override IEnumerable<Entity> InitialData =>
            [
                new Entity { Id = 1, Description = "Alpha" },
                new Entity { Id = 2, Description = "Alpha2" },
                new Entity { Id = 3, Description = "Bravo" },
                new Entity { Id = 4, Description = "Charlie" }
            ];
        }
    }
}

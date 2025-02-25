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
    public class CSharp4666Tests : LinqIntegrationTest<CSharp4666Tests.ClassFixture>
    {
        public CSharp4666Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Find_project_id_should_work()
        {
            var collection = Fixture.Collection;

            var find = collection
                .Find(_ => true)
                .Project(x => x.Id);

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ _id : 1 }");

            var result = find.Single();
            result.Should().Be(1);
        }

        [Fact]
        public void Find_project_field_should_work()
        {
            var collection = Fixture.Collection;

            var find = collection
                .Find(_ => true)
                .Project(x => x.X);

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ X : 1, _id : 0 }");

            var result = find.Single();
            result.Should().Be(2);
        }

        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, X = 2 }
            ];
        }
    }
}

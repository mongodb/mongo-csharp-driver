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
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4666Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Find_project_id_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var find = collection
                .Find(_ => true)
                .Project(x => x.Id);

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ _id : 1 }");

            var result = find.Single();
            result.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_project_field_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var find = collection
                .Find(_ => true)
                .Project(x => x.X);

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ X : 1, _id : 0 }");

            var result = find.Single();
            result.Should().Be(2);
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, X = 2 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}

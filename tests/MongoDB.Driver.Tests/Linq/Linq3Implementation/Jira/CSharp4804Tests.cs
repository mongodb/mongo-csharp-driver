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
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4804Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Find_Slice_with_field_name_and_limit_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var projection = Builders<C>.Projection.Slice("A", 3);

            var find = collection.Find("{}").Project(projection);

            var translatedProjection = TranslateFindProjection(collection, find);
            translatedProjection.Should().Be("{ A : { $slice : 3 } }");

            var result = find.Single();
            result["A"].AsBsonArray.Select(i => i.AsInt32).Should().Equal(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_Slice_with_field_expression_and_limit_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var projection = Builders<C>.Projection.Slice(x => x.A, 3);

            var find = collection.Find("{}").Project(projection);

            var translatedProjection = TranslateFindProjection(collection, find);
            translatedProjection.Should().Be("{ A : { $slice : 3 } }");

            var result = find.Single();
            result["A"].AsBsonArray.Select(i => i.AsInt32).Should().Equal(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_Slice_with_field_name_and_skip_and_limit_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var projection = Builders<C>.Projection.Slice("A", 1, 3);

            var find = collection.Find("{}").Project(projection);

            var translatedProjection = TranslateFindProjection(collection, find);
            translatedProjection.Should().Be("{ A : { $slice : [1, 3] } }");

            var result = find.Single();
            result["A"].AsBsonArray.Select(i => i.AsInt32).Should().Equal(2, 3, 4);
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_Slice_with_field_expression_and_skip_and_limit_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var projection = Builders<C>.Projection.Slice(x => x.A, 1, 3);

            var find = collection.Find("{}").Project(projection);

            var translatedProjection = TranslateFindProjection(collection, find);
            translatedProjection.Should().Be("{ A : { $slice : [1, 3] } }");

            var result = find.Single();
            result["A"].AsBsonArray.Select(i => i.AsInt32).Should().Equal(2, 3, 4);
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_Slice_with_field_name_and_limit_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var projection = Builders<C>.Projection.Slice("A", 3);

            var aggregate = collection.Aggregate()
                .Project(projection);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $project : { A : { $slice : ['$A', 3] } } }");

            var result = aggregate.Single();
            result["A"].AsBsonArray.Select(i => i.AsInt32).Should().Equal(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_Slice_with_field_expression_and_limit_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var projection = Builders<C>.Projection.Slice(x => x.A, 3);

            var aggregate = collection.Aggregate()
                .Project(projection);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $project : { A : { $slice : ['$A', 3] } } }");

            var result = aggregate.Single();
            result["A"].AsBsonArray.Select(i => i.AsInt32).Should().Equal(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void AggregateSlice_with_field_name_and_skip_and_limit_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var projection = Builders<C>.Projection.Slice("A", 1, 3);

            var aggregate = collection.Aggregate()
                .Project(projection);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $project : { A : { $slice : ['$A', 1, 3] } } }");

            var result = aggregate.Single();
            result["A"].AsBsonArray.Select(i => i.AsInt32).Should().Equal(2, 3, 4);
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_Slice_with_field_expression_and_skip_and_limit_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var projection = Builders<C>.Projection.Slice(x => x.A, 1, 3);

            var aggregate = collection.Aggregate()
                .Project(projection);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $project : { A : { $slice : ['$A', 1, 3] } } }");

            var result = aggregate.Single();
            result["A"].AsBsonArray.Select(i => i.AsInt32).Should().Equal(2, 3, 4);
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, A = new[] { 1, 2, 3, 4, 5 } });
            return collection;
        }

        public class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
        }
    }
}

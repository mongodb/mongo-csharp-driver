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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    public class UnionMethodToPipelineTranslatorTests: Linq3IntegrationTest
    {
        private readonly IMongoCollection<Company> _firstCollection;
        private readonly IMongoCollection<Company> _secondCollection;

        public UnionMethodToPipelineTranslatorTests()
        {
            _firstCollection = CreateCollection("clients",
                new Company { Id = 1, Name = "first client" },
                new Company { Id = 2, Name = "second client" },
                new Company { Id = 3, Name = "third client" });

            _secondCollection = CreateCollection("partners",
                new Company { Id = 4, Name = "partner" },
                new Company { Id = 5, Name = "another partner" });
        }

        [Fact]
        public void Union_should_combine_collections()
        {
            RequireServer.Check().Supports(Feature.AggregateUnionWith);

            var queryable = _firstCollection
                .AsQueryable()
                .Union(_secondCollection.AsQueryable());

            var stages = Translate(_firstCollection, queryable);
            AssertStages(
                stages,
                "{ $unionWith : 'partners' }",
                "{ $group : { _id : '$$ROOT' } }",
                "{ $replaceRoot : { newRoot : '$_id' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void Union_should_combine_collection_with_itself()
        {
            RequireServer.Check().Supports(Feature.AggregateUnionWith);

            var queryable = _firstCollection
                .AsQueryable()
                .Union(_firstCollection.AsQueryable());

            var stages = Translate(_firstCollection, queryable);
            AssertStages(
                stages,
                "{ $unionWith : 'clients' }",
                "{ $group : { _id : '$$ROOT' } }",
                "{ $replaceRoot : { newRoot : '$_id' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Union_should_combine_filtered_collections()
        {
            RequireServer.Check().Supports(Feature.AggregateUnionWith);

            var queryable = _firstCollection
                .AsQueryable()
                .Where(c => c.Name.StartsWith("second"))
                .Union(_secondCollection.AsQueryable().Where(c => c.Name.StartsWith("another")));

            var stages = Translate(_firstCollection, queryable);
            AssertStages(stages,
                "{ $match : { Name : /^second/s } }",
                "{ $unionWith : { coll : 'partners', pipeline : [{ $match : { Name : /^another/s } }] } }",
                "{ $group : { _id : '$$ROOT' } }",
                "{ $replaceRoot : { newRoot : '$_id' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().BeEquivalentTo(new[] { 2, 5 });
        }

        [Fact]
        public void Union_should_support_projection()
        {
            RequireServer.Check().Supports(Feature.AggregateUnionWith);

            var queryable = _firstCollection
                .AsQueryable()
                .Where(c => c.Name.StartsWith("second"))
                .Select(c => new ProjectedCompany { Number = c.Id })
                .Union(_secondCollection.AsQueryable().Select(c => new ProjectedCompany { Number = c.Id }));

            var stages = Translate(_firstCollection, queryable);
            AssertStages(stages,
                "{ $match : { Name : /^second/s } }",
                "{ $project : { Number : '$_id', _id : 0 } }",
                "{ $unionWith : { coll : 'partners', pipeline : [{ $project : { Number : '$_id', _id : 0 } }] } }",
                "{ $group : { _id : '$$ROOT' } }",
                "{ $replaceRoot : { newRoot : '$_id' } }");

            var results = queryable.ToList();
            results.Select(x => x.Number).Should().BeEquivalentTo(new[] { 2, 4, 5 });
        }

        [Fact]
        public void Union_should_support_projection_to_anonymous()
        {
            RequireServer.Check().Supports(Feature.AggregateUnionWith);

            var queryable = _firstCollection
                .AsQueryable()
                .Where(c => c.Name.StartsWith("second"))
                .Select(c => new { Number = c.Id })
                .Union(_secondCollection.AsQueryable().Select(c => new { Number = c.Id }));

            var stages = Translate(_firstCollection, queryable);
            AssertStages(stages,
                "{ $match : { Name : /^second/s } }",
                "{ $project : { Number : '$_id', _id : 0 } }",
                "{ $unionWith : { coll : 'partners', pipeline : [{ $project : { Number : '$_id', _id : 0 } }] } }",
                "{ $group : { _id : '$$ROOT' } }",
                "{ $replaceRoot : { newRoot : '$_id' } }");

            var results = queryable.ToList();
            results.Select(x => x.Number).Should().BeEquivalentTo(new[] { 2, 4, 5 });
        }

        private IMongoCollection<Company> CreateCollection(string collectionName, params Company[] data)
        {
            var collection = GetCollection<Company>(collectionName);
            CreateCollection(collection, data);

            return collection;
        }

        public class Company
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class ProjectedCompany
        {
            public int Number { get; set; }
        }
    }
}

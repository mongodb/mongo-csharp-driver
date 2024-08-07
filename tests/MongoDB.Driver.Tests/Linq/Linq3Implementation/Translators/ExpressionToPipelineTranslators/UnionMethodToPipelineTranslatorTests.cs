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
        private readonly IMongoCollection<Partner> _thirdCollection;

        public UnionMethodToPipelineTranslatorTests()
        {
            _firstCollection = CreateCollection("clients",
                new Company { Id = 1, Name = "first client" },
                new Company { Id = 2, Name = "second client" },
                new Company { Id = 3, Name = "third client" });

            _secondCollection = CreateCollection("partners",
                new Company { Id = 4, Name = "partner" },
                new Company { Id = 5, Name = "another partner" });

            _thirdCollection = CreateCollection("specialpartners",
                new Partner { Id = 4, PartnerName = "second special partner" },
                new Partner { Id = 5, PartnerName = "third special partner" });

        }

        [Fact]
        public void Union_should_combine_collections()
        {
            RequireServer.Check().Supports(Feature.AggregateUnionWith);

            var queryable = _firstCollection
                .AsQueryable()
                .Union(_secondCollection.AsQueryable());

            var stages = Translate(_firstCollection, queryable);
            AssertStages(stages, "{ $unionWith : 'partners' }");

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
            AssertStages(stages, "{ $unionWith : 'clients' }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().BeEquivalentTo(new[] { 1, 2, 3, 1, 2, 3 });
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
                "{ $unionWith : { coll : 'partners', pipeline : [{ $match : { Name : /^another/s } }] } }");

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
                "{ $unionWith : { coll : 'partners', pipeline : [{ $project : { Number : '$_id', _id : 0 } }] } }");

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
                "{ $unionWith : { coll : 'partners', pipeline : [{ $project : { Number : '$_id', _id : 0 } }] } }");

            var results = queryable.ToList();
            results.Select(x => x.Number).Should().BeEquivalentTo(new[] { 2, 4, 5 });
        }

        [Fact]
        public void Union_should_be_usable_in_a_join()
        {
            RequireServer.Check().Supports(Feature.AggregateUnionWith);

            var q1 = _firstCollection
                .AsQueryable()
                .Where(c => c.Name.StartsWith("second"))
                .Select(x => new { x.Id, x.Name })
                .Union(_secondCollection.AsQueryable()
                    .Select(x => new { x.Id, Name = x.Name }));

            var q2 = _thirdCollection
                .AsQueryable();

            var queryable = q1.Join(q2,
                    c => c.Id,
                    p => p.Id,
                    (c, p) => new { CID = c.Id, Cname = c.Name, Pname = p.PartnerName })
                .Where(x => x.Cname == "another partner" && x.Pname == "third special partner");

            var stages = Translate(_firstCollection, queryable);

            AssertStages(stages,
                "{ $match : { Name : /^second/s } }",
                "{ $project : { _id : '$_id', Name : '$Name' } }",
                "{ $unionWith : { coll : 'partners', pipeline : [{ '$project' : { _id : '$_id', Name : '$Name' } }] } }",
                "{ $project : { '_outer' : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'specialpartners', localField : '_outer._id', foreignField : '_id', as : '_inner' } }",
                "{ $unwind : '$_inner'}",
                "{ $project : { CID : '$_outer._id', Cname : '$_outer.Name', Pname : '$_inner.PartnerName', _id : 0 } }",
                "{ $match : { Cname : 'another partner', Pname : 'third special partner' } }"
            );

            var results = queryable.ToList();
            results.Count().Should().Be(1);
            results.Select(x => x.CID).Should().BeEquivalentTo(5);
            results.Select(x => x.Pname).Should().BeEquivalentTo("third special partner");
        }

        private IMongoCollection<Company> CreateCollection(string collectionName, params Company[] data)
        {
            var collection = GetCollection<Company>(collectionName);
            CreateCollection(collection, data);

            return collection;
        }

        private IMongoCollection<Partner> CreateCollection(string collectionName, params Partner[] data)
        {
            var collection = GetCollection<Partner>(collectionName);
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

        public class Partner
        {
            public int Id { get; set; }
            public string PartnerName { get; set; }
        }
    }
}

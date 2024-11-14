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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4248Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Lookup_should_work()
        {
            var localCollection = GetLocalCollection();
            var foreignCollection = GetForeignCollection();

            var queryable = localCollection.AsQueryable()
                .Lookup(
                    foreignCollection,
                    local => local.LocalField,
                    foreign => foreign.ForeignField);

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _local : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'foreign', localField : '_local.LocalField', foreignField : 'ForeignField', as : '_results' } }");

            var results = queryable.ToList();
            results.Select(x => x.Local.Id).Should().Equal(1, 2);
            results[0].Results.Select(foreign => foreign.Id).Should().Equal(1);
            results[1].Results.Select(foreign => foreign.Id).Should().Equal(2, 3);
        }

        [Fact]
        public void Lookup_with_concise_correlated_pipeline_should_work()
        {
            RequireServer.Check().Supports((Feature.LookupConciseSyntax));
            var localCollection = GetLocalCollection();
            var foreignCollection = GetForeignCollection();

            var queryable = localCollection.AsQueryable()
                .Lookup(
                    foreignCollection,
                    local => local.LocalField,
                    foreign => foreign.ForeignField,
                    (local, queryable) => queryable.Select(foreign => new { LocalId = local.Id, ForeignId = foreign.Id }));

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _local : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'foreign', localField : '_local.LocalField', foreignField : 'ForeignField', let : { local : '$_local' }, pipeline : [{ $project : { LocalId : '$$local._id', ForeignId : '$_id', _id : 0 } }], as : '_results' } }");

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].Local.Id.Should().Be(1);
            results[0].Results.Select(x => x.LocalId).Should().Equal(1);
            results[0].Results.Select(x => x.ForeignId).Should().Equal(1);
            results[1].Local.Id.Should().Be(2);
            results[1].Results.Select(x => x.LocalId).Should().Equal(2, 2);
            results[1].Results.Select(x => x.ForeignId).Should().Equal(2, 3);
        }

        [Fact]
        public void Lookup_with_correlated_pipeline_should_work()
        {
            var localCollection = GetLocalCollection();
            var foreignCollection = GetForeignCollection();

            var queryable = localCollection.AsQueryable()
                .Lookup(
                    foreignCollection,
                    (local, queryable) => queryable.Select(foreign => new { LocalId = local.Id, ForeignId = foreign.Id }));

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _local : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'foreign', let : { local : '$_local' }, pipeline : [{ $project : { LocalId : '$$local._id', ForeignId : '$_id', _id : 0 } }], as : '_results' } }");

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].Local.Id.Should().Be(1);
            results[0].Results.Select(x => x.LocalId).Should().Equal(1, 1, 1);
            results[0].Results.Select(x => x.ForeignId).Should().Equal(1, 2, 3);
            results[1].Local.Id.Should().Be(2);
            results[1].Results.Select(x => x.LocalId).Should().Equal(2, 2, 2);
            results[1].Results.Select(x => x.ForeignId).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Lookup_with_uncorrelated_pipeline_should_work()
        {
            var localCollection = GetLocalCollection();
            var foreignCollection = GetForeignCollection();

            var queryable = localCollection.AsQueryable()
                .Lookup(
                    foreignCollection,
                    (local, queryable) => queryable.Select(foreign => new { ForeignId = foreign.Id }));

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _local : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'foreign', pipeline : [{ $project : { ForeignId : '$_id', _id : 0 } }], as : '_results' } }");

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].Local.Id.Should().Be(1);
            results[0].Results.Select(x => x.ForeignId).Should().Equal(1, 2, 3);
            results[1].Local.Id.Should().Be(2);
            results[1].Results.Select(x => x.ForeignId).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Lookup_documents_should_work()
        {
            RequireServer.Check().Supports(Feature.LookupDocuments);
            var localCollection = GetLocalCollection();
            var foreignCollection = GetForeignCollection();
            var documents = foreignCollection.Find("{}").ToList();

            var queryable = localCollection.AsQueryable()
                .Lookup(
                    local => documents,
                    local => local.LocalField,
                    document => document.ForeignField);

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _local : '$$ROOT', _id : 0 } }",
                "{ $lookup : { localField : '_local.LocalField', foreignField : 'ForeignField', pipeline : [{ $documents : [{ _id : 1, ForeignField : 1 }, { _id : 2, ForeignField : 2 }, { _id : 3, ForeignField : 2 }] }], as : '_results' } }");

            var results = queryable.ToList();
            results.Select(x => x.Local.Id).Should().Equal(1, 2);
            results[0].Results.Select(foreign => foreign.Id).Should().Equal(1);
            results[1].Results.Select(foreign => foreign.Id).Should().Equal(2, 3);
        }

        [Fact]
        public void Lookup_documents_with_concise_correlated_pipeline_should_work()
        {
            RequireServer.Check().Supports(Feature.LookupDocuments);
            var localCollection = GetLocalCollection();
            var foreignCollection = GetForeignCollection();
            var documents = foreignCollection.Find("{}").ToList();

            var queryable = localCollection.AsQueryable()
                .Lookup(
                    local => documents,
                    local => local.LocalField,
                    document => document.ForeignField,
                    (local, queryable) => queryable.Select(foreign => new { LocalId = local.Id, ForeignId = foreign.Id }));

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _local : '$$ROOT', _id : 0 } }",
                "{ $lookup : { localField : '_local.LocalField', foreignField : 'ForeignField', let : { local : '$_local' }, pipeline : [{ $documents : [{ _id : 1, ForeignField : 1 }, { _id : 2, ForeignField : 2 }, { _id : 3, ForeignField : 2 }] }, { $project : { LocalId : '$$local._id', ForeignId : '$_id', _id : 0 } }], as : '_results' } }");

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].Local.Id.Should().Be(1);
            results[0].Results.Select(x => x.LocalId).Should().Equal(1);
            results[0].Results.Select(x => x.ForeignId).Should().Equal(1);
            results[1].Local.Id.Should().Be(2);
            results[1].Results.Select(x => x.LocalId).Should().Equal(2, 2);
            results[1].Results.Select(x => x.ForeignId).Should().Equal(2, 3);
        }

        [Fact]
        public void Lookup_documents_with_correlated_pipeline_should_work()
        {
            RequireServer.Check().Supports(Feature.LookupDocuments);
            var localCollection = GetLocalCollection();
            var foreignCollection = GetForeignCollection();
            var documents = foreignCollection.Find("{}").ToList();

            var queryable = localCollection.AsQueryable()
                .Lookup(
                    local => documents,
                    (local, queryable) => queryable.Select(foreign => new { LocalId = local.Id, ForeignId = foreign.Id }));

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _local : '$$ROOT', _id : 0 } }",
                "{ $lookup : { let : { local : '$_local' }, pipeline : [{ $documents : [{ _id : 1, ForeignField : 1 }, { _id : 2, ForeignField : 2 }, { _id : 3, ForeignField : 2 }] }, { $project : { LocalId : '$$local._id', ForeignId : '$_id', _id : 0 } }], as : '_results' } }");

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].Local.Id.Should().Be(1);
            results[0].Results.Select(x => x.LocalId).Should().Equal(1, 1, 1);
            results[0].Results.Select(x => x.ForeignId).Should().Equal(1, 2, 3);
            results[1].Local.Id.Should().Be(2);
            results[1].Results.Select(x => x.LocalId).Should().Equal(2, 2, 2);
            results[1].Results.Select(x => x.ForeignId).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Lookup_documents_with_uncorrelated_pipeline_should_work()
        {
            RequireServer.Check().Supports(Feature.LookupDocuments);
            var localCollection = GetLocalCollection();
            var foreignCollection = GetForeignCollection();
            var documents = foreignCollection.Find("{}").ToList();

            var queryable = localCollection.AsQueryable()
                .Lookup(
                    local => documents,
                    (local, queryable) => queryable.Select(foreign => new { ForeignId = foreign.Id }));

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _local : '$$ROOT', _id : 0 } }",
                "{ $lookup : { pipeline : [{ $documents : [{ _id : 1, ForeignField : 1 }, { _id : 2, ForeignField : 2 }, { _id : 3, ForeignField : 2 }] }, { $project : { ForeignId : '$_id', _id : 0 } }], as : '_results' } }");

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].Local.Id.Should().Be(1);
            results[0].Results.Select(x => x.ForeignId).Should().Equal(1, 2, 3);
            results[1].Local.Id.Should().Be(2);
            results[1].Results.Select(x => x.ForeignId).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Where_and_Select_against_LookupResult_should_work()
        {
            var localCollection = GetLocalCollection();
            var foreignCollection = GetForeignCollection();

            var queryable = localCollection.AsQueryable()
                .Lookup(
                    foreignCollection,
                    local => local.LocalField,
                    foreign => foreign.ForeignField)
                .Where(x => x.Local.Id == 2)
                .Select(x => x.Results.Select(x => x.Id).Sum());

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _local : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'foreign', localField : '_local.LocalField', foreignField : 'ForeignField', as : '_results' } }",
                "{ $match : { '_local._id' : 2 } }",
                "{ $project : { _v : { $sum : '$_results._id' }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(5);
        }

        private IMongoCollection<Local> GetLocalCollection()
        {
            var collection = GetCollection<Local>("local");
            CreateCollection(
                collection,
                new Local { Id = 1, LocalField = 1 },
                new Local { Id = 2, LocalField = 2 });
            return collection;
        }

        private IMongoCollection<Foreign> GetForeignCollection()
        {
            var collection = GetCollection<Foreign>("foreign");
            CreateCollection(
                collection,
                new Foreign { Id = 1, ForeignField = 1 },
                new Foreign { Id = 2, ForeignField = 2 },
                new Foreign { Id = 3, ForeignField = 2 });
            return collection;
        }

        private class Local
        {
            public int Id { get; set; }
            public int LocalField { get; set; }
        }

        private class Foreign
        {
            public int Id { get; set; }
            public int ForeignField { get; set; }
        }
    }
}

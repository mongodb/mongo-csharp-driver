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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5241Tests : LinqIntegrationTest<CSharp5241Tests.ClassFixture>
    {
        public CSharp5241Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Join_with_equality_match_and_correlated_document_result_should_work()
        {
            var localCollection = Fixture.OuterCollection;
            var foreignCollection = Fixture.InnerCollection;

            var queryable = localCollection.AsQueryable()
                .Join(foreignCollection, outer => outer.Local, inner => inner.Foreign, (outer, inner) => new { OuterId = outer.Id, InnerId = inner.Id });

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _outer : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'inner', localField : '_outer.Local', foreignField : 'Foreign' , as : '_inner' } }",
                "{ $unwind : '$_inner' }",
                "{ $project : { OuterId : '$_outer._id', InnerId : '$_inner._id', _id : 0 } }");
        }

        [Fact]
        public void Join_with_equality_match_and_correlated_scalar_result_should_work()
        {
            var localCollection = Fixture.OuterCollection;
            var foreignCollection = Fixture.InnerCollection;

            var queryable = localCollection.AsQueryable()
                .Join(foreignCollection, outer => outer.Local, inner => inner.Foreign, (outer, inner) => outer.Id);

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _outer : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'inner', localField : '_outer.Local', foreignField : 'Foreign', as : '_inner' } }",
                "{ $unwind : '$_inner' }",
                "{ $project : { _v : '$_outer._id', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 1);
        }

        [Fact]
        public void Join_with_equality_match_and_uncorrelated_document_result_should_work()
        {
            var localCollection = Fixture.OuterCollection;
            var foreignCollection = Fixture.InnerCollection;

            var queryable = localCollection.AsQueryable()
                .Join(foreignCollection, outer => outer.Local, inner => inner.Foreign, (outer, inner) => new { InnerId = inner.Id });

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _outer : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'inner', localField : '_outer.Local', foreignField : 'Foreign', as : '_inner' } }",
                "{ $unwind : '$_inner' }",
                "{ $project : { InnerId : '$_inner._id', _id : 0 } }");

            var results = queryable.ToList();
            results.Select(x => x.InnerId).Should().Equal(2, 3);
        }

        [Fact]
        public void Join_with_equality_match_and_uncorrelated_scalar_result_should_work()
        {
            var localCollection = Fixture.OuterCollection;
            var foreignCollection = Fixture.InnerCollection;

            var queryable = localCollection.AsQueryable()
                .Join(foreignCollection, outer => outer.Local, inner => inner.Foreign, (outer, inner) => inner.Id);

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _outer : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'inner', localField : '_outer.Local', foreignField : 'Foreign', as : '_inner' } }",
                "{ $unwind : '$_inner' }",
                "{ $project : { _v : '$_inner._id', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2, 3);
        }

        [Fact]
        public void Join_with_equality_match_and_uncorrelated_identity_result_should_work()
        {
            var localCollection = Fixture.OuterCollection;
            var foreignCollection = Fixture.InnerCollection;

            var queryable = localCollection.AsQueryable()
                .Join(foreignCollection, outer => outer.Local, inner => inner.Foreign, (outer, inner) => inner);

            var stages = Translate(localCollection, queryable);
            AssertStages(
                stages,
                "{ $project : { _outer : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'inner', localField : '_outer.Local', foreignField : 'Foreign', as : '_inner' } }",
                "{ $unwind : '$_inner' }",
                "{ $project : { _v : '$_inner', _id : 0 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        public class Outer
        {
            public int Id { get; set; }
            public int Local { get; set; }
        }

        public class Inner
        {
            public int Id { get; set; }
            public int Foreign { get; set; }
        }

        public sealed class ClassFixture : MongoDatabaseFixture
        {
            public IMongoCollection<Inner> InnerCollection { get; private set; }

            public IMongoCollection<Outer> OuterCollection { get; private set; }

            protected override void InitializeFixture()
            {
                InnerCollection = CreateCollection<Inner>("inner");
                InnerCollection.InsertMany(
                [
                    new Inner { Id = 2, Foreign = 4 },
                    new Inner { Id = 3, Foreign = 4 }
                ]);

                OuterCollection = CreateCollection<Outer>("outer");
                OuterCollection.InsertMany(
                [
                    new Outer { Id = 1, Local = 4 }
                ]);
            }
        }
    }
}

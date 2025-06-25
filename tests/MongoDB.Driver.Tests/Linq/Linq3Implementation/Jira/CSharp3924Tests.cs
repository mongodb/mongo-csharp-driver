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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3924Tests : LinqIntegrationTest<CSharp3924Tests.ClassFixture>
    {
        public CSharp3924Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Projection_with_call_to_Tuple1_constructor_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => new Tuple<int>(x.X));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$X'], _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(new Tuple<int>(1));
        }

        [Fact]
        public void Projection_with_call_to_Tuple2_constructor_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => new Tuple<int, int>(x.X, x.Y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$X', '$Y'], _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(new Tuple<int, int>(1, 11));
        }

        [Fact]
        public void Projection_with_call_to_Tuple3_constructor_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => new Tuple<int, int, int>(x.X, x.Y, x.Z));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$X', '$Y', '$Z'], _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(new Tuple<int, int, int>(1, 11, 111));
        }

        [Fact]
        public void Where_with_Tuple1_item_comparisons_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => new Tuple<int>(x.X))
                .Where(x => x.Item1 == 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$X'], _id : 0 } }",
                "{ $match : { '_v.0' : 1 } }");

            var result = queryable.Single();
            result.Should().Be(new Tuple<int>(1));
        }

        [Fact]
        public void Where_with_Tuple2_item_comparisons_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => new Tuple<int, int>(x.X, x.Y))
                .Where(x => x.Item1 == 1 && x.Item2 == 11);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$X', '$Y'], _id : 0 } }",
                "{ $match : { '_v.0' : 1, '_v.1' : 11 } }");

            var result = queryable.Single();
            result.Should().Be(new Tuple<int, int>(1, 11));
        }

        [Fact]
        public void Where_with_Tuple3_item_comparisons_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => new Tuple<int, int, int>(x.X, x.Y, x.Z))
                .Where(x => x.Item1 == 1 && x.Item2 == 11 && x.Item3 == 111);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : ['$X', '$Y', '$Z'], _id : 0 } }",
                "{ $match : { '_v.0' : 1, '_v.1' : 11, '_v.2' : 111 } }");

            var result = queryable.Single();
            result.Should().Be(new Tuple<int, int, int>(1, 11, 111));
        }

        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, X = 1, Y = 11, Z = 111 }
            ];
        }
    }
}

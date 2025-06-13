﻿/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class IsNullOrWhiteSpaceMethodToAggregationExpressionTranslatorTests : LinqIntegrationTest<IsNullOrWhiteSpaceMethodToAggregationExpressionTranslatorTests.ClassFixture>
    {
        public IsNullOrWhiteSpaceMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
            : base(fixture, server => server.Supports(Feature.TrimOperator))
        {
        }

        [Fact]
        public void Project_IsNullOrWhiteSpace_using_anonymous_class_should_return_expected_results()
        {
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);
            var collection = Fixture.Collection;

            var find = collection.Find("{}")
                .Project(x => new { R = string.IsNullOrWhiteSpace(x.S) })
                .SortBy(x => x.Id);

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ R : { $or : [{ $eq : ['$S', null] }, { $eq : [{ $trim : { input : '$S' } }, ''] }] }, _id : 0 }");

            var results = find.ToList();
            results.Select(x => x.R).Should().Equal(true, true, true, true, false);
        }

        [Fact]
        public void Project_IsNullOrWhiteSpace_using_named_class_should_return_expected_results()
        {
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);
            var collection = Fixture.Collection;

            var find = collection.Find("{}")
                .Project(x => new Result { R = string.IsNullOrWhiteSpace(x.S) })
                .SortBy(x => x.Id);

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ R : { $or : [{ $eq : ['$S', null] }, { $eq : [{ $trim : { input : '$S' } }, ''] }] }, _id : 0 }");

            var results = find.ToList();
            results.Select(x => x.R).Should().Equal(true, true, true, true, false);
        }

        [Fact]
        public void Select_IsNullOrWhiteSpace_using_scalar_result_should_return_expected_results()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .OrderBy(x => x.Id)
                .Select(x => string.IsNullOrWhiteSpace(x.S));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $project : { _v : { $or : [{ $eq : ['$S', null] }, { $eq : [{ $trim : { input : '$S' } }, ''] }] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(true, true, true, true, false);
        }

        [Fact]
        public void Select_IsNullOrWhiteSpace_using_anonymous_class_should_return_expected_results()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .OrderBy(x => x.Id)
                .Select(x => new { R = string.IsNullOrWhiteSpace(x.S) });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $project : { R : { $or : [{ $eq : ['$S', null] }, { $eq : [{ $trim : { input : '$S' } }, ''] }] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Select(x => x.R).Should().Equal(true, true, true, true, false);
        }

        [Fact]
        public void Select_IsNullOrWhiteSpace_using_named_class_should_return_expected_results()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .OrderBy(x => x.Id)
                .Select(x => new Result { R = string.IsNullOrWhiteSpace(x.S) });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $project : { R : { $or : [{ $eq : ['$S', null] }, { $eq : [{ $trim : { input : '$S' } }, ''] }] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Select(x => x.R).Should().Equal(true, true, true, true, false);
        }

        public class C
        {
            public int Id { get; set; }
            public string S { get; set; }
        }

        public class Result
        {
            public bool R { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, S = null },
                new C { Id = 2, S = "" },
                new C { Id = 3, S = " " },
                new C { Id = 4, S = " \t\r\n" },
                new C { Id = 5, S = "abc" }
            ];
        }
    }
}

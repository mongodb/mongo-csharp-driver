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
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class SelectManyMethodToAggregationExpressionTranslatorTests : LinqIntegrationTest<SelectManyMethodToAggregationExpressionTranslatorTests.ClassFixture>
{
    public SelectManyMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Enumerable_SelectMany_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable().Select(x => x.B.SelectMany(a => a));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $reduce : { input : { $map : { input : '$B', as : 'a', in : '$$a' } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void Enumerable_SelectMany_with_index_should_work()
    {
        RequireServer.Check().Supports(Feature.ArrayIndexAs);

        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable().Select(x => x.B.SelectMany((a, i) => a.Select(y => y + i)));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $reduce : { input : { $map : { input : '$B', as : 'a', arrayIndexAs : 'i', in : { $map : { input : '$$a', as : 'y', in : { $add : ['$$y', '$$i'] } } } } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(10, 20, 31);
    }

    [Fact]
    public void Queryable_SelectMany_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable().Select(x => x.B.AsQueryable().SelectMany(a => a));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $reduce : { input : { $map : { input : '$B', as : 'a', in : '$$a' } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void Queryable_SelectMany_with_index_should_work()
    {
        RequireServer.Check().Supports(Feature.ArrayIndexAs);

        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable().Select(x => x.B.AsQueryable().SelectMany((a, i) => a.Select(y => y + i)));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $reduce : { input : { $map : { input : '$B', as : 'a', arrayIndexAs : 'i', in : { $map : { input : '$$a', as : 'y', in : { $add : ['$$y', '$$i'] } } } } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(10, 20, 31);
    }

    public class C
    {
        public int Id { get; set; }
        public int[][] B { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new() { Id = 1, B = new int[][] { [10, 20], [30] } }
        ];
    }
}
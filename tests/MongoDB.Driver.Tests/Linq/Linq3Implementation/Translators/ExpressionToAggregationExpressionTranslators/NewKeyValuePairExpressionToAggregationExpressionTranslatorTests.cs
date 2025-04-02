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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

#if NET6_0_OR_GREATER || NETCOREAPP3_1_OR_GREATER
public class NewKeyValuePairExpressionToAggregationExpressionTranslatorTests : LinqIntegrationTest<NewKeyValuePairExpressionToAggregationExpressionTranslatorTests.ClassFixture>
{
    public NewKeyValuePairExpressionToAggregationExpressionTranslatorTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void NewKeyValuePair_should_translate()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(d => new KeyValuePair<string,int>("X", d.X));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { Key : 'X', Value : '$X', _id : 0 } }");

        var result = queryable.Single();
        result.Key.Should().Be("X");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void KeyValuePair_Create_should_translate()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(d => KeyValuePair.Create("X", d.X));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { Key : 'X', Value : '$X', _id : 0 } }");

        var result = queryable.Single();
        result.Key.Should().Be("X");
        result.Value.Should().Be(42);
    }

    public class C
    {
        public int X { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { X = 42 }
        ];
    }
}

#endif

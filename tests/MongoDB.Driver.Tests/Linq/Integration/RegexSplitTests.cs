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
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class RegexSplitTests : LinqIntegrationTest<RegexSplitTests.ClassFixture>
{
    public RegexSplitTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.SplitWithRegex))
    {
    }

    [Theory]
    [MemberData(nameof(SelectTestCases))]
    public void Split_in_select_should_work(
        int documentId,
        Expression<Func<C, string[]>> selector,
        string expectedProjectionStage,
        string[] expectedResults)
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == documentId)
            .Select(selector);

        var stages = Translate(collection, queryable);
        AssertStages(stages, $"{{ $match: {{ _id: {documentId} }} }}", expectedProjectionStage);

        var result = queryable.Single();
        result.Should().Equal(expectedResults);
    }

    public static IEnumerable<object[]> SelectTestCases =
    [
        // regex.Split(input)
        [
            1,
            (Expression<Func<C, string[]>>)(d => new Regex("[,;]").Split(d.Input)),
            "{ $project: { _v: { $split: ['$Input', /[,;]/] }, _id: 0 } }",
            new[] { "a", "b", "c" }
        ],
        [
            1,
            (Expression<Func<C, string[]>>)(d => d.SplitRegex.Split(d.Input)),
            "{ $project: { _v: { $split: ['$Input', '$SplitRegex'] }, _id: 0 } }",
            new[] { "a", "b", "c" }
        ],
        // Regex.Split(input, pattern) — static, no options
        [
            1,
            (Expression<Func<C, string[]>>)(d => Regex.Split(d.Input, "[,;]")),
            "{ $project: { _v: { $split: ['$Input', /[,;]/] }, _id: 0 } }",
            new[] { "a", "b", "c" }
        ],
        // Regex.Split(input, pattern, options) — static, with IgnoreCase
        [
            1,
            (Expression<Func<C, string[]>>)(d => Regex.Split(d.Input, "[,;]", RegexOptions.IgnoreCase)),
            "{ $project: { _v: { $split: ['$Input', /[,;]/i] }, _id: 0 } }",
            new[] { "a", "b", "c" }
        ],
    ];

    [Theory]
    [MemberData(nameof(WhereTestCases))]
    public void Split_in_where_should_work(
        Expression<Func<C, bool>> predicate,
        string expectedMatchStage,
        int[] expectedDocumentIds)
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(predicate)
            .Select(d => d.Id);

        var stages = Translate(collection, queryable);
        AssertStages(stages, expectedMatchStage, "{ $project: { _v: '$_id', _id: 0 } }");

        var result = queryable.ToArray();
        result.Should().Equal(expectedDocumentIds);
    }

    public static IEnumerable<object[]> WhereTestCases =
    [
        // regex.Split(input)
        [
            (Expression<Func<C, bool>>)(d => new Regex("[,;]").Split(d.Input).Length == 3),
            "{ $match: { $expr: { $eq: [ { $size: { $split: ['$Input', /[,;]/] } }, 3] } } }",
            new[] { 1 }
        ],
        [
            (Expression<Func<C, bool>>)(d => d.SplitRegex.Split(d.Input).Length == 3),
            "{ $match: { $expr: { $eq: [ { $size: { $split: ['$Input', '$SplitRegex'] } }, 3] } } }",
            new[] { 1 }
        ],
        // Regex.Split(input, pattern) — static, no options
        [
            (Expression<Func<C, bool>>)(d => Regex.Split(d.Input, "[,;]").Length == 3),
            "{ $match: { $expr: { $eq: [ { $size: { $split: ['$Input', /[,;]/] } }, 3] } } }",
            new[] { 1 }
        ],
        // Regex.Split(input, pattern, options) — static, with IgnoreCase
        [
            (Expression<Func<C, bool>>)(d => Regex.Split(d.Input, "[,;]", RegexOptions.IgnoreCase).Length == 3),
            "{ $match: { $expr: { $eq: [ { $size: { $split: ['$Input', /[,;]/i] } }, 3] } } }",
            new[] { 1 }
        ],
    ];

    public class C
    {
        public int Id { get; set; }
        public string Input { get; set; }

        public Regex SplitRegex { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new() { Id = 1, Input = "a,b;c", SplitRegex = new Regex("[,;]") },
            new() { Id = 2, Input = "a;b", SplitRegex = new Regex("[,;]") },
        ];
    }
}

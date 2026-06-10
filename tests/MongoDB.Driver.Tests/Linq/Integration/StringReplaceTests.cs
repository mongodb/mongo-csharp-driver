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
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class StringReplaceTests : LinqIntegrationTest<StringReplaceTests.ClassFixture>
{
    public StringReplaceTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.ReplaceAll))
    {
    }

    [Theory]
    [MemberData(nameof(SelectTestCases))]
    public void Replace_in_select_should_work(
        int documentId,
        Expression<Func<C, string>> selector,
        string expectedProjectionStage,
        string expectedResults)
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == documentId)
            .Select(selector);

        var stages = Translate(collection, queryable);
        AssertStages(stages, $"{{ $match: {{ _id: {documentId} }} }}", expectedProjectionStage);

        var result = queryable.Single();
        result.Should().Be(expectedResults);
    }

    public static IEnumerable<object[]> SelectTestCases =
    [
        // string.Replace(char, char)
        [
            1,
            (Expression<Func<C, string>>)(d => d.S.Replace(',', '!')),
            "{ $project: { _v: { $replaceAll: { input: '$S', find: ',', replacement: '!' } }, _id: 0 } }",
            "a!b!c"
        ],
        // string.Replace(string, string)
        [
            1,
            (Expression<Func<C, string>>)(d => d.S.Replace(",", "")),
            "{ $project: { _v: { $replaceAll: { input: '$S', find: ',', replacement: '' } }, _id: 0 } }",
            "abc"
        ],
    ];

    [Theory]
    [MemberData(nameof(WhereTestCases))]
    public void Replace_in_where_should_work(
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
        // string.Replace(char, char)
        [
            (Expression<Func<C, bool>>)(d => d.S.Replace(',', '!') == "a!b!c"),
            "{ $match: { $expr: { $eq: [{ $replaceAll: { input: '$S', find: ',', replacement: '!' } }, 'a!b!c'] } } }",
            new[] { 1 }
        ],
        // string.Replace(string, string)
        [
            (Expression<Func<C, bool>>)(d => d.S.Replace(",", "") == "abc"),
            "{ $match: { $expr: { $eq: [{ $replaceAll: { input: '$S', find: ',', replacement: '' } }, 'abc'] } } }",
            new[] { 1 }
        ],
    ];

    public class C
    {
        public int Id { get; set; }
        public string S { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new() { Id = 1, S = "a,b,c" },
            new() { Id = 2, S = "a,,b" },
        ];
    }
}

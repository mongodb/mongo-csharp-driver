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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class StringSplitTests : LinqIntegrationTest<StringSplitTests.ClassFixture>
{
    public StringSplitTests(ClassFixture fixture)
        : base(fixture)
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
        // string.Split(char[])
        [
            1,
            (Expression<Func<C, string[]>>)(d => d.S.Split(new[] { ',' })),
            "{ $project: { _v: { $split: ['$S', ','] }, _id: 0 } }",
            new[] { "a", "b", "c" }
        ],
        // string.Split(char[], StringSplitOptions.RemoveEmptyEntries)
        [
            1,
            (Expression<Func<C, string[]>>)(d => d.S.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)),
            "{ $project: { _v: { $filter: { input: { $split: ['$S', ','] }, as: 'item', cond: { $ne: ['$$item', ''] } } }, _id: 0 } }",
            new[] { "a", "b", "c" }
        ],
        [
            2,
            (Expression<Func<C, string[]>>)(d => d.S.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)),
            "{ $project: { _v: { $filter: { input: { $split: ['$S', ','] }, as: 'item', cond: { $ne: ['$$item', ''] } } }, _id: 0 } }",
            new[] { "a", "b" }
        ],
        // string.Split(char[], int)
        [
            1,
            (Expression<Func<C, string[]>>)(d => d.S.Split(new[] { ',' }, 2)),
            "{ $project: { _v: { $slice: [{ $split: ['$S', ','] }, 2] }, _id: 0 } }",
            new[] { "a", "b" }
        ],
        [
            1,
            (Expression<Func<C, string[]>>)(d => d.S.Split(new[] { ',' }, 5)),
            "{ $project: { _v: { $slice: [{ $split: ['$S', ','] }, 5] }, _id: 0 } }",
            new[] { "a", "b", "c" }
        ],
        [
            2,
            (Expression<Func<C, string[]>>)(d => d.S.Split(new[] { ',' }, 2)),
            "{ $project: { _v: { $slice: [{ $split: ['$S', ','] }, 2] }, _id: 0 } }",
            new[] { "a", "" }
        ],
        // string.Split(char[], int, StringSplitOptions.RemoveEmptyEntries)
        [
            1,
            (Expression<Func<C, string[]>>)(d => d.S.Split(new[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries)),
            "{ $project: { _v: { $slice: [{ $filter : { input : { $split : [ '$S', ','] }, as : 'item', cond : { $ne : ['$$item', ''] } } }, 2]}, _id: 0 } }",
            new[] { "a", "b" }
        ],
        [
            2,
            (Expression<Func<C, string[]>>)(d => d.S.Split(new[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries)),
            "{ $project: { _v: { $slice: [{ $filter : { input : { $split : [ '$S', ','] }, as : 'item', cond : { $ne : ['$$item', ''] } } }, 2]}, _id: 0 } }",
            new[] { "a", "b" }
        ]
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
        // string.Split(char[])
        [
            (Expression<Func<C, bool>>)(d => d.S.Split(new[] { ',' }).Length == 3),
            "{ $match: { $expr: { $eq: [{ $size: { $split: ['$S', ','] } }, 3] } } }",
            new[] { 1, 2 }
        ],
        // string.Split(char[], StringSplitOptions.RemoveEmptyEntries)
        [
            (Expression<Func<C, bool>>)(d => d.S.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length == 2),
            "{ $match: { $expr: { $eq: [{ $size: { $filter: { input: { $split: ['$S', ','] }, as: 'item', cond: { $ne: ['$$item', ''] } } } }, 2] } } }",
            new[] { 2 }
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

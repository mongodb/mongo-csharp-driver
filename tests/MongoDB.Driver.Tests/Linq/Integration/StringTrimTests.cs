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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class StringTrimTests : LinqIntegrationTest<StringTrimTests.ClassFixture>
{
    public StringTrimTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Select_with_Trim_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Str.Trim());

        var stages = Translate(collection, queryable);
        AssertStages(stages, """{ $project : { _v : { $trim : { input : "$Str" } }, _id : 0 } }""");

        var results = queryable.ToList();
        results.Should().Equal(Projected(s => s.Trim()));
    }

    [Fact]
    public void Select_with_Trim_with_chars_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Str.Trim(new[] { ' ', 'a' }));

        var stages = Translate(collection, queryable);
        AssertStages(stages, """{ $project : { _v : { $trim : { input : "$Str", chars : " a" } }, _id : 0 } }""");

        var results = queryable.ToList();
        results.Should().Equal(Projected(s => s.Trim(' ', 'a')));
    }

    [Fact]
    public void Select_with_Trim_with_empty_chars_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Str.Trim(new char[0]));

        var stages = Translate(collection, queryable);
        AssertStages(stages, """{ $project : { _v : { $trim : { input : "$Str" } }, _id : 0 } }""");

        var results = queryable.ToList();
        results.Should().Equal(Projected(s => s.Trim()));
    }

    [Fact]
    public void Select_with_TrimStart_with_chars_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Str.TrimStart(new[] { ' ', 'a' }));

        var stages = Translate(collection, queryable);
        AssertStages(stages, """{ $project : { _v : { $ltrim : { input : "$Str", chars : " a" } }, _id : 0 } }""");

        var results = queryable.ToList();
        results.Should().Equal(Projected(s => s.TrimStart(' ', 'a')));
    }

    [Fact]
    public void Select_with_TrimStart_with_empty_chars_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Str.TrimStart(new char[0]));

        var stages = Translate(collection, queryable);
        AssertStages(stages, """{ $project : { _v : { $ltrim : { input : "$Str" } }, _id : 0 } }""");

        var results = queryable.ToList();
        results.Should().Equal(Projected(s => s.TrimStart()));
    }

    [Fact]
    public void Select_with_TrimEnd_with_chars_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Str.TrimEnd(new[] { ' ', 'd' }));

        var stages = Translate(collection, queryable);
        AssertStages(stages, """{ $project : { _v : { $rtrim : { input : "$Str", chars : " d" } }, _id : 0 } }""");

        var results = queryable.ToList();
        results.Should().Equal(Projected(s => s.TrimEnd(' ', 'd')));
    }

    [Fact]
    public void Select_with_TrimEnd_with_empty_chars_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Str.TrimEnd(new char[0]));

        var stages = Translate(collection, queryable);
        AssertStages(stages, """{ $project : { _v : { $rtrim : { input : "$Str" } }, _id : 0 } }""");

        var results = queryable.ToList();
        results.Should().Equal(Projected(s => s.TrimEnd()));
    }

    // Add coverage for parameterless and single char overloads of Trim, TrimStart, and TrimEnd, see https://jira.mongodb.org/browse/CSHARP-5979
    // e.g. Trim(' '), TrimStart(), TrimStart(' '), TrimEnd(), TrimEnd(' ')

    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_Trim_with_chars_should_work(char value)
    {
        var collection = Fixture.Collection;
        var trimChars = new[] { value };

        var results = collection.AsQueryable().Where(x => x.Str.Trim(trimChars) == "a").ToList();

        AssertMatched(results, s => s.Trim(trimChars) == "a");
    }

    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_TrimStart_with_chars_should_work(char value)
    {
        var collection = Fixture.Collection;
        var trimChars = new[] { value };

        var results = collection.AsQueryable().Where(x => x.Str.TrimStart(trimChars) == "a").ToList();

        AssertMatched(results, s => s.TrimStart(trimChars) == "a");
    }

    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_TrimEnd_with_chars_should_work(char value)
    {
        var collection = Fixture.Collection;
        var trimChars = new[] { value };

        var results = collection.AsQueryable().Where(x => x.Str.TrimEnd(trimChars) == "a").ToList();

        AssertMatched(results, s => s.TrimEnd(trimChars) == "a");
    }

    // the parameterless overload has the same empty-result problem as the char overloads below
    // (TrimStart()/TrimEnd() with no arguments are not supported by the translator, see CSHARP-5979)
    [Fact]
    public void Where_with_Trim_should_work_when_trimmed_value_is_empty()
    {
        var collection = Fixture.Collection;

        var results = collection.AsQueryable().Where(x => x.Str.Trim() == "").ToList();

        AssertMatched(results, s => s.Trim() == "");
    }

    // a value consisting entirely of trim chars trims to "", so the lookarounds in the translated pattern
    // have to succeed at the start and end of the input where there is no character to test
    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_Trim_with_chars_should_work_when_trimmed_value_is_empty(char value)
    {
        var collection = Fixture.Collection;
        var trimChars = new[] { value };

        var results = collection.AsQueryable().Where(x => x.Str.Trim(trimChars) == "").ToList();

        AssertMatched(results, s => s.Trim(trimChars) == "");
    }

    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_TrimStart_with_chars_should_work_when_trimmed_value_is_empty(char value)
    {
        var collection = Fixture.Collection;
        var trimChars = new[] { value };

        var results = collection.AsQueryable().Where(x => x.Str.TrimStart(trimChars) == "").ToList();

        AssertMatched(results, s => s.TrimStart(trimChars) == "");
    }

    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_TrimEnd_with_chars_should_work_when_trimmed_value_is_empty(char value)
    {
        var collection = Fixture.Collection;
        var trimChars = new[] { value };

        var results = collection.AsQueryable().Where(x => x.Str.TrimEnd(trimChars) == "").ToList();

        AssertMatched(results, s => s.TrimEnd(trimChars) == "");
    }

    // a pattern that can never match would also make the negated filter match everything
    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_Trim_with_chars_and_not_equal_should_work(char value)
    {
        var collection = Fixture.Collection;
        var trimChars = new[] { value };

        var results = collection.AsQueryable().Where(x => x.Str.Trim(trimChars) != "").ToList();

        AssertMatched(results, s => s.Trim(trimChars) != "");
    }

    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_Trim_with_multiple_chars_should_work(char value)
    {
        var collection = Fixture.Collection;
        var trimChars = new[] { value, 'x' };

        var results = collection.AsQueryable().Where(x => x.Str.Trim(trimChars) == "a").ToList();

        AssertMatched(results, s => s.Trim(trimChars) == "a");
    }

    public static IEnumerable<object[]> TestChars =>
        ClassFixture.TestChars.Select(c => new object[] { c });

    private IEnumerable<string> Projected(Func<string, string> projection) =>
        Fixture.Documents.Select(d => projection(d.Str));

    private void AssertMatched(List<C> results, Func<string, bool> predicate)
    {
        var expectedIds = Fixture.Documents.Where(d => predicate(d.Str)).Select(d => d.Id).ToArray();
        expectedIds.Should().NotBeEmpty("otherwise the test would pass vacuously");

        results.Select(x => x.Id).Should().BeEquivalentTo(expectedIds);
    }

    public class C
    {
        public int Id { get; set; }
        public string Str { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        public static readonly char[] TestChars = ['*', '\\', ']', '[', '-', '^', '|', '$', '.', '+', '(', ')', '{', '?', 'b', 'z', '1', ' ', '_'];

        public readonly C[] Documents = CreateDocuments();

        protected override IEnumerable<C> InitialData => Documents;

        private static C[] CreateDocuments()
        {
            var documents = new List<C>
            {
                new C { Id = 1, Str = " abcd " },
                new C { Id = 2, Str = "abcd " },
                new C { Id = 3, Str = " abcd" },
                new C { Id = 4, Str = "abcd" }
            };

            var id = 5;
            foreach (var c in TestChars)
            {
                documents.Add(new C { Id = id++, Str = $"{c}{c}a" });    // TrimStart(c) == "a"
                documents.Add(new C { Id = id++, Str = $"a{c}{c}" });    // TrimEnd(c) == "a"
                documents.Add(new C { Id = id++, Str = $"{c}a{c}" });    // Trim(c) == "a"
                documents.Add(new C { Id = id++, Str = $"{c}ab{c}" });
                documents.Add(new C { Id = id++, Str = $"x{c}a{c}x" });  // Trim([c, 'x']) == "a"
                documents.Add(new C { Id = id++, Str = $"{c}{c}" });      // Trim(c) == ""
            }

            documents.Add(new C { Id = id++, Str = "a" });
            documents.Add(new C { Id = id++, Str = "abc" });
            documents.Add(new C { Id = id++, Str = "" });
            return documents.ToArray();
        }
    }
}

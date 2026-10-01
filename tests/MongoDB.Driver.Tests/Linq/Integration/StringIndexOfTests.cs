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

namespace MongoDB.Driver.Tests.Linq.Integration;

public class StringIndexOfTests : LinqIntegrationTest<StringIndexOfTests.ClassFixture>
{
    public StringIndexOfTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_IndexOf_char_equals_zero_should_work(char value)
    {
        var collection = Fixture.Collection;

        var results = collection.AsQueryable().Where(x => x.Str.IndexOf(value) == 0).ToList();

        AssertTestCase(results, s => s.IndexOf(value) == 0);
    }

    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_IndexOf_char_equals_one_should_work(char value)
    {
        var collection = Fixture.Collection;

        var results = collection.AsQueryable().Where(x => x.Str.IndexOf(value) == 1).ToList();

        AssertTestCase(results, s => s.IndexOf(value) == 1);
    }

    [Theory]
    [MemberData(nameof(TestStrings))]
    public void Where_with_IndexOf_string_equals_zero_should_work(string value)
    {
        var collection = Fixture.Collection;

        var results = collection.AsQueryable().Where(x => x.Str.IndexOf(value) == 0).ToList();

        AssertTestCase(results, s => s.IndexOf(value, StringComparison.Ordinal) == 0);
    }

    [Fact]
    public void Where_with_IndexOf_char_should_render_a_literal_prefix()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Str.IndexOf('*') == 0);

        var stages = Translate(collection, queryable);

        AssertStages(stages, """{ $match : { Str : /^\*/s } }""");
    }

    public static IEnumerable<object[]> TestChars =>
        ClassFixture.TestChars.Select(c => new object[] { c });

    public static IEnumerable<object[]> TestStrings =>
        ClassFixture.TestStrings.Select(s => new object[] { s });

    private void AssertTestCase(List<C> results, Func<string, bool> predicate)
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
        public static readonly string[] TestStrings = ["b", "ab", "bc", "abc", "a b", "*b", @"\b", "b.c", "[b]", "a|b"];

        public readonly C[] Documents = CreateDocuments();

        protected override IEnumerable<C> InitialData => Documents;

        private static C[] CreateDocuments()
        {
            var documents = new List<C>();
            var id = 1;
            foreach (var c in TestChars)
            {
                documents.Add(new C { Id = id++, Str = $"{c}{c}a" }); // IndexOf(c) == 0
                documents.Add(new C { Id = id++, Str = $"a{c}{c}" }); // IndexOf(c) == 1
                documents.Add(new C { Id = id++, Str = $"{c}a{c}" }); // IndexOf(c) == 0
                documents.Add(new C { Id = id++, Str = $"x{c}y" });   // IndexOf(c) == 1
            }

            foreach (var s in TestStrings)
            {
                documents.Add(new C { Id = id++, Str = s });             // IndexOf(s) == 0
                documents.Add(new C { Id = id++, Str = s + "abc" });     // IndexOf(s) == 0
                documents.Add(new C { Id = id++, Str = "abc" + s });
                documents.Add(new C { Id = id++, Str = "x" + s + "y" });
            }

            documents.Add(new C { Id = id++, Str = "a" });
            documents.Add(new C { Id = id++, Str = "abc" });
            documents.Add(new C { Id = id++, Str = "" });
            return documents.ToArray();
        }
    }
}

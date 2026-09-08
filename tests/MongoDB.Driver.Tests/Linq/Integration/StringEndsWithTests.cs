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

public class StringEndsWithTests : LinqIntegrationTest<StringEndsWithTests.ClassFixture>
{
    public StringEndsWithTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Theory]
    [MemberData(nameof(TestStrings))]
    public void Where_with_EndsWith_string_should_work(string value)
    {
        var collection = Fixture.Collection;

        var results = collection.AsQueryable().Where(x => x.Str.EndsWith(value)).ToList();

        AssertTestCase(results, s => s.EndsWith(value, StringComparison.Ordinal));
    }

#if !NET472_OR_GREATER
    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_EndsWith_char_should_work(char value)
    {
        var collection = Fixture.Collection;

        var results = collection.AsQueryable().Where(x => x.Str.EndsWith(value)).ToList();

        AssertTestCase(results, s => s.Length > 0 && s[s.Length - 1] == value);
    }
#endif

    public static IEnumerable<object[]> TestStrings =>
        ClassFixture.TestStrings.Select(s => new object[] { s });

    public static IEnumerable<object[]> TestChars =>
        ClassFixture.TestChars.Select(c => new object[] { c });

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
        public static readonly string[] TestStrings = ["b", "ab", "bc", "abc", "a b", "b*", @"b\", "b.c", "[b]", "a|b"];
        public static readonly char[] TestChars = ['*', '\\', ']', '[', '-', '^', '$', '.', 'b', ' '];

        public readonly C[] Documents = CreateDocuments();

        protected override IEnumerable<C> InitialData => Documents;

        private static C[] CreateDocuments()
        {
            var documents = new List<C>();
            var id = 1;
            foreach (var s in TestStrings)
            {
                documents.Add(new C { Id = id++, Str = s });             // EndsWith(s)
                documents.Add(new C { Id = id++, Str = "abc" + s });     // EndsWith(s)
                documents.Add(new C { Id = id++, Str = s + "abc" });
                documents.Add(new C { Id = id++, Str = "x" + s + "y" });
            }

            foreach (var c in TestChars)
            {
                documents.Add(new C { Id = id++, Str = $"abc{c}" });     // EndsWith(c)
                documents.Add(new C { Id = id++, Str = $"{c}abc" });
            }

            documents.Add(new C { Id = id++, Str = "abc" });
            documents.Add(new C { Id = id++, Str = "" });
            return documents.ToArray();
        }
    }
}

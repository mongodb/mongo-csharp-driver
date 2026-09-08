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

public class StringIndexOfAnyTests : LinqIntegrationTest<StringIndexOfAnyTests.ClassFixture>
{
    public StringIndexOfAnyTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_IndexOfAny_equals_zero_should_work(char value)
    {
        var collection = Fixture.Collection;
        var anyOf = new[] { value, 'z' };

        var results = collection.AsQueryable().Where(x => x.Str.IndexOfAny(anyOf) == 0).ToList();

        AssertTestCase(results, s => s.IndexOfAny(anyOf) == 0);
    }

    [Theory]
    [MemberData(nameof(TestChars))]
    public void Where_with_IndexOfAny_equals_one_should_work(char value)
    {
        var collection = Fixture.Collection;
        var anyOf = new[] { value, 'z' };

        var results = collection.AsQueryable().Where(x => x.Str.IndexOfAny(anyOf) == 1).ToList();

        AssertTestCase(results, s => s.IndexOfAny(anyOf) == 1);
    }

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
        public static readonly char[] TestChars = ['*', '\\', ']', '[', '-', '^', '|', '$', '.', '+', '(', ')', '{', '?', 'b', '1', ' ', '_'];

        public readonly C[] Documents = CreateDocuments();

        protected override IEnumerable<C> InitialData => Documents;

        private static C[] CreateDocuments()
        {
            var documents = new List<C>();
            var id = 1;
            foreach (var c in TestChars)
            {
                documents.Add(new C { Id = id++, Str = $"{c}{c}a" }); // IndexOfAny([c, 'z']) == 0
                documents.Add(new C { Id = id++, Str = $"a{c}{c}" }); // IndexOfAny([c, 'z']) == 1
                documents.Add(new C { Id = id++, Str = $"x{c}y" });   // IndexOfAny([c, 'z']) == 1
                documents.Add(new C { Id = id++, Str = $"z{c}y" });   // matched by the other char of the set
            }

            documents.Add(new C { Id = id++, Str = "az" });
            documents.Add(new C { Id = id++, Str = "abc" });
            documents.Add(new C { Id = id++, Str = "" });
            return documents.ToArray();
        }
    }
}

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
        results.Should().Equal("abcd", "abcd", "abcd", "abcd");
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
        results.Should().Equal("bcd", "bcd", "bcd", "bcd");
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
        results.Should().Equal("abcd", "abcd", "abcd", "abcd");
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
        results.Should().Equal("bcd ", "bcd ", "bcd", "bcd");
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
        results.Should().Equal("abcd ", "abcd ", "abcd", "abcd");
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
        results.Should().Equal(" abc", "abc", " abc", "abc");
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
        results.Should().Equal(" abcd", "abcd", " abcd", "abcd");
    }

    // TODO CSHARP-5979: Add coverage for parameterless and single char overloads of Trim, TrimStart, and TrimEnd.
    // e.g. Trim(' '), TrimStart(), TrimStart(' '), TrimEnd(), TrimEnd(' ')

    public class C
    {
        public int Id { get; set; }
        public string Str { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, Str = " abcd "},
            new C { Id = 2, Str = "abcd "},
            new C { Id = 3, Str = " abcd"},
            new C { Id = 4, Str = "abcd"},
        ];
    }
}

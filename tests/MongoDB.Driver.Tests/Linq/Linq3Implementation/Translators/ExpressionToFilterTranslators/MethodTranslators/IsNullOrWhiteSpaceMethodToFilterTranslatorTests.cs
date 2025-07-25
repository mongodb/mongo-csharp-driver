﻿/* Copyright 2010-present MongoDB Inc.
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

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    public class IsNullOrWhiteSpaceMethodToFilterTranslatorTests : LinqIntegrationTest<IsNullOrWhiteSpaceMethodToFilterTranslatorTests.ClassFixture>
    {
        public IsNullOrWhiteSpaceMethodToFilterTranslatorTests(ClassFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void Find_using_IsNullOrWhiteSpace_should_return_expected_results()
        {
            var collection = Fixture.Collection;

            var find = collection.Find(x => string.IsNullOrWhiteSpace(x.S));

            var translatedFilter = TranslateFindFilter(collection, find);
            translatedFilter.Should().Be("{ S : { $in : [null, /^\\s*$/ ] } }");

            var results = find.ToList();
            results.Select(x => x.Id).Should().BeEquivalentTo(1, 2, 3, 4);
        }

        [Fact]
        public void Where_using_IsNullOrWhiteSpace_should_return_expected_results()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(x => string.IsNullOrWhiteSpace(x.S));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { S : { $in : [null, /^\\s*$/ ] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().BeEquivalentTo(1, 2, 3, 4);
        }

        public class C
        {
            public int Id { get; set; }
            public string S { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, S = null },
                new C { Id = 2, S = "" },
                new C { Id = 3, S = " " },
                new C { Id = 4, S = " \t\r\n" },
                new C { Id = 5, S = "abc" }
            ];
        }
    }
}

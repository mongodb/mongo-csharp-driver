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

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    public class EqualsMethodToFilterTranslatorTests : LinqIntegrationTest<EqualsMethodToFilterTranslatorTests.ClassFixture>
    {
        public EqualsMethodToFilterTranslatorTests(ClassFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void Equals_with_uint64_and_nullable_int32_should_translate()
        {
            var collection = Fixture.Collection;
            ulong value = 2;

            var queryable = collection.AsQueryable()
                .Where(e => e.ReportsTo.Equals(value));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { ReportsTo : 2 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be(1);
        }

        [Fact]
        public void Equals_with_int32_and_nullable_int32_should_translate()
        {
            var collection = Fixture.Collection;
            int value = 2;

            var queryable = collection.AsQueryable()
                .Where(e => e.ReportsTo.Equals(value));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { ReportsTo : 2 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be(1);
        }

        [Fact]
        public void Equals_with_null_and_nullable_int32_should_translate()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(e => e.ReportsTo.Equals(null));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { ReportsTo : null } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be(3);
        }

        [Fact]
        public void Equals_with_string_and_nullable_int32_should_throw()
        {
            var collection = Fixture.Collection;
            var value = "2";

            var queryable = collection.AsQueryable()
                .Where(e => e.ReportsTo.Equals(value));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { ReportsTo : 2 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be(1);
        }

        [Fact]
        public void Equals_with_no_match_should_return_empty()
        {
            var collection = Fixture.Collection;
            ulong value = 999;

            var queryable = collection.AsQueryable()
                .Where(e => e.ReportsTo.Equals(value));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { ReportsTo : 999 } }");

            var results = queryable.ToList();
            results.Should().BeEmpty();
        }

        [Fact]
        public void Equals_with_overflowing_uint64_and_nullable_int32_should_throw()
        {
            var collection = Fixture.Collection;
            ulong value = (ulong)int.MaxValue + 1;

            var queryable = collection.AsQueryable()
                .Where(e => e.ReportsTo.Equals(value));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<OverflowException>();
        }

        public class C
        {
            public int Id { get; set; }
            public int? ReportsTo { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, ReportsTo = 2 },
                new C { Id = 2, ReportsTo = 5 },
                new C { Id = 3, ReportsTo = null }
            ];
        }
    }
}

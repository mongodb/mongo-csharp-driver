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
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4090Tests : Linq3IntegrationTest
    {
        [Fact]
        public void String_starts_with_with_current_culture_ignore_case_should_work()
        {
            var collection = GetCollection<C>();
            collection.Database.DropCollection(collection.CollectionNamespace.CollectionName);

            collection.InsertMany(
                new[]
                {
                    new C { Id = 100, Text = "Apple-Orange-Banana", Match = "apple" },
                    new C { Id = 101, Text = "Apple-Kiwi-Pear", Match = "mango" }
                });

            var find = collection.Find(x => x.Text.StartsWith(x.Match, StringComparison.CurrentCultureIgnoreCase));

            var rendered = find.ToString();
            rendered.Should().Be("find({ \"$expr\" : { \"$eq\" : [{ \"$indexOfCP\" : [{ \"$toLower\" : \"$Text\" }, { \"$toLower\" : \"$Match\" }] }, 0] } })");

            var results = find.ToList();
            results.Count.Should().Be(1);
            results[0].Id.Should().Be(100);
        }

        [Theory]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public void Should_throw_not_supported_exception_for_starts_with_with_unsupported_string_comparison_type(StringComparison comparison)
        {
            var collection = GetCollection<C>();

            var exception = Record.Exception(() => collection.Find(x => x.Text.StartsWith("orange", comparison)).ToList());

            exception.Should().BeOfType<ExpressionNotSupportedException>();
        }

#if NETCOREAPP3_1_OR_GREATER
        [Fact]
        public void String_contains_with_current_culture_ignore_case_should_work()
        {
            var collection = GetCollection<C>();
            collection.Database.DropCollection(collection.CollectionNamespace.CollectionName);

            collection.InsertMany(
                new[]
                {
                    new C { Id = 100, Text = "Apple-Orange-Banana", Match = "orange" },
                    new C { Id = 101, Text = "Apple-Kiwi-Pear", Match = "mango" }
                });

            var find = collection.Find(x => x.Text.Contains(x.Match, StringComparison.CurrentCultureIgnoreCase));

            var rendered = find.ToString();
            rendered.Should().Be("find({ \"$expr\" : { \"$gte\" : [{ \"$indexOfCP\" : [{ \"$toLower\" : \"$Text\" }, { \"$toLower\" : \"$Match\" }] }, 0] } })");

            var results = find.ToList();
            results.Count.Should().Be(1);
            results[0].Id.Should().Be(100);
        }

        [Theory]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public void Should_throw_not_supported_exception_for_contains_with_unsupported_string_comparison_type(StringComparison comparison)
        {
            var collection = GetCollection<C>();

            var exception = Record.Exception(() => collection.Find(x => x.Text.Contains("orange", comparison)).ToList());

            exception.Should().BeOfType<ExpressionNotSupportedException>();
        }
#endif

        [Fact]
        public void String_ends_with_with_current_culture_ignore_case_should_work()
        {
            var collection = GetCollection<C>();
            collection.Database.DropCollection(collection.CollectionNamespace.CollectionName);

            collection.InsertMany(
                new[]
                {
                    new C { Id = 100, Text = "Apple-Orange-Banana", Match = "banana" },
                    new C { Id = 101, Text = "Apple-Kiwi-Pear", Match = "mango" }
                });

            var find = collection.Find(x => x.Text.EndsWith(x.Match, StringComparison.CurrentCultureIgnoreCase));

            var rendered = find.ToString();
            rendered.Should().Be("find({ \"$expr\" : { \"$let\" : { \"vars\" : { \"string\" : { \"$toLower\" : \"$Text\" }, \"substring\" : { \"$toLower\" : \"$Match\" } }, \"in\" : { \"$let\" : { \"vars\" : { \"start\" : { \"$subtract\" : [{ \"$strLenCP\" : \"$$string\" }, { \"$strLenCP\" : \"$$substring\" }] } }, \"in\" : { \"$and\" : [{ \"$gte\" : [\"$$start\", 0] }, { \"$eq\" : [{ \"$indexOfCP\" : [\"$$string\", \"$$substring\", \"$$start\"] }, \"$$start\"] }] } } } } } })");

            var results = find.ToList();
            results.Count.Should().Be(1);
            results[0].Id.Should().Be(100);
        }

        [Theory]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public void Should_throw_not_supported_exception_for_ends_with_with_unsupported_string_comparison_type(StringComparison comparison)
        {
            var collection = GetCollection<C>();

            var exception = Record.Exception(() => collection.Find(x => x.Text.EndsWith("orange", comparison)).ToList());

            exception.Should().BeOfType<ExpressionNotSupportedException>();
        }

        public class C
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public string Match { get; set; }
        }
    }
}

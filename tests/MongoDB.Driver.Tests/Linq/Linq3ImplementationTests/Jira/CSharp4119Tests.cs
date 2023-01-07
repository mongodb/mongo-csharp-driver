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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4119Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Compare_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $cmp : ['$A', '$B'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_ignoreCase_should_work([Values(false, true)] bool ignoreCase)
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.B, ignoreCase));

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ {(ignoreCase ? "$strcasecmp" : "$cmp")} : ['$A', '$B'] }}, _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(ignoreCase ? new[] { 0, 0 } : new[] { 1, 0 });
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_ignoreCase_and_culture_should_work([Values(false, true)] bool ignoreCase)
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.B, ignoreCase, CultureInfo.CurrentCulture));

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ {(ignoreCase ? "$strcasecmp" : "$cmp")} : ['$A', '$B'] }}, _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(ignoreCase ? new[] { 0, 0 } : new[] { 1, 0 });
        }

        [Fact]
        public void Compare_with_ignoreCase_and_culture_should_throw_when_culture_is_invalid()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.B, false, CultureInfoHelper.GetNotCurrentCulture()));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because culture must be CultureInfo.CurrentCulture");
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_culture_and_options_should_work([Values(CompareOptions.None, CompareOptions.IgnoreCase)] CompareOptions options)
        {
            var collection = CreateCollection();
            var ignoreCase = options == CompareOptions.IgnoreCase;

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.B, CultureInfo.CurrentCulture, options));

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ {(ignoreCase ? "$strcasecmp" : "$cmp")} : ['$A', '$B'] }}, _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(ignoreCase ? new[] { 0, 0 } : new[] { 1, 0 });
        }

        [Fact]
        public void Compare_with_culture_and_options_should_throw_when_culture_is_invalid()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.B, CultureInfoHelper.GetNotCurrentCulture(), CompareOptions.None));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because culture must be CultureInfo.CurrentCulture");
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_culture_and_options_should_throw_when_options_is_invalid(
            [Values(CompareOptions.IgnoreKanaType, CompareOptions.IgnoreNonSpace, CompareOptions.IgnoreSymbols, CompareOptions.IgnoreWidth, CompareOptions.Ordinal, CompareOptions.OrdinalIgnoreCase, CompareOptions.StringSort)]
            CompareOptions options)
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.B, CultureInfo.CurrentCulture, options));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because options must be CompareOptions.None or CompareOptions.IgnoreCase");
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_comparisonType_should_work(
            [Values(StringComparison.CurrentCulture, StringComparison.CurrentCultureIgnoreCase)]
            StringComparison comparisonType)
        {
            var collection = CreateCollection();
            var ignoreCase = comparisonType == StringComparison.CurrentCultureIgnoreCase;

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.B, comparisonType));

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ {(ignoreCase ? "$strcasecmp" : "$cmp")} : ['$A', '$B'] }}, _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(ignoreCase ? new[] { 0, 0 } : new[] { 1, 0 });
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_comparisonType_should_throw_when_comparisonType_is_invalid(
            [Values(StringComparison.InvariantCulture, StringComparison.InvariantCultureIgnoreCase, StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase)]
            StringComparison comparisonType)
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.B, comparisonType));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because comparisonType must be StringComparison.CurrentCulture or StringComparison.CurrentCultureIgnoreCase");
        }

        [Fact]
        public void Compare_with_indexes_and_length_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.I, x.B, x.J, x.L));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $cmp : [{ $substrCP : ['$A', '$I', '$L'] }, { $substrCP : ['$B', '$J', '$L'] }] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_indexes_and_length_and_ignoreCase_should_work([Values(false, true)] bool ignoreCase)
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.I, x.B, x.J, x.L, ignoreCase));

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ {(ignoreCase ? "$strcasecmp" : "$cmp")} : [{{ $substrCP : ['$A', '$I', '$L'] }}, {{ $substrCP : ['$B', '$J', '$L'] }}] }}, _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(ignoreCase ? new[] { 0, 0 } : new[] { 1, 0 });
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_indexes_and_length_and_ignoreCase_and_culture_should_work([Values(false, true)] bool ignoreCase)
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.I, x.B, x.J, x.L, ignoreCase, CultureInfo.CurrentCulture));

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ {(ignoreCase ? "$strcasecmp" : "$cmp")} : [{{ $substrCP : ['$A', '$I', '$L'] }}, {{ $substrCP : ['$B', '$J', '$L'] }}] }}, _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(ignoreCase ? new[] { 0, 0 } : new[] { 1, 0 });
        }

        [Fact]
        public void Compare_with_indexes_and_length_and_ignoreCase_and_culture_should_throw_when_culture_is_invalid()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.I, x.B, x.J, x.L, false, CultureInfoHelper.GetNotCurrentCulture()));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because culture must be CultureInfo.CurrentCulture");
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_indexes_and_length_and_culture_and_options_should_work([Values(CompareOptions.None, CompareOptions.IgnoreCase)] CompareOptions options)
        {
            var collection = CreateCollection();
            var ignoreCase = options == CompareOptions.IgnoreCase;

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.I, x.B, x.J, x.L, CultureInfo.CurrentCulture, options));

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ {(ignoreCase ? "$strcasecmp" : "$cmp")} : [{{ $substrCP : ['$A', '$I', '$L'] }}, {{ $substrCP : ['$B', '$J', '$L'] }}] }}, _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(ignoreCase ? new[] { 0, 0 } : new[] { 1, 0 });
        }

        [Fact]
        public void Compare_with_indexes_and_length_and_culture_and_options_should_throw_when_culture_is_invalid()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.I, x.B, x.J, x.L, CultureInfoHelper.GetNotCurrentCulture(), CompareOptions.None));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because culture must be CultureInfo.CurrentCulture");
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_indexes_and_length_and_culture_and_options_should_throw_when_options_is_invalid(
            [Values(CompareOptions.IgnoreKanaType, CompareOptions.IgnoreNonSpace, CompareOptions.IgnoreSymbols, CompareOptions.IgnoreWidth, CompareOptions.Ordinal, CompareOptions.OrdinalIgnoreCase, CompareOptions.StringSort)]
            CompareOptions options)
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.I, x.B, x.J, x.L, CultureInfo.CurrentCulture, options));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because options must be CompareOptions.None or CompareOptions.IgnoreCase");
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_indexes_and_length_and_comparisonType_should_work(
            [Values(StringComparison.CurrentCulture, StringComparison.CurrentCultureIgnoreCase)]
            StringComparison comparisonType)
        {
            var collection = CreateCollection();
            var ignoreCase = comparisonType == StringComparison.CurrentCultureIgnoreCase;

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.I, x.B, x.J, x.L, comparisonType));

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $project : {{ _v : {{ {(ignoreCase ? "$strcasecmp" : "$cmp")} : [{{ $substrCP : ['$A', '$I', '$L'] }}, {{ $substrCP : ['$B', '$J', '$L'] }}] }}, _id : 0 }} }}");

            var results = queryable.ToList();
            results.Should().Equal(ignoreCase ? new[] { 0, 0 } : new[] { 1, 0 });
        }

        [Theory]
        [ParameterAttributeData]
        public void Compare_with_indexes_and_length_and_comparisonType_should_throw_when_comparisonType_is_invalid(
            [Values(StringComparison.InvariantCulture, StringComparison.InvariantCultureIgnoreCase, StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase)]
            StringComparison comparisonType)
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Compare(x.A, x.I, x.B, x.J, x.L, comparisonType));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because comparisonType must be StringComparison.CurrentCulture or StringComparison.CurrentCultureIgnoreCase");
        }

        [Fact]
        public void CompareTo_with_object_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => x.A.CompareTo((object)x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $cmp : ['$A', '$B'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 0);
        }

        [Fact]
        public void CompareTo_with_string_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => x.A.CompareTo(x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $cmp : ['$A', '$B'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 0);
        }

        [Fact]
        public void Equals_with_object_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => x.A.Equals((object)x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $eq : ['$A', '$B'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(false, true);
        }

        [Fact]
        public void Equals_with_string_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => x.A.Equals(x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $eq : ['$A', '$B'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(false, true);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_with_string_and_comparisonType_should_work(
            [Values(StringComparison.CurrentCulture, StringComparison.CurrentCultureIgnoreCase)]
            StringComparison comparisonType)
        {
            var collection = CreateCollection();
            var ignoreCase = comparisonType == StringComparison.CurrentCultureIgnoreCase;

            var queryable =
                collection.AsQueryable()
                .Select(x => x.A.Equals(x.B, comparisonType));

            var stages = Translate(collection, queryable);
            var expectedStage = ignoreCase ?
                "{ $project : { _v : { $eq : [{ $strcasecmp : ['$A', '$B'] }, 0] }, _id : 0 } }" :
                "{ $project : { _v : { $eq : ['$A', '$B'] }, _id : 0 } }";
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(ignoreCase ? new[] { true, true } : new[] { false, true });
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_with_string_and_comparisonType_should_throw_when_comparisonType_is_invalid(
            [Values(StringComparison.InvariantCulture, StringComparison.InvariantCultureIgnoreCase, StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase)]
            StringComparison comparisonType)
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => x.A.Equals(x.B, comparisonType));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because comparisonType must be StringComparison.CurrentCulture or StringComparison.CurrentCultureIgnoreCase");
        }

        [Fact]
        public void Equals_with_two_strings_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Equals(x.A, x.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $eq : ['$A', '$B'] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(false, true);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_with_two_strings_and_comparisonType_should_work(
            [Values(StringComparison.CurrentCulture, StringComparison.CurrentCultureIgnoreCase)]
            StringComparison comparisonType)
        {
            var collection = CreateCollection();
            var ignoreCase = comparisonType == StringComparison.CurrentCultureIgnoreCase;

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Equals(x.A, x.B, comparisonType));

            var stages = Translate(collection, queryable);
            var expectedStage = ignoreCase ?
                "{ $project : { _v : { $eq : [{ $strcasecmp : ['$A', '$B'] }, 0] }, _id : 0 } }" :
                "{ $project : { _v : { $eq : ['$A', '$B'] }, _id : 0 } }";
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(ignoreCase ? new[] { true, true } : new[] { false, true });
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_with_two_strings_and_comparisonType_should_throw_when_comparisonType_is_invalid(
            [Values(StringComparison.InvariantCulture, StringComparison.InvariantCultureIgnoreCase, StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase)]
            StringComparison comparisonType)
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => string.Equals(x.A, x.B, comparisonType));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because comparisonType must be StringComparison.CurrentCulture or StringComparison.CurrentCultureIgnoreCase");
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");

            CreateCollection(
                collection,
                new C { Id = 1, A = "a", B = "A", I = 0, J = 0, L = 1 },
                new C { Id = 2, A = "A", B = "A", I = 0, J = 0, L = 1 });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public string A { get; set; }
            public string B { get; set; }
            public int I { get; set; }
            public int J { get; set; }
            public int L { get; set; }
        }
    }
}

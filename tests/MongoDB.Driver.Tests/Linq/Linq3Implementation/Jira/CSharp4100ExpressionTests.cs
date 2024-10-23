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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4100ExpressionTests : Linq3IntegrationTest
    {
        [Fact]
        public void Contains_with_string_field_and_char_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.Contains('A') });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $gte : [{ $indexOfCP : ['$S', 'A'] }, 0] }, _id : 0 } }");
        }

        [Fact]
        public void Contains_with_string_constant_and_char_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".Contains('A') });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }");
        }

        [Fact]
        public void Contains_with_string_field_and_char_field_represented_as_string_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.Contains(x.CS) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $gte : [{ $indexOfCP : ['$S', '$CS'] }, 0] }, _id : 0 } }");
        }

        [Fact]
        public void Contains_with_string_constant_and_char_field_represented_as_string_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".Contains(x.CS) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $gte : [{ $indexOfCP : ['ABC', '$CS'] }, 0] }, _id : 0 } }");
        }

        [Theory]
        [ParameterAttributeData]
        public void Contains_with_string_field_and_char_field_not_represented_as_string_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.S.Contains(x.CC) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var serializer);
                AssertStages(stages, Array.Empty<string>());
                serializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("it is not serialized as a string");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Contains_with_string_constant_and_char_field_not_represented_as_string_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = "ABC".Contains(x.CC) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var serializer);
                AssertStages(stages, Array.Empty<string>());
                serializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("it is not serialized as a string");
            }
        }

#if !NETFRAMEWORK
        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $gte : [{ $indexOfCP : ['$S', 'A'] }, 0] }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $gte : [{ $indexOfCP : [{ $toLower : '$S' }, 'a'] }, 0] }, _id : 0 } }")]
        public void Contains_with_string_field_and_char_constant_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.Contains('A', comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        public void Contains_with_string_constant_and_char_constant_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".Contains('A', comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $gte : [{ $indexOfCP : ['$S', '$CS'] }, 0] } , _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $gte : [{ $indexOfCP : [{ $toLower :'$S' }, { $toLower : '$CS' }] }, 0] } , _id : 0 } }")]
        public void Contains_with_string_field_and_char_field_represented_as_string_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.Contains(x.CS, comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $gte : [{ $indexOfCP : ['ABC', '$CS'] }, 0] } , _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $gte : [{ $indexOfCP : ['abc', { $toLower : '$CS' }] }, 0] } , _id : 0 } }")]
        public void Contains_with_string_constant_and_char_field_represented_as_string_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".Contains(x.CS, comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [ParameterAttributeData]
        public void Contains_with_string_field_and_char_value_not_represented_as_string_and_comparisonType_should_throw(
            [Values(StringComparison.CurrentCulture, StringComparison.CurrentCultureIgnoreCase)]
            StringComparison comparisonType,
            [Values(false, true)]
            bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.S.Contains(x.CC, comparisonType) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("it is not serialized as a string");
            }
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [ParameterAttributeData]
        public void Contains_with_string_constant_and_char_value_not_represented_as_string_and_comparisonType_should_throw(
            [Values(StringComparison.CurrentCulture, StringComparison.CurrentCultureIgnoreCase)]
            StringComparison comparisonType,
            [Values(false, true)]
            bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = "ABC".Contains(x.CC, comparisonType) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("it is not serialized as a string");
            }
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [ParameterAttributeData]
        public void Contains_with_string_field_and_char_value_and_invalid_comparisonType_should_throw(
            [Values(
                StringComparison.InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase,
                StringComparison.Ordinal,
                StringComparison.OrdinalIgnoreCase)]
            StringComparison comparisonType,
            [Values(false, true)]
            bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.S.Contains('A', comparisonType) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain($"{comparisonType} is not supported");
            }
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public void Contains_with_string_constant_and_char_value_and_invalid_comparisonType_should_throw(StringComparison comparisonType)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".Contains('A', comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }");
        }
#endif

        [Fact]
        public void Contains_with_string_field_and_string_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.Contains("aBc") });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $gte : [{ $indexOfCP : ['$S', 'aBc'] }, 0] }, _id : 0 } }");
        }

        [Fact]
        public void Contains_with_string_constant_and_string_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".Contains("aBc") });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }");
        }

        [Fact]
        public void Contains_with_string_field_and_string_field_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.Contains(x.T) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $gte : [{ $indexOfCP : ['$S', '$T'] }, 0] }, _id : 0 } }");
        }

        [Fact]
        public void Contains_with_string_constant_and_string_field_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".Contains(x.T) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $gte : [{ $indexOfCP : ['ABC', '$T'] }, 0] }, _id : 0 } }");
        }

#if !NETFRAMEWORK
        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $gte : [{ $indexOfCP : ['$S', 'aBc'] }, 0] }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $gte : [{ $indexOfCP : [{ $toLower : '$S' }, 'abc'] }, 0] }, _id : 0 } }")]
        public void Contains_with_string_field_and_string_constant_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.Contains("aBc", comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { _v : { $literal : { R :  false } }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        public void Contains_with_string_constant_and_string_constant_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".Contains("aBc", comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $gte : [{ $indexOfCP : ['$S', '$T'] }, 0] }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $gte : [{ $indexOfCP : [{ $toLower : '$S' }, { $toLower : '$T' }] }, 0] }, _id : 0 } }")]
        public void Contains_with_string_field_and_string_field_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.Contains(x.T, comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $gte : [{ $indexOfCP : ['ABC', '$T'] }, 0] }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $gte : [{ $indexOfCP : ['abc', { $toLower : '$T' }] }, 0] }, _id : 0 } }")]
        public void Contains_with_string_constant_and_string_field_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".Contains(x.T, comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [ParameterAttributeData]
        public void Contains_with_string_field_and_string_value_and_invalid_comparisonType_should_throw(
            [Values(
                StringComparison.InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase,
                StringComparison.Ordinal,
                StringComparison.OrdinalIgnoreCase)]
            StringComparison comparisonType,
            [Values(false, true)]
            bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.S.Contains("aBc", comparisonType) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain($"{comparisonType} is not supported");
            }
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [InlineData(StringComparison.InvariantCulture, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }")]
        [InlineData(StringComparison.InvariantCultureIgnoreCase, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        [InlineData(StringComparison.Ordinal, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }")]
        [InlineData(StringComparison.OrdinalIgnoreCase, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        public void Contains_with_string_constant_and_string_value_and_invalid_comparisonType_should_throw(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".Contains("aBc", comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }
#endif

#if !NETFRAMEWORK
        [Fact]
        public void EndsWith_with_string_field_and_char_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.EndsWith('A') });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$S' }, 1] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$S', 'A', '$$start'] }, '$$start'] }] } } }, _id : 0 } }");
        }
#endif

#if !NETFRAMEWORK
        [Fact]
        public void EndsWith_with_string_constant_and_char_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".EndsWith('A') });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }");
        }
#endif

#if !NETFRAMEWORK
        [Fact]
        public void EndsWith_with_string_field_and_char_field_represented_as_string_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.EndsWith(x.CS) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$S' }, { $strLenCP : '$CS' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$S', '$CS', '$$start'] }, '$$start'] }] } } }, _id : 0 } }");
        }
#endif

#if !NETFRAMEWORK
        [Fact]
        public void EndsWith_with_string_constant_and_char_field_represented_as_string_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".EndsWith(x.CS) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $let : { vars : { start : { $subtract : [3, { $strLenCP : '$CS' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['ABC', '$CS', '$$start'] }, '$$start'] }] } } }, _id : 0 } }");
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [ParameterAttributeData]
        public void EndsWith_with_string_field_and_char_field_not_represented_as_string_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.S.EndsWith(x.CC) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("it is not serialized as a string");
            }
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [ParameterAttributeData]
        public void EndsWith_with_string_constant_and_char_field_not_represented_as_string_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = "ABC".EndsWith(x.CC) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("it is not serialized as a string");
            }
        }
#endif

        [Fact]
        public void EndsWith_with_string_field_and_string_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.EndsWith("aBc") });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$S' }, 3] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$S', 'aBc', '$$start'] }, '$$start'] }] } } }, _id : 0 } }");
        }

        [Fact]
        public void EndsWith_with_string_constant_and_string_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".EndsWith("aBc") });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }");
        }

        [Fact]
        public void EndsWith_with_string_field_and_string_field_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.EndsWith(x.T) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$S' }, { $strLenCP : '$T' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$S', '$T', '$$start'] }, '$$start'] }] } } }, _id : 0 } }");
        }

        [Fact]
        public void EndsWith_with_string_constant_and_string_field_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".EndsWith(x.T) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $let : { vars : { start : { $subtract : [3, { $strLenCP : '$T' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['ABC', '$T', '$$start'] }, '$$start'] }] } } }, _id : 0 } }");
        }

        [Theory]
        [InlineData(false, "{ $project : { R : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$S' }, 3] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$S', 'aBc', '$$start'] }, '$$start'] }] } } }, _id : 0 } }")]
        [InlineData(true, "{ $project : { R : { $let : { vars : { string : { $toLower : '$S' } }, in : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$$string' }, 3] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$$string', 'abc', '$$start'] }, '$$start'] }] } } } } }, _id : 0 } }")]
        public void EndsWith_with_string_field_and_string_constant_and_ignoreCase_and_culture_should_work(bool ignoreCase, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.EndsWith("aBc", ignoreCase, CultureInfo.CurrentCulture) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(false, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }")]
        [InlineData(true, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        public void EndsWith_with_string_constant_and_string_constant_and_ignoreCase_and_culture_should_work(bool ignoreCase, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".EndsWith("aBc", ignoreCase, CultureInfo.CurrentCulture) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(false, "{ $project : { R : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$S' }, { $strLenCP : '$T' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$S', '$T', '$$start'] }, '$$start'] }] } } }, _id : 0 } }")]
        [InlineData(true, "{ $project : { R : { $let : { vars : { string : { $toLower : '$S' }, substring : { $toLower : '$T' } }, in : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$$string' }, { $strLenCP : '$$substring' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$$string', '$$substring', '$$start'] }, '$$start'] }] } } } } }, _id : 0 } }")]
        public void EndsWith_with_string_field_and_string_field_and_ignoreCase_and_culture_should_work(bool ignoreCase, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.EndsWith(x.T, ignoreCase, CultureInfo.CurrentCulture) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(false, "{ $project : { R : { $let : { vars : { start : { $subtract : [3, { $strLenCP : '$T' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['ABC', '$T', '$$start'] }, '$$start'] }] } } }, _id : 0 } }")]
        [InlineData(true, "{ $project : { R : { $let : { vars : { substring : { $toLower : '$T' } }, in : { $let : { vars : { start : { $subtract : [3, { $strLenCP : '$$substring' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['abc', '$$substring', '$$start'] }, '$$start'] }] } } } } }, _id : 0 } }")]
        public void EndsWith_with_string_constant_and_string_field_and_ignoreCase_and_culture_should_work(bool ignoreCase, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".EndsWith(x.T, ignoreCase, CultureInfo.CurrentCulture) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [ParameterAttributeData]
        public void EndsWith_with_string_field_and_string_value_and_ignoreCase_and_invalid_culture_should_throw(
            [Values(false, true)] bool ignoreCase,
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };
            var notCurrentCulture = GetACultureThatIsNotTheCurrentCulture();

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.S.EndsWith("aBc", ignoreCase, notCurrentCulture) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain($"the supplied culture is not the current culture");
            }
        }

        [Theory]
        [InlineData(false, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }")]
        [InlineData(true, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        public void EndsWith_with_string_constant_and_string_value_and_ignoreCase_and_invalid_culture_should_throw(bool ignoreCase, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".EndsWith("aBc", ignoreCase, CultureInfo.InvariantCulture) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$S' }, 3] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$S', 'aBc', '$$start'] }, '$$start'] }] } } }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $let : { vars : { string : { $toLower : '$S' } }, in : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$$string' }, 3] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$$string', 'abc', '$$start'] }, '$$start'] }] } } } } }, _id : 0  } }")]
        public void EndsWith_with_string_field_and_string_constant_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.EndsWith("aBc", comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        public void EndsWith_with_string_constant_and_string_constant_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".EndsWith("aBc", comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$S' }, { $strLenCP : '$T' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$S', '$T', '$$start'] }, '$$start'] }] } } }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $let : { vars : { string : { $toLower : '$S' }, substring : { $toLower : '$T' } }, in : { $let : { vars : { start : { $subtract : [{ $strLenCP : '$$string' }, { $strLenCP : '$$substring' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['$$string', '$$substring', '$$start'] }, '$$start'] }] } } } } }, _id : 0 } }")]
        public void EndsWith_with_string_field_and_string_field_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.EndsWith(x.T, comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $let : { vars : { start : { $subtract : [3, { $strLenCP : '$T' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['ABC', '$T', '$$start'] }, '$$start'] }] } } }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $let : { vars : { substring : { $toLower : '$T' } }, in : { $let : { vars : { start : { $subtract : [3, { $strLenCP : '$$substring' }] } }, in : { $and : [{ $gte : ['$$start', 0] }, { $eq : [{ $indexOfCP : ['abc', '$$substring', '$$start'] }, '$$start'] }] } } } } }, _id : 0 } }")]
        public void EndsWith_with_string_constant_and_string_field_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".EndsWith(x.T, comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [ParameterAttributeData]
        public void EndsWith_with_string_field_and_string_value_and_invalid_comparisonType_should_throw(
            [Values(
                StringComparison.InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase,
                StringComparison.Ordinal,
                StringComparison.OrdinalIgnoreCase)]
            StringComparison comparisonType,
            [Values(false, true)]
            bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.S.EndsWith(x.T, comparisonType) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain($"{comparisonType} is not supported");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void EndsWith_with_string_constant_and_string_value_and_invalid_comparisonType_should_throw(
            [Values(
                StringComparison.InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase,
                StringComparison.Ordinal,
                StringComparison.OrdinalIgnoreCase)]
            StringComparison comparisonType,
            [Values(false, true)]
            bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = "ABC".EndsWith(x.T, comparisonType) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain($"{comparisonType} is not supported");
            }
        }

#if !NETFRAMEWORK
        [Fact]
        public void StartsWith_with_string_field_and_char_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.StartsWith('A') });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $eq : [{ $indexOfCP : ['$S', 'A'] }, 0] }, _id : 0 } }");
        }
#endif

#if !NETFRAMEWORK
        [Fact]
        public void StartsWith_with_string_constant_and_char_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".StartsWith('A') });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }");
        }
#endif

#if !NETFRAMEWORK
        [Fact]
        public void StartsWith_with_string_field_and_char_field_represented_as_string_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.StartsWith(x.CS) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $eq : [{ $indexOfCP : ['$S', '$CS'] }, 0] }, _id : 0 } }");
        }
#endif

#if !NETFRAMEWORK
        [Fact]
        public void StartsWith_with_string_constant_and_char_field_represented_as_string_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".StartsWith(x.CS) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $eq : [{ $indexOfCP : ['ABC', '$CS'] }, 0] }, _id : 0 } }");
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [ParameterAttributeData]
        public void StartsWith_with_string_field_and_char_field_not_represented_as_string_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.S.StartsWith(x.CC) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("it is not serialized as a string");
            }
        }
#endif

#if !NETFRAMEWORK
        [Theory]
        [ParameterAttributeData]
        public void StartsWith_with_string_constant_and_char_field_not_represented_as_string_should_throw(
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = "ABC".StartsWith(x.CC) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("it is not serialized as a string");
            }
        }
#endif

        [Fact]
        public void StartsWith_with_string_field_and_string_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.StartsWith("aBc") });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $eq : [{ $indexOfCP : ['$S', 'aBc'] }, 0] }, _id : 0 } }");
        }

        [Fact]
        public void StartsWith_with_string_constant_and_string_constant_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".StartsWith("aBc") });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }");
        }

        [Fact]
        public void StartsWith_with_string_field_and_string_field_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.StartsWith(x.T) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $eq : [{ $indexOfCP : ['$S', '$T'] }, 0] }, _id : 0 } }");
        }

        [Fact]
        public void StartsWith_with_string_constant_and_string_field_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".StartsWith(x.T) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { R : { $eq : [{ $indexOfCP : ['ABC', '$T'] }, 0] }, _id : 0 } }");
        }

        [Theory]
        [InlineData(false, "{ $project : { R : { $eq : [{ $indexOfCP : ['$S', 'aBc'] }, 0] }, _id : 0 } }")]
        [InlineData(true, "{ $project : { R : { $eq : [{ $indexOfCP : [{ $toLower : '$S' }, 'abc'] }, 0] }, _id : 0 } }")]
        public void StartsWith_with_string_field_and_string_constant_and_ignoreCase_and_culture_should_work(bool ignoreCase, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.StartsWith("aBc", ignoreCase, CultureInfo.CurrentCulture) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(false, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }")]
        [InlineData(true, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        public void StartsWith_with_string_constant_and_string_constant_and_ignoreCase_and_culture_should_work(bool ignoreCase, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".StartsWith("aBc", ignoreCase, CultureInfo.CurrentCulture) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(false, "{ $project : { R : { $eq : [{ $indexOfCP : ['$S', '$T'] }, 0] }, _id : 0 } }")]
        [InlineData(true, "{ $project : { R : { $eq : [{ $indexOfCP : [{ $toLower : '$S' }, { $toLower : '$T' }] }, 0] }, _id : 0 } }")]
        public void StartsWith_with_string_field_and_string_field_and_ignoreCase_and_culture_should_work(bool ignoreCase, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.StartsWith(x.T, ignoreCase, CultureInfo.CurrentCulture) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(false, "{ $project : { R : { $eq : [{ $indexOfCP : ['ABC', '$T'] }, 0] }, _id : 0 } }")]
        [InlineData(true, "{ $project : { R : { $eq : [{ $indexOfCP : ['abc', { $toLower : '$T' }] }, 0] }, _id : 0 } }")]
        public void StartsWith_with_string_constant_and_string_field_and_ignoreCase_and_culture_should_work(bool ignoreCase, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".StartsWith(x.T, ignoreCase, CultureInfo.CurrentCulture) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [ParameterAttributeData]
        public void StartsWith_with_string_field_and_string_value_and_ignoreCase_and_invalid_culture_should_throw(
            [Values(false, true)] bool ignoreCase,
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };
            var notCurrentCulture = GetACultureThatIsNotTheCurrentCulture();

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.S.StartsWith("aBc", ignoreCase, notCurrentCulture) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain($"the supplied culture is not the current culture");
            }
        }

        [Theory]
        [InlineData(false, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }")]
        [InlineData(true, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        public void StartsWith_with_string_constant_and_string_value_and_ignoreCase_and_invalid_culture_should_throw(bool ignoreCase, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".StartsWith("aBc", ignoreCase, CultureInfo.InvariantCulture) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $eq : [{ $indexOfCP : ['$S', 'aBc'] }, 0] }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $eq : [{ $indexOfCP : [{ $toLower : '$S' }, 'abc'] }, 0] }, _id : 0  } }")]
        public void StartsWith_with_string_field_and_string_constant_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.StartsWith("aBc", comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { _v : { $literal : { R : false } }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { _v : { $literal : { R : true } }, _id : 0 } }")]
        public void StartsWith_with_string_constant_and_string_constant_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".StartsWith("aBc", comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $eq : [{ $indexOfCP : ['$S', '$T'] }, 0] }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $eq : [{ $indexOfCP : [{ $toLower : '$S' }, { $toLower : '$T' }] }, 0] }, _id : 0 } }")]
        public void StartsWith_with_string_field_and_string_field_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = x.S.StartsWith(x.T, comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture, "{ $project : { R : { $eq : [{ $indexOfCP : ['ABC', '$T'] }, 0] }, _id : 0 } }")]
        [InlineData(StringComparison.CurrentCultureIgnoreCase, "{ $project : { R : { $eq : [{ $indexOfCP : ['abc', { $toLower : '$T' }] }, 0] }, _id : 0 } }")]
        public void StartsWith_with_string_constant_and_string_field_and_comparisonType_should_work(StringComparison comparisonType, string expectedStage)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                .Select(x => new { R = "ABC".StartsWith(x.T, comparisonType) });

            var stages = Translate(collection, queryable);

            AssertStages(stages, expectedStage);
        }

        [Theory]
        [ParameterAttributeData]
        public void StartsWith_with_string_field_and_string_value_and_invalid_comparisonType_should_throw(
            [Values(
                StringComparison.InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase,
                StringComparison.Ordinal,
                StringComparison.OrdinalIgnoreCase)]
            StringComparison comparisonType,
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = x.S.StartsWith(x.T, comparisonType) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain($"{comparisonType} is not supported");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void StartsWith_with_string_constant_and_string_value_and_invalid_comparisonType_should_throw(
            [Values(
                StringComparison.InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase,
                StringComparison.Ordinal,
                StringComparison.OrdinalIgnoreCase)]
            StringComparison comparisonType,
            [Values(false, true)]
            bool enableClientSideProjections)
        {
            var collection = GetCollection();
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new { R = "ABC".StartsWith(x.T, comparisonType) });

            if (enableClientSideProjections)
            {
                var stages = Translate(collection, queryable, out var outputSerializer);
                AssertStages(stages, Array.Empty<string>());
                outputSerializer.Should().BeAssignableTo<IClientSideProjectionDeserializer>();

                var results = queryable.ToList();
                results.Should().BeEmpty();
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain($"{comparisonType} is not supported");
            }
        }

        private IMongoCollection<Test> GetCollection()
        {
            var collection = GetCollection<Test>();
            CreateCollection(collection);
            return collection;
        }

        private CultureInfo GetACultureThatIsNotTheCurrentCulture()
        {
            var notCurrentCulture = CultureInfo.GetCultureInfo("zu-ZA");
            if (notCurrentCulture.Equals(CultureInfo.CurrentCulture))
            {
                notCurrentCulture = CultureInfo.GetCultureInfo("yo-NG");
            }
            return notCurrentCulture;
        }

        public class Test
        {
            public char CC { get; set; }
            [BsonRepresentation(BsonType.String)] public char CS { get; set; }
            public string S { get; set; }
            public string T { get; set; }
        }
    }
}

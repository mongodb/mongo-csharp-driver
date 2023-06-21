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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    public class ConstantExpressionToFilterTranslatorTests
    {
        [Theory]
        [MemberData(nameof(SupportedConstantExpressions))]
        public void Translate_should_process_supported_constants(ConstantExpression expression, BsonValue expectedFilter)
        {
            var result = ConstantExpressionToFilterTranslator.Translate(null, expression);

            result.Render().Should().Be(expectedFilter);
        }

        [Theory]
        [MemberData(nameof(NotSupportedConstantExpressions))]
        public void Translate_throws_on_not_supported_constants(ConstantExpression expression)
        {
            var ex = Record.Exception(() => ConstantExpressionToFilterTranslator.Translate(null, expression));

            ex.Should().BeOfType<ExpressionNotSupportedException>();
        }

        public static IEnumerable<object[]> SupportedConstantExpressions()
        {
            yield return new object[] { Expression.Constant(true), AstFilter.MatchesEverything().Render() };
            yield return new object[] { Expression.Constant(false), AstFilter.MatchesNothing().Render() };
        }

        public static IEnumerable<object[]> NotSupportedConstantExpressions()
        {
            yield return new object[] { Expression.Constant(null) };
            yield return new object[] { Expression.Constant(1) };
            yield return new object[] { Expression.Constant(0) };
            yield return new object[] { Expression.Constant(string.Empty) };
            yield return new object[] { Expression.Constant("ignore") };
        }
    }
}

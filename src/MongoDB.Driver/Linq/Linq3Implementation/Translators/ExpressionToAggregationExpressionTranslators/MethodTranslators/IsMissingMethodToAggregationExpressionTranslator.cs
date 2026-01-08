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
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class IsMissingMethodToAggregationExpressionTranslator
    {
        private static readonly IReadOnlyMethodInfoSet __translatableOverloads = MethodInfoSet.Create(
        [
            MqlMethod.Exists,
            MqlMethod.IsMissing,
            MqlMethod.IsNullOrMissing,
        ]);

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__translatableOverloads))
            {
                var fieldExpression = arguments[0];
                var fieldTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, fieldExpression);
                if (!(fieldTranslation.Ast is AstGetFieldExpression fieldAst))
                {
                    throw new ExpressionNotSupportedException(expression, because: $"argument to {method.Name} must be a reference to a field");
                }

                var ast = method.Name switch
                {
                    nameof(Mql.Exists) => AstExpression.IsNotMissing(fieldAst),
                    nameof(Mql.IsMissing) => AstExpression.IsMissing(fieldAst),
                    nameof(Mql.IsNullOrMissing) => AstExpression.IsNullOrMissing(fieldAst),
                    _ => throw new InvalidOperationException("Unexpected method."),
                };

                return new TranslatedExpression(expression, ast, BooleanSerializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}

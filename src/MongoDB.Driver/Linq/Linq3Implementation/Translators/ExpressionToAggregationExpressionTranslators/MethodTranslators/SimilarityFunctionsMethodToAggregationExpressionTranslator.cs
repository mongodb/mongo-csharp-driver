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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

internal static class SimilarityFunctionsMethodToAggregationExpressionTranslator
{
    public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
    {
        var method = expression.Method;
        var arguments = expression.Arguments;

        if (method.IsOneOf(MqlMethod.SimilarityFunctionOverloads))
        {
            var vectors1Translation = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[0]);
            SerializationHelper.EnsureRepresentationIsArray(expression, vectors1Translation.Serializer);

            var vectors2Translation = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[1]);
            SerializationHelper.EnsureRepresentationIsArray(expression, vectors2Translation.Serializer);

            var normalizeTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[2]);
            SerializationHelper.EnsureRepresentationIsBoolean(expression, arguments[2], normalizeTranslation);

            var ast = method.Name switch
            {
                nameof(Mql.SimilarityCosine) => AstExpression.Cosine(
                    vectors1Translation.Ast, vectors2Translation.Ast, normalizeTranslation.Ast),
                nameof(Mql.SimilarityDotProduct) => AstExpression.DotProduct(
                    vectors1Translation.Ast, vectors2Translation.Ast, normalizeTranslation.Ast),
                nameof(Mql.SimilarityEuclidean) => AstExpression.Euclidean(
                    vectors1Translation.Ast, vectors2Translation.Ast, normalizeTranslation.Ast),
                _ => throw new ArgumentException($"Unexpected method name: {method.Name}.", nameof(method))
            };

            return new TranslatedExpression(expression, ast, DoubleSerializer.Instance);
        }

        throw new ExpressionNotSupportedException(expression);
    }
}

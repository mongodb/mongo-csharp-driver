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

using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class AppendOrPrependMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __appendOrPrependMethods =
        {
            EnumerableMethod.Append,
            EnumerableMethod.Prepend,
            QueryableMethod.Append,
            QueryableMethod.Prepend
        };

        private static readonly MethodInfo[] __appendMethods =
        {
            EnumerableMethod.Append,
            QueryableMethod.Append
        };

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__appendOrPrependMethods))
            {
                var sourceExpression = arguments[0];
                var elementExpression = arguments[1];

                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                TranslatedExpression elementTranslation;
                if (elementExpression is ConstantExpression elementConstantExpression)
                {
                    var value = elementConstantExpression.Value;
                    var serializedValue = SerializationHelper.SerializeValue(context.SerializationDomain, itemSerializer, value);
                    elementTranslation = new TranslatedExpression(elementExpression, AstExpression.Constant(serializedValue), itemSerializer);
                }
                else
                {
                    elementTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, elementExpression);
                    if (!elementTranslation.Serializer.Equals(itemSerializer))
                    {
                        throw new ExpressionNotSupportedException(expression, because: "argument serializers are not compatible");
                    }
                }

                var ast = method.IsOneOf(__appendMethods) ?
                    AstExpression.ConcatArrays(sourceTranslation.Ast, AstExpression.ComputedArray(elementTranslation.Ast)) :
                    AstExpression.ConcatArrays(AstExpression.ComputedArray(elementTranslation.Ast), sourceTranslation.Ast);
                var serializer = NestedAsQueryableSerializer.CreateIEnumerableOrNestedAsQueryableSerializer(expression.Type, itemSerializer);

                return new TranslatedExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}

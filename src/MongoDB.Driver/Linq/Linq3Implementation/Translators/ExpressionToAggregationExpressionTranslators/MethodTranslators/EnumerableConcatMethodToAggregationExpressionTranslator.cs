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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class EnumerableConcatMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __concatMethods =
        {
            EnumerableMethod.Concat,
            QueryableMethod.Concat
        };

        public static bool CanTranslate(MethodCallExpression expression)
            => expression.Method.IsOneOf(__concatMethods);

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression, IBsonSerializer targetSerializer)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__concatMethods))
            {
                var firstExpression = arguments[0];
                var firstTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, firstExpression, targetSerializer);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, firstTranslation);
                var secondExpression = arguments[1];
                var secondTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, secondExpression, firstTranslation.Serializer);
                var ast = AstExpression.ConcatArrays(firstTranslation.Ast, secondTranslation.Ast);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(firstTranslation.Serializer);
                var serializer = NestedAsQueryableSerializer.CreateIEnumerableOrNestedAsQueryableSerializer(expression.Type, itemSerializer);
                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}

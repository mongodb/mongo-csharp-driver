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
    internal static class SelectMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __selectMethods =
        {
            EnumerableMethod.Select,
            QueryableMethod.Select
        };

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__selectMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

                var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var selectorParameter = selectorLambda.Parameters[0];
                var selectorParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                var selectorParameterSymbol = context.CreateSymbol(selectorParameter, selectorParameterSerializer);
                var selectorContext = context.WithSymbol(selectorParameterSymbol);
                var translatedSelector = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorLambda.Body);

                var ast = AstExpression.Map(
                    sourceTranslation.Ast,
                    selectorParameterSymbol.Var,
                    translatedSelector.Ast);
                var serializer = NestedAsQueryableSerializer.CreateIEnumerableOrNestedAsQueryableSerializer(expression.Type, itemSerializer : translatedSelector.Serializer);

                return new TranslatedExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}

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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class OrderByMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(EnumerableOrQueryableMethod.OrderByOrThenByOverloads))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                if (method.IsOneOf(EnumerableOrQueryableMethod.ThenByOverloads))
                {
                    NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsOrderedQueryableSource(expression, sourceTranslation);
                }
                else
                {
                    NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);
                }
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                var orderedEnumerableSerializer = NestedAsOrderedQueryableSerializer.CreateIOrderedEnumerableOrNestedAsOrderedQueryableSerializer(expression.Type, itemSerializer);

                var keySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var order = GetOrder(method);

                if (IsIdentityLambda(keySelectorLambda))
                {
                    if (method.IsOneOf(EnumerableOrQueryableMethod.OrderByOverloads))
                    {
                        var ast = AstExpression.SortArray(sourceTranslation.Ast, order);
                        return new TranslatedExpression(expression, ast, orderedEnumerableSerializer);
                    }

                    throw new ExpressionNotSupportedException(keySelectorLambda, expression, because: "ThenBy and ThenByDescending cannot be used to sort on the entire object");
                }

                var sortFieldPath = keySelectorLambda.TranslateToDottedFieldName(context, itemSerializer);
                var sortField = AstSort.Field(sortFieldPath, order);

                if (method.IsOneOf(EnumerableOrQueryableMethod.OrderByOverloads))
                {
                    var ast = AstExpression.SortArray(sourceTranslation.Ast, sortField);
                    return new TranslatedExpression(expression, ast, orderedEnumerableSerializer);
                }

                if (method.IsOneOf(EnumerableOrQueryableMethod.ThenByOverloads))
                {
                    if (sourceTranslation.Ast is AstSortArrayExpression originalAst)
                    {
                        if (originalAst.Order != null)
                        {
                            throw new ExpressionNotSupportedException(expression, because: "ThenBy and ThenByDescending cannot be used when OrderBy or OrderByDescending is sorting on the entire object");
                        }

                        var combinedSortFields = originalAst.Fields.AddSortField(sortField);
                        var ast = AstExpression.SortArray(originalAst.Input, combinedSortFields);
                        return new TranslatedExpression(expression, ast, orderedEnumerableSerializer);
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstSortOrder GetOrder(MethodInfo method)
        {
            return method.Name switch
            {
                "OrderBy" => AstSortOrder.Ascending,
                "OrderByDescending" => AstSortOrder.Descending,
                "ThenBy" => AstSortOrder.Ascending,
                "ThenByDescending" => AstSortOrder.Descending,
                _ => throw new InvalidOperationException($"Invalid method: {method.Name}.")
            };
        }

        private static bool IsIdentityLambda(LambdaExpression lambdaExpression)
        {
            return
                lambdaExpression.Parameters.Count == 1 &&
                lambdaExpression.Body == lambdaExpression.Parameters[0];
        }
    }
}

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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class GetItemMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsEnumerableGetItemMethodWithIntIndex(expression, out var sourceExpression, out var indexExpression))
            {
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
                var indexTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, indexExpression);
                var ast = AstExpression.ArrayElemAt(sourceTranslation.Ast, indexTranslation.Ast);
                var serializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                return new AggregationExpression(expression, ast, serializer);
            }

            if (IsDictionaryGetItemMethodWithStringKey(expression, out sourceExpression, out var keyExpression))
            {
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
                if (keyExpression is ConstantExpression keyConstantExpression)
                {
                    var key = (string)keyConstantExpression.Value;
                    var ast = AstExpression.SubField(sourceTranslation.Ast, key);
                    var valueSerializer = GetDictionaryValueSerializer(sourceTranslation.Serializer);

                    return new AggregationExpression(expression, ast, valueSerializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static IBsonSerializer GetDictionaryValueSerializer(IBsonSerializer serializer)
        {
            if (serializer is IBsonDictionarySerializer dictionarySerializer)
            {
                return dictionarySerializer.ValueSerializer;
            }

            throw new InvalidOperationException($"Unable to determine value serializer for dictionary serializer: {serializer.GetType().FullName}.");
        }

        private static bool IsEnumerableGetItemMethodWithIntIndex(MethodCallExpression expression, out Expression sourceExpression, out Expression indexExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (!method.IsStatic && method.Name == "get_Item")
            {
                sourceExpression = expression.Object;
                indexExpression = arguments[0];

                if (sourceExpression.Type.TryGetIEnumerableGenericInterface(out var sourceEnumerableInterface))
                {
                    var sourceItemType = sourceEnumerableInterface.GetGenericArguments()[0];
                    if (expression.Type == sourceItemType)
                    {
                        if (indexExpression.Type == typeof(int))
                        {
                            return true;
                        }
                    }
                }
            }

            sourceExpression = null;
            indexExpression = null;
            return false;
        }

        private static bool IsDictionaryGetItemMethodWithStringKey(MethodCallExpression expression, out Expression sourceExpression, out Expression keyExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (!method.IsStatic && method.Name == "get_Item")
            {
                sourceExpression = expression.Object;
                keyExpression = arguments[0];

                if (sourceExpression.Type.TryGetIDictionaryGenericInterface(out var sourceDictionaryInterface))
                {
                    var sourceTypeParameters = sourceDictionaryInterface.GetGenericArguments();
                    var sourceKeyType = sourceTypeParameters[0];
                    var sourceValueType = sourceTypeParameters[1];
                    if (sourceKeyType == typeof(string) && expression.Type == sourceValueType)
                    {
                        if (keyExpression.Type == typeof(string))
                        {
                            return true;
                        }
                    }
                }
            }

            sourceExpression = null;
            keyExpression = null;
            return false;
        }
    }
}

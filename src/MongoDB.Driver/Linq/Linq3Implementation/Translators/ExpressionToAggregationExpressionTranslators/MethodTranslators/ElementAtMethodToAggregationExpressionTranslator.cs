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

using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class ElementAtMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(EnumerableOrQueryableMethod.ElementAtOverloads))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                var indexExpression = arguments[1];
                var indexTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, indexExpression);

                AstExpression ast;
                if (method.IsOneOf(EnumerableOrQueryableMethod.ElementAtOrDefault))
                {
                    var defaultValue = itemSerializer.ValueType.GetDefaultValue();
                    var serializedDefaultValue = SerializationHelper.SerializeValue(itemSerializer, defaultValue);

                    var (sourceVarBinding, sourceAst) = AstExpression.UseVarIfNotSimple("source", sourceTranslation.Ast);
                    var (indexVarBinding, indexAst) = AstExpression.UseVarIfNotSimple("index", indexTranslation.Ast);
                    ast = AstExpression.Let(
                        var1: sourceVarBinding,
                        var2: indexVarBinding,
                        @in: AstExpression.Cond(
                            @if: AstExpression.Gte(indexAst, AstExpression.Size(sourceAst)),
                            then: serializedDefaultValue,
                            @else: AstExpression.ArrayElemAt(sourceAst, indexAst)));
                }
                else
                {
                    ast = AstExpression.ArrayElemAt(sourceTranslation.Ast, indexTranslation.Ast);
                }

                return new TranslatedExpression(expression, ast, itemSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}

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
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class DefaultIfEmptyMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __defaultIfEmptyMethods =
        {
            EnumerableMethod.DefaultIfEmpty,
            EnumerableMethod.DefaultIfEmptyWithDefaultValue,
            QueryableMethod.DefaultIfEmpty,
            QueryableMethod.DefaultIfEmptyWithDefaultValue,
        };

        private static readonly MethodInfo[] __defaultIfEmptyWithDefaultValueMethods =
        {
            EnumerableMethod.DefaultIfEmptyWithDefaultValue,
            QueryableMethod.DefaultIfEmptyWithDefaultValue,
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__defaultIfEmptyMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

                var (sourceVarBinding, sourceAst) = AstExpression.UseVarIfNotSimple("source", sourceTranslation.Ast);
                AstExpression defaultValueAst;
                if (method.IsOneOf(__defaultIfEmptyWithDefaultValueMethods))
                {
                    var defaultValueExpression = arguments[1];
                    var defaultValueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, defaultValueExpression);
                    defaultValueAst = AstExpression.ComputedArray(new[] { defaultValueTranslation.Ast });
                }
                else
                {
                    var sourceItemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                    var defaultValue = sourceItemSerializer.ValueType.GetDefaultValue();
                    var serializedDefaultValue = SerializationHelper.SerializeValue(sourceItemSerializer, defaultValue);
                    defaultValueAst = AstExpression.Constant(new BsonArray { serializedDefaultValue });
                }
                var ast = AstExpression.Let(
                    sourceVarBinding,
                    AstExpression.Cond(
                        AstExpression.Eq(AstExpression.Size(sourceAst), 0),
                        defaultValueAst,
                        sourceAst));

                var serializer = NestedAsQueryableSerializer.CreateIEnumerableOrNestedAsQueryableSerializer(expression.Type, itemSerializer);
                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}

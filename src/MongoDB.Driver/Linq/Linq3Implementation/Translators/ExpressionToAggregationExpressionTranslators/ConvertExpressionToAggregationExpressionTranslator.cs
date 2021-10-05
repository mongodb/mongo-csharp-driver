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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class ConvertExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var operandExpression = expression.Operand;
                var operandTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, operandExpression);

                var expressionType = expression.Type;
                if (expressionType.IsConstructedGenericType && expressionType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var valueType = expressionType.GetGenericArguments()[0];
                    if (operandExpression.Type == valueType)
                    {
                        // use the same AST but with a new nullable serializer
                        var nullableSerializerType = typeof(NullableSerializer<>).MakeGenericType(valueType);
                        var valueSerializerType = typeof(IBsonSerializer<>).MakeGenericType(valueType);
                        var constructorInfo = nullableSerializerType.GetConstructor(new[] { valueSerializerType });
                        var nullableSerializer = (IBsonSerializer)constructorInfo.Invoke(new[] { operandTranslation.Serializer });
                        return new AggregationExpression(expression, operandTranslation.Ast, nullableSerializer);
                    }
                }

                var ast = AstExpression.Convert(operandTranslation.Ast, expressionType);
                var serializer = context.KnownSerializersRegistry.GetSerializer(expression);
                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}

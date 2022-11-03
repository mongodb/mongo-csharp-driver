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
using System.Text.RegularExpressions;
using System.Xml.Schema;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class ConvertExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var expressionType = expression.Type;
                if (expressionType == typeof(BsonValue))
                {
                    return TranslateConvertToBsonValue(context, expression, expression.Operand);
                }

                var operandExpression = expression.Operand;
                var operandTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, operandExpression);

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

                var ast = operandTranslation.Ast;
                IBsonSerializer serializer;
                if (expressionType.IsInterface)
                {
                    // when an expression is cast to an interface it's a no-op as far as we're concerned
                    // and we can just use the serializer for the concrete type and members not defined in the interface will just be ignored
                    serializer = operandTranslation.Serializer;
                }
                else
                {
                    ast = AstExpression.Convert(ast, expressionType);
                    serializer = context.KnownSerializersRegistry.GetSerializer(expression);
                }

                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AggregationExpression TranslateConvertToBsonValue(TranslationContext context, UnaryExpression expression, Expression operand)
        {
            // handle double conversions like `(BsonValue)(object)x.Anything`
            if (operand is UnaryExpression unaryExpression &&
                unaryExpression.NodeType == ExpressionType.Convert &&
                unaryExpression.Type == typeof(object))
            {
                operand = unaryExpression.Operand;
            }

            var operandTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, operand);

            return new AggregationExpression(expression, operandTranslation.Ast, BsonValueSerializer.Instance);
        }
    }
}

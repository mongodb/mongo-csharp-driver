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

using System;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class ConvertUnaryExpressionTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, UnaryExpression expression)
        {
            var operandExpression = expression.Operand;

            var operandTranslation = ExpressionTranslator.Translate(context, operandExpression);

            var expressionType = expression.Type;
            if (expressionType.IsConstructedGenericType && expressionType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var valueType = expressionType.GetGenericArguments()[0];
                if (operandExpression.Type == valueType)
                {
                    // use the same AST but with a new nullable serializer
                    var nullableSerializerType = typeof(NullableSerializer<>).MakeGenericType(valueType);
                    Type valueSerializerType = typeof(IBsonSerializer<>).MakeGenericType(valueType);
                    var constructorInfo = nullableSerializerType.GetConstructor(new[] { valueSerializerType });
                    var nullableSerializer = (IBsonSerializer)constructorInfo.Invoke(new[] { operandTranslation.Serializer });
                    return new ExpressionTranslation(expression, operandTranslation.Ast, null);
                }
            }

            var ast = new AstConvertExpression(operandTranslation.Ast, expressionType);
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer(expressionType); // TODO: find correct serializer

            return new ExpressionTranslation(expression, ast, serializer);
        }
    }
}

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

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class ConstantExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, ConstantExpression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
            {
                var value = expression.Value;
                var defaultValueSerializer = expression.Type switch
                {
                    Type t when t == typeof(bool) => new BooleanSerializer(),
                    Type t when t == typeof(string) => new StringSerializer(),
                    Type t when t == typeof(byte) => new ByteSerializer(),
                    Type t when t == typeof(short) => new Int16Serializer(),
                    Type t when t == typeof(ushort) => new UInt16Serializer(),
                    Type t when t == typeof(int) => new Int32Serializer(),
                    Type t when t == typeof(uint) => new UInt32Serializer(),
                    Type t when t == typeof(long) => new Int64Serializer(),
                    Type t when t == typeof(ulong) => new UInt64Serializer(),
                    Type t when t == typeof(float) => new SingleSerializer(),
                    Type t when t == typeof(double) => new DoubleSerializer(),
                    Type t when t == typeof(decimal) => new DecimalSerializer(),
                    Type { IsConstructedGenericType: true } t when t.GetGenericTypeDefinition() == typeof(Nullable<>) => (IBsonSerializer)Activator.CreateInstance(typeof(NullableSerializer<>).MakeGenericType(t.GenericTypeArguments[0])),
                    Type { IsArray: true } t => (IBsonSerializer)Activator.CreateInstance(typeof(ArraySerializer<>).MakeGenericType(t.GetElementType())),
                    _ => null
                };
                // If we need an EnumSerializer, we have to look up the correct one based on the current expression.
                var valueSerializer = context.KnownSerializersRegistry.GetSerializer(expression, defaultValueSerializer);
                var serializedValue = valueSerializer.ToBsonValue(value);
                var ast = AstExpression.Constant(serializedValue);
                return new AggregationExpression(expression, ast, valueSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}

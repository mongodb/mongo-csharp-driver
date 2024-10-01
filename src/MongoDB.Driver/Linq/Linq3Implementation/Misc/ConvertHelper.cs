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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class ConvertHelper
    {
        private readonly static (Type SourceType, Type TargetType)[] __wideningConverts = new[]
        {
            (typeof(byte), typeof(short)),
            (typeof(byte), typeof(ushort)),
            (typeof(byte), typeof(int)),
            (typeof(byte), typeof(uint)),
            (typeof(byte), typeof(long)),
            (typeof(byte), typeof(ulong)),
            (typeof(byte), typeof(float)),
            (typeof(byte), typeof(double)),
            (typeof(byte), typeof(decimal)),
            (typeof(sbyte), typeof(short)),
            (typeof(sbyte), typeof(ushort)),
            (typeof(sbyte), typeof(int)),
            (typeof(sbyte), typeof(uint)),
            (typeof(sbyte), typeof(long)),
            (typeof(sbyte), typeof(ulong)),
            (typeof(sbyte), typeof(float)),
            (typeof(sbyte), typeof(double)),
            (typeof(sbyte), typeof(decimal)),
            (typeof(short), typeof(int)),
            (typeof(short), typeof(uint)),
            (typeof(short), typeof(long)),
            (typeof(short), typeof(ulong)),
            (typeof(short), typeof(float)),
            (typeof(short), typeof(double)),
            (typeof(short), typeof(decimal)),
            (typeof(ushort), typeof(int)),
            (typeof(ushort), typeof(uint)),
            (typeof(ushort), typeof(long)),
            (typeof(ushort), typeof(ulong)),
            (typeof(ushort), typeof(float)),
            (typeof(ushort), typeof(double)),
            (typeof(ushort), typeof(decimal)),
            (typeof(int), typeof(long)),
            (typeof(int), typeof(ulong)),
            (typeof(int), typeof(float)),
            (typeof(int), typeof(double)),
            (typeof(int), typeof(decimal)),
            (typeof(uint), typeof(long)),
            (typeof(uint), typeof(ulong)),
            (typeof(uint), typeof(float)),
            (typeof(uint), typeof(double)),
            (typeof(uint), typeof(decimal)),
            (typeof(long), typeof(float)),
            (typeof(long), typeof(double)),
            (typeof(long), typeof(decimal)),
            (typeof(ulong), typeof(float)),
            (typeof(ulong), typeof(double)),
            (typeof(ulong), typeof(decimal)),
            (typeof(float), typeof(double)),
            (typeof(float), typeof(decimal)),
            (typeof(double), typeof(decimal))
        };

        public static IBsonSerializer CreateWiderSerializer(Type narrowerType, Type widerType)
        {
            if (IsWideningConvert(narrowerType, widerType))
            {
                return widerType switch
                {
                    _ when widerType == typeof(int) => new Int32Serializer(BsonType.Int32),
                    _ when widerType == typeof(long) => new Int64Serializer(BsonType.Int64),
                    _ when widerType == typeof(decimal) => new DecimalSerializer(BsonType.Decimal128),
                    _ when widerType == typeof(double) => new DoubleSerializer(BsonType.Double),
                    _ => throw new ArgumentException($"Cannot create a wider serializer of type {widerType}.", nameof(widerType))
                };
            }

            throw new ArgumentException($"{widerType} is not a wider type for {narrowerType}", nameof(widerType));
        }

        public static bool IsWideningConvert(Type sourceType, Type targetType)
        {
            return __wideningConverts.Contains((sourceType, targetType));
        }

        public static Expression RemoveConvertToEnumUnderlyingType(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var convertExpression = (UnaryExpression)expression;
                var sourceType = convertExpression.Operand.Type;
                var targetType = convertExpression.Type;

                if (sourceType.IsEnumOrNullableEnum(out _, out var underlyingType) &&
                    targetType.IsSameAsOrNullableOf(underlyingType))
                {
                    return convertExpression.Operand;
                }
            }

            return expression;
        }

        public static Expression RemoveConvertToInterface(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var convertExpression = (UnaryExpression)expression;
                var targetType = convertExpression.Type;
                if (targetType.IsInterface)
                {
                    return convertExpression.Operand;
                }
            }

            return expression;
        }

        public static Expression RemoveConvertToObject(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var convertExpression = (UnaryExpression)expression;
                var targetType = convertExpression.Type;
                if (targetType == typeof(object))
                {
                    return convertExpression.Operand;
                }
            }

            return expression;
        }

        public static AstExpression RemoveWideningConvert(AggregationExpression translation)
        {
            if (translation.Expression is UnaryExpression convertExpression &&
                convertExpression.NodeType == ExpressionType.Convert &&
                convertExpression.Operand.Type is var sourceType &&
                convertExpression.Type is var targetType &&
                IsWideningConvert(sourceType, targetType) &&
                translation.Ast is AstUnaryExpression astUnaryExpression &&
                astUnaryExpression.Operator.IsConvertOperator())
            {
                return astUnaryExpression.Arg;
            }

            return translation.Ast;
        }
    }
}

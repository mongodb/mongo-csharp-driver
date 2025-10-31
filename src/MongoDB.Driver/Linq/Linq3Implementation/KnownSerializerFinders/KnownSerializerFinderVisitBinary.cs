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
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.KnownSerializerFinders;

internal partial class KnownSerializerFinderVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        base.VisitBinary(node);

        var @operator = node.NodeType;
        var leftExpression = node.Left;
        var rightExpression = node.Right;

        if (node.NodeType == ExpressionType.Add && node.Type == typeof(string))
        {
            DeduceStringSerializer(node);
            return node;
        }

        if (IsSymmetricalBinaryOperator(@operator) &&
            CanDeduceSerializer(leftExpression, rightExpression, out var unknownNode, out var knownSerializer))
        {
            // expr1 op expr2 => expr1: expr2Serializer or expr2: expr1Serializer
            if (knownSerializer.ValueType == unknownNode.Type)
            {
                AddKnownSerializer(unknownNode, knownSerializer);
            }
        }

        if (@operator == ExpressionType.ArrayIndex)
        {
            if (IsNotKnown(node) &&
                IsKnown(leftExpression, out var leftSerializer))
            {
                IBsonSerializer itemSerializer;
                if (leftSerializer is IPolymorphicArraySerializer polymorphicArraySerializer)
                {
                    var index = rightExpression.GetConstantValue<int>(node);
                    itemSerializer = polymorphicArraySerializer.GetItemSerializer(index);
                }
                else
                {
                    itemSerializer = leftSerializer.GetItemSerializer();
                }

                // expr[index] => node: itemSerializer
                AddKnownSerializer(node, itemSerializer);
            }
        }

        if (@operator == ExpressionType.Coalesce)
        {
            if (IsNotKnown(node) &&
                IsKnown(leftExpression, out var leftSerializer))
            {
                if (leftSerializer.ValueType == node.Type)
                {
                    AddKnownSerializer(node, leftSerializer);
                }
                else if (
                    leftSerializer is INullableSerializer nullableSerializer &&
                    nullableSerializer.ValueSerializer is var nullableSerializerValueSerializer &&
                    nullableSerializerValueSerializer.ValueType == node.Type)
                {
                    AddKnownSerializer(node, nullableSerializerValueSerializer);
                }
                else
                {
                    DeduceUnknowableSerializer(node);
                }
            }
        }

        if (leftExpression.IsConvert(out var leftConvertOperand) &&
            rightExpression.IsConvert(out var rightConvertOperand) &&
            leftConvertOperand.Type == rightConvertOperand.Type)
        {
            // TODO: verify left and right operands are same type
            if (CanDeduceSerializer(leftConvertOperand, rightConvertOperand, out unknownNode, out knownSerializer))
            {
                // Convert(expr1, T) op Convert(expr2, T) => expr1: expr2Serializer or expr2: expr1Serializer
                AddKnownSerializer(unknownNode, knownSerializer);
            }
        }

        if (IsNotKnown(node))
        {
            var resultSerializer = GetResultSerializer(node, @operator);
            if (resultSerializer != null)
            {
                AddKnownSerializer(node, resultSerializer);
            }
        }

        return node;

        static IBsonSerializer GetResultSerializer(Expression node, ExpressionType @operator)
        {
            switch (@operator)
            {
                case ExpressionType.And:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Or:
                    switch (node.Type)
                    {
                        case Type t when t == typeof(bool): return BooleanSerializer.Instance;
                        case Type t when t == typeof(int): return Int32Serializer.Instance;
                    }
                    goto default;

                case ExpressionType.AndAlso:
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                case ExpressionType.OrElse:
                case ExpressionType.TypeEqual:
                    return BooleanSerializer.Instance;

                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    if (StandardSerializers.TryGetSerializer(node.Type, out var resultSerializer))
                    {
                        return resultSerializer;
                    }
                    goto default;

                default:
                    return null;
            }
        }

        static bool IsSymmetricalBinaryOperator(ExpressionType @operator)
            => @operator is
                ExpressionType.Add or
                ExpressionType.AddChecked or
                ExpressionType.And or
                ExpressionType.AndAlso or
                ExpressionType.Coalesce or
                ExpressionType.Divide or
                ExpressionType.Equal or
                ExpressionType.GreaterThan or
                ExpressionType.GreaterThanOrEqual or
                ExpressionType.Modulo or
                ExpressionType.Multiply or
                ExpressionType.MultiplyChecked or
                ExpressionType.Or or
                ExpressionType.OrElse or
                ExpressionType.Subtract or
                ExpressionType.SubtractChecked;
    }
}

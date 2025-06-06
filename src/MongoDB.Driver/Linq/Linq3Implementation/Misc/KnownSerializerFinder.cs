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

using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc;

internal class KnownSerializerFinder : ExpressionVisitor
{
    public static KnownSerializerMap FindKnownSerializers(
        Expression expression)
    {
        var knownSerializers = new KnownSerializerMap();
        return FindKnownSerializers(expression, knownSerializers);
    }

    public static KnownSerializerMap FindKnownSerializers(
        Expression expression,
        Expression initialNode,
        IBsonSerializer initialSerializer)
    {
        var knownSerializers = new KnownSerializerMap();
        knownSerializers.AddSerializer(initialNode, initialSerializer);
        return FindKnownSerializers(expression, knownSerializers);
    }

    public static KnownSerializerMap FindKnownSerializers(
        Expression expression,
        KnownSerializerMap knownSerializers)
    {
        var finder = new KnownSerializerFinder(knownSerializers);

        int oldSerializerCount;
        int newSerializerCount;
        do
        {
            oldSerializerCount = finder._knownSerializers.Count;
            finder.Visit(expression);
            newSerializerCount = finder._knownSerializers.Count;
        }
        while (newSerializerCount > oldSerializerCount); // I don't know yet if this can be done in a single pass

        #if DEBUG
        var expressionWithUnknownSerializer = UnknownSerializerFinder.FindExpressionWithUnknownSerializer(expression, knownSerializers);
        if (expressionWithUnknownSerializer != null)
        {
            throw new ExpressionNotSupportedException(expressionWithUnknownSerializer, because: "unable to determine which serializer to use");
        }
        #endif

        return knownSerializers;
    }

    private readonly KnownSerializerMap _knownSerializers;

    public KnownSerializerFinder(KnownSerializerMap knownSerializers)
    {
        _knownSerializers = knownSerializers;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        base.VisitBinary(node);

        var @operator = node.NodeType;
        var left = node.Left;
        var right = node.Right;

        if (IsSymmetricalBinaryOperator(@operator) &&
            CanDeduceSerializer(left, right, out var unknownNode, out var knownSerializer))
        {
            // expr1 op expr2 => expr1: expr2Serializer or expr2: expr1Serializer
            _knownSerializers.AddSerializer(unknownNode, knownSerializer);
            return node;
        }

        if (@operator == ExpressionType.ArrayIndex)
        {
            if (_knownSerializers.IsNotKnown(node) &&
                _knownSerializers.IsKnown(left, out var leftSerializer))
            {
                if (leftSerializer is not IBsonSerializer arraySerializer)
                {
                    throw new ExpressionNotSupportedException(node, because: $"serializer type {leftSerializer.GetType()} does not implement IBsonArraySerializer");
                }

                var itemSerializer = ArraySerializerHelper.GetItemSerializer(arraySerializer);

                // expr[index] => node: itemSerializer
                _knownSerializers.AddSerializer(node, itemSerializer);
            }

            return node;
        }

        if (left.IsConvert(out var leftConvertOperand) && right.IsConvert(out var rightConvertOperand))
        {
            if (CanDeduceSerializer(leftConvertOperand, rightConvertOperand, out unknownNode, out knownSerializer))
            {
                // Convert(expr1, T) op Convert(expr2, T) => expr1: expr2Serializer or expr2: expr1Serializer
                _knownSerializers.AddSerializer(unknownNode, knownSerializer);
            }

            return node;
        }

        return node;

        static bool IsSymmetricalBinaryOperator(ExpressionType @operator)
            => @operator is
                ExpressionType.Add or
                ExpressionType.AddChecked or
                ExpressionType.And or
                ExpressionType.AndAlso or
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

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var value = node.Value;

        if (_knownSerializers.IsNotKnown(node) &&
            value is IQueryable queryable &&
            queryable.Provider is IMongoQueryProviderInternal provider &&
            queryable.Expression is ConstantExpression constantExpression &&
            constantExpression.Value == value)
        {
            var documentSerializer = provider.PipelineInputSerializer;
            var queryableSerializer = QueryableSerializer.Create(itemSerializer: documentSerializer);

            // originalSource => node: new QueryableSerializer<TDocument>(documentSerializer)
            _knownSerializers.AddSerializer(node, queryableSerializer);
        }

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        base.VisitMember(node);

        var containingExpression = node.Expression;
        if (_knownSerializers.IsKnown(containingExpression, out var containingSerializer) &&
            _knownSerializers.IsNotKnown(node))
        {
            // TODO: handle special cases like DateTime.Year etc.

            if (containingSerializer is not IBsonDocumentSerializer documentSerializer)
            {
                throw new ExpressionNotSupportedException(node, because: $"serializer type {containingSerializer.GetType()} does not implement the {nameof(IBsonDocumentSerializer)} interface");
            }

            var memberName = node.Member.Name;
            if (!documentSerializer.TryGetMemberSerializationInfo(memberName, out var memberSerializationInfo))
            {
                throw new ExpressionNotSupportedException(node, because: $"serializer type {containingSerializer.GetType()} does not support a member named: {memberName}");
            }
            var memberSerializer = memberSerializationInfo.Serializer;

            // expr.Member => node: memberSerializer
            _knownSerializers.AddSerializer(node, memberSerializer);
        }

        return node;
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        base.VisitMemberInit(node);

        if (_knownSerializers.IsKnown(node, out var newSerializer))
        {
            if (newSerializer is not IBsonDocumentSerializer documentSerializer)
            {
                throw new ExpressionNotSupportedException(node, because:  $"serializer type {newSerializer.GetType()} does not implement IBsonDocumentSerializer interface");
            }

            foreach (var binding in node.Bindings)
            {
                if (binding is MemberAssignment memberAssignment)
                {
                    if (_knownSerializers.IsNotKnown(memberAssignment.Expression))
                    {
                        var member = memberAssignment.Member;
                        var memberName = member.Name;
                        if (!documentSerializer.TryGetMemberSerializationInfo(memberName, out var memberSerializationInfo))
                        {
                            throw new ExpressionNotSupportedException(node, because: $"type {member.DeclaringType} does not have a member named: {memberName}");
                        }
                        var expressionSerializer = memberSerializationInfo.Serializer;

                        if (expressionSerializer.ValueType != memberAssignment.Expression.Type &&
                            expressionSerializer.ValueType.IsAssignableFrom(memberAssignment.Expression.Type))
                        {
                            expressionSerializer = expressionSerializer.GetDerivedTypeSerializer(memberAssignment.Expression.Type);
                        }

                        // member = expression => expression: memberSerializer (or derivedTypeSerializer)
                        _knownSerializers.AddSerializer(memberAssignment.Expression, expressionSerializer);
                    }
                }
            }
        }

        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        base.VisitMethodCall(node);

        var method = node.Method;
        var arguments = node.Arguments;

        if (method.IsStatic &&
            arguments.Count >= 2 &&
            arguments[0] is var sourceExpression &&
            arguments[1] is LambdaExpression lambdaExpression &&
            sourceExpression.Type.ImplementsIEnumerable(out var sourceItemType) &&
            lambdaExpression.Parameters.Count == 1 &&
            lambdaExpression.Parameters[0] is var itemExpression &&
            itemExpression.Type == sourceItemType)
        {
            IBsonSerializer itemSerializer;

            if (_knownSerializers.IsKnown(sourceExpression, out var sourceSerializer) &&
                _knownSerializers.IsNotKnown(itemExpression))
            {
                itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceSerializer);

                // source.method(item => ...) => item: itemSerializer
                _knownSerializers.AddSerializer(itemExpression, itemSerializer);
            }

            if (_knownSerializers.IsNotKnown(sourceExpression) &&
                _knownSerializers.IsKnown(itemExpression, out itemSerializer))
            {
                sourceSerializer = BsonSerializer.LookupSerializer(sourceExpression.Type); // TODO: is it OK to use BsonSerializer registry?
                if (sourceSerializer is IChildSerializerConfigurable childSerializerConfigurable)
                {
                    sourceSerializer = childSerializerConfigurable.WithChildSerializer(itemSerializer);

                    // source.method(item => ...) => source: sourceSerializer
                    _knownSerializers.AddSerializer(sourceExpression, sourceSerializer);
                }
            }
        }

        return node;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        base.VisitNew(node);

        if (_knownSerializers.IsKnown(node, out var nodeSerializer) &&
            node.Arguments.Any(_knownSerializers.IsNotKnown))
        {
            var matchingMemberSerializationInfos = nodeSerializer.GetMatchingMemberSerializationInfosForConstructorParameters(node, node.Constructor);
            for (var i = 0; i < matchingMemberSerializationInfos.Count; i++)
            {
                var argumentExpression = node.Arguments[i];
                var matchingMemberSerializationInfo = matchingMemberSerializationInfos[i];

                if (_knownSerializers.IsNotKnown(argumentExpression))
                {
                    // arg => arg: matchingMemberSerializer
                    _knownSerializers.AddSerializer(argumentExpression, matchingMemberSerializationInfo.Serializer);
                }
            }
        }

        return node;
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        base.VisitNewArray(node);

        if (_knownSerializers.IsKnown(node, out var nodeSerializer))
        {
            if (nodeSerializer is not IBsonArraySerializer arraySerializer)
            {
                throw new ExpressionNotSupportedException(node, because: $"serializer type {nodeSerializer.GetType()} does not implement IBsonArraySerializer");
            }
            var itemSerializer = ArraySerializerHelper.GetItemSerializer(arraySerializer);

            foreach (var expression in node.Expressions)
            {
                if (_knownSerializers.IsNotKnown(expression))
                {
                    // new T[] { ..., expr, ... } => expr: itemSerializer
                    _knownSerializers.AddSerializer(expression, itemSerializer);
                }
            }
        }

        return node;
    }

    private bool CanDeduceSerializer(Expression node1, Expression node2, out Expression unknownNode, out IBsonSerializer knownSerializer)
    {
        if (_knownSerializers.IsKnown(node1, out var node1Serializer) &&
            _knownSerializers.IsNotKnown(node2))
        {
            unknownNode = node2;
            knownSerializer = node1Serializer;
            return true;
        }

        if (_knownSerializers.IsNotKnown(node1) &&
            _knownSerializers.IsKnown(node2, out var node2Serializer))
        {
            unknownNode = node1;
            knownSerializer = node2Serializer;
            return true;
        }

        unknownNode = null;
        knownSerializer = null;
        return false;
    }
}

internal class UnknownSerializerFinder : ExpressionVisitor
{
    public static Expression FindExpressionWithUnknownSerializer(Expression expression, KnownSerializerMap knownSerializers)
    {
        return null; // TODO: implement
    }

    private Expression _expressionWithUnknownSerializer = null;
    private readonly KnownSerializerMap _knownSerializers;

    public UnknownSerializerFinder(KnownSerializerMap knownSerializers)
    {
        _knownSerializers = knownSerializers;
    }

    public Expression ExpressionWithUnknownSerialier => _expressionWithUnknownSerializer;
}

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
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace MongoDB.Driver.Linq.Linq3Implementation.KnownSerializerFinders;

internal class UnknownSerializerFinder : ExpressionVisitor
{
    public static Expression FindExpressionWithUnknownSerializer(Expression expression, KnownSerializerMap knownSerializers)
    {
        var visitor = new UnknownSerializerFinder(knownSerializers);
        visitor.Visit(expression);
        return visitor._expressionWithUnknownSerializer;
    }

    private Expression _expressionWithUnknownSerializer = null;
    private readonly KnownSerializerMap _knownSerializers;

    public UnknownSerializerFinder(KnownSerializerMap knownSerializers)
    {
        _knownSerializers = knownSerializers;
    }

    public Expression ExpressionWithUnknownSerialier => _expressionWithUnknownSerializer;

    public override Expression Visit(Expression node)
    {
        if (_knownSerializers.IsKnown(node, out var knownSerializer))
        {
            if (knownSerializer is IIgnoreSubtreeSerializer or IUnknowableSerializer)
            {
                return node; // don't visit subtree
            }
        }

        base.Visit(node);

        if (_expressionWithUnknownSerializer == null &&
            node != null &&
            _knownSerializers.IsNotKnown(node) &&
            ShouldHaveKnownSerializer(node))
        {
            _expressionWithUnknownSerializer = node;
        }

        return node;

        static bool ShouldHaveKnownSerializer(Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Lambda:
                case ExpressionType.Quote:
                    return false;

                default:
                    return true;
            }
        }
    }
}

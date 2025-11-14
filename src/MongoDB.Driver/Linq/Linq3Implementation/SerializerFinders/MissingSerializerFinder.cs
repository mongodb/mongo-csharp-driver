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
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal class MissingSerializerFinder : ExpressionVisitor
{
    public static Expression FindExpressionWithMissingSerializer(Expression expression, SerializerMap nodeSerializers)
    {
        var visitor = new MissingSerializerFinder(nodeSerializers);
        visitor.Visit(expression);
        return visitor._expressionWithMissingSerializer;
    }

    private Expression _expressionWithMissingSerializer = null;
    private readonly SerializerMap _nodeSerializers;

    public MissingSerializerFinder(SerializerMap nodeSerializers)
    {
        _nodeSerializers = nodeSerializers;
    }

    public Expression ExpressionWithMissingSerializer => _expressionWithMissingSerializer;

    public override Expression Visit(Expression node)
    {
        if (_nodeSerializers.IsKnown(node, out var nodeSerializer))
        {
            if (nodeSerializer is IIgnoreSubtreeSerializer or IUnknowableSerializer)
            {
                return node; // don't visit subtree
            }
        }

        base.Visit(node);

        if (_expressionWithMissingSerializer == null &&
            node != null &&
            _nodeSerializers.IsNotKnown(node))
        {
            _expressionWithMissingSerializer = node;
        }

        return node;
    }
}

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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        if (IsKnown(node, out var nodeSerializer))
        {
            var newExpression = node.NewExpression;
            if (newExpression != null)
            {
                if (IsNotKnown(newExpression))
                {
                    AddNodeSerializer(newExpression, nodeSerializer);
                }
            }

            if (node.Bindings.Count > 0)
            {
                if (nodeSerializer is not IBsonDocumentSerializer documentSerializer)
                {
                    throw new ExpressionNotSupportedException(node, because:  $"serializer type {nodeSerializer.GetType()} does not implement IBsonDocumentSerializer interface");
                }

                foreach (var binding in node.Bindings)
                {
                    if (binding is MemberAssignment memberAssignment)
                    {
                        if (IsNotKnown(memberAssignment.Expression))
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
                            AddNodeSerializer(memberAssignment.Expression, expressionSerializer);
                        }
                    }
                }
            }
        }

        base.VisitMemberInit(node);

        if (IsNotKnown(node))
        {
            var resultSerializer = GetResultSerializer();
            if (resultSerializer != null)
            {
                AddNodeSerializer(node, resultSerializer);
            }
        }

        return node;

        IBsonSerializer GetResultSerializer()
        {
            if (node.Type == typeof(BsonDocument))
            {
                return BsonDocumentSerializer.Instance;
            }
            var newExpression = node.NewExpression;
            var bindings = node.Bindings;
            return CreateNewExpressionSerializer(node, newExpression, bindings);
        }
    }
}

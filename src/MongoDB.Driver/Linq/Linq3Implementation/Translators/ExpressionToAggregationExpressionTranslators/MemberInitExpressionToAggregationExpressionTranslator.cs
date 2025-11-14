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

using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class MemberInitExpressionToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MemberInitExpression expression)
        {
            if (expression.Type == typeof(BsonDocument))
            {
                return NewBsonDocumentExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }

            return Translate(context, expression, expression.NewExpression, expression.Bindings);
        }

        public static TranslatedExpression Translate(
            TranslationContext context,
            Expression expression,
            NewExpression newExpression,
            IReadOnlyList<MemberBinding> bindings)
        {
            var nodeSerializer = context.NodeSerializers.GetSerializer(expression);
            var constructorInfo = newExpression.Constructor; // note: can be null when using the default constructor with a struct
            var constructorArguments = newExpression.Arguments;

            var computedFields = new List<AstComputedField>();
            if (constructorInfo != null && constructorArguments.Count > 0)
            {
                var matchingMemberSerializationInfos = nodeSerializer.GetMatchingMemberSerializationInfosForConstructorParameters(expression, constructorInfo);

                for (var i = 0; i < constructorArguments.Count; i++)
                {
                    var argument = constructorArguments[i];
                    var argumentTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, argument);
                    var matchingMemberSerializationInfo = matchingMemberSerializationInfos[i];

                    if (!argumentTranslation.Serializer.CanBeAssignedTo(matchingMemberSerializationInfo.Serializer))
                    {
                        throw new ExpressionNotSupportedException(argument, expression, because: "constructor argument serializer is not compatible with matching member serializer");
                    }

                    var computedField = AstExpression.ComputedField(matchingMemberSerializationInfo.ElementName, argumentTranslation.Ast);
                    computedFields.Add(computedField);
                }
            }

            if (bindings.Count > 0)
            {
                if (nodeSerializer is not IBsonDocumentSerializer documentSerializer)
                {
                    throw new ExpressionNotSupportedException(expression, because: $"serializer type {nodeSerializer.GetType()} does not implement IBsonDocumentSerializer");
                }

                foreach (var binding in bindings)
                {
                    var memberAssignment = (MemberAssignment)binding;
                    var member = memberAssignment.Member;

                    if (!documentSerializer.TryGetMemberSerializationInfo(member.Name, out var memberSerializationInfo))
                    {
                        throw new ExpressionNotSupportedException(expression, because: $"member {member.Name} was not found");
                    }

                    var valueExpression = memberAssignment.Expression;
                    var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);

                    if (!valueTranslation.Serializer.CanBeAssignedTo(memberSerializationInfo.Serializer))
                    {
                        throw new ExpressionNotSupportedException(valueExpression, expression, because: $"value serializer is not compatible with serializer for member {member.Name}");
                    }

                    var computedField = AstExpression.ComputedField(memberSerializationInfo.ElementName, valueTranslation.Ast);
                    computedFields.Add(computedField);
                }
            }

            var ast = AstExpression.ComputedDocument(computedFields);
            return new TranslatedExpression(expression, ast, nodeSerializer);
        }
    }
}

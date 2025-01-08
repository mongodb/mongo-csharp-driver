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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class MemberInitExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MemberInitExpression expression)
        {
            if (expression.Type == typeof(BsonDocument))
            {
                return NewBsonDocumentExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }

            return Translate(context, expression, expression.NewExpression, expression.Bindings);
        }

        public static AggregationExpression Translate(
            TranslationContext context,
            Expression expression,
            NewExpression newExpression,
            IReadOnlyList<MemberBinding> bindings)
        {
            var constructorInfo = newExpression.Constructor; // note: can be null when using the default constructor with a struct
            var constructorArguments = newExpression.Arguments;
            var computedFields = new List<AstComputedField>();

            var targetSerializer = context.Data?.GetValueOrDefault<IBsonSerializer>("TargetSerializer", null);
            var targetType = newExpression.Type;
            var serializer = targetSerializer?.ValueType == targetType ? targetSerializer : BsonSerializer.LookupSerializer(targetType);
            if (!(serializer is IBsonDocumentSerializer documentSerializer))
            {
                throw new ExpressionNotSupportedException(expression, because: $"serializer class {serializer.GetType()} does not implement IBsonDocumentSerializer.");
            }

            if (constructorInfo != null && constructorInfo.GetParameters().Length > 0)
            {
                // for now we only support constructors with BsonClassMapSerializers
                if (!(documentSerializer is IBsonClassMapSerializer bsonClassMapSerializer))
                {
                    throw new ExpressionNotSupportedException(expression, because: "constructors are only supported for BsonClassMapSerializer");
                }
                var classMap = bsonClassMapSerializer.ClassMap;
                var creatorMap = FindMatchingCreatorMap(classMap, constructorInfo);
                if (creatorMap == null)
                {
                    throw new ExpressionNotSupportedException(expression, because: "couldn't find matching creator map");
                }

                var constructorParameters = constructorInfo.GetParameters();
                var creatorMapParameters = creatorMap.Arguments?.ToArray();
                if (constructorParameters.Length > 0)
                {
                    if (creatorMapParameters == null)
                    {
                        throw new ExpressionNotSupportedException(expression, because: $"couldn't find matching properties for constructor parameters.");
                    }
                    if (creatorMapParameters.Length != constructorParameters.Length)
                    {
                        throw new ExpressionNotSupportedException(expression, because: $"the constructor has {constructorParameters} parameters but the creatorMap has {creatorMapParameters.Length} parameters.");
                    }

                    for (var i = 0; i < creatorMapParameters.Length; i++)
                    {
                        var creatorMapParameter = creatorMapParameters[i];
                        var memberMap = FindMatchingMemberMap(creatorMap, creatorMapParameter.Name);
                        if (memberMap == null)
                        {
                            throw new ExpressionNotSupportedException(expression, because: $"couldn't find matching member map for constructor parameter {creatorMapParameter.Name}");
                        }

                        var argumentContext = context.WithData("TargetSerializer", memberMap.GetSerializer());
                        var argumentExpression = constructorArguments[i];
                        var argumentTranslation = ExpressionToAggregationExpressionTranslator.Translate(argumentContext, argumentExpression);
                        computedFields.Add(AstExpression.ComputedField(memberMap.ElementName, argumentTranslation.Ast));
                    }
                }
            }

            foreach (var binding in bindings)
            {
                var memberAssignment = (MemberAssignment)binding;
                var valueExpression = memberAssignment.Expression;
                var member = memberAssignment.Member;
                if (!documentSerializer.TryGetMemberSerializationInfo(member.Name, out var memberSerializationInfo))
                {
                    throw new ExpressionNotSupportedException(valueExpression, expression, because: $"couldn't find member {member.Name}");
                }

                var valueContext = context.WithData("TargetSerializer", memberSerializationInfo.Serializer);
                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(valueContext, valueExpression);
                computedFields.Add(AstExpression.ComputedField(memberSerializationInfo.ElementName, valueTranslation.Ast));
            }

            var ast = AstExpression.ComputedDocument(computedFields);

            return new AggregationExpression(expression, ast, documentSerializer);
        }

        private static BsonCreatorMap FindMatchingCreatorMap(BsonClassMap classMap, ConstructorInfo constructorInfo)
            => classMap.CreatorMaps.FirstOrDefault(m => m.MemberInfo.Equals(constructorInfo));

        private static BsonMemberMap FindMatchingMemberMap(BsonCreatorMap creatorMap, string memberName)
        {
            var arguments = creatorMap.Arguments.ToArray();
            for (var index = 0; index < arguments.Length; index++)
            {
                if (arguments[index].Name.Equals(memberName, StringComparison.Ordinal))
                {
                    var elementName = creatorMap.ElementNames.ElementAt(index);
                    return creatorMap.ClassMap.AllMemberMaps.FirstOrDefault(m => m.ElementName.Equals(elementName, StringComparison.Ordinal));
                }
            }

            return null;
        }
    }
}

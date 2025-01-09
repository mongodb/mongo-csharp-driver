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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToSetStageTranslators
{
    internal static class ExpressionToSetStageTranslator
    {
        public static AstStage Translate(TranslationContext context, IBsonSerializer inputSerializer, LambdaExpression expression)
        {
            if (!DocumentSerializerHelper.AreMembersRepresentedAsFields(inputSerializer, out var documentSerializer))
            {
                throw new ExpressionNotSupportedException(expression, because: $"serializer {inputSerializer.GetType()} does not represent members as fields");
            }

            if (IsNewAnonymousClass(expression, out var newExpression))
            {
                return TranslateNewAnonymousClass(context, documentSerializer, newExpression);
            }

            if (IsNewWithOptionalMemberInitializers(expression, out var memberInitExpression))
            {
                return TranslateNewWithOptionalMemberInitializers(context, documentSerializer, memberInitExpression);
            }

            throw new ExpressionNotSupportedException(expression, because: "expression is not valid for Set");
        }

        private static bool IsNewAnonymousClass(LambdaExpression expression, out NewExpression newExpression)
        {
            if (expression.Body is NewExpression tempNewExpression &&
                tempNewExpression.Type.IsAnonymous())
            {
                newExpression = tempNewExpression;
                return true;
            }

            newExpression = null;
            return false;
        }

        private static bool IsNewWithOptionalMemberInitializers(LambdaExpression expression, out MemberInitExpression memberInitExpression)
        {
            if (expression.Body.NodeType == ExpressionType.New)
            {
                memberInitExpression = null;
                return true;
            }

            if (expression.Body is MemberInitExpression tempMemberInitExpression)
            {
                var constructor = tempMemberInitExpression.NewExpression.Constructor; // will be null for default constructor of struct
                if (constructor == null || IsDefaultConstructor(constructor) || IsCopyConstructor(constructor))
                {
                    memberInitExpression = tempMemberInitExpression;
                    return true;
                }
            }

            memberInitExpression = null;
            return false;

            static bool IsDefaultConstructor(ConstructorInfo constructor)
                => constructor.GetParameters().Length == 0;

            static bool IsCopyConstructor(ConstructorInfo constructor)
                =>
                    constructor.GetParameters() is var parameters &&
                    parameters.Length == 1 &&
                    parameters[0].ParameterType == constructor.DeclaringType;
        }

        private static AstStage TranslateNewAnonymousClass(TranslationContext context, IBsonDocumentSerializer documentSerializer, NewExpression newExpression)
        {
            var members = newExpression.Members; // will be null in the case of "new { }"
            var arguments = newExpression.Arguments;

            var fields = new List<AstComputedField>();
            if (members != null)
            {
                for (var i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    var valueExpression = PartialEvaluator.EvaluatePartially(arguments[i]);
                    var computedField = CreateComputedField(context, documentSerializer, member, valueExpression);
                    fields.Add(computedField);
                }
            }

            return AstStage.Set(fields);
        }

        private static AstStage TranslateNewWithOptionalMemberInitializers(TranslationContext context, IBsonDocumentSerializer documentSerializer, MemberInitExpression memberInitExpression)
        {
            var fields = new List<AstComputedField>();
            if (memberInitExpression != null)
            {
                var bindings = memberInitExpression.Bindings;

                for (var i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];
                    if (binding is not MemberAssignment assignment)
                    {
                        throw new ExpressionNotSupportedException(memberInitExpression, because: $"the member initializer for {binding.Member.Name} is not a simple assignment");
                    }

                    var member = binding.Member;
                    var valueExpression = PartialEvaluator.EvaluatePartially(assignment.Expression);
                    var computedField = CreateComputedField(context, documentSerializer, member, valueExpression);
                    fields.Add(computedField);
                }
            }

            return AstStage.Set(fields);
        }

        private static AstComputedField CreateComputedField(TranslationContext context, IBsonDocumentSerializer documentSerializer, MemberInfo member, Expression valueExpression)
        {
            string elementName;
            AstExpression valueAst;
            if (documentSerializer.TryGetMemberSerializationInfo(member.Name, out var serializationInfo))
            {
                elementName = serializationInfo.ElementName;
                var memberSerializer = serializationInfo.Serializer;

                if (valueExpression is ConstantExpression constantValueExpression)
                {
                    var value = constantValueExpression.Value;
                    var serializedValue = SerializationHelper.SerializeValue(memberSerializer, value);
                    valueAst = AstExpression.Constant(serializedValue);
                }
                else
                {
                    var valueSerializer = serializationInfo.Serializer;
                    var valueContext = context.WithKnownSerializer(valueSerializer);
                    var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(valueContext, valueExpression);
                    ThrowIfMemberAndValueSerializersAreNotCompatible(valueExpression, memberSerializer, valueTranslation.Serializer);
                    valueAst = valueTranslation.Ast;
                }
            }
            else
            {
                elementName = member.Name;
                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                valueAst = valueTranslation.Ast;
            }

            return AstExpression.ComputedField(elementName, valueAst);
        }

        private static void ThrowIfMemberAndValueSerializersAreNotCompatible(Expression expression, IBsonSerializer memberSerializer, IBsonSerializer valueSerializer)
        {
            // TODO: depends on CSHARP-3315
            if (!memberSerializer.Equals(valueSerializer))
            {
                throw new ExpressionNotSupportedException(expression, because: "member and value serializers are not compatible");
            }
        }
    }
}

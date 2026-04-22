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
using System.Linq;
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
        public static AstStage Translate(IBsonSerializer inputSerializer, LambdaExpression expression, IBsonSerializationDomain serializationDomain, ExpressionTranslationOptions translationOptions)
        {
            if (!DocumentSerializerHelper.AreMembersRepresentedAsFields(inputSerializer, out var documentSerializer))
            {
                throw new ExpressionNotSupportedException(expression, because: $"serializer {inputSerializer.GetType()} does not represent members as fields");
            }

            if (IsNewAnonymousClass(expression, out var newExpression))
            {
                return TranslateNewAnonymousClass(newExpression, expression, documentSerializer, serializationDomain, translationOptions);
            }

            if (IsNewWithOptionalMemberInitializers(expression, out var memberInitExpression))
            {
                return TranslateNewWithOptionalMemberInitializers(memberInitExpression, expression, documentSerializer, serializationDomain, translationOptions);
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

        private static AstStage TranslateNewAnonymousClass(NewExpression newExpression, LambdaExpression expression, IBsonDocumentSerializer documentSerializer, IBsonSerializationDomain serializationDomain, ExpressionTranslationOptions translationOptions)
        {
            var members = newExpression.Members; // will be null in the case of "new { }"
            var arguments = newExpression.Arguments;

            var fields = new List<AstComputedField>();
            if (members != null)
            {
                for (var i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    var computedField = CreateComputedField(member, arguments[i], expression, documentSerializer, serializationDomain, translationOptions);
                    fields.Add(computedField);
                }
            }

            return AstStage.Set(fields);
        }

        private static AstStage TranslateNewWithOptionalMemberInitializers(MemberInitExpression memberInitExpression, LambdaExpression expression, IBsonDocumentSerializer documentSerializer, IBsonSerializationDomain serializationDomain, ExpressionTranslationOptions translationOptions)
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
                    var computedField = CreateComputedField(member, assignment.Expression, expression, documentSerializer, serializationDomain, translationOptions);
                    fields.Add(computedField);
                }
            }

            return AstStage.Set(fields);
        }

        private static AstComputedField CreateComputedField(MemberInfo member, Expression valueExpression, LambdaExpression rootExpression, IBsonDocumentSerializer documentSerializer, IBsonSerializationDomain serializationDomain, ExpressionTranslationOptions translationOptions)
        {
            valueExpression = LinqExpressionPreprocessor.Preprocess(valueExpression);
            var elementName = member.Name;

            var rootDocumentParameter = rootExpression.Parameters.Single();
            var initialSerializers = new List<(Expression Node, IBsonSerializer Serializer)> { (rootDocumentParameter, documentSerializer) };
            if (documentSerializer.TryGetMemberSerializationInfo(member.Name, out var serializationInfo))
            {
                elementName = serializationInfo.ElementName;

                if (valueExpression is ConstantExpression constantValueExpression)
                {
                    var value = constantValueExpression.Value;
                    var serializedValue = SerializationHelper.SerializeValue(serializationInfo.Serializer, value);

                    return AstExpression.ComputedField(elementName, AstExpression.Constant(serializedValue));
                }

                if (valueExpression.Type == serializationInfo.Serializer.ValueType)
                {
                    initialSerializers.Add((valueExpression, serializationInfo.Serializer));
                }
            }

            var context = TranslationContext.Create(serializationDomain, valueExpression, initialSerializers, translationOptions);
            var symbol = context.CreateRootSymbol(rootDocumentParameter, documentSerializer);
            context = context.WithSymbol(symbol);

            var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
            if (serializationInfo != null)
            {
                ThrowIfMemberAndValueSerializersAreNotCompatible(valueExpression, serializationInfo.Serializer, valueTranslation.Serializer);
            }

            return AstExpression.ComputedField(elementName, valueTranslation.Ast);
        }

        private static void ThrowIfMemberAndValueSerializersAreNotCompatible(Expression expression, IBsonSerializer memberSerializer, IBsonSerializer valueSerializer)
        {
            if (memberSerializer.ValueType != valueSerializer.ValueType &&
                memberSerializer.ValueType.IsAssignableFrom(valueSerializer.ValueType))
            {
                valueSerializer = valueSerializer.GetBaseTypeSerializer(memberSerializer.ValueType);
            }

            if (!memberSerializer.Equals(valueSerializer))
            {
                throw new ExpressionNotSupportedException(expression, because: "member and value serializers are not compatible");
            }
        }
    }
}

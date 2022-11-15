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
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class MemberInitExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MemberInitExpression expression)
        {
            var computedFields = new List<AstComputedField>();
            var classMap = CreateClassMap(expression.Type);

            var newExpression = expression.NewExpression;
            var constructorParameters = newExpression.Constructor.GetParameters();
            var constructorArguments = newExpression.Arguments;
            for (var i = 0; i < constructorParameters.Length; i++)
            {
                var constructorParameter = constructorParameters[i];
                var memberMap = FindMatchingMemberMap(expression, classMap, constructorParameter);

                var argumentExpression = constructorArguments[i];
                var argumentTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, argumentExpression);
                computedFields.Add(AstExpression.ComputedField(memberMap.ElementName, argumentTranslation.Ast));

                memberMap.SetSerializer(argumentTranslation.Serializer);
            }

            foreach (var binding in expression.Bindings)
            {
                var memberAssignment = (MemberAssignment)binding;
                var member = memberAssignment.Member;
                var memberMap = FindMemberMap(expression, classMap, member.Name);

                var valueExpression = memberAssignment.Expression;
                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                computedFields.Add(AstExpression.ComputedField(memberMap.ElementName, valueTranslation.Ast));

                memberMap.SetSerializer(valueTranslation.Serializer);
            }

            var ast = AstExpression.ComputedDocument(computedFields);

            classMap.Freeze();
            var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(expression.Type);
            var serializer = (IBsonSerializer)Activator.CreateInstance(serializerType, classMap);

            return new AggregationExpression(expression, ast, serializer);
        }

        private static BsonClassMap CreateClassMap(Type classType)
        {
            BsonClassMap baseClassMap = null;
            if (classType.BaseType  != null)
            {
                baseClassMap = CreateClassMap(classType.BaseType);
            }

            var classMapType = typeof(BsonClassMap<>).MakeGenericType(classType);
            var constructorInfo = classMapType.GetConstructor(new Type[] { typeof(BsonClassMap) });
            var classMap = (BsonClassMap)constructorInfo.Invoke(new object[] { baseClassMap });
            classMap.AutoMap();
            classMap.IdMemberMap?.SetElementName("_id"); // normally happens when Freeze is called but we need it sooner here

            return classMap;
        }

        private static BsonMemberMap FindMatchingMemberMap(Expression expression, BsonClassMap classMap, ParameterInfo parameterInfo)
        {
            foreach (var memberMap in classMap.DeclaredMemberMaps)
            {
                if (memberMap.MemberType == parameterInfo.ParameterType && memberMap.MemberName.Equals(parameterInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return memberMap;
                }
            }

            if (classMap.BaseClassMap != null)
            {
                return FindMatchingMemberMap(expression, classMap.BaseClassMap, parameterInfo);
            }

            throw new ExpressionNotSupportedException(expression, because: $"can't find matching property for constructor parameter : {parameterInfo.Name}");
        }

        private static BsonMemberMap FindMemberMap(Expression expression, BsonClassMap classMap, string memberName)
        {
            foreach (var memberMap in classMap.DeclaredMemberMaps)
            {
                if (memberMap.MemberName == memberName)
                {
                    return memberMap;
                }
            }

            if (classMap.BaseClassMap != null)
            {
                return FindMemberMap(expression, classMap.BaseClassMap, memberName);
            }

            throw new ExpressionNotSupportedException(expression, because: $"can't find member map: {memberName}");
        }
    }
}

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
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class NewExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, NewExpression expression)
        {
            var expressionType = expression.Type;
            var constructorInfo = expression.Constructor;
            var arguments = expression.Arguments.ToArray();
            var members = expression.Members;

            if (expressionType == typeof(DateTime))
            {
                return NewDateTimeExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }
            if (expressionType.IsConstructedGenericType && expressionType.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                return NewHashSetExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }
            if (expressionType.IsConstructedGenericType && expressionType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return NewListExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }
            if (NewTupleExpressionToAggregationExpressionTranslator.CanTranslate(expression))
            {
                return NewTupleExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }

            var classMapType = typeof(BsonClassMap<>).MakeGenericType(expressionType);
            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);
            var computedFields = new List<AstComputedField>();

            // if Members is not null then trust Members more than the constructor parameter names (which are compiler generated for anonymous types)
            if (members == null)
            {
                var membersList = constructorInfo.GetParameters().Select(p => GetMatchingMember(expression, p.Name)).ToList();
                members = new ReadOnlyCollection<MemberInfo>(membersList);
            }

            for (var i = 0; i < arguments.Length; i++)
            {
                var valueExpression = arguments[i];
                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                var valueType = valueExpression.Type;
                var valueSerializer = valueTranslation.Serializer ?? BsonSerializer.LookupSerializer(valueType);
                var defaultValue = GetDefaultValue(valueType);
                var memberMap = classMap.MapMember(members[i]).SetSerializer(valueSerializer).SetDefaultValue(defaultValue);
                computedFields.Add(AstExpression.ComputedField(memberMap.ElementName, valueTranslation.Ast));
            }

            // map any public fields or properties that didn't match a constructor argument
            foreach (var member in expressionType.GetFields().Cast<MemberInfo>().Concat(expressionType.GetProperties()))
            {
                if (!members.Contains(member))
                {
                    var valueType = member switch
                    {
                        FieldInfo fieldInfo => fieldInfo.FieldType,
                        PropertyInfo propertyInfo => propertyInfo.PropertyType,
                        _ => throw new Exception($"Unexpected member type: {member.MemberType}")
                    };
                    var valueSerializer = context.KnownSerializersRegistry.GetSerializer(expression, valueType);
                    var defaultValue = GetDefaultValue(valueType);
                    classMap.MapMember(member).SetSerializer(valueSerializer).SetDefaultValue(defaultValue);
                }
            }

            classMap.MapConstructor(constructorInfo, members.Select(m => m.Name).ToArray());
            classMap.Freeze();

            var ast = AstExpression.ComputedDocument(computedFields);
            var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(expression.Type);
            // Note that we should use context.KnownSerializersRegistry to find the serializer,
            // but the above implementation builds up computedFields during the mapping process.
            // We need to figure out how to resolve the serializer from KnownSerializers and then
            // populate computedFields from that resolved serializer.
            var serializer = (IBsonSerializer)Activator.CreateInstance(serializerType, classMap);

            return new AggregationExpression(expression, ast, serializer);
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }

        private static MemberInfo GetMatchingMember(NewExpression expression, string constructorParameterName)
        {
            foreach (var field in expression.Type.GetFields())
            {
                if (field.Name.Equals(constructorParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return field;
                }
            }

            foreach (var property in expression.Type.GetProperties())
            {
                if (property.Name.Equals(constructorParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return property;
                }
            }

            throw new ExpressionNotSupportedException(expression, because: $"constructor parameter {constructorParameterName} does not match any public field or property");
        }
    }
}

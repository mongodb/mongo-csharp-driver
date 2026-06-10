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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    public IBsonSerializer CreateNewExpressionSerializer(
        Expression expression,
        NewExpression newExpression,
        IReadOnlyList<MemberBinding> bindings)
    {
        var constructorInfo = newExpression.Constructor; // note: can be null when using the default constructor with a struct
        var constructorArguments = newExpression.Arguments;
        var classMap = CreateClassMap(newExpression.Type, constructorInfo, out var creatorMap);

        if (constructorInfo != null && creatorMap != null)
        {
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
                    var constructorArgumentExpression = constructorArguments[i];
                    if (!IsKnown(constructorArgumentExpression, out var constructorArgumentSerializer))
                    {
                        return null;
                    }
                    var memberMap = EnsureMemberMap(expression, classMap, creatorMapParameter);
                    EnsureDefaultValue(memberMap);
                    var memberSerializer = CoerceSourceSerializerToMemberSerializer(memberMap, constructorArgumentSerializer);
                    memberMap.SetSerializer(memberSerializer);
                }
            }
        }

        if (bindings != null)
        {
            foreach (var binding in bindings)
            {
                var memberAssignment = (MemberAssignment)binding;
                var member = memberAssignment.Member;
                var memberMap = FindMemberMap(expression, classMap, member.Name);
                var valueExpression = memberAssignment.Expression;
                if (!IsKnown(valueExpression, out var valueSerializer))
                {
                    return null;
                }
                var memberSerializer = CoerceSourceSerializerToMemberSerializer(memberMap, valueSerializer);
                memberMap.SetSerializer(memberSerializer);
            }
        }

        classMap.Freeze();

        var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(newExpression.Type);
        return (IBsonSerializer)Activator.CreateInstance(serializerType, classMap);
    }

    private static BsonClassMap CreateClassMap(Type classType, ConstructorInfo constructorInfo, out BsonCreatorMap creatorMap)
    {
        BsonClassMap baseClassMap = null;
        if (classType.BaseType != null)
        {
            baseClassMap = CreateClassMap(classType.BaseType, null, out _);
        }

        var classMapType = typeof(BsonClassMap<>).MakeGenericType(classType);
        var classMapConstructorInfo = classMapType.GetConstructor(new Type[] { typeof(BsonClassMap) });
        var classMap = (BsonClassMap)classMapConstructorInfo.Invoke(new object[] { baseClassMap });
        if (constructorInfo != null)
        {
            creatorMap = classMap.MapConstructor(constructorInfo);
        }
        else
        {
            creatorMap = null;
        }

        classMap.AutoMap();
        classMap.IdMemberMap?.SetElementName("_id"); // normally happens when Freeze is called but we need it sooner here

        return classMap;
    }

    private static IBsonSerializer CoerceSourceSerializerToMemberSerializer(BsonMemberMap memberMap, IBsonSerializer sourceSerializer)
    {
        var memberType = memberMap.MemberType;
        var memberSerializer = memberMap.GetSerializer();
        var sourceType = sourceSerializer.ValueType;

        if (memberType != sourceType &&
            memberType.ImplementsIEnumerable(out var memberItemType) &&
            sourceType.ImplementsIEnumerable(out var sourceItemType) &&
            sourceItemType == memberItemType &&
            sourceSerializer is IBsonArraySerializer sourceArraySerializer &&
            sourceArraySerializer.TryGetItemSerializationInfo(out var sourceItemSerializationInfo) &&
            memberSerializer is IChildSerializerConfigurable memberChildSerializerConfigurable)
        {
            var sourceItemSerializer = sourceItemSerializationInfo.Serializer;
            return memberChildSerializerConfigurable.WithChildSerializer(sourceItemSerializer);
        }

        return sourceSerializer;
    }

    private static BsonMemberMap EnsureMemberMap(Expression expression, BsonClassMap classMap, MemberInfo creatorMapParameter)
    {
        var declaringClassMap = classMap;
        while (declaringClassMap.ClassType != creatorMapParameter.DeclaringType)
        {
            declaringClassMap = declaringClassMap.BaseClassMap;

            if (declaringClassMap == null)
            {
                throw new ExpressionNotSupportedException(expression, because: $"couldn't find matching property for constructor parameter: {creatorMapParameter.Name}");
            }
        }

        foreach (var memberMap in declaringClassMap.DeclaredMemberMaps)
        {
            if (MemberMapMatchesCreatorMapParameter(memberMap, creatorMapParameter))
            {
                return memberMap;
            }
        }

        return declaringClassMap.MapMember(creatorMapParameter);

        static bool MemberMapMatchesCreatorMapParameter(BsonMemberMap memberMap, MemberInfo creatorMapParameter)
        {
            var memberInfo = memberMap.MemberInfo;
            return
                memberInfo.MemberType == creatorMapParameter.MemberType &&
                memberInfo.Name.Equals(creatorMapParameter.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void EnsureDefaultValue(BsonMemberMap memberMap)
    {
        if (memberMap.IsDefaultValueSpecified)
        {
            return;
        }

        var defaultValue = memberMap.MemberType.IsValueType ? Activator.CreateInstance(memberMap.MemberType) : null;
        memberMap.SetDefaultValue(defaultValue);
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

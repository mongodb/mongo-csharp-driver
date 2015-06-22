/* Copyright 2010-2014 MongoDB Inc.
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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors
{
    internal class SerializerBuilder
    {
        public static IBsonSerializer Build(Expression node, IBsonSerializerRegistry serializerRegistry)
        {
            var builder = new SerializerBuilder(serializerRegistry);
            return builder.Build(node);
        }

        private IBsonSerializerRegistry _serializerRegistry;

        private SerializerBuilder(IBsonSerializerRegistry serializerRegistry)
        {
            _serializerRegistry = serializerRegistry;
        }

        public IBsonSerializer Build(Expression node)
        {
            if (node is ISerializationExpression)
            {
                return ((ISerializationExpression)node).SerializationInfo.Serializer;
            }

            IBsonSerializer serializer;
            switch (node.NodeType)
            {
                case ExpressionType.MemberInit:
                    serializer = BuildMemberInit((MemberInitExpression)node);
                    break;
                case ExpressionType.New:
                    serializer = BuildNew((NewExpression)node);
                    break;
                default:
                    if (!PreviouslyUsedSerializerFinder.TryFindSerializer(node, out serializer))
                    {
                        serializer = _serializerRegistry.GetSerializer(node.Type);
                    }
                    break;
            }

            return serializer;
        }

        protected IBsonSerializer BuildMemberInit(MemberInitExpression node)
        {
            var mapping = ProjectionMapper.Map(node);
            return BuildProjectedSerializer(mapping);
        }

        protected IBsonSerializer BuildNew(NewExpression node)
        {
            var mapping = ProjectionMapper.Map(node);
            return BuildProjectedSerializer(mapping);
        }

        private IBsonSerializer BuildProjectedSerializer(ProjectionMapping mapping)
        {
            // We are building a serializer specifically for a projected type based
            // on serialization information collected from other serializers.
            // We cannot cache this in the serializer registry because the compiler reuses 
            // the same anonymous type definition in different contexts as long as they 
            // are structurally equatable. As such, it might be that two different queries 
            // projecting the same shape might need to be deserialized differently.
            var classMap = BuildClassMap(mapping.Expression.Type, mapping);

            var mappedParameters = mapping.Members
                .Where(x => x.Parameter != null)
                .OrderBy(x => x.Parameter.Position)
                .Select(x => x.Member)
                .ToList();

            if (mappedParameters.Count > 0)
            {
                classMap.MapConstructor(mapping.Constructor)
                    .SetArguments(mappedParameters);
            }

            var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(mapping.Expression.Type);
            return (IBsonSerializer)Activator.CreateInstance(serializerType, classMap.Freeze());
        }

        private BsonClassMap BuildClassMap(Type type, ProjectionMapping mapping)
        {
            if (type == null || type == typeof(object))
            {
                return null;
            }

            var baseClassMap = BuildClassMap(type.BaseType, mapping);
            if (baseClassMap != null)
            {
                baseClassMap.Freeze();
            }
            var classMap = new BsonClassMap(type, baseClassMap);

            foreach (var memberMapping in mapping.Members.Where(x => x.Member.DeclaringType == type))
            {
                var serializationExpression = memberMapping.Expression as ISerializationExpression;
                if (serializationExpression == null)
                {
                    var serializer = Build(memberMapping.Expression);
                    var serializationInfo = new BsonSerializationInfo(
                        memberMapping.Member.Name,
                        serializer,
                        GetMemberType(memberMapping.Member));
                    serializationExpression = new SerializationExpression(
                        memberMapping.Expression,
                        serializationInfo);
                }

                var memberMap = classMap.MapMember(memberMapping.Member)
                    .SetSerializer(serializationExpression.SerializationInfo.Serializer)
                    .SetElementName(memberMapping.Member.Name);

                if (classMap.IdMemberMap == null && serializationExpression is GroupIdExpression)
                {
                    classMap.SetIdMember(memberMap);
                }
            }

            return classMap;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new MongoInternalException("Can't get member type.");
            }
        }

        private class PreviouslyUsedSerializerFinder : ExtensionExpressionVisitor
        {
            public static bool TryFindSerializer(Expression node, out IBsonSerializer serializer)
            {
                var finder = new PreviouslyUsedSerializerFinder(node.Type);
                finder.Visit(node);

                serializer = finder._serializer;
                return serializer != null;
            }

            private readonly Type _valueType;
            private IBsonSerializer _serializer;

            private PreviouslyUsedSerializerFinder(Type valueType)
            {
                _valueType = valueType;
            }

            public override Expression Visit(Expression node)
            {
                if (_serializer != null)
                {
                    return node;
                }

                return base.Visit(node);
            }

            protected internal override Expression VisitSerialization(SerializationExpression node)
            {
                if (node.SerializationInfo.Serializer.ValueType == _valueType)
                {
                    _serializer = node.SerializationInfo.Serializer;
                    return node;
                }

                return base.Visit(node);
            }

        }
    }
}

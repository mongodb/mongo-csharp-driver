﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers.KnownSerializers
{
    internal class KnownSerializerFinder : ExpressionVisitor
    {
        #region static
        // public static methods
        public static KnownSerializersRegistry FindKnownSerializers(Expression root, IBsonDocumentSerializer rootSerializer)
        {
            var visitor = new KnownSerializerFinder(root, rootSerializer);
            visitor.Visit(root);
            return visitor._registry;
        }
        #endregion

        // private fields
        private KnownSerializersNode _currentKnownSerializersNode;
        private IBsonDocumentSerializer _currentSerializer;
        private readonly KnownSerializersRegistry _registry = new();
        private readonly Expression _root;
        private readonly IBsonDocumentSerializer _rootSerializer;

        // constructors
        private KnownSerializerFinder(Expression root, IBsonDocumentSerializer rootSerializer)
        {
            _rootSerializer = rootSerializer;
            _root = root;
        }

        // public methods
        public override Expression Visit(Expression node)
        {
            if (node == null)
            {
                return null;
            }

            _currentKnownSerializersNode = new KnownSerializersNode(node, _currentKnownSerializersNode);

            if (node == _root)
            {
                _currentSerializer = _rootSerializer;
            }

            var result = base.Visit(node);
            _registry.Add(node, _currentKnownSerializersNode);

            var parent = _currentKnownSerializersNode.Parent;
            if (ShouldPropagateKnownSerializersToParent(parent))
            {
                parent.AddKnownSerializersFromChild(_currentKnownSerializersNode);
            }
            _currentKnownSerializersNode = parent;

            return result;
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var result = base.VisitConditional(node);

            if (_currentKnownSerializersNode.KnownSerializers.TryGetValue(node.Type, out var resultSerializers) &&
                resultSerializers.Count > 1)
            {
                var ifTrueSerializer = _registry.GetSerializerAtThisLevel(node.IfTrue);
                var ifFalseSerializer = _registry.GetSerializerAtThisLevel(node.IfFalse);

                if (ifTrueSerializer != null && ifFalseSerializer != null && !ifTrueSerializer.Equals(ifFalseSerializer))
                {
                    throw new ExpressionNotSupportedException(node, because: "IfTrue and IfFalse expressions have different serializers");
                }

                if (ifTrueSerializer != null)
                {
                    _currentKnownSerializersNode.SetKnownSerializerForType(node.Type, ifTrueSerializer);
                }
                else if (ifFalseSerializer != null)
                {
                    _currentKnownSerializersNode.SetKnownSerializerForType(node.Type, ifFalseSerializer);
                }
            }

            return result;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var result = base.VisitBinary(node);

            if (result is BinaryExpression binaryExpression)
            {
                if (BinaryExpressionToAggregationExpressionTranslator.IsEnumComparisonExpression(binaryExpression))
                {
                    var leftExpression = ConvertHelper.RemoveConvertToEnumUnderlyingType(binaryExpression.Left);
                    var rightExpression = ConvertHelper.RemoveConvertToEnumUnderlyingType(binaryExpression.Right);

                    if (leftExpression is ConstantExpression leftConstantExpression)
                    {
                        var rightExpressionSerializer = _registry.GetSerializer(rightExpression);
                        var leftExpressionSerializer = EnumUnderlyingTypeSerializer.Create(rightExpressionSerializer);
                        _registry.SetNodeSerializer(leftExpression, leftExpressionSerializer);
                    }

                    if (rightExpression is ConstantExpression rightConstantExpression)
                    {
                        var leftExpressionSerializer = _registry.GetSerializer(leftExpression);
                        var rightExpressionSerializer = EnumUnderlyingTypeSerializer.Create(leftExpressionSerializer);
                        _registry.SetNodeSerializer(rightExpression, rightExpressionSerializer);
                    }
                }
            }

            return result;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var result = base.VisitMember(node);

            var containerSerializer = _registry.GetSerializer(node.Expression);
            if (containerSerializer is IBsonDocumentSerializer documentSerializer)
            {
                if (documentSerializer.TryGetMemberSerializationInfo(node.Member.Name, out var memberSerializationInfo))
                {
                    _currentKnownSerializersNode.AddKnownSerializer(node.Type, memberSerializationInfo.Serializer);

                    if (memberSerializationInfo.Serializer is IBsonDocumentSerializer bsonDocumentSerializer)
                    {
                        _currentSerializer = bsonDocumentSerializer;
                    }
                    else
                    {
                        _currentSerializer = null;
                    }
                }
            }

            return result;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var result = base.VisitMethodCall(node);

            if (node.Method.Is(QueryableMethod.OfType) || node.Method.Is(EnumerableMethod.OfType))
            {
                var actualType = node.Method.GetGenericArguments()[0];
                var serializer = BsonSerializer.LookupSerializer(actualType);
                _currentKnownSerializersNode.AddKnownSerializer(node.Type, serializer);
            }

            if (node.Method.DeclaringType == typeof(BsonValue) && node.Method.Name == "get_Item")
            {
                var serializer = BsonValueSerializer.Instance;
                _currentKnownSerializersNode.AddKnownSerializer(node.Type, serializer);
            }

            return result;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            var result = base.VisitNew(node);

            if (node.Type == _rootSerializer.ValueType)
            {
                return result;
            }

            IBsonSerializer serializer;
            if (node.Type == typeof(DateTime))
            {
                serializer = new DateTimeSerializer();
            }
            else if (node.Type == typeof(DateTimeOffset))
            {
                serializer = new DateTimeOffsetSerializer();
            }
            else
            {
                var classMapType = typeof(BsonClassMap<>).MakeGenericType(node.Type);
                var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);
                classMap.AutoMap();
                classMap.Freeze();

                var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(node.Type);
                serializer = (IBsonSerializer)Activator.CreateInstance(serializerType, classMap);
            }

            _currentKnownSerializersNode.AddKnownSerializer(node.Type, serializer);

            return result;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            var result = base.VisitParameter(node);

            if (node.Type == _rootSerializer.ValueType)
            {
                _currentSerializer = _rootSerializer;
                _currentKnownSerializersNode.AddKnownSerializer(node.Type, _rootSerializer);
            }

            if (_currentSerializer is IBsonArraySerializer arraySerializer &&
                arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo) &&
                node.Type == itemSerializationInfo.NominalType &&
                itemSerializationInfo.Serializer is IBsonDocumentSerializer documentSerializer)
            {
                _currentSerializer = documentSerializer;
                _currentKnownSerializersNode.AddKnownSerializer(node.Type, documentSerializer);
            }

            return result;
        }

        private bool ShouldPropagateKnownSerializersToParent(KnownSerializersNode parent)
        {
            if (parent == null)
            {
                return false;
            }

            return parent.Expression.NodeType switch
            {
                ExpressionType.MemberInit => false,
                ExpressionType.New => false,
                _ => true
            };
        }
    }
}

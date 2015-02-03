/* Copyright 2010-2014 10gen Inc.
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
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors
{
    internal class SerializationInfoBinder : MongoExpressionVisitor
    {
        // private fields
        private readonly Dictionary<MemberInfo, Expression> _memberMap;
        private readonly Dictionary<ParameterExpression, Expression> _parameterMap;
        private readonly IBsonSerializerRegistry _serializerRegistry;

        // constructors
        public SerializationInfoBinder(IBsonSerializerRegistry serializerRegistry)
        {
            _serializerRegistry = serializerRegistry;
            _memberMap = new Dictionary<MemberInfo, Expression>();
            _parameterMap = new Dictionary<ParameterExpression, Expression>();
        }

        // public methods
        public Expression Bind(Expression node)
        {
            return Visit(node);
        }

        public void RegisterMemberReplacement(MemberInfo member, Expression replacement)
        {
            _memberMap[member] = replacement;
        }

        public void RegisterParameterReplacement(ParameterExpression parameter, Expression replacement)
        {
            _parameterMap[parameter] = replacement;
        }

        // protected methods
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var newNode = base.VisitBinary(node);
            var binary = newNode as BinaryExpression;
            if (binary != null && binary.NodeType == ExpressionType.ArrayIndex)
            {
                var serializationExpression = binary.Left as IBsonSerializationInfoExpression;
                if (serializationExpression != null)
                {
                    var arraySerializer = serializationExpression.SerializationInfo.Serializer as IBsonArraySerializer;
                    var indexExpression = binary.Right as ConstantExpression;
                    if (arraySerializer != null && indexExpression != null && indexExpression.Type == typeof(int))
                    {
                        var index = (int)indexExpression.Value;
                        var itemSerializationInfo = arraySerializer.GetItemSerializationInfo();
                        itemSerializationInfo = new BsonSerializationInfo(
                            index.ToString(),
                            itemSerializationInfo.Serializer,
                            itemSerializationInfo.NominalType);

                        var serializationInfo = serializationExpression.SerializationInfo.Merge(itemSerializationInfo);
                        newNode = new FieldExpression(binary, serializationInfo);
                    }
                }
            }

            return newNode;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Expression newNode;
            if (_memberMap.TryGetValue(node.Member, out newNode))
            {
                if (newNode is IBsonSerializationInfoExpression)
                {
                    return newNode;
                }

                var message = string.Format("Could not determine serialization information for member {0}", node.Member);
                throw new MongoInternalException(message);
            }

            newNode = base.VisitMember(node);
            var mex = newNode as MemberExpression;
            if (mex != null)
            {
                var serializationExpression = mex.Expression as IBsonSerializationInfoExpression;
                if (serializationExpression != null)
                {
                    var documentSerializer = serializationExpression.SerializationInfo.Serializer as IBsonDocumentSerializer;
                    if (documentSerializer != null)
                    {
                        var memberSerializationInfo = documentSerializer.GetMemberSerializationInfo(node.Member.Name);
                        var serializationInfo = serializationExpression.SerializationInfo.Merge(memberSerializationInfo);
                        return new FieldExpression(mex, serializationInfo);
                    }
                }
            }

            return newNode;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case "ElementAt":
                    return BindElementAt(node);
                case "get_Item":
                    return BindGetItem(node);
                case "All":
                case "Any":
                case "Average":
                case "Count":
                case "LongCount":
                case "Max":
                case "Min":
                case "Select":
                case "SelectMany":
                case "Sum":
                    if (IsLinqMethod(node) && node.Arguments.Count == 2)
                    {
                        return BindTwoArgumentLinqMethodWithSelector(node);
                    }
                    break;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Expression replacement;
            if (_parameterMap.TryGetValue(node, out replacement))
            {
                return replacement;
            }

            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var newNode = base.VisitUnary(node);
            var unaryExpression = newNode as UnaryExpression;
            if (node != newNode &&
                unaryExpression != null &&
                !unaryExpression.Operand.Type.IsEnum && // enums are weird, so we skip them
                (newNode.NodeType == ExpressionType.Convert || newNode.NodeType == ExpressionType.ConvertChecked))
            {
                if (unaryExpression.Operand.Type.IsGenericType && unaryExpression.Operand.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(node.Operand.Type);
                    if (underlyingType.IsEnum)
                    {
                        // we skip enums because they are weird
                        return newNode;
                    }
                }

                var serializationExpression = unaryExpression.Operand as IBsonSerializationInfoExpression;
                if (serializationExpression != null)
                {
                    var serializer = _serializerRegistry.GetSerializer(node.Type);
                    var serializationInfo = new BsonSerializationInfo(serializationExpression.SerializationInfo.ElementName, serializer, node.Type);
                    if (unaryExpression.Operand is DocumentExpression)
                    {
                        return new DocumentExpression(newNode, serializationInfo);
                    }
                    else
                    {
                        return new FieldExpression(unaryExpression, serializationInfo);
                    }
                }
            }

            return newNode;
        }

        // private methods
        private Expression BindElementAt(MethodCallExpression node)
        {
            if (!IsLinqMethod(node))
            {
                return base.VisitMethodCall(node);
            }

            var newNode = base.VisitMethodCall(node);
            var methodCallExpression = newNode as MethodCallExpression;
            if (node != newNode && methodCallExpression != null &&
                (methodCallExpression.Method.DeclaringType == typeof(Enumerable) || methodCallExpression.Method.DeclaringType == typeof(Queryable)))
            {
                var serializationExpression = methodCallExpression.Arguments[0] as IBsonSerializationInfoExpression;
                if (serializationExpression != null)
                {
                    var arraySerializer = serializationExpression.SerializationInfo.Serializer as IBsonArraySerializer;
                    if (arraySerializer != null)
                    {
                        var index = (int)((ConstantExpression)methodCallExpression.Arguments[1]).Value;
                        var itemSerializationInfo = arraySerializer.GetItemSerializationInfo();
                        itemSerializationInfo = new BsonSerializationInfo(
                            index.ToString(),
                            itemSerializationInfo.Serializer,
                            itemSerializationInfo.NominalType);

                        var serializationInfo = serializationExpression.SerializationInfo.Merge(itemSerializationInfo);
                        return new FieldExpression(methodCallExpression, serializationInfo);
                    }
                }
            }

            return newNode;
        }

        private Expression BindGetItem(MethodCallExpression node)
        {
            var newNode = base.VisitMethodCall(node);
            var methodCallExpression = newNode as MethodCallExpression;
            if (node == newNode ||
                methodCallExpression == null ||
                node.Object == null ||
                node.Arguments.Count != 1 ||
                node.Arguments[0].NodeType != ExpressionType.Constant)
            {
                return newNode;
            }

            var serializationExpression = methodCallExpression.Object as IBsonSerializationInfoExpression;
            if (serializationExpression == null)
            {
                return newNode;
            }

            var indexExpression = (ConstantExpression)node.Arguments[0];
            var indexName = indexExpression.Value.ToString();
            if (indexExpression.Type == typeof(int) ||
                indexExpression.Type == typeof(uint) ||
                indexExpression.Type == typeof(long) ||
                indexExpression.Type == typeof(ulong))
            {
                var arraySerializer = serializationExpression.SerializationInfo.Serializer as IBsonArraySerializer;
                if (arraySerializer != null)
                {
                    var itemSerializationInfo = arraySerializer.GetItemSerializationInfo().WithNewName(indexName);
                    var serializationInfo = serializationExpression.SerializationInfo.Merge(itemSerializationInfo);
                    return new FieldExpression(methodCallExpression, serializationInfo);
                }
            }
            else if (indexExpression.Type == typeof(string))
            {
                var documentSerializer = serializationExpression.SerializationInfo.Serializer as IBsonDocumentSerializer;
                if (documentSerializer != null)
                {
                    var memberSerializationInfo = documentSerializer.GetMemberSerializationInfo(indexName);
                    var serializationInfo = serializationExpression.SerializationInfo.Merge(memberSerializationInfo);
                    return new FieldExpression(methodCallExpression, serializationInfo);
                }
            }

            return newNode;
        }

        private Expression BindTwoArgumentLinqMethodWithSelector(MethodCallExpression node)
        {
            List<Expression> arguments = new List<Expression>();
            arguments.Add(Visit(node.Arguments[0]));

            // we need to make sure that the serialization info for the parameter
            // is the item serialization from the parent IBsonArraySerializer
            var serializationExpression = arguments[0] as IBsonSerializationInfoExpression;
            if (serializationExpression != null)
            {
                var arraySerializer = serializationExpression.SerializationInfo.Serializer as IBsonArraySerializer;
                if (arraySerializer != null)
                {
                    var itemSerializationInfo = arraySerializer.GetItemSerializationInfo();
                    var lambda = (LambdaExpression)node.Arguments[1];
                    RegisterParameterReplacement(
                        lambda.Parameters[0],
                        new FieldExpression(
                            lambda.Parameters[0],
                            itemSerializationInfo.WithNewName(serializationExpression.SerializationInfo.ElementName)));
                }
            }

            arguments.Add(Visit(node.Arguments[1]));

            if (node.Arguments[0] != arguments[0] || node.Arguments[1] != arguments[1])
            {
                node = Expression.Call(node.Method, arguments.ToArray());
            }

            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            // Don't visit the parameters. We cannot replace a parameter expression
            // with a document and we don't have a new parameter type to use because
            // we don't know why we are binding yet.
            var newBody = Visit(node.Body);
            if (node.Body != newBody)
            {
                return Expression.Lambda<T>(newBody, node.Parameters);
            }

            return node;
        }
    }
}
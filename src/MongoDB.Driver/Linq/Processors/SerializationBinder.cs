/* Copyright 2015 MongoDB Inc.
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
using MongoDB.Driver.Linq.Processors.EmbeddedPipeline;

namespace MongoDB.Driver.Linq.Processors
{
    internal sealed class SerializationBinder : ExtensionExpressionVisitor
    {
        public static Expression Bind(Expression node, IBindingContext context, bool isClientSideProjection = false)
        {
            var binder = new SerializationBinder(context, isClientSideProjection);
            return binder.Visit(node);
        }

        private readonly IBindingContext _bindingContext;
        private readonly Dictionary<MemberInfo, Expression> _memberMap;
        private bool _isInEmbeddedPipeline;
        private readonly bool _isClientSideProjection;

        private SerializationBinder(IBindingContext bindingContext, bool isClientSideProjection)
        {
            _bindingContext = bindingContext;
            _isClientSideProjection = isClientSideProjection;
            _memberMap = new Dictionary<MemberInfo, Expression>();
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
            {
                return null;
            }

            Expression replacement;
            if (_bindingContext.TryGetExpressionMapping(node, out replacement))
            {
                return replacement;
            }

            return base.Visit(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var newNode = base.VisitBinary(node);
            var binaryExpression = newNode as BinaryExpression;
            if (binaryExpression != null && binaryExpression.NodeType == ExpressionType.ArrayIndex)
            {
                var serializationExpression = binaryExpression.Left as ISerializationExpression;
                if (serializationExpression != null)
                {
                    var arraySerializer = serializationExpression.Serializer as IBsonArraySerializer;
                    var indexExpression = binaryExpression.Right as ConstantExpression;
                    BsonSerializationInfo itemSerializationInfo;
                    if (arraySerializer != null &&
                        indexExpression != null &&
                        indexExpression.Type == typeof(int) &&
                        arraySerializer.TryGetItemSerializationInfo(out itemSerializationInfo))
                    {
                        var index = (int)indexExpression.Value;
                        var name = index >= 0 ? index.ToString() : "$";
                        var newName = serializationExpression.AppendFieldName(name);

                        newNode = new FieldExpression(newName, itemSerializationInfo.Serializer, binaryExpression);
                    }
                }
            }

            return newNode;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            // Don't visit the parameters. We cannot replace a parameter expression
            // with a document and we don't have a new parameter type to use because
            // we don't know why we are binding yet.
            return node.Update(
                Visit(node.Body),
                node.Parameters);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Expression newNode;
            if (_bindingContext.TryGetMemberMapping(node.Member, out newNode))
            {
                if (newNode is ISerializationExpression)
                {
                    return newNode;
                }

                var message = string.Format("Could not determine serialization information for member {0} in the expression tree {1}.",
                    node.Member,
                    node.ToString());
                throw new MongoInternalException(message);
            }

            newNode = base.VisitMember(node);
            var mex = newNode as MemberExpression;
            if (mex != null)
            {
                var serializationExpression = mex.Expression as ISerializationExpression;
                if (serializationExpression != null)
                {
                    var documentSerializer = serializationExpression.Serializer as IBsonDocumentSerializer;
                    BsonSerializationInfo memberSerializationInfo;
                    if (documentSerializer != null && documentSerializer.TryGetMemberSerializationInfo(node.Member.Name, out memberSerializationInfo))
                    {
                        newNode = new FieldExpression(
                            serializationExpression.AppendFieldName(memberSerializationInfo.ElementName),
                            memberSerializationInfo.Serializer,
                            mex);
                    }
                }
            }

            return newNode;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!_isClientSideProjection && EmbeddedPipelineBinder.SupportsNode(node))
            {
                return BindEmbeddedPipeline(node);
            }

            switch (node.Method.Name)
            {
                case "ElementAt":
                    return BindElementAt(node);
                case "get_Item":
                    return BindGetItem(node);
            }

            // Select and SelectMany are the only supported client-side projection operators
            if (_isClientSideProjection &&
                ExpressionHelper.IsLinqMethod(node, "Select", "SelectMany") &&
                node.Arguments.Count == 2)
            {
                return BindClientSideProjector(node);
            }

            return base.VisitMethodCall(node);
        }

        // private methods
        private Expression BindElementAt(MethodCallExpression node)
        {
            if (!ExpressionHelper.IsLinqMethod(node))
            {
                return base.VisitMethodCall(node);
            }

            var newNode = base.VisitMethodCall(node);
            var methodCallExpression = newNode as MethodCallExpression;
            if (node != newNode && methodCallExpression != null &&
                (methodCallExpression.Method.DeclaringType == typeof(Enumerable) || methodCallExpression.Method.DeclaringType == typeof(Queryable)))
            {
                var serializationExpression = methodCallExpression.Arguments[0] as ISerializationExpression;
                if (serializationExpression != null)
                {
                    var arraySerializer = serializationExpression.Serializer as IBsonArraySerializer;
                    BsonSerializationInfo itemSerializationInfo;
                    if (arraySerializer != null && arraySerializer.TryGetItemSerializationInfo(out itemSerializationInfo))
                    {
                        var index = (int)((ConstantExpression)methodCallExpression.Arguments[1]).Value;
                        var name = index >= 0 ? index.ToString() : "$";
                        var newName = serializationExpression.AppendFieldName(name);

                        newNode = new FieldExpression(newName, itemSerializationInfo.Serializer, methodCallExpression);
                    }
                }
            }

            return newNode;
        }

        private Expression BindEmbeddedPipeline(MethodCallExpression node)
        {
            var oldIsInEmbeddedPipeline = _isInEmbeddedPipeline;
            _isInEmbeddedPipeline = true;

            node = node.Update(
                Visit(node.Object),
                Visit(node.Arguments));

            _isInEmbeddedPipeline = oldIsInEmbeddedPipeline;
            if (_isInEmbeddedPipeline)
            {
                return node;
            }

            return EmbeddedPipelineBinder.Bind(node, _bindingContext);
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

            var serializationExpression = methodCallExpression.Object as ISerializationExpression;
            if (serializationExpression == null)
            {
                return newNode;
            }

            var indexExpression = (ConstantExpression)node.Arguments[0];
            var index = indexExpression.Value;
            switch (Type.GetTypeCode(index.GetType()))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    var arraySerializer = serializationExpression.Serializer as IBsonArraySerializer;
                    BsonSerializationInfo itemSerializationInfo;
                    if (arraySerializer != null && arraySerializer.TryGetItemSerializationInfo(out itemSerializationInfo))
                    {
                        string name;
                        switch (Type.GetTypeCode(indexExpression.Type))
                        {
                            case TypeCode.Int16:
                                name = (short)index >= 0 ? index.ToString() : "$";
                                break;
                            case TypeCode.Int32:
                                name = (int)index >= 0 ? index.ToString() : "$";
                                break;
                            case TypeCode.Int64:
                                name = (long)index >= 0 ? index.ToString() : "$";
                                break;
                            default:
                                name = index.ToString();
                                break;
                        }


                        return new FieldExpression(
                            serializationExpression.AppendFieldName(name),
                            itemSerializationInfo.Serializer,
                            methodCallExpression);
                    }
                    break;
                case TypeCode.String:
                    var documentSerializer = serializationExpression.Serializer as IBsonDocumentSerializer;
                    BsonSerializationInfo memberSerializationInfo;
                    if (documentSerializer != null && documentSerializer.TryGetMemberSerializationInfo(index.ToString(), out memberSerializationInfo))
                    {
                        return new FieldExpression(
                            serializationExpression.AppendFieldName(memberSerializationInfo.ElementName),
                            memberSerializationInfo.Serializer,
                            methodCallExpression);
                    }
                    break;
            }

            return node; // return the original node because we can't translate this expression.
        }

        private Expression BindClientSideProjector(MethodCallExpression node)
        {
            var arguments = new List<Expression>();
            arguments.Add(Visit(node.Arguments[0]));

            // we need to make sure that the serialization info for the parameter
            // is the item serialization from the parent IBsonArraySerializer
            var fieldExpression = arguments[0] as IFieldExpression;
            if (fieldExpression != null)
            {
                var arraySerializer = fieldExpression.Serializer as IBsonArraySerializer;
                BsonSerializationInfo itemSerializationInfo;
                if (arraySerializer != null && arraySerializer.TryGetItemSerializationInfo(out itemSerializationInfo))
                {
                    var lambda = ExpressionHelper.GetLambda(node.Arguments[1]);
                    _bindingContext.AddExpressionMapping(
                        lambda.Parameters[0],
                        new FieldExpression(
                            fieldExpression.FieldName,
                            itemSerializationInfo.Serializer,
                            lambda.Parameters[0]));
                }
            }

            arguments.Add(Visit(node.Arguments[1]));

            if (node.Arguments[0] != arguments[0] || node.Arguments[1] != arguments[1])
            {
                node = Expression.Call(node.Method, arguments.ToArray());
            }

            return node;
        }
    }
}
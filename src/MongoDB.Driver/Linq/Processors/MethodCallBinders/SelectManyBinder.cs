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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors.MethodCallBinders
{
    internal class SelectManyBinder : IMethodCallBinder
    {
        public Expression Bind(ProjectionExpression projection, ProjectionBindingContext context, MethodCallExpression node, IEnumerable<Expression> arguments)
        {
            var collectionLambda = ExtensionExpressionVisitor.GetLambda(arguments.First());
            var binder = new AccumulatorBinder(context.GroupMap, context.SerializerRegistry);
            binder.RegisterParameterReplacement(collectionLambda.Parameters[0], projection.Projector);

            var collectionSelector = binder.Bind(collectionLambda.Body) as SerializationExpression;
            if (collectionSelector == null)
            {
                var message = string.Format("Unable to determine the collection selector in the tree: {0}", node.ToString());
                throw new NotSupportedException(message);
            }
            var collectionArraySerializer = collectionSelector.SerializationInfo.Serializer as IBsonArraySerializer;
            BsonSerializationInfo collectionItemSerializationInfo;
            if (collectionArraySerializer == null || !collectionArraySerializer.TryGetItemSerializationInfo(out collectionItemSerializationInfo))
            {
                var message = string.Format("The collection selector's serializer must implement IBsonArraySerializer: {0}", node.ToString());
                throw new NotSupportedException(message);
            }

            Expression resultSelector;
            if (arguments.Count() == 2)
            {
                var resultLambda = ExtensionExpressionVisitor.GetLambda(arguments.Last());

                binder.RegisterParameterReplacement(resultLambda.Parameters[0], projection.Projector);
                binder.RegisterParameterReplacement(
                    resultLambda.Parameters[1],
                    new SerializationExpression(
                        resultLambda.Parameters[1],
                        collectionItemSerializationInfo.WithNewName(collectionSelector.SerializationInfo.ElementName)));

                resultSelector = binder.Bind(resultLambda.Body);
            }
            else
            {
                resultSelector = new SerializationExpression(
                    collectionSelector,
                    collectionItemSerializationInfo.WithNewName(collectionSelector.SerializationInfo.ElementName));
            }

            var projector = BuildProjector(resultSelector, context);

            return new ProjectionExpression(
                new SelectManyExpression(
                    projection.Source,
                    collectionSelector,
                    resultSelector),
                projector);
        }

        private Expression BuildProjector(Expression selector, ProjectionBindingContext context)
        {
            var selectorNode = selector;
            if (!(selectorNode is ISerializationExpression))
            {
                var serializer = SerializerBuilder.Build(selector, context.SerializerRegistry);
                BsonSerializationInfo info;
                switch (selector.NodeType)
                {
                    case ExpressionType.MemberInit:
                    case ExpressionType.New:
                        info = new BsonSerializationInfo(null, serializer, serializer.ValueType);
                        break;
                    default:
                        // this occurs when a computed field is used. This is a magic string
                        // that shouldn't ever be reference anywhere else...
                        info = new BsonSerializationInfo("__fld0", serializer, serializer.ValueType);
                        break;
                }

                selectorNode = new SerializationExpression(selector, info);
            }

            return selectorNode;
        }
    }
}

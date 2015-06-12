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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors.MethodCallBinders
{
    internal class GroupByWithResultSelectorBinder : IMethodCallBinder
    {
        public Expression Bind(ProjectionExpression projection, ProjectionBindingContext context, MethodCallExpression node, IEnumerable<Expression> arguments)
        {
            var id = BindId(projection, context, arguments.First());
            var selector = BindSelector(projection, context, id, arguments.Last());

            var projector = BuildProjector(selector, context);

            return new ProjectionExpression(
                new GroupByWithResultSelectorExpression(
                    projection.Source,
                    selector),
                projector);
        }

        private GroupIdExpression BindId(ProjectionExpression projection, ProjectionBindingContext context, Expression node)
        {
            var lambda = ExtensionExpressionVisitor.GetLambda(node);
            var binder = new AccumulatorBinder(context.GroupMap, context.SerializerRegistry);
            binder.RegisterParameterReplacement(lambda.Parameters[0], projection.Projector);
            var selector = binder.Bind(lambda.Body);

            if (!(selector is ISerializationExpression))
            {
                var serializer = SerializerBuilder.Build(selector, context.SerializerRegistry);
                selector = new SerializationExpression(
                    selector,
                    new BsonSerializationInfo(null, serializer, serializer.ValueType));
            }

            return new GroupIdExpression(selector, ((ISerializationExpression)selector).SerializationInfo);
        }

        private Expression BindSelector(ProjectionExpression projection, ProjectionBindingContext context, Expression id, Expression node)
        {
            var lambda = ExtensionExpressionVisitor.GetLambda(node);
            var binder = new AccumulatorBinder(context.GroupMap, context.SerializerRegistry);

            var serializer = SerializerBuilder.Build(projection.Projector, context.SerializerRegistry);
            var sequenceSerializer = (IBsonSerializer)Activator.CreateInstance(
                typeof(ArraySerializer<>).MakeGenericType(projection.Projector.Type),
                new object[] { serializer });
            var sequenceInfo = new BsonSerializationInfo(
                null,
                sequenceSerializer,
                sequenceSerializer.ValueType);
            var sequenceExpression = new SerializationExpression(
                lambda.Parameters[1],
                sequenceInfo);

            binder.RegisterParameterReplacement(lambda.Parameters[0], id);
            binder.RegisterParameterReplacement(lambda.Parameters[1], sequenceExpression);
            var correlationId = Guid.NewGuid();
            context.GroupMap.Add(sequenceExpression, correlationId);
            var bound = binder.Bind(lambda.Body);
            return CorrelatedAccumulatorRemover.Remove(bound, correlationId);
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

        private class CorrelatedAccumulatorRemover : ExtensionExpressionVisitor
        {
            public static Expression Remove(Expression node, Guid correlationId)
            {
                var remover = new CorrelatedAccumulatorRemover(correlationId);
                return remover.Visit(node);
            }

            private readonly Guid _correlationId;

            private CorrelatedAccumulatorRemover(Guid correlationId)
            {
                _correlationId = correlationId;
            }

            protected internal override Expression VisitCorrelatedAccumulator(CorrelatedAccumulatorExpression node)
            {
                if(node.CorrelationId == _correlationId)
                {
                    return Visit(node.Accumulator);
                }

                return base.VisitCorrelatedAccumulator(node);
            }
        }
    }
}

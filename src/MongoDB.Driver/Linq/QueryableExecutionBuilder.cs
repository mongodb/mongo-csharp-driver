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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq
{
    internal class QueryableExecutionBuilder : ExtensionExpressionVisitor
    {
        public static Expression Build(Expression node, Expression provider, IBsonSerializerRegistry serializerRegistry)
        {
            return new QueryableExecutionBuilder(provider, null, serializerRegistry).Visit(node);
        }

        public static Expression BuildAsync(Expression node, Expression provider, Expression cancellationToken, IBsonSerializerRegistry serializerRegistry)
        {
            return new QueryableExecutionBuilder(provider, cancellationToken, serializerRegistry).Visit(node);
        }

        private readonly Expression _cancellationToken;
        private readonly Expression _provider;
        private readonly IBsonSerializerRegistry _serializerRegistry;

        public QueryableExecutionBuilder(Expression provider, Expression cancellationToken, IBsonSerializerRegistry serializerRegistry)
        {
            _provider = provider;
            _cancellationToken = cancellationToken;
            _serializerRegistry = serializerRegistry;
        }

        protected internal override Expression VisitProjection(ProjectionExpression node)
        {
            var model = QueryableExecutionModelBuilder.Build(node, _serializerRegistry);

            Expression executor;
            if (_cancellationToken != null)
            {
                // we are async
                executor = Expression.Call(
                    _provider,
                    "ExecuteAsync",
                    Type.EmptyTypes,
                    Expression.Constant(model, typeof(QueryableExecutionModel)),
                    _cancellationToken);

                if (node.Aggregator != null)
                {
                    executor = Expression.Invoke(
                        node.Aggregator,
                        Expression.Convert(executor, node.Aggregator.Parameters[0].Type),
                        _cancellationToken);
                }
            }
            else
            {
                // we are sync
                executor = Expression.Call(
                    _provider,
                    "Execute",
                    Type.EmptyTypes,
                    Expression.Constant(model, typeof(QueryableExecutionModel)));

                if (node.Aggregator != null)
                {
                    executor = Expression.Invoke(
                        node.Aggregator,
                        Expression.Convert(executor, node.Aggregator.Parameters[0].Type));
                }
            }

            return executor;
        }
    }
}

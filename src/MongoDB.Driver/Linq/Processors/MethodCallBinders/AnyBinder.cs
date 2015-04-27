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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors.MethodCallBinders
{
    internal class AnyBinder : MethodCallBinderBase
    {
        public override Expression Bind(ProjectionExpression projection, ProjectionBindingContext context, MethodCallExpression node, IEnumerable<Expression> arguments)
        {
            LambdaExpression aggregator;
            if (node.Method.Name.EndsWith("Async"))
            {
                aggregator = CreateAsyncAggregator();
            }
            else
            {
                aggregator = CreateSyncAggregator();
            }

            var source = projection.Source;
            var argument = arguments.FirstOrDefault();
            if (argument != null && ExtensionExpressionVisitor.IsLambda(argument))
            {
                source = BindPredicate(projection, context, source, argument);
            }

            source = new TakeExpression(source, 1);

            var serializer = context.SerializerRegistry.GetSerializer(typeof(int));
            var accumulator = new AccumulatorExpression(typeof(int), AccumulatorType.Count, null);
            var serializationAccumulator = new SerializationExpression(
                accumulator,
                new BsonSerializationInfo("__agg0", serializer, serializer.ValueType));

            var rootAccumulator = new RootAccumulatorExpression(source, serializationAccumulator);

            return new ProjectionExpression(
                rootAccumulator,
                serializationAccumulator,
                aggregator);
        }

        private LambdaExpression CreateSyncAggregator()
        {
            var sourceParameter = Expression.Parameter(typeof(IEnumerable<int>), "source");
            return Expression.Lambda(
                Expression.Call(
                    typeof(AnyBinder),
                    "Any",
                    Type.EmptyTypes,
                    sourceParameter),
                sourceParameter);
        }

        private static LambdaExpression CreateAsyncAggregator()
        {
            var sourceParameter = Expression.Parameter(typeof(Task<IAsyncCursor<int>>), "source");
            var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "ct");
            return Expression.Lambda(
                Expression.Call(
                    typeof(AnyBinder),
                    "AnyAsync",
                    Type.EmptyTypes,
                    sourceParameter,
                    cancellationTokenParameter),
                sourceParameter,
                cancellationTokenParameter);
        }

        private static bool Any(IEnumerable<int> source)
        {
            return source.Any() && source.Single() > 0;
        }

        private async static Task<bool> AnyAsync(Task<IAsyncCursor<int>> cursorTask, CancellationToken cancellationToken)
        {
            using (var cursor = await cursorTask.ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    return cursor.Current.Any() && cursor.Current.Single() >= 0;
                }

                return false;
            }
        }
    }
}

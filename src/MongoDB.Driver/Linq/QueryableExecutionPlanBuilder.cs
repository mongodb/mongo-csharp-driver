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

using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Processors;

namespace MongoDB.Driver.Linq
{
    internal class QueryableExecutionPlanBuilder
    {
        public static IQueryableExecutionPlan Build(Expression expression, AggregateOptions options, IBsonSerializer documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            expression = PartialEvaluator.Evaluate(expression);
            expression = Transformer.Transform(expression);
            expression = ProjectionBinder.Bind(expression, documentSerializer, serializerRegistry);

            return new ExecutionPlan(expression, options, serializerRegistry);
        }

        private class ExecutionPlan : IQueryableExecutionPlan
        {
            private readonly Expression _expression;
            private readonly AggregateOptions _options;
            private readonly IBsonSerializerRegistry _serializerRegistry;

            public ExecutionPlan(Expression expression, AggregateOptions options, IBsonSerializerRegistry serializerRegistry)
            {
                _expression = expression;
                _options = options;
                _serializerRegistry = serializerRegistry;
            }

            public QueryableExecutionModel BuildExecutionModel()
            {
                var executor = BuildExecutor(_serializerRegistry);
                return executor.ExecutionModel;
            }

            public object Execute<TInput>(IMongoCollection<TInput> collection)
            {
                var executor = BuildExecutor(collection.Settings.SerializerRegistry);
                return executor.Execute(collection);
            }

            public Task ExecuteAsync<TInput>(IMongoCollection<TInput> collection, CancellationToken cancellationToken)
            {
                var executor = BuildExecutor(collection.Settings.SerializerRegistry);
                return executor.ExecuteAsync(collection, cancellationToken);
            }

            private IQueryableExecutor BuildExecutor(IBsonSerializerRegistry serializerRegistry)
            {
                return AggregateQueryableExecutorBuilder.Build(_options, serializerRegistry, _expression);
            }
        }
    }
}

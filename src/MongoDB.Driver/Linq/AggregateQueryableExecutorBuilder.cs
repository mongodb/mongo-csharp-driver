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
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Expressions;
using MongoDB.Driver.Linq.Processors;
using MongoDB.Driver.Linq.Translators;
using MongoDB.Driver.Support;
using MongoDB.Driver.Sync;

namespace MongoDB.Driver.Linq
{
    internal class AggregateQueryableExecutorBuilder
    {
        public static IQueryableExecutor Build(AggregateOptions options, IBsonSerializerRegistry serializerRegistry, Expression expression)
        {
            var builder = new AggregateQueryableExecutorBuilder(serializerRegistry);
            var stages = new List<BsonDocument>();
            builder.Visit(stages, expression);

            var model = Activator.CreateInstance(
                typeof(AggregateQuerableExecutionModel<>).MakeGenericType(builder._serializer.ValueType),
                stages,
                builder._serializer);

            return (IQueryableExecutor)Activator.CreateInstance(
                typeof(AggregateExecutor<>).MakeGenericType(builder._serializer.ValueType),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { options, model, builder._aggregator },
                null);
        }

        private LambdaExpression _aggregator;
        private IBsonSerializer _serializer;
        private readonly IBsonSerializerRegistry _serializerRegistry;

        private AggregateQueryableExecutorBuilder(IBsonSerializerRegistry serializerRegistry)
        {
            _serializerRegistry = serializerRegistry;
        }

        private void Visit(List<BsonDocument> stages, Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Extension:
                    var mongoExpression = (ExtensionExpression)node;
                    switch (mongoExpression.ExtensionType)
                    {
                        case ExtensionExpressionType.CorrelatedGroupBy:
                            VisitCorrelatedGroupBy(stages, (CorrelatedGroupByExpression)node);
                            return;
                        case ExtensionExpressionType.Distinct:
                            VisitDistinct(stages, (DistinctExpression)node);
                            return;
                        case ExtensionExpressionType.GroupByWithResultSelector:
                            VisitGroupByWithResultSelector(stages, (GroupByWithResultSelectorExpression)node);
                            return;
                        case ExtensionExpressionType.Projection:
                            VisitProjection(stages, (ProjectionExpression)node);
                            return;
                        case ExtensionExpressionType.OrderBy:
                            VisitOrderBy(stages, (OrderByExpression)node);
                            return;
                        case ExtensionExpressionType.RootAccumulator:
                            VisitRootAccumulator(stages, (RootAccumulatorExpression)node);
                            return;
                        case ExtensionExpressionType.Select:
                            VisitSelect(stages, (SelectExpression)node);
                            return;
                        case ExtensionExpressionType.Serialization:
                            return;
                        case ExtensionExpressionType.Skip:
                            VisitSkip(stages, (SkipExpression)node);
                            return;
                        case ExtensionExpressionType.Take:
                            VisitTake(stages, (TakeExpression)node);
                            return;
                        case ExtensionExpressionType.Where:
                            VisitWhere(stages, (WhereExpression)node);
                            return;
                    }
                    break;
            }

            throw new NotSupportedException();
        }

        private void VisitCorrelatedGroupBy(List<BsonDocument> stages, CorrelatedGroupByExpression node)
        {
            Visit(stages, node.Source);

            var doc = new BsonDocument();
            doc.Add("_id", AggregateLanguageTranslator.Translate(node.Id));

            foreach (var accumulator in node.Accumulators)
            {
                var serializationExpression = (SerializationExpression)accumulator;
                doc.Add(
                    serializationExpression.SerializationInfo.ElementName,
                    AggregateLanguageTranslator.Translate(serializationExpression.Expression));
            }

            stages.Add(new BsonDocument("$group", doc));
        }

        private void VisitDistinct(List<BsonDocument> stages, DistinctExpression node)
        {
            Visit(stages, node.Source);

            var id = new BsonDocument("_id", AggregateLanguageTranslator.Translate(node.Selector));
            stages.Add(new BsonDocument("$group", id));
        }

        private void VisitGroupByWithResultSelector(List<BsonDocument> stages, GroupByWithResultSelectorExpression node)
        {
            Visit(stages, node.Source);

            var projection = AggregateProjectionTranslator.TranslateProject(node.Selector);
            stages.Add(new BsonDocument("$group", projection));
        }

        private void VisitProjection(List<BsonDocument> stages, ProjectionExpression node)
        {
            Visit(stages, node.Source);

            _aggregator = node.Aggregator;

            var serializationExpression = node.Projector as ISerializationExpression;
            if (serializationExpression != null)
            {
                var info = serializationExpression.SerializationInfo;
                if (info.ElementName != null)
                {
                    // We are projecting a field, however the server only responds
                    // with documents. So we'll create a projector that reads a document
                    // and then projects the field out of it.
                    var parameter = Expression.Parameter(typeof(ProjectedObject), "document");
                    var projector = Expression.Lambda(
                        Expression.Call(
                            parameter,
                            "GetValue",
                            new Type[] { info.Serializer.ValueType },
                            Expression.Constant(info.ElementName),
                            Expression.Constant(info.Serializer.ValueType.GetDefaultValue(), typeof(object))),
                        parameter);
                    var innerSerializer = new ProjectedObjectDeserializer(new[] { info });
                    _serializer = (IBsonSerializer)Activator.CreateInstance(
                        typeof(ProjectingDeserializer<,>).MakeGenericType(typeof(ProjectedObject), info.Serializer.ValueType),
                        new object[] 
                            { 
                                innerSerializer,
                                projector.Compile()
                            });
                    return;
                }
            }

            _serializer = SerializerBuilder.Build(node.Projector, _serializerRegistry);
        }

        private void VisitOrderBy(List<BsonDocument> stages, OrderByExpression node)
        {
            Visit(stages, node.Source);

            BsonDocument sort = new BsonDocument();
            foreach (var clause in node.Clauses)
            {
                var serializationExpression = (SerializationExpression)clause.Expression;
                var info = serializationExpression.SerializationInfo;
                var name = info.ElementName;
                var direction = clause.Direction == SortDirection.Ascending ? 1 : -1;
                if (!sort.Contains(name))
                {
                    sort.Add(name, direction);
                }
                else if (sort[name] != direction)
                {
                    sort.Remove(name);
                    sort.Add(name, direction);
                }
            }

            stages.Add(new BsonDocument("$sort", sort));
        }

        private void VisitRootAccumulator(List<BsonDocument> stages, RootAccumulatorExpression node)
        {
            Visit(stages, node.Source);

            var group = new BsonDocument("_id", BsonNull.Value);

            var serializationAccumulator = (SerializationExpression)node.Accumulator;

            group.Add(
                serializationAccumulator.SerializationInfo.ElementName,
                AggregateLanguageTranslator.Translate(serializationAccumulator.Expression));

            stages.Add(new BsonDocument("$group", group));
        }

        private void VisitSelect(List<BsonDocument> stages, SelectExpression node)
        {
            Visit(stages, node.Source);

            var projection = AggregateProjectionTranslator.TranslateProject(node.Selector);
            stages.Add(new BsonDocument("$project", projection));
        }

        private void VisitSkip(List<BsonDocument> stages, SkipExpression node)
        {
            Visit(stages, node.Source);
            stages.Add(new BsonDocument("$skip", node.Count));
        }

        private void VisitTake(List<BsonDocument> stages, TakeExpression node)
        {
            Visit(stages, node.Source);
            stages.Add(new BsonDocument("$limit", node.Count));
        }

        private void VisitWhere(List<BsonDocument> stages, WhereExpression node)
        {
            Visit(stages, node.Source);

            var renderedPredicate = PredicateTranslator.Translate(node.Predicate, _serializerRegistry);
            stages.Add(new BsonDocument("$match", renderedPredicate));
        }

        public class AggregateExecutor<TOutput> : IQueryableExecutor
        {
            private readonly LambdaExpression _aggregator;
            private readonly AggregateQuerableExecutionModel<TOutput> _executionModel;
            private readonly AggregateOptions _options;

            internal AggregateExecutor(
                AggregateOptions options,
                AggregateQuerableExecutionModel<TOutput> executionModel,
                LambdaExpression aggregator)
            {
                _options = Ensure.IsNotNull(options, "options");
                _executionModel = Ensure.IsNotNull(executionModel, "executionModel");
                _aggregator = aggregator; // can be null
            }

            public QueryableExecutionModel ExecutionModel
            {
                get { return _executionModel; }
            }

            public Task ExecuteAsync<TInput>(IMongoCollection<TInput> collection, CancellationToken cancellationToken)
            {
                if (_aggregator == null)
                {
                    return ExecuteCursorAsync(collection, cancellationToken);
                }

                return ExecuteScalarAsync(collection, cancellationToken);
            }

            public object Execute<TInput>(IMongoCollection<TInput> collection)
            {
                if (_aggregator == null)
                {
                    return ExecuteEnumerable(collection);
                }

                return ExecuteScalar(collection);
            }

            private Task<IAsyncCursor<TOutput>> ExecuteCursorAsync<TInput>(IMongoCollection<TInput> collection, CancellationToken cancellationToken)
            {
                var pipelineDefinition = new BsonDocumentStagePipelineDefinition<TInput, TOutput>(
                    _executionModel.Documents,
                    _executionModel.OutputSerializer);

                return collection.AggregateAsync(pipelineDefinition, _options, cancellationToken);
            }

            private Task<TOutput> ExecuteScalarAsync<TInput>(IMongoCollection<TInput> collection, CancellationToken cancellationToken)
            {
                var cursorTaskParameter = _aggregator.Parameters[0];
                var cancellationTokenParameter = _aggregator.Parameters[1];

                var call = Expression.Call(
                    Expression.Constant(this),
                    "ExecuteCursorAsync",
                    new Type[] { typeof(TInput) },
                    Expression.Constant(collection),
                    cancellationTokenParameter);

                var aggregate = Expression.Invoke(_aggregator, call, cancellationTokenParameter);

                var executor = Expression.Lambda<Func<CancellationToken, Task<TOutput>>>(aggregate, cancellationTokenParameter).Compile();

                return executor(cancellationToken);
            }

            private IEnumerable<TOutput> ExecuteEnumerable<TInput>(IMongoCollection<TInput> collection)
            {
                return new AsyncCursorEnumerableAdapter<TOutput>(x => ExecuteCursorAsync(collection, CancellationToken.None), CancellationToken.None);
            }

            private TOutput ExecuteScalar<TInput>(IMongoCollection<TInput> collection)
            {
                return ExecuteScalarAsync(collection, CancellationToken.None).GetAwaiter().GetResult();
            }
        }
    }
}

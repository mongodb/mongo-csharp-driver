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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Expressions;
using MongoDB.Driver.Linq.Processors;
using MongoDB.Driver.Linq.Translators;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq
{
    internal class QueryableExecutionModelBuilder
    {
        public static QueryableExecutionModel Build(Expression node, IBsonSerializerRegistry serializerRegistry)
        {
            var builder = new QueryableExecutionModelBuilder(serializerRegistry);
            builder.Visit(node);

            return (QueryableExecutionModel)Activator.CreateInstance(
                typeof(AggregateQueryableExecutionModel<>).MakeGenericType(builder._serializer.ValueType),
                builder._stages,
                builder._serializer);
        }

        private IBsonSerializer _serializer;
        private readonly IBsonSerializerRegistry _serializerRegistry;
        private readonly List<BsonDocument> _stages;

        private QueryableExecutionModelBuilder(IBsonSerializerRegistry serializerRegistry)
        {
            _serializerRegistry = serializerRegistry;
            _stages = new List<BsonDocument>();
        }

        private void Visit(Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Extension:
                    var mongoExpression = (ExtensionExpression)node;
                    switch (mongoExpression.ExtensionType)
                    {
                        case ExtensionExpressionType.CorrelatedGroupBy:
                            VisitCorrelatedGroupBy((CorrelatedGroupByExpression)node);
                            return;
                        case ExtensionExpressionType.Distinct:
                            VisitDistinct((DistinctExpression)node);
                            return;
                        case ExtensionExpressionType.GroupByWithResultSelector:
                            VisitGroupByWithResultSelector((GroupByWithResultSelectorExpression)node);
                            return;
                        case ExtensionExpressionType.Projection:
                            VisitProjection((ProjectionExpression)node);
                            return;
                        case ExtensionExpressionType.OrderBy:
                            VisitOrderBy((OrderByExpression)node);
                            return;
                        case ExtensionExpressionType.RootAccumulator:
                            VisitRootAccumulator((RootAccumulatorExpression)node);
                            return;
                        case ExtensionExpressionType.Select:
                            VisitSelect((SelectExpression)node);
                            return;
                        case ExtensionExpressionType.SelectMany:
                            VisitSelectMany((SelectManyExpression)node);
                            return;
                        case ExtensionExpressionType.Serialization:
                            return;
                        case ExtensionExpressionType.Skip:
                            VisitSkip((SkipExpression)node);
                            return;
                        case ExtensionExpressionType.Take:
                            VisitTake((TakeExpression)node);
                            return;
                        case ExtensionExpressionType.Where:
                            VisitWhere((WhereExpression)node);
                            return;
                    }
                    break;
            }

            throw new NotSupportedException();
        }

        private void VisitCorrelatedGroupBy(CorrelatedGroupByExpression node)
        {
            Visit(node.Source);

            var group = new BsonDocument();
            group.Add("_id", AggregateLanguageTranslator.Translate(node.Id));

            foreach (var accumulator in node.Accumulators)
            {
                var serializationExpression = (SerializationExpression)accumulator;
                group.Add(
                    serializationExpression.SerializationInfo.ElementName,
                    AggregateLanguageTranslator.Translate(serializationExpression.Expression));
            }

            _stages.Add(new BsonDocument("$group", group));
        }

        private void VisitDistinct(DistinctExpression node)
        {
            Visit(node.Source);

            var id = new BsonDocument("_id", AggregateLanguageTranslator.Translate(node.Selector));
            _stages.Add(new BsonDocument("$group", id));
        }

        private void VisitGroupByWithResultSelector(GroupByWithResultSelectorExpression node)
        {
            Visit(node.Source);

            var projection = AggregateProjectionTranslator.TranslateProject(node.Selector);
            _stages.Add(new BsonDocument("$group", projection));
        }

        private void VisitProjection(ProjectionExpression node)
        {
            Visit(node.Source);

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

        private void VisitOrderBy(OrderByExpression node)
        {
            Visit(node.Source);

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

            _stages.Add(new BsonDocument("$sort", sort));
        }

        private void VisitRootAccumulator(RootAccumulatorExpression node)
        {
            Visit(node.Source);

            var group = new BsonDocument("_id", BsonNull.Value);

            var serializationAccumulator = (SerializationExpression)node.Accumulator;

            group.Add(
                serializationAccumulator.SerializationInfo.ElementName,
                AggregateLanguageTranslator.Translate(serializationAccumulator.Expression));

            _stages.Add(new BsonDocument("$group", group));
        }

        private void VisitSelect(SelectExpression node)
        {
            Visit(node.Source);

            var projection = AggregateProjectionTranslator.TranslateProject(node.Selector);
            _stages.Add(new BsonDocument("$project", projection));
        }

        private void VisitSelectMany(SelectManyExpression node)
        {
            Visit(node.Source);

            var field = node.CollectionSelector as ISerializationExpression;
            if (field == null || field.SerializationInfo.ElementName == null)
            {
                var message = string.Format("The collection selector must be an ISerializationExpression: {0}", node.ToString());
                throw new NotSupportedException(message);
            }

            _stages.Add(new BsonDocument("$unwind", "$" + field.SerializationInfo.ElementName));

            var projection = AggregateProjectionTranslator.TranslateProject(node.ResultSelector);
            _stages.Add(new BsonDocument("$project", projection));
        }

        private void VisitSkip(SkipExpression node)
        {
            Visit(node.Source);
            _stages.Add(new BsonDocument("$skip", node.Count));
        }

        private void VisitTake(TakeExpression node)
        {
            Visit(node.Source);
            _stages.Add(new BsonDocument("$limit", node.Count));
        }

        private void VisitWhere(WhereExpression node)
        {
            Visit(node.Source);

            var renderedPredicate = PredicateTranslator.Translate(node.Predicate, _serializerRegistry);
            _stages.Add(new BsonDocument("$match", renderedPredicate));
        }
    }
}

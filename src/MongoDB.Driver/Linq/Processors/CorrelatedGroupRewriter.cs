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

namespace MongoDB.Driver.Linq.Processors
{
    internal class CorrelatedGroupRewriter : ExtensionExpressionVisitor
    {
        public static Expression Rewrite(Expression node, IBsonSerializerRegistry serializerRegistry)
        {
            var rewriter = new CorrelatedGroupRewriter(serializerRegistry);
            return rewriter.Visit(node);
        }

        private ILookup<Guid, CorrelatedAccumulatorExpression> _lookup;
        private readonly Dictionary<CorrelatedAccumulatorExpression, Expression> _map;
        private readonly IBsonSerializerRegistry _serializerRegistry;

        private CorrelatedGroupRewriter(IBsonSerializerRegistry serializerRegistry)
        {
            _serializerRegistry = serializerRegistry;
            _map = new Dictionary<CorrelatedAccumulatorExpression, Expression>();
        }

        protected internal override Expression VisitCorrelatedAccumulator(CorrelatedAccumulatorExpression node)
        {
            Expression mapped;
            if (_map.TryGetValue(node, out mapped))
            {
                return mapped;
            }

            return base.VisitCorrelatedAccumulator(node);
        }

        protected internal override Expression VisitCorrelatedGroupBy(CorrelatedGroupByExpression node)
        {
            if (_lookup != null && _lookup.Contains(node.CorrelationId))
            {
                var source = Visit(node.Source);
                var accumulators = new List<SerializationExpression>();
                var comparer = new ExpressionComparer();
                foreach (var correlatedAccumulator in _lookup[node.CorrelationId])
                {
                    var index = accumulators.FindIndex(x => comparer.Compare(x.Expression, correlatedAccumulator.Accumulator));

                    if (index == -1)
                    {
                        var serializer = _serializerRegistry.GetSerializer(correlatedAccumulator.Type);
                        var info = new BsonSerializationInfo(
                            "__agg" + accumulators.Count,
                            serializer,
                            serializer.ValueType);

                        var serializationExpression = new SerializationExpression(correlatedAccumulator.Accumulator, info);
                        accumulators.Add(serializationExpression);
                        _map[correlatedAccumulator] = serializationExpression;
                    }
                    else
                    {
                        _map[correlatedAccumulator] = accumulators[index];
                    }
                }

                node = node.Update(
                    source,
                    node.Id,
                    accumulators.OfType<Expression>());
            }

            return base.VisitCorrelatedGroupBy(node);
        }

        protected internal override Expression VisitProjection(ProjectionExpression node)
        {
            _lookup = AccumulatorGatherer.Gather(node.Source).ToLookup(x => x.CorrelationId);

            return base.VisitProjection(node);
        }

        private class AccumulatorGatherer : ExtensionExpressionVisitor
        {
            public static List<CorrelatedAccumulatorExpression> Gather(Expression node)
            {
                var gatherer = new AccumulatorGatherer();
                gatherer.Visit(node);
                return gatherer._accumulators;
            }

            private readonly List<CorrelatedAccumulatorExpression> _accumulators;

            public AccumulatorGatherer()
            {
                _accumulators = new List<CorrelatedAccumulatorExpression>();
            }

            protected internal override Expression VisitCorrelatedAccumulator(CorrelatedAccumulatorExpression node)
            {
                _accumulators.Add(node);

                // there could be nested accumulators we need to get...
                return base.VisitCorrelatedAccumulator(node);
            }
        }
    }
}

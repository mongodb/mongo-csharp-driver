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
    internal class CountBinder : ImmediateResultBinderBase
    {
        public override Expression Bind(ProjectionExpression projection, ProjectionBindingContext context, MethodCallExpression node, IEnumerable<Expression> arguments)
        {
            var aggregatorName = "SingleOrDefault";
            var returnType = node.Method.ReturnType;
            if (node.Method.Name.EndsWith("Async"))
            {
                aggregatorName += "Async";
                returnType = returnType.GetGenericArguments()[0]; // it's a task
            }
            var aggregator = CreateAggregator(aggregatorName, returnType);

            var source = projection.Source;
            var argument = arguments.FirstOrDefault();
            if (argument != null && ExtensionExpressionVisitor.IsLambda(argument))
            {
                source = BindPredicate(projection, context, source, argument);
            }

            var serializer = context.SerializerRegistry.GetSerializer(returnType);
            var accumulator = new AccumulatorExpression(returnType, AccumulatorType.Count, null);
            var serializationAccumulator = new SerializationExpression(
                accumulator,
                new BsonSerializationInfo("__agg0", serializer, serializer.ValueType));

            var rootAccumulator = new RootAccumulatorExpression(source, serializationAccumulator);

            return new ProjectionExpression(
                rootAccumulator,
                serializationAccumulator,
                aggregator);
        }
    }
}

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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors
{
    internal class AccumulatorBinder : SerializationInfoBinder
    {
        private readonly CorrelatedGroupMap _groupMap;
        private bool _isPotentialAccumulatorMethod;

        public AccumulatorBinder(IBsonSerializerRegistry serializerRegistry)
            : base(serializerRegistry)
        {
            _isPotentialAccumulatorMethod = true;
        }

        public AccumulatorBinder(CorrelatedGroupMap groupMap, IBsonSerializerRegistry serializerRegistry)
            : this(serializerRegistry)
        {
            _groupMap = groupMap;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // only the top-level method calls are potential accumulator methods
            // For instance, in g => g.Select(x => x.Age).Sum(), only the Sum() is
            // a potential accumulator method. Select is not. 
            var oldIsPotentialAccumulatorMethod = _isPotentialAccumulatorMethod;
            _isPotentialAccumulatorMethod = false;
            var newNode = (MethodCallExpression)base.VisitMethodCall(node);
            _isPotentialAccumulatorMethod = oldIsPotentialAccumulatorMethod;
            Expression accumulator;
            if (_isPotentialAccumulatorMethod && newNode.NodeType == ExpressionType.Call && TryBindAccumulatorExpression(newNode, out accumulator))
            {
                return accumulator;
            }

            return newNode;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            var newNode = (NewExpression)base.VisitNew(node);
            if (newNode.Type.IsGenericType
                && newNode.Type.GetGenericTypeDefinition() == typeof(HashSet<>)
                && newNode.Arguments.Count == 1
                && newNode.Arguments[0] is AccumulatorExpression
                && ((AccumulatorExpression)newNode.Arguments[0]).AccumulatorType == AccumulatorType.Push)
            {
                Guid correlationId = Guid.Empty;
                if (_groupMap == null || TryGetCorrelatedGroup(node.Arguments[0], out correlationId))
                {
                    Expression accumulator = new AccumulatorExpression(
                        newNode.Type,
                        AccumulatorType.AddToSet,
                        ((AccumulatorExpression)newNode.Arguments[0]).Argument);

                    if (_groupMap != null)
                    {
                        accumulator = new CorrelatedAccumulatorExpression(
                            correlationId,
                            (AccumulatorExpression)accumulator);
                    }

                    return accumulator;
                }
            }

            return newNode;
        }

        private bool TryBindAccumulatorExpression(MethodCallExpression node, out Expression accumulator)
        {
            AccumulatorType accumulatorType;
            if (TryGetAccumulatorType(node.Method.Name, out accumulatorType))
            {
                Guid correlationId = Guid.Empty;
                if (_groupMap == null || TryGetCorrelatedGroup(node.Arguments[0], out correlationId))
                {
                    accumulator = new AccumulatorExpression(node.Type,
                        accumulatorType,
                        GetAccumulatorArgument(node));

                    if (_groupMap != null)
                    {
                        accumulator = new CorrelatedAccumulatorExpression(
                            correlationId,
                            (AccumulatorExpression)accumulator);
                    }

                    return true;
                }
            }

            accumulator = null;
            return false;
        }

        private Expression GetAccumulatorArgument(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case "Count":
                case "LongCount":
                    if (node.Arguments.Count == 1)
                    {
                        return Expression.Constant(1);
                    }
                    break;
                case "Average":
                case "Min":
                case "Max":
                case "Sum":
                    if (node.Arguments.Count == 2)
                    {
                        return GetLambda(node.Arguments[1]).Body;
                    }
                    else if (node.Arguments.Count == 1)
                    {
                        return GetBodyFromSelector(node);
                    }
                    break;
                case "First":
                case "Last":
                    // we have already normalized First/Last calls to only have 1 argument...
                    if (node.Arguments.Count == 1)
                    {
                        return GetBodyFromSelector(node);
                    }
                    break;
                case "Select":
                    if (node.Arguments.Count == 2)
                    {
                        return GetLambda(node.Arguments[1]).Body;
                    }
                    break;
                case "Distinct":
                case "ToArray":
                case "ToList":
                    if (node.Arguments.Count == 1)
                    {
                        return GetBodyFromSelector(node);
                    }
                    break;
            }

            var message = string.Format("Unsupported version of accumulator method {0} in the expression tree: {1}.",
                node.Method.Name,
                node.ToString());
            throw new NotSupportedException(message);
        }

        private Expression GetBodyFromSelector(MethodCallExpression node)
        {
            // yes, this is difficult to understand what is going on.
            // we are getting the x.Field from an expression that looks like this:
            // group.Select(x => x.Field).First();
            return GetLambda(((MethodCallExpression)node.Arguments[0]).Arguments[1]).Body;
        }

        private bool TryGetCorrelatedGroup(Expression source, out Guid correlationId)
        {
            if (_groupMap == null)
            {
                correlationId = Guid.Empty;
                return false;
            }

            if (_groupMap.TryGet(source, out correlationId))
            {
                return true;
            }

            switch (source.NodeType)
            {
                case ExpressionType.Call:
                    var call = (MethodCallExpression)source;
                    if (call.Method.Name == "Select")
                    {
                        return TryGetCorrelatedGroup(call.Arguments[0], out correlationId);
                    }
                    break;
            }

            correlationId = Guid.Empty;
            return false;
        }

        private bool TryGetAccumulatorType(string methodName, out AccumulatorType accumulatorType)
        {
            switch (methodName)
            {
                case "Average":
                    accumulatorType = AccumulatorType.Average;
                    return true;
                case "Count":
                case "LongCount":
                case "Sum":
                    accumulatorType = AccumulatorType.Sum;
                    return true;
                case "Distinct":
                    accumulatorType = AccumulatorType.AddToSet;
                    return true;
                case "First":
                    accumulatorType = AccumulatorType.First;
                    return true;
                case "Last":
                    accumulatorType = AccumulatorType.Last;
                    return true;
                case "Max":
                    accumulatorType = AccumulatorType.Max;
                    return true;
                case "Min":
                    accumulatorType = AccumulatorType.Min;
                    return true;
                case "Select":
                case "ToArray":
                case "ToList":
                    accumulatorType = AccumulatorType.Push;
                    return true;
            }

            accumulatorType = 0; // dummy assignment to appease compiler
            return false;
        }
    }
}

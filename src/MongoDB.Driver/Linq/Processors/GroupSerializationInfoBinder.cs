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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors
{
    internal class GroupSerializationInfoBinder : SerializationInfoBinder
    {
        private bool _isPotentialAggregationMethod;

        public GroupSerializationInfoBinder(IBsonSerializerRegistry serializerRegistry)
            : base(serializerRegistry)
        {
            _isPotentialAggregationMethod = true;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            // First and Last don't take selector expressions in their method 
            // calls, unlike Average, Min, Max, etc...  MongoDB requires a selector.
            // Therefore, we will take g.Last().Member and translate it to g.Select(x => x.Member).Last()
            // in order to make working with this expression more normal.
            var newNode = new FirstLastNormalizer().Normalize(node);
            if (newNode != node)
            {
                return Visit(newNode);
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // only the top-level method calls are potential aggregation methods
            // For instance, in g => g.Select(x => x.Age).Sum(), only the Sum() is
            // a potential aggregation method. Select is not. 
            var oldIsPotentialAggregationMethod = _isPotentialAggregationMethod;
            _isPotentialAggregationMethod = false;
            var newNode = base.VisitMethodCall(node);
            _isPotentialAggregationMethod = oldIsPotentialAggregationMethod;
            Expression aggregationNode;
            if (_isPotentialAggregationMethod && newNode.NodeType == ExpressionType.Call && TryBindAggregationExpression((MethodCallExpression)newNode, out aggregationNode))
            {
                return aggregationNode;
            }

            return newNode;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            var newNode = (NewExpression)base.VisitNew(node);
            if (newNode.Type.IsGenericType
                && newNode.Type.GetGenericTypeDefinition() == typeof(HashSet<>)
                && newNode.Arguments.Count == 1
                && newNode.Arguments[0] is AggregationExpression
                && ((AggregationExpression)newNode.Arguments[0]).AggregationType == AggregationType.Push)
            {
                return new AggregationExpression(
                    newNode.Type,
                    AggregationType.AddToSet,
                    ((AggregationExpression)newNode.Arguments[0]).Argument);
            }

            return newNode;
        }

        private bool TryBindAggregationExpression(MethodCallExpression node, out Expression aggregationNode)
        {
            AggregationType aggregationType;
            if (TryGetAggregationType(node.Method.Name, out aggregationType))
            {
                var argument = GetAggregationArgument(node);
                aggregationNode = new AggregationExpression(node.Type, aggregationType, argument);
                return true;
            }

            aggregationNode = null;
            return false;
        }

        private Expression GetAggregationArgument(MethodCallExpression node)
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

            var message = string.Format("Unsupported version of aggregation method {0}.", node.Method.Name);
            throw new NotSupportedException(message);
        }

        private Expression GetBodyFromSelector(MethodCallExpression node)
        {
            // yes, this is difficult to understand what is going on.
            // we are getting the x.Field from an expression that looks like this:
            // group.Select(x => x.Field).First();
            return GetLambda(((MethodCallExpression)node.Arguments[0]).Arguments[1]).Body;
        }

        private bool TryGetAggregationType(string methodName, out AggregationType aggregationType)
        {
            switch (methodName)
            {
                case "Average":
                    aggregationType = AggregationType.Average;
                    return true;
                case "Count":
                case "LongCount":
                case "Sum":
                    aggregationType = AggregationType.Sum;
                    return true;
                case "Distinct":
                    aggregationType = AggregationType.AddToSet;
                    return true;
                case "First":
                    aggregationType = AggregationType.First;
                    return true;
                case "Last":
                    aggregationType = AggregationType.Last;
                    return true;
                case "Max":
                    aggregationType = AggregationType.Max;
                    return true;
                case "Min":
                    aggregationType = AggregationType.Min;
                    return true;
                case "Select":
                case "ToArray":
                case "ToList":
                    aggregationType = AggregationType.Push;
                    return true;
            }

            aggregationType = 0; // dummy assignment to appease compiler
            return false;
        }

        // We need to normalize how first and last calls are represented.
        // both group.Last().Member and group.Select(x => x.Member).Last() are
        // valid representations.  We are going to make all of the look like
        // the latter...
        private class FirstLastNormalizer : MongoExpressionVisitor
        {
            public Expression Normalize(Expression node)
            {
                return Visit(node);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var members = new Stack<MemberInfo>();
                Expression currentNode = node;
                while (currentNode.NodeType == ExpressionType.MemberAccess)
                {
                    var mex = (MemberExpression)currentNode;
                    members.Push(mex.Member);
                    currentNode = mex.Expression;
                }

                // we are going to rewrite g.Last().Member to g.Select(x => x.Member).Last()
                var call = currentNode as MethodCallExpression;
                if (call != null && IsAggregateMethod(call.Method.Name))
                {
                    var source = Visit(call.Arguments[0]);
                    var typeArguments = call.Method.GetGenericArguments();
                    var parameter = Expression.Parameter(typeArguments[0], "x");

                    Expression lambdaBody = parameter;
                    while (members.Count > 0)
                    {
                        var currentMember = members.Pop();
                        lambdaBody = Expression.MakeMemberAccess(lambdaBody, currentMember);
                    }

                    var select = Expression.Call(
                        typeof(Enumerable),
                        "Select",
                        new[] { typeArguments[0], lambdaBody.Type },
                        source,
                        Expression.Lambda(
                            typeof(Func<,>).MakeGenericType(typeArguments[0], lambdaBody.Type),
                            lambdaBody,
                            parameter));

                    return Expression.Call(
                        typeof(Enumerable),
                        call.Method.Name,
                        new[] { lambdaBody.Type },
                        select);
                }

                return base.VisitMember(node);
            }

            private bool IsAggregateMethod(string methodName)
            {
                return methodName == "First" || methodName == "Last";
            }
        }
    }
}

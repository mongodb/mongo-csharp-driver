﻿/* Copyright 2010-present MongoDB Inc.
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

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class ExpressionHelper
    {
        public static LambdaExpression UnquoteLambda(Expression expression)
        {
            Ensure.IsNotNull(expression, nameof(expression));
            Ensure.That(expression.NodeType == ExpressionType.Quote, "NodeType must be Quote.", nameof(expression));
            var unaryExpression = (UnaryExpression)expression;
            return (LambdaExpression)unaryExpression.Operand;
        }

        public static LambdaExpression UnquoteLambdaIfQueryableMethod(MethodInfo method, Expression expression)
        {
            Ensure.IsNotNull(method, nameof(method));
            Ensure.IsNotNull(expression, nameof(expression));

            if (method.DeclaringType == typeof(Queryable))
            {
                return UnquoteLambda(expression);
            }
            else
            {
                return (LambdaExpression)expression;
            }
        }
    }
}

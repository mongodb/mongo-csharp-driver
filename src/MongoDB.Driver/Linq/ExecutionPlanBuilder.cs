/* Copyright 2015 MongoDB Inc.
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

namespace MongoDB.Driver.Linq
{
    internal static class ExecutionPlanBuilder
    {
        public static Expression BuildPlan(Expression provider, QueryableTranslation translation)
        {
            Expression executor = Expression.Call(
                provider,
                "ExecuteModel",
                null,
                Expression.Constant(translation.Model, typeof(QueryableExecutionModel)));

            if (translation.ResultTransformer != null)
            {
                var lambda = translation.ResultTransformer.CreateAggregator(translation.Model.OutputType);
                executor = Expression.Invoke(
                    lambda,
                    Expression.Convert(executor, lambda.Parameters[0].Type));
            }

            return executor;
        }

        public static Expression BuildAsyncPlan(Expression provider, QueryableTranslation translation, Expression cancellationToken)
        {
            Expression executor = Expression.Call(
                    provider,
                    "ExecuteModelAsync",
                    null,
                    Expression.Constant(translation.Model, typeof(QueryableExecutionModel)),
                    cancellationToken);

            if (translation.ResultTransformer != null)
            {
                var lambda = translation.ResultTransformer.CreateAsyncAggregator(translation.Model.OutputType);
                executor = Expression.Invoke(
                    lambda,
                    Expression.Convert(executor, lambda.Parameters[0].Type),
                    cancellationToken);
            }

            return executor;
        }
    }
}

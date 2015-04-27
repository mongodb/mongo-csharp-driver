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

namespace MongoDB.Driver.Linq.Processors.MethodCallBinders
{
    internal abstract class ImmediateResultBinderBase : MethodCallBinderBase
    {
        protected static LambdaExpression CreateAggregator(string aggregatorName, Type returnType)
        {
            if (aggregatorName.EndsWith("Async"))
            {
                return CreateAsyncAggregator(aggregatorName, returnType);
            }

            return CreateSyncAggregator(aggregatorName, returnType);
        }

        private static LambdaExpression CreateSyncAggregator(string aggregatorName, Type returnType)
        {
            var sourceType = typeof(IEnumerable<>).MakeGenericType(returnType);
            var sourceParameter = Expression.Parameter(sourceType, "source");
            return Expression.Lambda(
                Expression.Call(typeof(Enumerable),
                    aggregatorName,
                    new[] { returnType },
                    sourceParameter),
                sourceParameter);
        }

        private static LambdaExpression CreateAsyncAggregator(string aggregatorName, Type returnType)
        {
            var sourceType = typeof(IAsyncCursor<>).MakeGenericType(returnType);
            var sourceParameter = Expression.Parameter(typeof(Task<>).MakeGenericType(sourceType), "source");
            var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "ct");
            return Expression.Lambda(
                Expression.Call(typeof(AsyncCursorHelper),
                    aggregatorName,
                    new[] { returnType },
                    sourceParameter,
                    cancellationTokenParameter),
                sourceParameter,
                cancellationTokenParameter);
        }
    }
}

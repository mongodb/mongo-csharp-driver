/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Linq3.Methods
{
    public static class MongoQueryableMethod
    {
        // private static fields
        private static readonly MethodInfo __anyAsync;
        private static readonly MethodInfo __anyWithPredicateAsync;
        private static readonly MethodInfo __averageDecimalAsync;
        private static readonly MethodInfo __averageDecimalWithSelectorAsync;
        private static readonly MethodInfo __averageDoubleAsync;
        private static readonly MethodInfo __averageDoubleWithSelectorAsync;
        private static readonly MethodInfo __averageInt32Async;
        private static readonly MethodInfo __averageInt32WithSelectorAsync;
        private static readonly MethodInfo __averageInt64Async;
        private static readonly MethodInfo __averageInt64WithSelectorAsync;
        private static readonly MethodInfo __averageNullableDecimalAsync;
        private static readonly MethodInfo __averageNullableDecimalWithSelectorAsync;
        private static readonly MethodInfo __averageNullableDoubleAsync;
        private static readonly MethodInfo __averageNullableDoubleWithSelectorAsync;
        private static readonly MethodInfo __averageNullableInt32Async;
        private static readonly MethodInfo __averageNullableInt32WithSelectorAsync;
        private static readonly MethodInfo __averageNullableInt64Async;
        private static readonly MethodInfo __averageNullableInt64WithSelectorAsync;
        private static readonly MethodInfo __averageNullableSingleAsync;
        private static readonly MethodInfo __averageNullableSingleWithSelectorAsync;
        private static readonly MethodInfo __averageSingleAsync;
        private static readonly MethodInfo __averageSingleWithSelectorAsync;

        // static constructor
        static MongoQueryableMethod()
        {
            __anyAsync = new Func<IQueryable<object>, CancellationToken, Task<bool>>(MongoQueryable.AnyAsync).Method.GetGenericMethodDefinition();
            __anyWithPredicateAsync = new Func<IQueryable<object>, Expression<Func<object, bool>>, CancellationToken, Task<bool>>(MongoQueryable.AnyAsync).Method.GetGenericMethodDefinition();
            __averageDecimalAsync = new Func<IQueryable<decimal>, CancellationToken, Task<decimal>>(MongoQueryable.AverageAsync).Method;
            __averageDecimalWithSelectorAsync = new Func<IQueryable<object>, Expression<Func<object, decimal>>, CancellationToken, Task<decimal>>(MongoQueryable.AverageAsync).Method.GetGenericMethodDefinition();
            __averageDoubleAsync = new Func<IQueryable<double>, CancellationToken, Task<double>>(MongoQueryable.AverageAsync).Method;
            __averageDoubleWithSelectorAsync = new Func<IQueryable<object>, Expression<Func<object, double>>, CancellationToken, Task<double>>(MongoQueryable.AverageAsync).Method.GetGenericMethodDefinition();
            __averageInt32Async = new Func<IQueryable<int>, CancellationToken, Task<double>>(MongoQueryable.AverageAsync).Method;
            __averageInt32WithSelectorAsync = new Func<IQueryable<object>, Expression<Func<object, int>>, CancellationToken, Task<double>>(MongoQueryable.AverageAsync).Method.GetGenericMethodDefinition();
            __averageInt64Async = new Func<IQueryable<long>, CancellationToken, Task<double>>(MongoQueryable.AverageAsync).Method;
            __averageInt64WithSelectorAsync = new Func<IQueryable<object>, Expression<Func<object, long>>, CancellationToken, Task<double>>(MongoQueryable.AverageAsync).Method.GetGenericMethodDefinition();
            __averageNullableDecimalAsync = new Func<IQueryable<decimal?>, CancellationToken, Task<decimal?>>(MongoQueryable.AverageAsync).Method;
            __averageNullableDecimalWithSelectorAsync = new Func<IQueryable<object>, Expression<Func<object, decimal?>>, CancellationToken, Task<decimal?>>(MongoQueryable.AverageAsync).Method.GetGenericMethodDefinition();
            __averageNullableDoubleAsync = new Func<IQueryable<double?>, CancellationToken, Task<double?>>(MongoQueryable.AverageAsync).Method;
            __averageNullableDoubleWithSelectorAsync = new Func<IQueryable<object>, Expression<Func<object, double?>>, CancellationToken, Task<double?>>(MongoQueryable.AverageAsync).Method.GetGenericMethodDefinition();
            __averageNullableInt32Async = new Func<IQueryable<int?>, CancellationToken, Task<double?>>(MongoQueryable.AverageAsync).Method;
            __averageNullableInt32WithSelectorAsync = new Func<IQueryable<object>, Expression<Func<object, int?>>, CancellationToken, Task<double?>>(MongoQueryable.AverageAsync).Method.GetGenericMethodDefinition();
            __averageNullableInt64Async = new Func<IQueryable<long?>, CancellationToken, Task<double?>>(MongoQueryable.AverageAsync).Method;
            __averageNullableInt64WithSelectorAsync = new Func<IQueryable<object>, Expression<Func<object, long?>>, CancellationToken, Task<double?>>(MongoQueryable.AverageAsync).Method.GetGenericMethodDefinition();
            __averageNullableSingleAsync = new Func<IQueryable<float?>, CancellationToken, Task<float?>>(MongoQueryable.AverageAsync).Method;
            __averageNullableSingleWithSelectorAsync = new Func<IQueryable<object>, Expression<Func<object, float?>>, CancellationToken, Task<float?>>(MongoQueryable.AverageAsync).Method.GetGenericMethodDefinition();
            __averageSingleAsync = new Func<IQueryable<float>, CancellationToken, Task<float>>(MongoQueryable.AverageAsync).Method;
            __averageSingleWithSelectorAsync = new Func<IQueryable<object>, Expression<Func<object, float>>, CancellationToken, Task<float>>(MongoQueryable.AverageAsync).Method.GetGenericMethodDefinition();
        }

        // public properties
        public static MethodInfo AnyAsync => __anyAsync;
        public static MethodInfo AnyWithPredicateAsync => __anyWithPredicateAsync;
        public static MethodInfo AverageDecimalAsync => __averageDecimalAsync;
        public static MethodInfo AverageDecimalWithSelectorAsync => __averageDecimalWithSelectorAsync;
        public static MethodInfo AverageDoubleAsync => __averageDoubleAsync;
        public static MethodInfo AverageDoubleWithSelectorAsync => __averageDoubleWithSelectorAsync;
        public static MethodInfo AverageInt32Async => __averageInt32Async;
        public static MethodInfo AverageInt32WithSelectorAsync => __averageInt32WithSelectorAsync;
        public static MethodInfo AverageInt64Async => __averageInt64Async;
        public static MethodInfo AverageInt64WithSelectorAsync => __averageInt64WithSelectorAsync;
        public static MethodInfo AverageNullableDecimalAsync => __averageNullableDecimalAsync;
        public static MethodInfo AverageNullableDecimalWithSelectorAsync => __averageNullableDecimalWithSelectorAsync;
        public static MethodInfo AverageNullableDoubleAsync => __averageNullableDoubleAsync;
        public static MethodInfo AverageNullableDoubleWithSelectorAsync => __averageNullableDoubleWithSelectorAsync;
        public static MethodInfo AverageNullableInt32Async => __averageNullableInt32Async;
        public static MethodInfo AverageNullableInt32WithSelectorAsync => __averageNullableInt32WithSelectorAsync;
        public static MethodInfo AverageNullableInt64Async => __averageNullableInt64Async;
        public static MethodInfo AverageNullableInt64WithSelectorAsync => __averageNullableInt64WithSelectorAsync;
        public static MethodInfo AverageNullableSingleAsync => __averageNullableSingleAsync;
        public static MethodInfo AverageNullableSingleWithSelectorAsync => __averageNullableSingleWithSelectorAsync;
        public static MethodInfo AverageSingleAsync => __averageSingleAsync;
        public static MethodInfo AverageSingleWithSelectorAsync => __averageSingleWithSelectorAsync;
    }
}

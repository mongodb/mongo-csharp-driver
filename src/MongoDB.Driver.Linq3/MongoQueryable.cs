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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Linq3
{
    // this class is analogous to .NET's Queryable class and contains MongoDB specific extension methods for IQueryable
    public static class MongoQueryable
    {
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            var arguments = new[] { source.Expression, Expression.Constant(cancellationToken) };
            return ((MongoQueryProvider)source.Provider).ExecuteAsync<bool>(
                Expression.Call(
                    GetMethodInfo<IQueryable<TSource>, CancellationToken, Task<bool>>(MongoQueryable.AnyAsync, source, cancellationToken),
                    arguments),
                cancellationToken);
        }

        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var arguments = new[] { source.Expression, Expression.Quote(predicate), Expression.Constant(cancellationToken) };
            return ((MongoQueryProvider)source.Provider).ExecuteAsync<bool>(
                Expression.Call(
                    GetMethodInfo<IQueryable<TSource>, Expression<Func<TSource, bool>>, CancellationToken, Task<bool>>(MongoQueryable.AnyAsync, source, predicate, cancellationToken),
                    arguments),
                cancellationToken);
        }

        public static Task<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> AverageAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float?> AverageAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<int> AverageAsync(this IQueryable<int> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<int?> AverageAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<long> AverageAsync(this IQueryable<long> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<long?> AverageAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<int> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<int?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<long> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<long?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate , CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IMongoCollection<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector)
        {
            throw new NotImplementedException();
        }

        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TResult> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TResult> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static IMongoQueryable<TSource> Sample<TSource>(this IQueryable<TSource> source, long count)
        {
            throw new NotImplementedException();
        }


        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static decimal StandardDeviationPopulation(this IQueryable<decimal> source)
        {
            throw new NotImplementedException();
        }

        public static decimal StandardDeviationPopulation(this IQueryable<decimal?> source)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationPopulation(this IQueryable<double> source)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationPopulation(this IQueryable<double?> source)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationPopulation(this IQueryable<int> source)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationPopulation(this IQueryable<int?> source)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationPopulation(this IQueryable<long> source)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationPopulation(this IQueryable<long?> source)
        {
            throw new NotImplementedException();
        }

        public static float StandardDeviationPopulation(this IQueryable<float> source)
        {
            throw new NotImplementedException();
        }

        public static float? StandardDeviationPopulation(this IQueryable<float?> source)
        {
            throw new NotImplementedException();
        }

        public static decimal StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Func<TSource, decimal> selector)
        {
            throw new NotImplementedException();
        }

        public static decimal? StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Func<TSource, decimal?> selector)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Func<TSource, double> selector)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Func<TSource, double?> selector)
        {
            throw new NotImplementedException();
        }

        public static float StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Func<TSource, float> selector)
        {
            throw new NotImplementedException();
        }

        public static float? StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Func<TSource, float?> selector)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Func<TSource, int> selector)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Func<TSource, int?> selector)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Func<TSource, long> selector)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationPopulation<TSource>(this IQueryable<TSource> source, Func<TSource, long?> selector)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal> StandardDeviationPopulationAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal> StandardDeviationPopulationAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationPopulationAsync(this IQueryable<double> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationPopulationAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationPopulationAsync(this IQueryable<int> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationPopulationAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationPopulationAsync(this IQueryable<long> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationPopulationAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float> StandardDeviationPopulationAsync(this IQueryable<float> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float?> StandardDeviationPopulationAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Func<TSource, decimal> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal?> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Func<TSource, decimal?> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Func<TSource, double> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Func<TSource, double?> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Func<TSource, float> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float?> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Func<TSource, float?> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Func<TSource, int> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Func<TSource, int?> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Func<TSource, long> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationPopulationAsync<TSource>(this IQueryable<TSource> source, Func<TSource, long?> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static decimal StandardDeviationSample(this IQueryable<decimal> source)
        {
            throw new NotImplementedException();
        }

        public static decimal StandardDeviationSample(this IQueryable<decimal?> source)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationSample(this IQueryable<double> source)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationSample(this IQueryable<double?> source)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationSample(this IQueryable<int> source)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationSample(this IQueryable<int?> source)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationSample(this IQueryable<long> source)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationSample(this IQueryable<long?> source)
        {
            throw new NotImplementedException();
        }

        public static float StandardDeviationSample(this IQueryable<float> source)
        {
            throw new NotImplementedException();
        }

        public static float? StandardDeviationSample(this IQueryable<float?> source)
        {
            throw new NotImplementedException();
        }

        public static decimal StandardDeviationSample<TSource>(this IQueryable<TSource> source, Func<TSource, decimal> selector)
        {
            throw new NotImplementedException();
        }

        public static decimal? StandardDeviationSample<TSource>(this IQueryable<TSource> source, Func<TSource, decimal?> selector)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationSample<TSource>(this IQueryable<TSource> source, Func<TSource, double> selector)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationSample<TSource>(this IQueryable<TSource> source, Func<TSource, double?> selector)
        {
            throw new NotImplementedException();
        }

        public static float StandardDeviationSample<TSource>(this IQueryable<TSource> source, Func<TSource, float> selector)
        {
            throw new NotImplementedException();
        }

        public static float? StandardDeviationSample<TSource>(this IQueryable<TSource> source, Func<TSource, float?> selector)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationSample<TSource>(this IQueryable<TSource> source, Func<TSource, int> selector)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationSample<TSource>(this IQueryable<TSource> source, Func<TSource, int?> selector)
        {
            throw new NotImplementedException();
        }

        public static double StandardDeviationSample<TSource>(this IQueryable<TSource> source, Func<TSource, long> selector)
        {
            throw new NotImplementedException();
        }

        public static double? StandardDeviationSample<TSource>(this IQueryable<TSource> source, Func<TSource, long?> selector)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal> StandardDeviationSampleAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal> StandardDeviationSampleAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationSampleAsync(this IQueryable<double> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationSampleAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationSampleAsync(this IQueryable<int> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationSampleAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationSampleAsync(this IQueryable<long> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationSampleAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float> StandardDeviationSampleAsync(this IQueryable<float> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float?> StandardDeviationSampleAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Func<TSource, decimal> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal?> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Func<TSource, decimal?> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Func<TSource, double> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Func<TSource, double?> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Func<TSource, float> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float?> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Func<TSource, float?> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Func<TSource, int> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Func<TSource, int?> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Func<TSource, long> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> StandardDeviationSampleAsync<TSource>(this IQueryable<TSource> source, Func<TSource, long?> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 unused)
        {
            return f.GetMethodInfo();
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 unused1, T2 unused2)
        {
            return f.GetMethodInfo();
        }

        private static MethodInfo GetMethodInfo<T1, T2, T3, T4>(Func<T1, T2, T3, T4> f, T1 unused1, T2 unused2, T3 unused3)
        {
            return f.GetMethodInfo();
        }
    }
}

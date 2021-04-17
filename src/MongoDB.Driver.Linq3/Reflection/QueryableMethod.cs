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

namespace MongoDB.Driver.Linq3.Reflection
{
    public static class QueryableMethod
    {
        // private static fields
        private static readonly MethodInfo __aggregate;
        private static readonly MethodInfo __aggregateWithSeedAndFunc;
        private static readonly MethodInfo __aggregateWithSeedFuncAndSelector;
        private static readonly MethodInfo __all;
        private static readonly MethodInfo __any;
        private static readonly MethodInfo __anyWithPredicate;
        private static readonly MethodInfo __averageDecimal;
        private static readonly MethodInfo __averageDecimalWithSelector;
        private static readonly MethodInfo __averageDouble;
        private static readonly MethodInfo __averageDoubleWithSelector;
        private static readonly MethodInfo __averageInt32;
        private static readonly MethodInfo __averageInt32WithSelector;
        private static readonly MethodInfo __averageInt64;
        private static readonly MethodInfo __averageInt64WithSelector;
        private static readonly MethodInfo __averageNullableDecimal;
        private static readonly MethodInfo __averageNullableDecimalWithSelector;
        private static readonly MethodInfo __averageNullableDouble;
        private static readonly MethodInfo __averageNullableDoubleWithSelector;
        private static readonly MethodInfo __averageNullableInt32;
        private static readonly MethodInfo __averageNullableInt32WithSelector;
        private static readonly MethodInfo __averageNullableInt64;
        private static readonly MethodInfo __averageNullableInt64WithSelector;
        private static readonly MethodInfo __averageNullableSingle;
        private static readonly MethodInfo __averageNullableSingleWithSelector;
        private static readonly MethodInfo __averageSingle;
        private static readonly MethodInfo __averageSingleWithSelector;
        private static readonly MethodInfo __cast;
        private static readonly MethodInfo __concat;
        private static readonly MethodInfo __contains;
        private static readonly MethodInfo __count;
        private static readonly MethodInfo __countWithPredicate;
        private static readonly MethodInfo __defaultIfEmpty;
        private static readonly MethodInfo __defaultIfEmptyWithDefaultValue;
        private static readonly MethodInfo __distinct;
        private static readonly MethodInfo __elementAt;
        private static readonly MethodInfo __elementAtOrDefault;
        private static readonly MethodInfo __except;
        private static readonly MethodInfo __first;
        private static readonly MethodInfo __firstOrDefault;
        private static readonly MethodInfo __firstOrDefaultWithPredicate;
        private static readonly MethodInfo __firstWithPredicate;
        private static readonly MethodInfo __groupByWithKeySelector;
        private static readonly MethodInfo __groupByWithKeySelectorAndElementSelector;
        private static readonly MethodInfo __groupByWithKeySelectorAndResultSelector;
        private static readonly MethodInfo __groupByWithKeySelectorElementSelectorAndResultSelector;
        private static readonly MethodInfo __groupJoin;
        private static readonly MethodInfo __intersect;
        private static readonly MethodInfo __join;
        private static readonly MethodInfo __last;
        private static readonly MethodInfo __lastOrDefault;
        private static readonly MethodInfo __lastOrDefaultWithPredicate;
        private static readonly MethodInfo __lastWithPredicate;
        private static readonly MethodInfo __longCount;
        private static readonly MethodInfo __longCountWithPredicate;
        private static readonly MethodInfo __max;
        private static readonly MethodInfo __maxWithSelector;
        private static readonly MethodInfo __min;
        private static readonly MethodInfo __minWithSelector;
        private static readonly MethodInfo __ofType;
        private static readonly MethodInfo __orderBy;
        private static readonly MethodInfo __orderByDescending;
        private static readonly MethodInfo __reverse;
        private static readonly MethodInfo __select;
        private static readonly MethodInfo __selectMany;
        private static readonly MethodInfo __selectManyWithCollectionSelectorAndResultSelector;
        private static readonly MethodInfo __selectManyWithCollectionSelectorTakingIndexAndResultSelector;
        private static readonly MethodInfo __selectManyWithSelectorTakingIndex;
        private static readonly MethodInfo __selectWithSelectorTakingIndex;
        private static readonly MethodInfo __single;
        private static readonly MethodInfo __singleOrDefault;
        private static readonly MethodInfo __singleOrDefaultWithPredicate;
        private static readonly MethodInfo __singleWithPredicate;
        private static readonly MethodInfo __skip;
        private static readonly MethodInfo __sumDecimal;
        private static readonly MethodInfo __sumDecimalWithSelector;
        private static readonly MethodInfo __sumDouble;
        private static readonly MethodInfo __sumDoubleWithSelector;
        private static readonly MethodInfo __sumInt32;
        private static readonly MethodInfo __sumInt32WithSelector;
        private static readonly MethodInfo __sumInt64;
        private static readonly MethodInfo __sumInt64WithSelector;
        private static readonly MethodInfo __sumNullableDecimal;
        private static readonly MethodInfo __sumNullableDecimalWithSelector;
        private static readonly MethodInfo __sumNullableDouble;
        private static readonly MethodInfo __sumNullableDoubleWithSelector;
        private static readonly MethodInfo __sumNullableInt32;
        private static readonly MethodInfo __sumNullableInt32WithSelector;
        private static readonly MethodInfo __sumNullableInt64;
        private static readonly MethodInfo __sumNullableInt64WithSelector;
        private static readonly MethodInfo __sumNullableSingle;
        private static readonly MethodInfo __sumNullableSingleWithSelector;
        private static readonly MethodInfo __sumSingle;
        private static readonly MethodInfo __sumSingleWithSelector;
        private static readonly MethodInfo __take;
        private static readonly MethodInfo __thenBy;
        private static readonly MethodInfo __thenByDescending;
        private static readonly MethodInfo __union;
        private static readonly MethodInfo __where;
        private static readonly MethodInfo __whereWithPredicateTakingIndex;
        private static readonly MethodInfo __zip;

        // static constructor
        static QueryableMethod()
        {
            __aggregate = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, object, object>> func) => source.Aggregate(func));
            __aggregateWithSeedAndFunc = ReflectionInfo.Method((IQueryable<object> source, object seed, Expression<Func<object, object, object>> func) => source.Aggregate(seed, func));
            __aggregateWithSeedFuncAndSelector = ReflectionInfo.Method((IQueryable<object> source, object seed, Expression<Func<object, object, object>> func, Expression<Func<object, object>> selector) => source.Aggregate(seed, func, selector));
            __all = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.All(predicate));
            __any = ReflectionInfo.Method((IQueryable<object> source) => source.Any());
            __anyWithPredicate = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.Any(predicate));
            __averageDecimal = ReflectionInfo.Method((IQueryable<decimal> source) => source.Average());
            __averageDecimalWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, decimal>> selector) => source.Average(selector));
            __averageDouble = ReflectionInfo.Method((IQueryable<double> source) => source.Average());
            __averageDoubleWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, double>> selector) => source.Average(selector));
            __averageInt32 = ReflectionInfo.Method((IQueryable<int> source) => source.Average());
            __averageInt32WithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, int>> selector) => source.Average(selector));
            __averageInt64 = ReflectionInfo.Method((IQueryable<long> source) => source.Average());
            __averageInt64WithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, long>> selector) => source.Average(selector));
            __averageNullableDecimal = ReflectionInfo.Method((IQueryable<decimal?> source) => source.Average());
            __averageNullableDecimalWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, decimal?>> selector) => source.Average(selector));
            __averageNullableDouble = ReflectionInfo.Method((IQueryable<double?> source) => source.Average());
            __averageNullableDoubleWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, double?>> selector) => source.Average(selector));
            __averageNullableInt32 = ReflectionInfo.Method((IQueryable<int?> source) => source.Average());
            __averageNullableInt32WithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, int?>> selector) => source.Average(selector));
            __averageNullableInt64 = ReflectionInfo.Method((IQueryable<long?> source) => source.Average());
            __averageNullableInt64WithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, double?>> selector) => source.Average(selector));
            __averageNullableSingle = ReflectionInfo.Method((IQueryable<float?> source) => source.Average());
            __averageNullableSingleWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, float?>> selector) => source.Average(selector));
            __averageSingle = ReflectionInfo.Method((IQueryable<float> source) => source.Average());
            __averageSingleWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, float>> selector) => source.Average(selector));
            __cast = ReflectionInfo.Method((IQueryable<object> source) => source.Cast<object>());
            __concat = ReflectionInfo.Method((IQueryable<object> source1, IEnumerable<object> source2) => source1.Concat(source2));
            __contains = ReflectionInfo.Method((IQueryable<object> source, object item) => source.Contains(item));
            __count = ReflectionInfo.Method((IQueryable<object> source) => source.Count());
            __countWithPredicate = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.Count(predicate));
            __defaultIfEmpty = ReflectionInfo.Method((IQueryable<object> source) => source.DefaultIfEmpty());
            __defaultIfEmptyWithDefaultValue = ReflectionInfo.Method((IQueryable<object> source, object defaultValue) => source.DefaultIfEmpty(defaultValue));
            __distinct = ReflectionInfo.Method((IQueryable<object> source) => source.Distinct());
            __elementAt = ReflectionInfo.Method((IQueryable<object> source, int index) => source.ElementAt(index));
            __elementAtOrDefault = ReflectionInfo.Method((IQueryable<object> source, int index) => source.ElementAtOrDefault(index));
            __except = ReflectionInfo.Method((IQueryable<object> source1, IEnumerable<object> source2) => source1.Except(source2));
            __first = ReflectionInfo.Method((IQueryable<object> source) => source.First());
            __firstOrDefault = ReflectionInfo.Method((IQueryable<object> source) => source.FirstOrDefault());
            __firstOrDefaultWithPredicate = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.FirstOrDefault(predicate));
            __firstWithPredicate = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.First(predicate));
            __groupByWithKeySelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, object>> keySelector) => source.GroupBy(keySelector));
            __groupByWithKeySelectorAndElementSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, object>> keySelector, Expression<Func<object, object>> elementSelector) => source.GroupBy(keySelector, elementSelector));
            __groupByWithKeySelectorAndResultSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, object>> keySelector, Expression<Func<object, IEnumerable<object>, object>> resultSelector) => source.GroupBy(keySelector, resultSelector));
            __groupByWithKeySelectorElementSelectorAndResultSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, object>> keySelector, Expression<Func<object, object>> elementSelector, Expression<Func<object, IEnumerable<object>, object>> resultSelector) => source.GroupBy(keySelector, elementSelector, resultSelector));
            __groupJoin = ReflectionInfo.Method((IQueryable<object> outer, IEnumerable<object> inner, Expression<Func<object, object>> outerKeySelector, Expression<Func<object, object>> innerKeySelector, Expression<Func<object, IEnumerable<object>, object>> resultSelector) => outer.GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector));
            __intersect = ReflectionInfo.Method((IQueryable<object> source1, IEnumerable<object> source2) => source1.Intersect(source2));
            __join = ReflectionInfo.Method((IQueryable<object> outer, IEnumerable<object> inner, Expression<Func<object, object>> outerKeySelector, Expression<Func<object, object>> innerKeySelector, Expression<Func<object, object, object>> resultSelector) => outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector));
            __last = ReflectionInfo.Method((IQueryable<object> source) => source.Last());
            __lastOrDefault = ReflectionInfo.Method((IQueryable<object> source) => source.LastOrDefault());
            __lastOrDefaultWithPredicate = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.LastOrDefault(predicate));
            __lastWithPredicate = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.Last(predicate));
            __longCount = ReflectionInfo.Method((IQueryable<object> source) => source.LongCount());
            __longCountWithPredicate = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.LongCount(predicate));
            __max = ReflectionInfo.Method((IQueryable<object> source) => source.Max());
            __maxWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, object>> selector) => source.Max(selector));
            __min = ReflectionInfo.Method((IQueryable<object> source) => source.Min());
            __minWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, object>> selector) => source.Min(selector));
            __ofType = ReflectionInfo.Method((IQueryable source) => source.OfType<object>());
            __orderBy = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, object>> keySelector) => source.OrderBy(keySelector));
            __orderByDescending = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, object>> keySelector) => source.OrderByDescending(keySelector));
            __reverse = ReflectionInfo.Method((IQueryable<object> source) => source.Reverse());
            __select = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, object>> selector) => source.Select(selector));
            __selectMany = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, IEnumerable<object>>> selector) => source.SelectMany(selector));
            __selectManyWithCollectionSelectorAndResultSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, IEnumerable<object>>> collectionSelector, Expression<Func<object, object, object>> resultSelector) => source.SelectMany(collectionSelector, resultSelector));
            __selectManyWithCollectionSelectorTakingIndexAndResultSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, int, IEnumerable<object>>> collectionSelector, Expression<Func<object, object, object>> resultSelector) => source.SelectMany(collectionSelector, resultSelector));
            __selectManyWithSelectorTakingIndex = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, int, IEnumerable<object>>> selector) => source.SelectMany(selector));
            __selectWithSelectorTakingIndex = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, int, object>> selector) => source.Select(selector));
            __single = ReflectionInfo.Method((IQueryable<object> source) => source.Single());
            __singleOrDefault = ReflectionInfo.Method((IQueryable<object> source) => source.SingleOrDefault());
            __singleOrDefaultWithPredicate = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.SingleOrDefault(predicate));
            __singleWithPredicate = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.Single(predicate));
            __skip = ReflectionInfo.Method((IQueryable<object> source, int count) => source.Skip(count));
            __sumDecimal = ReflectionInfo.Method((IQueryable<decimal> source) => source.Sum());
            __sumDecimalWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, decimal>> selector) => source.Sum(selector));
            __sumDouble = ReflectionInfo.Method((IQueryable<double> source) => source.Sum());
            __sumDoubleWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, double>> selector) => source.Sum(selector));
            __sumInt32 = ReflectionInfo.Method((IQueryable<int> source) => source.Sum());
            __sumInt32WithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, int>> selector) => source.Sum(selector));
            __sumInt64 = ReflectionInfo.Method((IQueryable<long> source) => source.Sum());
            __sumInt64WithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, long>> selector) => source.Sum(selector));
            __sumNullableDecimal = ReflectionInfo.Method((IQueryable<decimal?> source) => source.Sum());
            __sumNullableDecimalWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, decimal?>> selector) => source.Sum(selector));
            __sumNullableDouble = ReflectionInfo.Method((IQueryable<double?> source) => source.Sum());
            __sumNullableDoubleWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, double?>> selector) => source.Sum(selector));
            __sumNullableInt32 = ReflectionInfo.Method((IQueryable<int?> source) => source.Sum());
            __sumNullableInt32WithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, int?>> selector) => source.Sum(selector));
            __sumNullableInt64 = ReflectionInfo.Method((IQueryable<long?> source) => source.Sum());
            __sumNullableInt64WithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, long?>> selector) => source.Sum(selector));
            __sumNullableSingle = ReflectionInfo.Method((IQueryable<float?> source) => source.Sum());
            __sumNullableSingleWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, float?>> selector) => source.Sum(selector));
            __sumSingle = ReflectionInfo.Method((IQueryable<float> source) => source.Sum());
            __sumSingleWithSelector = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, float>> selector) => source.Sum(selector));
            __take = ReflectionInfo.Method((IQueryable<object> source, int count) => source.Take(count));
            __thenBy = ReflectionInfo.Method((IOrderedQueryable<object> source, Expression<Func<object, object>> keySelector) => source.ThenBy(keySelector));
            __thenByDescending = ReflectionInfo.Method((IOrderedQueryable<object> source, Expression<Func<object, object>> keySelector) => source.ThenByDescending(keySelector));
            __union = ReflectionInfo.Method((IQueryable<object> source1, IEnumerable<object> source2) => source1.Union(source2));
            __where = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, bool>> predicate) => source.Where(predicate));
            __whereWithPredicateTakingIndex = ReflectionInfo.Method((IQueryable<object> source, Expression<Func<object, int, bool>> predicate) => source.Where(predicate));
            __zip = ReflectionInfo.Method((IQueryable<object> source1, IEnumerable<object> source2, Expression<Func<object, object, object>> resultSelector) => source1.Zip(source2, resultSelector));
        }

        // public properties
        public static MethodInfo Aggregate => __aggregate;
        public static MethodInfo AggregateWithSeedAndFunc => __aggregateWithSeedAndFunc;
        public static MethodInfo AggregateWithSeedFuncAndSelector => __aggregateWithSeedFuncAndSelector;
        public static MethodInfo All => __all;
        public static MethodInfo Any => __any;
        public static MethodInfo AverageDecimal => __averageDecimal;
        public static MethodInfo AverageDecimalWithSelector => __averageDecimalWithSelector;
        public static MethodInfo AverageDouble => __averageDouble;
        public static MethodInfo AverageDoubleWithSelector => __averageDoubleWithSelector;
        public static MethodInfo AverageInt32 => __averageInt32;
        public static MethodInfo AverageInt32WithSelector => __averageInt32WithSelector;
        public static MethodInfo AverageInt64 => __averageInt64;
        public static MethodInfo AverageInt64WithSelector => __averageInt64WithSelector;
        public static MethodInfo AverageNullableDecimal => __averageNullableDecimal;
        public static MethodInfo AverageNullableDecimalWithSelector => __averageNullableDecimalWithSelector;
        public static MethodInfo AverageNullableDouble => __averageNullableDouble;
        public static MethodInfo AverageNullableDoubleWithSelector => __averageNullableDoubleWithSelector;
        public static MethodInfo AverageNullableInt32 => __averageNullableInt32;
        public static MethodInfo AverageNullableInt32WithSelector => __averageNullableInt32WithSelector;
        public static MethodInfo AverageNullableInt64 => __averageNullableInt64;
        public static MethodInfo AverageNullableInt64WithSelector => __averageNullableInt64WithSelector;
        public static MethodInfo AverageNullableSingle => __averageNullableSingle;
        public static MethodInfo AverageNullableSingleWithSelector => __averageNullableSingleWithSelector;
        public static MethodInfo AverageSingle => __averageSingle;
        public static MethodInfo AverageSingleWithSelector => __averageSingleWithSelector;
        public static MethodInfo AnyWithPredicate => __anyWithPredicate;
        public static MethodInfo Cast => __cast;
        public static MethodInfo Concat => __concat;
        public static MethodInfo Contains => __contains;
        public static MethodInfo Count => __count;
        public static MethodInfo CountWithPredicate => __countWithPredicate;
        public static MethodInfo DefaultIfEmpty => __defaultIfEmpty;
        public static MethodInfo DefaultIfEmptyWithDefaultValue => __defaultIfEmptyWithDefaultValue;
        public static MethodInfo Distinct => __distinct;
        public static MethodInfo ElementAt => __elementAt;
        public static MethodInfo ElementAtOrDefault => __elementAtOrDefault;
        public static MethodInfo Except => __except;
        public static MethodInfo First => __first;
        public static MethodInfo FirstOrDefault => __firstOrDefault;
        public static MethodInfo FirstOrDefaultWithPredicate => __firstOrDefaultWithPredicate;
        public static MethodInfo FirstWithPredicate => __firstWithPredicate;
        public static MethodInfo GroupByWithKeySelector => __groupByWithKeySelector;
        public static MethodInfo GroupByWithKeySelectorAndElementSelector => __groupByWithKeySelectorAndElementSelector;
        public static MethodInfo GroupByWithKeySelectorAndResultSelector => __groupByWithKeySelectorAndResultSelector;
        public static MethodInfo GroupByWithKeySelectorElementSelectorAndResultSelector => __groupByWithKeySelectorElementSelectorAndResultSelector;
        public static MethodInfo GroupJoin => __groupJoin;
        public static MethodInfo Interset => __intersect;
        public static MethodInfo Join => __join;
        public static MethodInfo Last => __last;
        public static MethodInfo LastOrDefault => __lastOrDefault;
        public static MethodInfo LastOrDefaultWithPredicate => __lastOrDefaultWithPredicate;
        public static MethodInfo LastWithPredicate => __lastWithPredicate;
        public static MethodInfo LongCount => __longCount;
        public static MethodInfo LongCountWithPredicate => __longCountWithPredicate;
        public static MethodInfo Max => __max;
        public static MethodInfo MaxWithSelector => __maxWithSelector;
        public static MethodInfo Min => __min;
        public static MethodInfo MinWithSelector => __minWithSelector;
        public static MethodInfo OfType => __ofType;
        public static MethodInfo OrderBy => __orderBy;
        public static MethodInfo OrderByDescending => __orderByDescending;
        public static MethodInfo Reverse => __reverse;
        public static MethodInfo Select => __select;
        public static MethodInfo SelectMany => __selectMany;
        public static MethodInfo SelectManyWithCollectionSelectorAndResultSelector => __selectManyWithCollectionSelectorAndResultSelector;
        public static MethodInfo SelectManyWithCollectionSelectorTakingIndexAndResultSelector => __selectManyWithCollectionSelectorTakingIndexAndResultSelector;
        public static MethodInfo SelectManyWithSelectorTakingIndex => __selectManyWithSelectorTakingIndex;
        public static MethodInfo SelectWithSelectorTakingIndex => __selectWithSelectorTakingIndex;
        public static MethodInfo Single => __single;
        public static MethodInfo SingleOrDefault => __singleOrDefault;
        public static MethodInfo SingleOrDefaultWithPredicate => __singleOrDefaultWithPredicate;
        public static MethodInfo SingleWithPredicate => __singleWithPredicate;
        public static MethodInfo Skip => __skip;
        public static MethodInfo SumDecimal => __sumDecimal;
        public static MethodInfo SumDecimalWithSelector => __sumDecimalWithSelector;
        public static MethodInfo SumDouble => __sumDouble;
        public static MethodInfo SumDoubleWithSelector => __sumDoubleWithSelector;
        public static MethodInfo SumInt32 => __sumInt32;
        public static MethodInfo SumInt32WithSelector => __sumInt32WithSelector;
        public static MethodInfo SumInt64 => __sumInt64;
        public static MethodInfo SumInt64WithSelector => __sumInt64WithSelector;
        public static MethodInfo SumNullableDecimal => __sumNullableDecimal;
        public static MethodInfo SumNullableDecimalWithSelector => __sumNullableDecimalWithSelector;
        public static MethodInfo SumNullableDouble => __sumNullableDouble;
        public static MethodInfo SumNullableDoubleWithSelector => __sumNullableDoubleWithSelector;
        public static MethodInfo SumNullableInt32 => __sumNullableInt32;
        public static MethodInfo SumNullableInt32WithSelector => __sumNullableInt32WithSelector;
        public static MethodInfo SumNullableInt64 => __sumNullableInt64;
        public static MethodInfo SumNullableInt64WithSelector => __sumNullableInt64WithSelector;
        public static MethodInfo SumNullableSingle => __sumNullableSingle;
        public static MethodInfo SumNullableSingleWithSelector => __sumNullableSingleWithSelector;
        public static MethodInfo SumSingle => __sumSingle;
        public static MethodInfo SumSingleWithSelector => __sumSingleWithSelector;
        public static MethodInfo Take => __take;
        public static MethodInfo ThenBy => __thenBy;
        public static MethodInfo ThenByDescending => __thenByDescending;
        public static MethodInfo Union => __union;
        public static MethodInfo Where => __where;
        public static MethodInfo WhereWithPredicateTakingIndex => __whereWithPredicateTakingIndex;
        public static MethodInfo Zip => __zip;

        // public methods
        public static MethodInfo MakeSelect(Type tsource, Type tresult)
        {
            return __select.MakeGenericMethod(tsource, tresult);
        }

        public static MethodInfo MakeWhere(Type tsource)
        {
            return __where.MakeGenericMethod(tsource);
        }
    }
}

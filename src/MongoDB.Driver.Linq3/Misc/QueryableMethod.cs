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

namespace MongoDB.Driver.Linq3.Misc
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
            __aggregate = new Func<IQueryable<object>, Expression<Func<object, object, object>>, object>(Queryable.Aggregate).Method.GetGenericMethodDefinition();
            __aggregateWithSeedAndFunc = new Func<IQueryable<object>, object, Expression<Func<object, object, object>>, object>(Queryable.Aggregate).Method.GetGenericMethodDefinition();
            __aggregateWithSeedFuncAndSelector = new Func<IQueryable<object>, object, Expression<Func<object, object, object>>, Expression<Func<object, object>>, object>(Queryable.Aggregate).Method.GetGenericMethodDefinition();
            __all = new Func<IQueryable<object>, Expression<Func<object, bool>>, bool>(Queryable.All).Method.GetGenericMethodDefinition();
            __any = new Func<IQueryable<object>, bool>(Queryable.Any).Method.GetGenericMethodDefinition();
            __anyWithPredicate = new Func<IQueryable<object>, Expression<Func<object, bool>>, bool>(Queryable.Any).Method.GetGenericMethodDefinition();
            __averageDecimal = new Func<IQueryable<decimal>, decimal>(Queryable.Average).Method;
            __averageDecimalWithSelector = new Func<IQueryable<object>, Expression<Func<object, decimal>>, decimal>(Queryable.Average).Method.GetGenericMethodDefinition();
            __averageDouble = new Func<IQueryable<double>, double>(Queryable.Average).Method;
            __averageDoubleWithSelector = new Func<IQueryable<object>, Expression<Func<object, double>>, double>(Queryable.Average).Method.GetGenericMethodDefinition();
            __averageInt32 = new Func<IQueryable<int>, double>(Queryable.Average).Method;
            __averageInt32WithSelector = new Func<IQueryable<object>, Expression<Func<object, int>>, double>(Queryable.Average).Method.GetGenericMethodDefinition();
            __averageInt64 = new Func<IQueryable<long>, double>(Queryable.Average).Method;
            __averageInt64WithSelector = new Func<IQueryable<object>, Expression<Func<object, long>>, double>(Queryable.Average).Method.GetGenericMethodDefinition();
            __averageNullableDecimal = new Func<IQueryable<decimal?>, decimal?>(Queryable.Average).Method;
            __averageNullableDecimalWithSelector = new Func<IQueryable<object>, Expression<Func<object, decimal?>>, decimal?>(Queryable.Average).Method.GetGenericMethodDefinition();
            __averageNullableDouble = new Func<IQueryable<double?>, double?>(Queryable.Average).Method;
            __averageNullableDoubleWithSelector = new Func<IQueryable<object>, Expression<Func<object, double?>>, double?>(Queryable.Average).Method.GetGenericMethodDefinition();
            __averageNullableInt32 = new Func<IQueryable<int?>, double?>(Queryable.Average).Method;
            __averageNullableInt32WithSelector = new Func<IQueryable<object>, Expression<Func<object, int?>>, double?>(Queryable.Average).Method.GetGenericMethodDefinition();
            __averageNullableInt64 = new Func<IQueryable<long?>, double?>(Queryable.Average).Method;
            __averageNullableInt64WithSelector = new Func<IQueryable<object>, Expression<Func<object, double?>>, double?>(Queryable.Average).Method.GetGenericMethodDefinition();
            __averageNullableSingle = new Func<IQueryable<float?>, float?>(Queryable.Average).Method;
            __averageNullableSingleWithSelector = new Func<IQueryable<object>, Expression<Func<object, float?>>, float?>(Queryable.Average).Method.GetGenericMethodDefinition();
            __averageSingle = new Func<IQueryable<float>, float>(Queryable.Average).Method;
            __averageSingleWithSelector = new Func<IQueryable<object>, Expression<Func<object, float>>, float>(Queryable.Average).Method.GetGenericMethodDefinition();
            __cast = new Func<IQueryable<object>, IQueryable<object>>(Queryable.Cast<object>).Method;
            __concat = new Func<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Concat).Method.GetGenericMethodDefinition();
            __contains = new Func<IQueryable<object>, object, bool>(Queryable.Contains).Method.GetGenericMethodDefinition();
            __count = new Func<IQueryable<object>, int>(Queryable.Count).Method.GetGenericMethodDefinition();
            __countWithPredicate = new Func<IQueryable<object>, Expression<Func<object, bool>>, int>(Queryable.Count).Method.GetGenericMethodDefinition();
            __defaultIfEmpty = new Func<IQueryable<object>, IQueryable<object>>(Queryable.DefaultIfEmpty).Method.GetGenericMethodDefinition();
            __defaultIfEmptyWithDefaultValue = new Func<IQueryable<object>, object, IQueryable<object>>(Queryable.DefaultIfEmpty).Method.GetGenericMethodDefinition();
            __distinct = new Func<IQueryable<object>, IQueryable<object>>(Queryable.Distinct).Method.GetGenericMethodDefinition();
            __elementAt = new Func<IQueryable<object>, int, object>(Queryable.ElementAt).Method.GetGenericMethodDefinition();
            __elementAtOrDefault = new Func<IQueryable<object>, int, object>(Queryable.ElementAtOrDefault).Method.GetGenericMethodDefinition();
            __except = new Func<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Except).Method.GetGenericMethodDefinition();
            __first = new Func<IQueryable<object>, object>(Queryable.First).Method.GetGenericMethodDefinition();
            __firstOrDefault = new Func<IQueryable<object>, object>(Queryable.FirstOrDefault).Method.GetGenericMethodDefinition();
            __firstOrDefaultWithPredicate = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.FirstOrDefault).Method.GetGenericMethodDefinition();
            __firstWithPredicate = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.First).Method.GetGenericMethodDefinition();
            __groupByWithKeySelector = new Func<IQueryable<object>, Expression<Func<object, object>>, IQueryable<IGrouping<object, object>>>(Queryable.GroupBy).Method.GetGenericMethodDefinition();
            __groupByWithKeySelectorAndElementSelector = new Func<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, IQueryable<IGrouping<object, object>>>(Queryable.GroupBy).Method.GetGenericMethodDefinition();
            __groupByWithKeySelectorAndResultSelector = new Func<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupBy).Method.GetGenericMethodDefinition();
            __groupByWithKeySelectorElementSelectorAndResultSelector = new Func<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupBy).Method.GetGenericMethodDefinition();
            __groupJoin = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupJoin).Method.GetGenericMethodDefinition();
            __intersect = new Func<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Intersect).Method.GetGenericMethodDefinition();
            __join = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.Join).Method.GetGenericMethodDefinition();
            __last = new Func<IQueryable<object>, object>(Queryable.Last).Method.GetGenericMethodDefinition();
            __lastOrDefault = new Func<IQueryable<object>, object>(Queryable.LastOrDefault).Method.GetGenericMethodDefinition();
            __lastOrDefaultWithPredicate = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.LastOrDefault).Method.GetGenericMethodDefinition();
            __lastWithPredicate = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.Last).Method.GetGenericMethodDefinition();
            __longCount = new Func<IQueryable<object>, long>(Queryable.LongCount).Method.GetGenericMethodDefinition();
            __longCountWithPredicate = new Func<IQueryable<object>, Expression<Func<object, bool>>, long>(Queryable.LongCount).Method.GetGenericMethodDefinition();
            __max = new Func<IQueryable<object>, object>(Queryable.Max).Method.GetGenericMethodDefinition();
            __maxWithSelector = new Func<IQueryable<object>, Expression<Func<object, object>>, object>(Queryable.Max).Method.GetGenericMethodDefinition();
            __min = new Func<IQueryable<object>, object>(Queryable.Min).Method.GetGenericMethodDefinition();
            __minWithSelector = new Func<IQueryable<object>, Expression<Func<object, object>>, object>(Queryable.Min).Method.GetGenericMethodDefinition();
            __ofType = new Func<IQueryable, IQueryable<object>>(Queryable.OfType<object>).Method.GetGenericMethodDefinition();
            __orderBy = new Func<IQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.OrderBy).Method.GetGenericMethodDefinition();
            __orderByDescending = new Func<IQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.OrderByDescending).Method.GetGenericMethodDefinition();
            __reverse = new Func<IQueryable<object>, IQueryable<object>>(Queryable.Reverse).Method.GetGenericMethodDefinition();
            __select = new Func<IQueryable<object>, Expression<Func<object, object>>, IQueryable<object>>(Queryable.Select).Method.GetGenericMethodDefinition();
            __selectMany = new Func<IQueryable<object>, Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>(Queryable.SelectMany).Method.GetGenericMethodDefinition();
            __selectManyWithCollectionSelectorAndResultSelector = new Func<IQueryable<object>, Expression<Func<object, IEnumerable<object>>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.SelectMany).Method.GetGenericMethodDefinition();
            __selectManyWithCollectionSelectorTakingIndexAndResultSelector = new Func<IQueryable<object>, Expression<Func<object, int, IEnumerable<object>>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.SelectMany).Method.GetGenericMethodDefinition();
            __selectManyWithSelectorTakingIndex = new Func<IQueryable<object>, Expression<Func<object, int, IEnumerable<object>>>, IQueryable<object>>(Queryable.SelectMany).Method.GetGenericMethodDefinition();
            __selectWithSelectorTakingIndex = new Func<IQueryable<object>, Expression<Func<object, int, object>>, IQueryable<object>>(Queryable.Select).Method.GetGenericMethodDefinition();
            __single = new Func<IQueryable<object>, object>(Queryable.Single).Method.GetGenericMethodDefinition();
            __singleOrDefault = new Func<IQueryable<object>, object>(Queryable.SingleOrDefault).Method.GetGenericMethodDefinition();
            __singleOrDefaultWithPredicate = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.SingleOrDefault).Method.GetGenericMethodDefinition();
            __singleWithPredicate = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.Single).Method.GetGenericMethodDefinition();
            __skip = new Func<IQueryable<object>, int, IQueryable<object>>(Queryable.Skip).Method.GetGenericMethodDefinition();
            __sumDecimal = new Func<IQueryable<decimal>, decimal>(Queryable.Sum).Method;
            __sumDecimalWithSelector = new Func<IQueryable<object>, Expression<Func<object, decimal>>, decimal>(Queryable.Sum).Method.GetGenericMethodDefinition();
            __sumDouble = new Func<IQueryable<double>, double>(Queryable.Sum).Method;
            __sumDoubleWithSelector = new Func<IQueryable<object>, Expression<Func<object, double>>, double>(Queryable.Sum).Method.GetGenericMethodDefinition();
            __sumInt32 = new Func<IQueryable<int>, int>(Queryable.Sum).Method;
            __sumInt32WithSelector = new Func<IQueryable<object>, Expression<Func<object, int>>, int>(Queryable.Sum).Method.GetGenericMethodDefinition();
            __sumInt64 = new Func<IQueryable<long>, long>(Queryable.Sum).Method;
            __sumInt64WithSelector = new Func<IQueryable<object>, Expression<Func<object, long>>, long>(Queryable.Sum).Method.GetGenericMethodDefinition();
            __sumNullableDecimal = new Func<IQueryable<decimal?>, decimal?>(Queryable.Sum).Method;
            __sumNullableDecimalWithSelector = new Func<IQueryable<object>, Expression<Func<object, decimal?>>, decimal?>(Queryable.Sum).Method.GetGenericMethodDefinition();
            __sumNullableDouble = new Func<IQueryable<double?>, double?>(Queryable.Sum).Method;
            __sumNullableDoubleWithSelector = new Func<IQueryable<object>, Expression<Func<object, double?>>, double?>(Queryable.Sum).Method.GetGenericMethodDefinition();
            __sumNullableInt32 = new Func<IQueryable<int?>, int?>(Queryable.Sum).Method;
            __sumNullableInt32WithSelector = new Func<IQueryable<object>, Expression<Func<object, int?>>, int?>(Queryable.Sum).Method.GetGenericMethodDefinition();
            __sumNullableInt64 = new Func<IQueryable<long?>, long?>(Queryable.Sum).Method;
            __sumNullableInt64WithSelector = new Func<IQueryable<object>, Expression<Func<object, long?>>, long?>(Queryable.Sum).Method.GetGenericMethodDefinition();
            __sumNullableSingle = new Func<IQueryable<float?>, float?>(Queryable.Sum).Method;
            __sumNullableSingleWithSelector = new Func<IQueryable<object>, Expression<Func<object, float?>>, float?>(Queryable.Sum).Method.GetGenericMethodDefinition();
            __sumSingle = new Func<IQueryable<float>, float>(Queryable.Sum).Method;
            __sumSingleWithSelector = new Func<IQueryable<object>, Expression<Func<object, float>>, float>(Queryable.Sum).Method.GetGenericMethodDefinition();
            __take = new Func<IQueryable<object>, int, IQueryable<object>>(Queryable.Take).Method.GetGenericMethodDefinition();
            __thenBy = new Func<IOrderedQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.ThenBy).Method.GetGenericMethodDefinition();
            __thenByDescending = new Func<IOrderedQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.ThenByDescending).Method.GetGenericMethodDefinition();
            __union = new Func<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Union).Method.GetGenericMethodDefinition();
            __where = new Func<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.Where).Method.GetGenericMethodDefinition();
            __whereWithPredicateTakingIndex = new Func<IQueryable<object>, Expression<Func<object, int, bool>>, IQueryable<object>>(Queryable.Where).Method.GetGenericMethodDefinition();
            __zip = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.Zip).Method.GetGenericMethodDefinition();
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

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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class EnumerableMethod
    {
        // private static fields
        private static readonly MethodInfo __aggregateWithFunc;
        private static readonly MethodInfo __aggregateWithSeedAndFunc;
        private static readonly MethodInfo __aggregateWithSeedFuncAndResultSelector;
        private static readonly MethodInfo __all;
        private static readonly MethodInfo __allWithPredicate;
        private static readonly MethodInfo __any;
        private static readonly MethodInfo __anyWithPredicate;
        private static readonly MethodInfo __append;
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
        private static readonly MethodInfo __bottom;
        private static readonly MethodInfo __bottomN;
        private static readonly MethodInfo __bottomNWithComputedN;
        private static readonly MethodInfo __cast;
        private static readonly MethodInfo __concat;
        private static readonly MethodInfo __contains;
        private static readonly MethodInfo __containsWithComparer;
        private static readonly MethodInfo __count;
        private static readonly MethodInfo __countWithPredicate;
        private static readonly MethodInfo __defaultIfEmpty;
        private static readonly MethodInfo __defaultIfEmptyWithDefaultValue;
        private static readonly MethodInfo __distinct;
        private static readonly MethodInfo __elementAt;
        private static readonly MethodInfo __elementAtOrDefault;
        private static readonly MethodInfo __except;
        private static readonly MethodInfo __first;
        private static readonly MethodInfo __firstN;
        private static readonly MethodInfo __firstNWithComputedN;
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
        private static readonly MethodInfo __lastN;
        private static readonly MethodInfo __lastNWithComputedN;
        private static readonly MethodInfo __lastOrDefault;
        private static readonly MethodInfo __lastOrDefaultWithPredicate;
        private static readonly MethodInfo __lastWithPredicate;
        private static readonly MethodInfo __longCount;
        private static readonly MethodInfo __longCountWithPredicate;
        private static readonly MethodInfo __max;
        private static readonly MethodInfo __maxDecimal;
        private static readonly MethodInfo __maxDecimalWithSelector;
        private static readonly MethodInfo __maxDouble;
        private static readonly MethodInfo __maxDoubleWithSelector;
        private static readonly MethodInfo __maxInt32;
        private static readonly MethodInfo __maxInt32WithSelector;
        private static readonly MethodInfo __maxInt64;
        private static readonly MethodInfo __maxInt64WithSelector;
        private static readonly MethodInfo __maxN;
        private static readonly MethodInfo __maxNullableDecimal;
        private static readonly MethodInfo __maxNullableDecimalWithSelector;
        private static readonly MethodInfo __maxNullableDouble;
        private static readonly MethodInfo __maxNullableDoubleWithSelector;
        private static readonly MethodInfo __maxNullableInt32;
        private static readonly MethodInfo __maxNullableInt32WithSelector;
        private static readonly MethodInfo __maxNullableInt64;
        private static readonly MethodInfo __maxNullableInt64WithSelector;
        private static readonly MethodInfo __maxNullableSingle;
        private static readonly MethodInfo __maxNullableSingleWithSelector;
        private static readonly MethodInfo __maxNWithComputedN;
        private static readonly MethodInfo __maxSingle;
        private static readonly MethodInfo __maxSingleWithSelector;
        private static readonly MethodInfo __maxWithSelector;
        private static readonly MethodInfo __min;
        private static readonly MethodInfo __minDecimal;
        private static readonly MethodInfo __minDecimalWithSelector;
        private static readonly MethodInfo __minDouble;
        private static readonly MethodInfo __minDoubleWithSelector;
        private static readonly MethodInfo __minInt32;
        private static readonly MethodInfo __minInt32WithSelector;
        private static readonly MethodInfo __minInt64;
        private static readonly MethodInfo __minInt64WithSelector;
        private static readonly MethodInfo __minN;
        private static readonly MethodInfo __minNullableDecimal;
        private static readonly MethodInfo __minNullableDecimalWithSelector;
        private static readonly MethodInfo __minNullableDouble;
        private static readonly MethodInfo __minNullableDoubleWithSelector;
        private static readonly MethodInfo __minNullableInt32;
        private static readonly MethodInfo __minNullableInt32WithSelector;
        private static readonly MethodInfo __minNullableInt64;
        private static readonly MethodInfo __minNullableInt64WithSelector;
        private static readonly MethodInfo __minNullableSingle;
        private static readonly MethodInfo __minNullableSingleWithSelector;
        private static readonly MethodInfo __minNWithComputedN;
        private static readonly MethodInfo __minSingle;
        private static readonly MethodInfo __minSingleWithSelector;
        private static readonly MethodInfo __minWithSelector;
        private static readonly MethodInfo __ofType;
        private static readonly MethodInfo __orderBy;
        private static readonly MethodInfo __orderByDescending;
        private static readonly MethodInfo __prepend;
        private static readonly MethodInfo __range;
        private static readonly MethodInfo __repeat;
        private static readonly MethodInfo __reverse;
        private static readonly MethodInfo __reverseWithArray; // will be null on target frameworks that don't have this method
        private static readonly MethodInfo __select;
        private static readonly MethodInfo __selectManyWithSelector;
        private static readonly MethodInfo __selectManyWithCollectionSelectorAndResultSelector;
        private static readonly MethodInfo __selectManyWithCollectionSelectorTakingIndexAndResultSelector;
        private static readonly MethodInfo __selectManyWithSelectorTakingIndex;
        private static readonly MethodInfo __selectWithSelectorTakingIndex;
        private static readonly MethodInfo __sequenceEqual;
        private static readonly MethodInfo __sequenceEqualWithComparer;
        private static readonly MethodInfo __single;
        private static readonly MethodInfo __singleOrDefault;
        private static readonly MethodInfo __singleOrDefaultWithPredicate;
        private static readonly MethodInfo __singleWithPredicate;
        private static readonly MethodInfo __skip;
        private static readonly MethodInfo __skipWhile;
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
        private static readonly MethodInfo __takeWhile;
        private static readonly MethodInfo __thenBy;
        private static readonly MethodInfo __thenByDescending;
        private static readonly MethodInfo __toArray;
        private static readonly MethodInfo __toList;
        private static readonly MethodInfo __top;
        private static readonly MethodInfo __topN;
        private static readonly MethodInfo __topNWithComputedN;
        private static readonly MethodInfo __union;
        private static readonly MethodInfo __where;
        private static readonly MethodInfo __whereWithPredicateTakingIndex;
        private static readonly MethodInfo __zip;

        // sets of methods
        private static readonly IReadOnlyMethodInfoSet __pickOverloads;
        private static readonly IReadOnlyMethodInfoSet __pickOverloadsThatCanOnlyBeUsedAsGroupByAccumulators;
        private static readonly IReadOnlyMethodInfoSet __pickWithComputedNOverloads;
        private static readonly IReadOnlyMethodInfoSet __pickWithNOverloads;
        private static readonly IReadOnlyMethodInfoSet __pickWithSortByOverloads;
        private static readonly IReadOnlyMethodInfoSet __reverseOverloads;

        // static constructor
        static EnumerableMethod()
        {
            // initialize methods before sets of methods
#if NET10_OR_GREATER
            __reverseWithArray = ReflectionInfo.Method(array source) => source.Reverse());
#else
            __reverseWithArray = GetReverseWithArrayMethodInfo(); // support users running net10 even though we don't target net10 yet
#endif

            __aggregateWithFunc = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object, object> func) => source.Aggregate(func));
            __aggregateWithSeedAndFunc = ReflectionInfo.Method((IEnumerable<object> source, object seed, Func<object, object, object> func) => source.Aggregate(seed, func));
            __aggregateWithSeedFuncAndResultSelector = ReflectionInfo.Method((IEnumerable<object> source, object seed, Func<object, object, object> func, Func<object, object> resultSelector) => source.Aggregate(seed, func, resultSelector));
            __all = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.All(predicate));
            __allWithPredicate = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.All(predicate));
            __any = ReflectionInfo.Method((IEnumerable<object> source) => source.Any());
            __anyWithPredicate = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.Any(predicate));
            __append = ReflectionInfo.Method((IEnumerable<object> source, object element) => source.Append(element));
            __averageDecimal = ReflectionInfo.Method((IEnumerable<decimal> source) => source.Average());
            __averageDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal> selector) => source.Average(selector));
            __averageDouble = ReflectionInfo.Method((IEnumerable<double> source) => source.Average());
            __averageDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double> selector) => source.Average(selector));
            __averageInt32 = ReflectionInfo.Method((IEnumerable<int> source) => source.Average());
            __averageInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int> selector) => source.Average(selector));
            __averageInt64 = ReflectionInfo.Method((IEnumerable<long> source) => source.Average());
            __averageInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long> selector) => source.Average(selector));
            __averageNullableDecimal = ReflectionInfo.Method((IEnumerable<decimal?> source) => source.Average());
            __averageNullableDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal?> selector) => source.Average(selector));
            __averageNullableDouble = ReflectionInfo.Method((IEnumerable<double?> source) => source.Average());
            __averageNullableDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double?> selector) => source.Average(selector));
            __averageNullableInt32 = ReflectionInfo.Method((IEnumerable<int?> source) => source.Average());
            __averageNullableInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int?> selector) => source.Average(selector));
            __averageNullableInt64 = ReflectionInfo.Method((IEnumerable<long?> source) => source.Average());
            __averageNullableInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long?> selector) => source.Average(selector));
            __averageNullableSingle = ReflectionInfo.Method((IEnumerable<float?> source) => source.Average());
            __averageNullableSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float?> selector) => source.Average(selector));
            __averageSingle = ReflectionInfo.Method((IEnumerable<float> source) => source.Average());
            __averageSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float> selector) => source.Average(selector));
            __bottom = ReflectionInfo.Method((IEnumerable<object> source, SortDefinition<object> sortBy, Func<object, object> selector) => source.Bottom(sortBy, selector));
            __bottomN = ReflectionInfo.Method((IEnumerable<object> source, SortDefinition<object> sortBy, Func<object, object> selector, int n) => source.BottomN(sortBy, selector, n));
            __bottomNWithComputedN = ReflectionInfo.Method((IEnumerable<object> source, SortDefinition<object> sortBy, Func<object, object> selector, object key, Func<object, int> n) => source.BottomN(sortBy, selector, key, n));
            __cast = ReflectionInfo.Method((IEnumerable source) => source.Cast<object>());
            __concat = ReflectionInfo.Method((IEnumerable<object> first, IEnumerable<object> second) => first.Concat(second));
            __contains = ReflectionInfo.Method((IEnumerable<object> source, object value) => source.Contains(value));
            __containsWithComparer = ReflectionInfo.Method((IEnumerable<object> source, object value, IEqualityComparer<object> comparer) => source.Contains(value, comparer));
            __count = ReflectionInfo.Method((IEnumerable<object> source) => source.Count());
            __countWithPredicate = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.Count(predicate));
            __defaultIfEmpty = ReflectionInfo.Method((IEnumerable<object> source) => source.DefaultIfEmpty());
            __defaultIfEmptyWithDefaultValue = ReflectionInfo.Method((IEnumerable<object> source, object defaultValue) => source.DefaultIfEmpty(defaultValue));
            __distinct = ReflectionInfo.Method((IEnumerable<object> source) => source.Distinct());
            __elementAt = ReflectionInfo.Method((IEnumerable<object> source, int index) => source.ElementAt(index));
            __elementAtOrDefault = ReflectionInfo.Method((IEnumerable<object> source, int index) => source.ElementAtOrDefault(index));
            __except = ReflectionInfo.Method((IEnumerable<object> first, IEnumerable<object> second) => first.Except(second));
            __first = ReflectionInfo.Method((IEnumerable<object> source) => source.First());
            __firstN = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector, int n) => source.FirstN(selector, n));
            __firstNWithComputedN = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector, object key, Func<object, int> n) => source.FirstN(selector, key, n));
            __firstOrDefault = ReflectionInfo.Method((IEnumerable<object> source) => source.FirstOrDefault());
            __firstOrDefaultWithPredicate = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.FirstOrDefault(predicate));
            __firstWithPredicate = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.First(predicate));
            __groupByWithKeySelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> keySelector) => source.GroupBy(keySelector));
            __groupByWithKeySelectorAndElementSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> keySelector, Func<object, object> elementSelector) => source.GroupBy(keySelector, elementSelector));
            __groupByWithKeySelectorAndResultSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> keySelector, Func<object, object, object> resultSelector) => source.GroupBy(keySelector, resultSelector));
            __groupByWithKeySelectorElementSelectorAndResultSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> keySelector, Func<object, object> elementSelector, Func<object, IEnumerable<object>, object> resultSelector) => source.GroupBy(keySelector, elementSelector, resultSelector));
            __groupJoin = ReflectionInfo.Method((IEnumerable<object> outer, IEnumerable<object> inner, Func<object, object> outerKeySelector, Func<object, object> innerKeySelector, Func<object, IEnumerable<object>, object> resultSelector) => outer.GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector));
            __intersect = ReflectionInfo.Method((IEnumerable<object> first, IEnumerable<object> second) => first.Intersect(second));
            __join = ReflectionInfo.Method((IEnumerable<object> outer, IEnumerable<object> inner, Func<object, object> outerKeySelector, Func<object, object> innerKeySelector, Func<object, object, object> resultSelector) => outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector));
            __last = ReflectionInfo.Method((IEnumerable<object> source) => source.Last());
            __lastN = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector, int n) => source.LastN(selector, n));
            __lastNWithComputedN = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector, object key, Func<object, int> n) => source.LastN(selector, key, n));
            __lastOrDefault = ReflectionInfo.Method((IEnumerable<object> source) => source.LastOrDefault());
            __lastOrDefaultWithPredicate = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.LastOrDefault(predicate));
            __lastWithPredicate = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.Last(predicate));
            __longCount = ReflectionInfo.Method((IEnumerable<object> source) => source.LongCount());
            __longCountWithPredicate = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.LongCount(predicate));
            __max = ReflectionInfo.Method((IEnumerable<object> source) => source.Max());
            __maxDecimal = ReflectionInfo.Method((IEnumerable<decimal> source) => source.Max());
            __maxDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal> selector) => source.Max(selector));
            __maxDouble = ReflectionInfo.Method((IEnumerable<double> source) => source.Max());
            __maxDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double> selector) => source.Max(selector));
            __maxInt32 = ReflectionInfo.Method((IEnumerable<int> source) => source.Max());
            __maxInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int> selector) => source.Max(selector));
            __maxInt64 = ReflectionInfo.Method((IEnumerable<long> source) => source.Max());
            __maxInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long> selector) => source.Max(selector));
            __maxN = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector, int n) => source.MaxN(selector, n));
            __maxNullableDecimal = ReflectionInfo.Method((IEnumerable<decimal?> source) => source.Max());
            __maxNullableDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal?> selector) => source.Max(selector));
            __maxNullableDouble = ReflectionInfo.Method((IEnumerable<double?> source) => source.Max());
            __maxNullableDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double?> selector) => source.Max(selector));
            __maxNullableInt32 = ReflectionInfo.Method((IEnumerable<int?> source) => source.Max());
            __maxNullableInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int?> selector) => source.Max(selector));
            __maxNullableInt64 = ReflectionInfo.Method((IEnumerable<long?> source) => source.Max());
            __maxNullableInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long?> selector) => source.Max(selector));
            __maxNullableSingle = ReflectionInfo.Method((IEnumerable<float?> source) => source.Max());
            __maxNullableSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float?> selector) => source.Max(selector));
            __maxNWithComputedN = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector, object key, Func<object, int> n) => source.MaxN(selector, key, n));
            __maxSingle = ReflectionInfo.Method((IEnumerable<float> source) => source.Max());
            __maxSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float> selector) => source.Max(selector));
            __maxWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector) => source.Max(selector));
            __min = ReflectionInfo.Method((IEnumerable<object> source) => source.Min());
            __minDecimal = ReflectionInfo.Method((IEnumerable<decimal> source) => source.Min());
            __minDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal> selector) => source.Min(selector));
            __minDouble = ReflectionInfo.Method((IEnumerable<double> source) => source.Min());
            __minDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double> selector) => source.Min(selector));
            __minN = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector, int n) => source.MinN(selector, n));
            __minInt32 = ReflectionInfo.Method((IEnumerable<int> source) => source.Min());
            __minInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int> selector) => source.Min(selector));
            __minInt64 = ReflectionInfo.Method((IEnumerable<long> source) => source.Min());
            __minInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long> selector) => source.Min(selector));
            __minNullableDecimal = ReflectionInfo.Method((IEnumerable<decimal?> source) => source.Min());
            __minNullableDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal?> selector) => source.Min(selector));
            __minNullableDouble = ReflectionInfo.Method((IEnumerable<double?> source) => source.Min());
            __minNullableDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double?> selector) => source.Min(selector));
            __minNullableInt32 = ReflectionInfo.Method((IEnumerable<int?> source) => source.Min());
            __minNullableInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int?> selector) => source.Min(selector));
            __minNullableInt64 = ReflectionInfo.Method((IEnumerable<long?> source) => source.Min());
            __minNullableInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long?> selector) => source.Min(selector));
            __minNullableSingle = ReflectionInfo.Method((IEnumerable<float?> source) => source.Min());
            __minNullableSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float?> selector) => source.Min(selector));
            __minNWithComputedN = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector, object key, Func<object, int> n) => source.MinN(selector, key, n));
            __minSingle = ReflectionInfo.Method((IEnumerable<float> source) => source.Min());
            __minSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float> selector) => source.Min(selector));
            __minWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector) => source.Min(selector));
            __ofType = ReflectionInfo.Method((IEnumerable source) => source.OfType<object>());
            __orderBy = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> keySelector) => source.OrderBy(keySelector));
            __orderByDescending = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> keySelector) => source.OrderByDescending(keySelector));
            __prepend = ReflectionInfo.Method((IEnumerable<object> source, object element) => source.Prepend(element));
            __range = ReflectionInfo.Method((int start, int count) => Enumerable.Range(start, count));
            __repeat = ReflectionInfo.Method((object element, int count) => Enumerable.Repeat(element, count));
            __reverse = ReflectionInfo.Method((IEnumerable<object> source) => source.Reverse());
            __reverseWithArray = GetReverseWithArrayMethodInfo(); // support users running net10 even though we don't target net10 yet
            __select = ReflectionInfo.Method((IEnumerable<object> source, Func<object, object> selector) => source.Select(selector));
            __selectManyWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, IEnumerable<object>> selector) => source.SelectMany(selector));
            __selectManyWithCollectionSelectorAndResultSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, IEnumerable<object>> collectionSelector, Func<object, object, object> resultSelector) => source.SelectMany(collectionSelector, resultSelector));
            __selectManyWithCollectionSelectorTakingIndexAndResultSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int, IEnumerable<object>> collectionSelector, Func<object, object, object> resultSelector) => source.SelectMany(collectionSelector, resultSelector));
            __selectManyWithSelectorTakingIndex = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int, IEnumerable<object>> selector) => source.SelectMany(selector));
            __selectWithSelectorTakingIndex = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int, object> selector) => source.Select(selector));
            __sequenceEqual = ReflectionInfo.Method((IEnumerable<object> first, IEnumerable<object> second) => first.SequenceEqual(second));
            __sequenceEqualWithComparer = ReflectionInfo.Method((IEnumerable<object> first, IEnumerable<object> second, IEqualityComparer<object> comparer) => first.SequenceEqual(second, comparer));
            __single = ReflectionInfo.Method((IEnumerable<object> source) => source.Single());
            __singleOrDefault = ReflectionInfo.Method((IEnumerable<object> source) => source.SingleOrDefault());
            __singleOrDefaultWithPredicate = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.SingleOrDefault(predicate));
            __singleWithPredicate = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.Single(predicate));
            __skip = ReflectionInfo.Method((IEnumerable<object> source, int count) => source.Skip(count));
            __skipWhile = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.SkipWhile(predicate));
            __sumDecimal = ReflectionInfo.Method((IEnumerable<decimal> source) => source.Sum());
            __sumDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal> selector) => source.Sum(selector));
            __sumDouble = ReflectionInfo.Method((IEnumerable<double> source) => source.Sum());
            __sumDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double> selector) => source.Sum(selector));
            __sumInt32 = ReflectionInfo.Method((IEnumerable<int> source) => source.Sum());
            __sumInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int> selector) => source.Sum(selector));
            __sumInt64 = ReflectionInfo.Method((IEnumerable<long> source) => source.Sum());
            __sumInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long> selector) => source.Sum(selector));
            __sumNullableDecimal = ReflectionInfo.Method((IEnumerable<decimal?> source) => source.Sum());
            __sumNullableDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal?> selector) => source.Sum(selector));
            __sumNullableDouble = ReflectionInfo.Method((IEnumerable<double?> source) => source.Sum());
            __sumNullableDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double?> selector) => source.Sum(selector));
            __sumNullableInt32 = ReflectionInfo.Method((IEnumerable<int?> source) => source.Sum());
            __sumNullableInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int?> selector) => source.Sum(selector));
            __sumNullableInt64 = ReflectionInfo.Method((IEnumerable<long?> source) => source.Sum());
            __sumNullableInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long?> selector) => source.Sum(selector));
            __sumNullableSingle = ReflectionInfo.Method((IEnumerable<float?> source) => source.Sum());
            __sumNullableSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float?> selector) => source.Sum(selector));
            __sumSingle = ReflectionInfo.Method((IEnumerable<float> source) => source.Sum());
            __sumSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float> selector) => source.Sum(selector));
            __take = ReflectionInfo.Method((IEnumerable<object> source, int count) => source.Take(count));
            __takeWhile = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.TakeWhile(predicate));
            __thenBy = ReflectionInfo.Method((IOrderedEnumerable<object> source, Func<object, object> keySelector) => source.ThenBy(keySelector));
            __thenByDescending = ReflectionInfo.Method((IOrderedEnumerable<object> source, Func<object, object> keySelector) => source.ThenByDescending(keySelector));
            __toArray = ReflectionInfo.Method((IEnumerable<object> source) => source.ToArray());
            __toList = ReflectionInfo.Method((IEnumerable<object> source) => source.ToList());
            __top = ReflectionInfo.Method((IEnumerable<object> source, SortDefinition<object> sortBy, Func<object, object> selector) => source.Top(sortBy, selector));
            __topN = ReflectionInfo.Method((IEnumerable<object> source, SortDefinition<object> sortBy, Func<object, object> selector, int n) => source.TopN(sortBy, selector, n));
            __topNWithComputedN = ReflectionInfo.Method((IEnumerable<object> source, SortDefinition<object> sortBy, Func<object, object> selector, object key, Func<object, int> n) => source.TopN(sortBy, selector, key, n));
            __union = ReflectionInfo.Method((IEnumerable<object> first, IEnumerable<object> second) => first.Union(second));
            __where = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate) => source.Where(predicate));
            __whereWithPredicateTakingIndex = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int, bool> predicate) => source.Where(predicate));
            __zip = ReflectionInfo.Method((IEnumerable<object> first, IEnumerable<object> second, Func<object, object, object> resultSelector) => first.Zip(second, resultSelector));

            // initialize sets of methods after methods
            __pickOverloads = MethodInfoSet.Create(
            [
                __bottom,
                __bottomN,
                __bottomNWithComputedN,
                __firstN,
                __firstNWithComputedN,
                __lastN,
                __lastNWithComputedN,
                __maxN,
                __maxNWithComputedN,
                __minN,
                __minNWithComputedN,
                __top,
                __topN,
                __topNWithComputedN
            ]);

            __pickOverloadsThatCanOnlyBeUsedAsGroupByAccumulators = MethodInfoSet.Create(
            [
                __bottom,
                __bottomN,
                __bottomNWithComputedN,
                __firstNWithComputedN,
                __lastNWithComputedN,
                __maxNWithComputedN,
                __minNWithComputedN,
                __top,
                __topN,
                __topNWithComputedN
            ]);

            __pickWithComputedNOverloads = MethodInfoSet.Create(
            [
                __bottomNWithComputedN,
                __firstNWithComputedN,
                __lastNWithComputedN,
                __maxNWithComputedN,
                __minNWithComputedN,
                __topNWithComputedN
            ]);

            __pickWithNOverloads = MethodInfoSet.Create(
            [
                __bottomN,
                __firstN,
                __lastN,
                __maxN,
                __minN,
                __topN
            ]);

            __pickWithSortByOverloads = MethodInfoSet.Create(
            [
                __bottom,
                __bottomN,
                __bottomNWithComputedN,
                __top,
                __topN,
                __topNWithComputedN
            ]);

            __reverseOverloads = MethodInfoSet.Create(
            [
                __reverse,
                __reverseWithArray
            ]);
        }

        // public properties
        public static MethodInfo AggregateWithFunc => __aggregateWithFunc;
        public static MethodInfo AggregateWithSeedAndFunc => __aggregateWithSeedAndFunc;
        public static MethodInfo AggregateWithSeedFuncAndResultSelector => __aggregateWithSeedFuncAndResultSelector;
        public static MethodInfo All => __all;
        public static MethodInfo AllWithPredicate => __allWithPredicate;
        public static MethodInfo Any => __any;
        public static MethodInfo AnyWithPredicate => __anyWithPredicate;
        public static MethodInfo Append => __append;
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
        public static MethodInfo Bottom => __bottom;
        public static MethodInfo BottomN => __bottomN;
        public static MethodInfo BottomNWithComputedN  => __bottomNWithComputedN;
        public static MethodInfo Cast => __cast;
        public static MethodInfo Concat => __concat;
        public static MethodInfo Contains => __contains;
        public static MethodInfo ContainsWithComparer => __containsWithComparer;
        public static MethodInfo Count => __count;
        public static MethodInfo CountWithPredicate => __countWithPredicate;
        public static MethodInfo DefaultIfEmpty => __defaultIfEmpty;
        public static MethodInfo DefaultIfEmptyWithDefaultValue => __defaultIfEmptyWithDefaultValue;
        public static MethodInfo Distinct => __distinct;
        public static MethodInfo ElementAt => __elementAt;
        public static MethodInfo ElementAtOrDefault => __elementAtOrDefault;
        public static MethodInfo Except => __except;
        public static MethodInfo First => __first;
        public static MethodInfo FirstN => __firstN;
        public static MethodInfo FirstNWithComputedN => __firstNWithComputedN;
        public static MethodInfo FirstOrDefault => __firstOrDefault;
        public static MethodInfo FirstOrDefaultWithPredicate => __firstOrDefaultWithPredicate;
        public static MethodInfo FirstWithPredicate => __firstWithPredicate;
        public static MethodInfo GroupByWithKeySelector => __groupByWithKeySelector;
        public static MethodInfo GroupByWithKeySelectorAndElementSelector => __groupByWithKeySelectorAndElementSelector;
        public static MethodInfo GroupByWithKeySelectorAndResultSelector => __groupByWithKeySelectorAndResultSelector;
        public static MethodInfo GroupByWithKeySelectorElementSelectorAndResultSelector => __groupByWithKeySelectorElementSelectorAndResultSelector;
        public static MethodInfo GroupJoin => __groupJoin;
        public static MethodInfo Intersect => __intersect;
        public static MethodInfo Join => __join;
        public static MethodInfo Last => __last;
        public static MethodInfo LastN => __lastN;
        public static MethodInfo LastNWithComputedN => __lastNWithComputedN;
        public static MethodInfo LastOrDefault => __lastOrDefault;
        public static MethodInfo LastOrDefaultWithPredicate => __lastOrDefaultWithPredicate;
        public static MethodInfo LastWithPredicate => __lastWithPredicate;
        public static MethodInfo LongCount => __longCount;
        public static MethodInfo LongCountWithPredicate => __longCountWithPredicate;
        public static MethodInfo Max => __max;
        public static MethodInfo MaxDecimal => __maxDecimal;
        public static MethodInfo MaxDecimalWithSelector => __maxDecimalWithSelector;
        public static MethodInfo MaxDouble => __maxDouble;
        public static MethodInfo MaxDoubleWithSelector => __maxDoubleWithSelector;
        public static MethodInfo MaxInt32 => __maxInt32;
        public static MethodInfo MaxInt32WithSelector => __maxInt32WithSelector;
        public static MethodInfo MaxInt64 => __maxInt64;
        public static MethodInfo MaxInt64WithSelector => __maxInt64WithSelector;
        public static MethodInfo MaxN => __maxN;
        public static MethodInfo MaxNullableDecimal => __maxNullableDecimal;
        public static MethodInfo MaxNullableDecimalWithSelector => __maxNullableDecimalWithSelector;
        public static MethodInfo MaxNullableDouble => __maxNullableDouble;
        public static MethodInfo MaxNullableDoubleWithSelector => __maxNullableDoubleWithSelector;
        public static MethodInfo MaxNullableInt32 => __maxNullableInt32;
        public static MethodInfo MaxNullableInt32WithSelector => __maxNullableInt32WithSelector;
        public static MethodInfo MaxNullableInt64 => __maxNullableInt64;
        public static MethodInfo MaxNullableInt64WithSelector => __maxNullableInt64WithSelector;
        public static MethodInfo MaxNullableSingle => __maxNullableSingle;
        public static MethodInfo MaxNullableSingleWithSelector => __maxNullableSingleWithSelector;
        public static MethodInfo MaxNWithComputedN => __maxNWithComputedN;
        public static MethodInfo MaxSingle => __maxSingle;
        public static MethodInfo MaxSingleWithSelector => __maxSingleWithSelector;
        public static MethodInfo MaxWithSelector => __maxWithSelector;
        public static MethodInfo Min => __min;
        public static MethodInfo MinDecimal => __minDecimal;
        public static MethodInfo MinDecimalWithSelector => __minDecimalWithSelector;
        public static MethodInfo MinDouble => __minDouble;
        public static MethodInfo MinDoubleWithSelector => __minDoubleWithSelector;
        public static MethodInfo MinInt32 => __minInt32;
        public static MethodInfo MinInt32WithSelector => __minInt32WithSelector;
        public static MethodInfo MinInt64 => __minInt64;
        public static MethodInfo MinInt64WithSelector => __minInt64WithSelector;
        public static MethodInfo MinN => __minN;
        public static MethodInfo MinNullableDecimal => __minNullableDecimal;
        public static MethodInfo MinNullableDecimalWithSelector => __minNullableDecimalWithSelector;
        public static MethodInfo MinNullableDouble => __minNullableDouble;
        public static MethodInfo MinNullableDoubleWithSelector => __minNullableDoubleWithSelector;
        public static MethodInfo MinNullableInt32 => __minNullableInt32;
        public static MethodInfo MinNullableInt32WithSelector => __minNullableInt32WithSelector;
        public static MethodInfo MinNullableInt64 => __minNullableInt64;
        public static MethodInfo MinNullableInt64WithSelector => __minNullableInt64WithSelector;
        public static MethodInfo MinNullableSingle => __minNullableSingle;
        public static MethodInfo MinNullableSingleWithSelector => __minNullableSingleWithSelector;
        public static MethodInfo MinNWithComputedN => __minNWithComputedN;
        public static MethodInfo MinSingle => __minSingle;
        public static MethodInfo MinSingleWithSelector => __minSingleWithSelector;
        public static MethodInfo MinWithSelector => __minWithSelector;
        public static MethodInfo OfType => __ofType;
        public static MethodInfo OrderBy => __orderBy;
        public static MethodInfo OrderByDescending => __orderByDescending;
        public static MethodInfo Prepend => __prepend;
        public static MethodInfo Range => __range;
        public static MethodInfo Repeat => __repeat;
        public static MethodInfo Reverse => __reverse;
        public static MethodInfo ReverseWithArray => __reverseWithArray;
        public static MethodInfo Select => __select;
        public static MethodInfo SelectManyWithSelector => __selectManyWithSelector;
        public static MethodInfo SelectManyWithCollectionSelectorAndResultSelector => __selectManyWithCollectionSelectorAndResultSelector;
        public static MethodInfo SelectManyWithCollectionSelectorTakingIndexAndResultSelector => __selectManyWithCollectionSelectorTakingIndexAndResultSelector;
        public static MethodInfo SelectManyWithSelectorTakingIndex => __selectManyWithSelectorTakingIndex;
        public static MethodInfo SelectWithSelectorTakingIndex => __selectWithSelectorTakingIndex;
        public static MethodInfo SequenceEqual => __sequenceEqual;
        public static MethodInfo SequenceEqualWithComparer => __sequenceEqualWithComparer;
        public static MethodInfo Single => __single;
        public static MethodInfo SingleOrDefault => __singleOrDefault;
        public static MethodInfo SingleOrDefaultWithPredicate => __singleOrDefaultWithPredicate;
        public static MethodInfo SingleWithPredicate => __singleWithPredicate;
        public static MethodInfo Skip => __skip;
        public static MethodInfo SkipWhile => __skipWhile;
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
        public static MethodInfo TakeWhile => __takeWhile;
        public static MethodInfo ThenBy => __thenBy;
        public static MethodInfo ThenByDescending => __thenByDescending;
        public static MethodInfo ToArray => __toArray;
        public static MethodInfo ToList => __toList;
        public static MethodInfo Top => __top;
        public static MethodInfo TopN => __topN;
        public static MethodInfo TopNWithComputedN => __topNWithComputedN;
        public static MethodInfo Union => __union;
        public static MethodInfo Where => __where;
        public static MethodInfo WhereWithPredicateTakingIndex => __whereWithPredicateTakingIndex;
        public static MethodInfo Zip => __zip;

        // sets of methods
        public static IReadOnlyMethodInfoSet PickOverloads => __pickOverloads;
        public static IReadOnlyMethodInfoSet PickOverloadsThatCanOnlyBeUsedAsGroupByAccumulators => __pickOverloadsThatCanOnlyBeUsedAsGroupByAccumulators;
        public static IReadOnlyMethodInfoSet PickWithComputedNOverloads => __pickWithComputedNOverloads;
        public static IReadOnlyMethodInfoSet PickWithNOverloads => __pickWithNOverloads;
        public static IReadOnlyMethodInfoSet PickWithSortByOverloads => __pickWithSortByOverloads;
        public static IReadOnlyMethodInfoSet ReverseOverloads => __reverseOverloads;

        // public methods
        public static bool IsContainsMethod(MethodCallExpression methodCallExpression, out Expression sourceExpression, out Expression valueExpression)
        {
            var method = methodCallExpression.Method;
            var parameters = method.GetParameters();
            var arguments = methodCallExpression.Arguments;

            if (method.Name == "Contains" && method.ReturnType == typeof(bool))
            {
                if (method.IsStatic)
                {
                    if (parameters.Length == 2)
                    {
                        if (parameters[0].ParameterType.ImplementsIEnumerableOf(parameters[1].ParameterType))
                        {
                            sourceExpression = arguments[0];
                            valueExpression = arguments[1];
                            return true;
                        }
                    }
                }
                else
                {
                    if (parameters.Length == 1)
                    {
                        if (method.DeclaringType.ImplementsIEnumerableOf(parameters[0].ParameterType))
                        {
                            sourceExpression = methodCallExpression.Object;
                            valueExpression = arguments[0];
                            return true;
                        }
                    }
                }
            }

            sourceExpression = null;
            valueExpression = null;
            return false;
        }

        public static bool IsToArrayMethod(MethodCallExpression methodCallExpression, out Expression sourceExpression)
        {
            var method = methodCallExpression.Method;
            var parameters = method.GetParameters();
            var arguments = methodCallExpression.Arguments;

            if (method.Name == "ToArray")
            {
                var returnType = method.ReturnType;
                if (returnType.IsArray)
                {
                    var returnItemType = returnType.GetElementType();

                    sourceExpression = method switch
                    {
                        _ when method.IsStatic && parameters.Length == 1 => arguments[0],
                        _ when !method.IsStatic && parameters.Length == 0 => methodCallExpression.Object,
                        _ => null
                    };
                    if (sourceExpression != null)
                    {
                        var sourceType = sourceExpression.Type;
                        if (sourceType.ImplementsIEnumerable(out var sourceItemType) &&
                            sourceItemType == returnItemType)
                        {
                            return true;
                        }
                    }
                }
            }

            sourceExpression = null;
            return false;
        }

        public static MethodInfo MakeSelect(Type sourceType, Type resultType)
        {
            return __select.MakeGenericMethod(sourceType, resultType);
        }

        public static MethodInfo MakeWhere(Type tsource)
        {
            return __where.MakeGenericMethod(tsource);
        }

#if !NET10_OR_GREATER
        private static MethodInfo GetReverseWithArrayMethodInfo()
        {
            // returns null on target frameworks that don't have this method
            return
                typeof(Enumerable)
                .GetMethods()
                .SingleOrDefault(m =>
                    m.IsPublic &&
                    m.IsStatic &&
                    m.Name == "Reverse" &&
                    m.IsGenericMethodDefinition &&
                    m.GetGenericArguments() is var genericArguments &&
                    genericArguments.Length == 1 &&
                    genericArguments[0] is var tsource &&
                    m.ReturnType == typeof(IEnumerable<>).MakeGenericType(tsource) &&
                    m.GetParameters() is var parameters &&
                    parameters.Length == 1 &&
                    parameters[0] is var sourceParameter &&
                    sourceParameter.ParameterType == tsource.MakeArrayType());
        }
#endif
    }
}

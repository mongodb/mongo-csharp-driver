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

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection;

internal static class EnumerableOrQueryableMethod
{
    // methods (in this file matching Enumerable and Queryable methods are treated as if they were one method)
    private static readonly IReadOnlyMethodInfoSet __aggregateWithFunc;
    private static readonly IReadOnlyMethodInfoSet __aggregateWithSeedAndFunc;
    private static readonly IReadOnlyMethodInfoSet __aggregateWithSeedFuncAndResultSelector;
    private static readonly IReadOnlyMethodInfoSet __all;
    private static readonly IReadOnlyMethodInfoSet __any;
    private static readonly IReadOnlyMethodInfoSet __anyWithPredicate;
    private static readonly IReadOnlyMethodInfoSet __append;
    private static readonly IReadOnlyMethodInfoSet __concat;
    private static readonly IReadOnlyMethodInfoSet __count;
    private static readonly IReadOnlyMethodInfoSet __countWithPredicate;
    private static readonly IReadOnlyMethodInfoSet __defaultIfEmpty;
    private static readonly IReadOnlyMethodInfoSet __defaultIfEmptyWithDefaultValue;
    private static readonly IReadOnlyMethodInfoSet __distinct;
    private static readonly IReadOnlyMethodInfoSet __elementAt;
    private static readonly IReadOnlyMethodInfoSet __elementAtOrDefault;
    private static readonly IReadOnlyMethodInfoSet __except;
    private static readonly IReadOnlyMethodInfoSet __first;
    private static readonly IReadOnlyMethodInfoSet __firstOrDefault;
    private static readonly IReadOnlyMethodInfoSet __firstWithPredicate;
    private static readonly IReadOnlyMethodInfoSet __firstOrDefaultWithPredicate;
    private static readonly IReadOnlyMethodInfoSet __groupByWithKeySelector;
    private static readonly IReadOnlyMethodInfoSet __groupByWithKeySelectorAndElementSelector;
    private static readonly IReadOnlyMethodInfoSet __groupByWithKeySelectorAndResultSelector;
    private static readonly IReadOnlyMethodInfoSet __groupByWithKeySelectorElementSelectorAndResultSelector;
    private static readonly IReadOnlyMethodInfoSet __intersect;
    private static readonly IReadOnlyMethodInfoSet __last;
    private static readonly IReadOnlyMethodInfoSet __lastOrDefault;
    private static readonly IReadOnlyMethodInfoSet __lastWithPredicate;
    private static readonly IReadOnlyMethodInfoSet __lastOrDefaultWithPredicate;
    private static readonly IReadOnlyMethodInfoSet __longCount;
    private static readonly IReadOnlyMethodInfoSet __longCountWithPredicate;
    private static readonly IReadOnlyMethodInfoSet __ofType;
    private static readonly IReadOnlyMethodInfoSet __orderBy;
    private static readonly IReadOnlyMethodInfoSet __orderByDescending;
    private static readonly IReadOnlyMethodInfoSet __prepend;
    private static readonly IReadOnlyMethodInfoSet __reverse;
    private static readonly IReadOnlyMethodInfoSet __select;
    private static readonly IReadOnlyMethodInfoSet __selectManyWithCollectionSelectorAndResultSelector;
    private static readonly IReadOnlyMethodInfoSet __selectManyWithSelector;
    private static readonly IReadOnlyMethodInfoSet __single;
    private static readonly IReadOnlyMethodInfoSet __singleOrDefault;
    private static readonly IReadOnlyMethodInfoSet __singleWithPredicate;
    private static readonly IReadOnlyMethodInfoSet __singleOrDefaultWithPredicate;
    private static readonly IReadOnlyMethodInfoSet __skip;
    private static readonly IReadOnlyMethodInfoSet __skipWhile;
    private static readonly IReadOnlyMethodInfoSet __take;
    private static readonly IReadOnlyMethodInfoSet __takeWhile;
    private static readonly IReadOnlyMethodInfoSet __thenBy;
    private static readonly IReadOnlyMethodInfoSet __thenByDescending;
    private static readonly IReadOnlyMethodInfoSet __union;
    private static readonly IReadOnlyMethodInfoSet __where;
    private static readonly IReadOnlyMethodInfoSet __zip;

    // sets of methods
    private static readonly IReadOnlyMethodInfoSet __aggregateOverloads;
    private static readonly IReadOnlyMethodInfoSet __aggregateWithSeedOverloads;
    private static readonly IReadOnlyMethodInfoSet __anyOverloads;
    private static readonly IReadOnlyMethodInfoSet __appendOrPrepend;
    private static readonly IReadOnlyMethodInfoSet __averageOverloads;
    private static readonly IReadOnlyMethodInfoSet __averageWithSelectorOverloads;
    private static readonly IReadOnlyMethodInfoSet __countOverloads;
    private static readonly IReadOnlyMethodInfoSet __countWithPredicateOverloads;
    private static readonly IReadOnlyMethodInfoSet __defaultIfEmptyOverloads;
    private static readonly IReadOnlyMethodInfoSet __elementAtOverloads;
    private static readonly IReadOnlyMethodInfoSet __firstOrLastWithPredicateOverloads;
    private static readonly IReadOnlyMethodInfoSet __firstOrLastOrSingleOverloads;
    private static readonly IReadOnlyMethodInfoSet __firstOrLastOrSingleWithPredicateOverloads;
    private static readonly IReadOnlyMethodInfoSet __firstOverloads;
    private static readonly IReadOnlyMethodInfoSet __firstOrDefaultOverloads;
    private static readonly IReadOnlyMethodInfoSet __firstOrLastOverloads;
    private static readonly IReadOnlyMethodInfoSet __firstWithPredicateOverloads;
    private static readonly IReadOnlyMethodInfoSet __groupByOverloads;
    private static readonly IReadOnlyMethodInfoSet __lastOverloads;
    private static readonly IReadOnlyMethodInfoSet __lastOrDefaultOverloads;
    private static readonly IReadOnlyMethodInfoSet __lastWithPredicateOverloads;
    private static readonly IReadOnlyMethodInfoSet __maxOrMinOverloads;
    private static readonly IReadOnlyMethodInfoSet __maxOrMinWithSelectorOverloads;
    private static readonly IReadOnlyMethodInfoSet __maxOverloads;
    private static readonly IReadOnlyMethodInfoSet __maxWithSelectorOverloads;
    private static readonly IReadOnlyMethodInfoSet __minOverloads;
    private static readonly IReadOnlyMethodInfoSet __minWithSelectorOverloads;
    private static readonly IReadOnlyMethodInfoSet __orderByOrThenByOverloads;
    private static readonly IReadOnlyMethodInfoSet __orderByOverloads;
    private static readonly IReadOnlyMethodInfoSet __reverseOverloads;
    private static readonly IReadOnlyMethodInfoSet __selectManyOverloads;
    private static readonly IReadOnlyMethodInfoSet __singleOverloads;
    private static readonly IReadOnlyMethodInfoSet __singleWithPredicateOverloads;
    private static readonly IReadOnlyMethodInfoSet __skipOrTakeOverloads;
    private static readonly IReadOnlyMethodInfoSet __skipOverloads;
    private static readonly IReadOnlyMethodInfoSet __skipWhileOrTakeWhile;
    private static readonly IReadOnlyMethodInfoSet __sumOverloads;
    private static readonly IReadOnlyMethodInfoSet __sumWithSelectorOverloads;
    private static readonly IReadOnlyMethodInfoSet __takeOverloads;
    private static readonly IReadOnlyMethodInfoSet __thenByOverloads;

    static EnumerableOrQueryableMethod()
    {
        // initialize methods before sets of methods (in this file matching Enumerable and Queryable methods are treated as if they were one method)
        __aggregateWithFunc = MethodInfoSet.Create(
        [
            EnumerableMethod.AggregateWithFunc,
            QueryableMethod.AggregateWithFunc
        ]);

        __aggregateWithSeedAndFunc = MethodInfoSet.Create(
        [
            EnumerableMethod.AggregateWithSeedAndFunc,
            QueryableMethod.AggregateWithSeedAndFunc
        ]);

        __aggregateWithSeedFuncAndResultSelector = MethodInfoSet.Create(
        [
            EnumerableMethod.AggregateWithSeedFuncAndResultSelector,
            QueryableMethod.AggregateWithSeedFuncAndResultSelector
        ]);

        __all = MethodInfoSet.Create(
        [
            EnumerableMethod.All,
            QueryableMethod.All
        ]);

        __any = MethodInfoSet.Create(
        [
            EnumerableMethod.Any,
            QueryableMethod.Any,
        ]);

        __anyWithPredicate = MethodInfoSet.Create(
        [
            EnumerableMethod.AnyWithPredicate,
            QueryableMethod.AnyWithPredicate
        ]);

        __append = MethodInfoSet.Create(
        [
            EnumerableMethod.Append,
            QueryableMethod.Append
        ]);

        __concat = MethodInfoSet.Create(
        [
            EnumerableMethod.Concat,
            QueryableMethod.Concat
        ]);

        __count = MethodInfoSet.Create(
        [
            EnumerableMethod.Count,
            QueryableMethod.Count
        ]);

        __countWithPredicate = MethodInfoSet.Create(
        [
            EnumerableMethod.CountWithPredicate,
            QueryableMethod.CountWithPredicate
        ]);

        __defaultIfEmpty = MethodInfoSet.Create(
        [
            EnumerableMethod.DefaultIfEmpty,
            QueryableMethod.DefaultIfEmpty
        ]);

        __defaultIfEmptyWithDefaultValue = MethodInfoSet.Create(
        [
            EnumerableMethod.DefaultIfEmptyWithDefaultValue,
            QueryableMethod.DefaultIfEmptyWithDefaultValue,
        ]);

        __distinct = MethodInfoSet.Create(
        [
            EnumerableMethod.Distinct,
            QueryableMethod.Distinct
        ]);

        __elementAt = MethodInfoSet.Create(
        [
            EnumerableMethod.ElementAt,
            QueryableMethod.ElementAt
        ]);

        __elementAtOrDefault = MethodInfoSet.Create(
        [
            EnumerableMethod.ElementAtOrDefault,
            QueryableMethod.ElementAtOrDefault
        ]);

        __except = MethodInfoSet.Create(
        [
            EnumerableMethod.Except,
            QueryableMethod.Except
        ]);

        __first = MethodInfoSet.Create(
        [
            EnumerableMethod.First,
            QueryableMethod.First
        ]);

        __firstOrDefault = MethodInfoSet.Create(
        [
            EnumerableMethod.FirstOrDefault,
            QueryableMethod.FirstOrDefault
        ]);

        __firstOrDefaultWithPredicate = MethodInfoSet.Create(
        [
            EnumerableMethod.FirstOrDefaultWithPredicate,
            QueryableMethod.FirstOrDefaultWithPredicate
        ]);

        __firstWithPredicate = MethodInfoSet.Create(
        [
            EnumerableMethod.FirstWithPredicate,
            QueryableMethod.FirstWithPredicate
        ]);

        __groupByWithKeySelector = MethodInfoSet.Create(
        [
            EnumerableMethod.GroupByWithKeySelector,
            QueryableMethod.GroupByWithKeySelector
        ]);

        __groupByWithKeySelectorAndElementSelector = MethodInfoSet.Create(
        [
            EnumerableMethod.GroupByWithKeySelectorAndElementSelector,
            QueryableMethod.GroupByWithKeySelectorAndElementSelector
        ]);

        __groupByWithKeySelectorAndResultSelector = MethodInfoSet.Create(
        [
            EnumerableMethod.GroupByWithKeySelectorAndResultSelector,
            QueryableMethod.GroupByWithKeySelectorAndResultSelector
        ]);

        __groupByWithKeySelectorElementSelectorAndResultSelector = MethodInfoSet.Create(
        [
            EnumerableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector,
            QueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector
        ]);

        __intersect = MethodInfoSet.Create(
        [
            EnumerableMethod.Intersect,
            QueryableMethod.Intersect
        ]);

        __last = MethodInfoSet.Create(
        [
            EnumerableMethod.Last,
            QueryableMethod.Last
        ]);

        __lastOrDefault = MethodInfoSet.Create(
        [
            EnumerableMethod.LastOrDefault,
            QueryableMethod.LastOrDefault
        ]);

        __lastOrDefaultWithPredicate = MethodInfoSet.Create(
        [
            EnumerableMethod.LastOrDefaultWithPredicate,
            QueryableMethod.LastOrDefaultWithPredicate
        ]);

        __lastWithPredicate = MethodInfoSet.Create(
        [
            EnumerableMethod.LastWithPredicate,
            QueryableMethod.LastWithPredicate
        ]);

        __longCount = MethodInfoSet.Create(
        [
            EnumerableMethod.LongCount,
            QueryableMethod.LongCount
        ]);

        __longCountWithPredicate = MethodInfoSet.Create(
        [
            EnumerableMethod.LongCountWithPredicate,
            QueryableMethod.LongCountWithPredicate
        ]);

        __ofType = MethodInfoSet.Create(
        [
            EnumerableMethod.OfType,
            QueryableMethod.OfType
        ]);

        __orderBy = MethodInfoSet.Create(
        [
            EnumerableMethod.OrderBy,
            QueryableMethod.OrderBy
        ]);

        __orderByDescending = MethodInfoSet.Create(
        [
            EnumerableMethod.OrderByDescending,
            QueryableMethod.OrderByDescending
        ]);

        __prepend = MethodInfoSet.Create(
        [
            EnumerableMethod.Prepend,
            QueryableMethod.Prepend
        ]);

        __reverse = MethodInfoSet.Create(
        [
            EnumerableMethod.Reverse,
            QueryableMethod.Reverse
        ]);

        __select = MethodInfoSet.Create(
        [
            EnumerableMethod.Select,
            QueryableMethod.Select
        ]);

        __selectManyWithCollectionSelectorAndResultSelector = MethodInfoSet.Create(
        [
            EnumerableMethod.SelectManyWithCollectionSelectorAndResultSelector,
            QueryableMethod.SelectManyWithCollectionSelectorAndResultSelector
        ]);

        __selectManyWithSelector = MethodInfoSet.Create(
        [
            EnumerableMethod.SelectManyWithSelector,
            QueryableMethod.SelectManyWithSelector
        ]);

        __single = MethodInfoSet.Create(
        [
            EnumerableMethod.Single,
            QueryableMethod.Single
        ]);

        __singleOrDefault = MethodInfoSet.Create(
        [
            EnumerableMethod.SingleOrDefault,
            QueryableMethod.SingleOrDefault
        ]);

        __singleOrDefaultWithPredicate = MethodInfoSet.Create(
        [
            EnumerableMethod.SingleOrDefaultWithPredicate,
            QueryableMethod.SingleOrDefaultWithPredicate
        ]);

        __singleWithPredicate = MethodInfoSet.Create(
        [
            EnumerableMethod.SingleWithPredicate,
            QueryableMethod.SingleWithPredicate
        ]);

        __skip = MethodInfoSet.Create(
        [
            EnumerableMethod.Skip,
            QueryableMethod.Skip
        ]);

        __skipWhile = MethodInfoSet.Create(
        [
            EnumerableMethod.SkipWhile,
            QueryableMethod.SkipWhile
        ]);

        __take = MethodInfoSet.Create(
        [
            EnumerableMethod.Take,
            QueryableMethod.Take
        ]);

        __takeWhile = MethodInfoSet.Create(
        [
            EnumerableMethod.TakeWhile,
            QueryableMethod.TakeWhile
        ]);

        __thenBy = MethodInfoSet.Create(
        [
            EnumerableMethod.ThenBy,
            QueryableMethod.ThenBy
        ]);

        __thenByDescending = MethodInfoSet.Create(
        [
            EnumerableMethod.ThenByDescending,
            QueryableMethod.ThenByDescending
        ]);

        __union = MethodInfoSet.Create(
        [
            EnumerableMethod.Union,
            QueryableMethod.Union,
        ]);

        __where = MethodInfoSet.Create(
        [
            EnumerableMethod.Where,
            QueryableMethod.Where,
        ]);

        __zip = MethodInfoSet.Create(
        [
            EnumerableMethod.Zip,
            QueryableMethod.Zip
        ]);

        // initialize sets of methods after methods
        __aggregateOverloads = MethodInfoSet.Create(
        [
            __aggregateWithFunc,
            __aggregateWithSeedAndFunc,
            __aggregateWithSeedFuncAndResultSelector
        ]);

        __aggregateWithSeedOverloads = MethodInfoSet.Create(
        [
            __aggregateWithSeedAndFunc,
            __aggregateWithSeedFuncAndResultSelector
        ]);

        __anyOverloads = MethodInfoSet.Create(
        [
            __any,
            __anyWithPredicate
        ]);

        __appendOrPrepend = MethodInfoSet.Create(
        [
            __append,
            __prepend
        ]);

        __averageOverloads = MethodInfoSet.Create(
        [
            EnumerableMethod.AverageDecimal,
            EnumerableMethod.AverageDecimalWithSelector,
            EnumerableMethod.AverageDouble,
            EnumerableMethod.AverageDoubleWithSelector,
            EnumerableMethod.AverageInt32,
            EnumerableMethod.AverageInt32WithSelector,
            EnumerableMethod.AverageInt64,
            EnumerableMethod.AverageInt64WithSelector,
            EnumerableMethod.AverageNullableDecimal,
            EnumerableMethod.AverageNullableDecimalWithSelector,
            EnumerableMethod.AverageNullableDouble,
            EnumerableMethod.AverageNullableDoubleWithSelector,
            EnumerableMethod.AverageNullableInt32,
            EnumerableMethod.AverageNullableInt32WithSelector,
            EnumerableMethod.AverageNullableInt64,
            EnumerableMethod.AverageNullableInt64WithSelector,
            EnumerableMethod.AverageNullableSingle,
            EnumerableMethod.AverageNullableSingleWithSelector,
            EnumerableMethod.AverageSingle,
            EnumerableMethod.AverageSingleWithSelector,
            QueryableMethod.AverageDecimal,
            QueryableMethod.AverageDecimalWithSelector,
            QueryableMethod.AverageDouble,
            QueryableMethod.AverageDoubleWithSelector,
            QueryableMethod.AverageInt32,
            QueryableMethod.AverageInt32WithSelector,
            QueryableMethod.AverageInt64,
            QueryableMethod.AverageInt64WithSelector,
            QueryableMethod.AverageNullableDecimal,
            QueryableMethod.AverageNullableDecimalWithSelector,
            QueryableMethod.AverageNullableDouble,
            QueryableMethod.AverageNullableDoubleWithSelector,
            QueryableMethod.AverageNullableInt32,
            QueryableMethod.AverageNullableInt32WithSelector,
            QueryableMethod.AverageNullableInt64,
            QueryableMethod.AverageNullableInt64WithSelector,
            QueryableMethod.AverageNullableSingle,
            QueryableMethod.AverageNullableSingleWithSelector,
            QueryableMethod.AverageSingle,
            QueryableMethod.AverageSingleWithSelector
        ]);

        __averageWithSelectorOverloads = MethodInfoSet.Create(
        [
            EnumerableMethod.AverageDecimalWithSelector,
            EnumerableMethod.AverageDoubleWithSelector,
            EnumerableMethod.AverageInt32WithSelector,
            EnumerableMethod.AverageInt64WithSelector,
            EnumerableMethod.AverageNullableDecimalWithSelector,
            EnumerableMethod.AverageNullableDoubleWithSelector,
            EnumerableMethod.AverageNullableInt32WithSelector,
            EnumerableMethod.AverageNullableInt64WithSelector,
            EnumerableMethod.AverageNullableSingleWithSelector,
            EnumerableMethod.AverageSingleWithSelector,
            QueryableMethod.AverageDecimalWithSelector,
            QueryableMethod.AverageDoubleWithSelector,
            QueryableMethod.AverageInt32WithSelector,
            QueryableMethod.AverageInt64WithSelector,
            QueryableMethod.AverageNullableDecimalWithSelector,
            QueryableMethod.AverageNullableDoubleWithSelector,
            QueryableMethod.AverageNullableInt32WithSelector,
            QueryableMethod.AverageNullableInt64WithSelector,
            QueryableMethod.AverageNullableSingleWithSelector,
            QueryableMethod.AverageSingleWithSelector,
        ]);

        __countOverloads = MethodInfoSet.Create(
        [
            __count,
            __countWithPredicate,
            __longCount, // it's conventiont to treat LongCount as if it was an overload of Count
            __longCountWithPredicate
        ]);

        __countWithPredicateOverloads = MethodInfoSet.Create(
        [
            __countWithPredicate,
            __longCountWithPredicate // it's conventiont to treat LongCount as if it was an overload of Count
        ]);

        __defaultIfEmptyOverloads = MethodInfoSet.Create(
        [
            __defaultIfEmpty,
            __defaultIfEmptyWithDefaultValue
        ]);

        __elementAtOverloads = MethodInfoSet.Create(
        [
            __elementAt,
            __elementAtOrDefault // it's conventiont to treat ElementAtOrDefault as if it was an overload of ElementAt
        ]);

        __firstOverloads = MethodInfoSet.Create(
        [
            __first,
            __firstOrDefault, // it's convenient to treat FirstOrDefault as if it was an overload
            __firstOrDefaultWithPredicate,
            __firstWithPredicate
        ]);

        __firstOrDefaultOverloads = MethodInfoSet.Create(
        [
            __firstOrDefault,
            __firstOrDefaultWithPredicate
        ]);

        __firstWithPredicateOverloads = MethodInfoSet.Create(
        [
            __firstOrDefaultWithPredicate,
            __firstWithPredicate
        ]);

        __groupByOverloads = MethodInfoSet.Create(
        [
            __groupByWithKeySelector,
            __groupByWithKeySelectorAndElementSelector,
            __groupByWithKeySelectorAndResultSelector,
            __groupByWithKeySelectorElementSelectorAndResultSelector
        ]);

        __lastOverloads = MethodInfoSet.Create(
        [
            __last,
            __lastOrDefault, // it's convenient to treat LastOrDefault as if it was an overload
            __lastOrDefaultWithPredicate,
            __lastWithPredicate
        ]);

        __lastOrDefaultOverloads = MethodInfoSet.Create(
        [
            __lastOrDefault,
            __lastOrDefaultWithPredicate
        ]);

        __lastWithPredicateOverloads = MethodInfoSet.Create(
        [
            __lastOrDefaultWithPredicate,
            __lastWithPredicate
        ]);

        __maxOverloads = MethodInfoSet.Create(
        [
            EnumerableMethod.Max,
            EnumerableMethod.MaxDecimal,
            EnumerableMethod.MaxDecimalWithSelector,
            EnumerableMethod.MaxDouble,
            EnumerableMethod.MaxDoubleWithSelector,
            EnumerableMethod.MaxInt32,
            EnumerableMethod.MaxInt32WithSelector,
            EnumerableMethod.MaxInt64,
            EnumerableMethod.MaxInt64WithSelector,
            EnumerableMethod.MaxNullableDecimal,
            EnumerableMethod.MaxNullableDecimalWithSelector,
            EnumerableMethod.MaxNullableDouble,
            EnumerableMethod.MaxNullableDoubleWithSelector,
            EnumerableMethod.MaxNullableInt32,
            EnumerableMethod.MaxNullableInt32WithSelector,
            EnumerableMethod.MaxNullableInt64,
            EnumerableMethod.MaxNullableInt64WithSelector,
            EnumerableMethod.MaxNullableSingle,
            EnumerableMethod.MaxNullableSingleWithSelector,
            EnumerableMethod.MaxSingle,
            EnumerableMethod.MaxSingleWithSelector,
            EnumerableMethod.MaxWithSelector,
            QueryableMethod.Max,
            QueryableMethod.MaxWithSelector,
        ]);

        __maxWithSelectorOverloads = MethodInfoSet.Create(
        [
            EnumerableMethod.MaxDecimalWithSelector,
            EnumerableMethod.MaxDoubleWithSelector,
            EnumerableMethod.MaxInt32WithSelector,
            EnumerableMethod.MaxInt64WithSelector,
            EnumerableMethod.MaxNullableDecimalWithSelector,
            EnumerableMethod.MaxNullableDoubleWithSelector,
            EnumerableMethod.MaxNullableInt32WithSelector,
            EnumerableMethod.MaxNullableInt64WithSelector,
            EnumerableMethod.MaxNullableSingleWithSelector,
            EnumerableMethod.MaxSingleWithSelector,
            EnumerableMethod.MaxWithSelector,
            QueryableMethod.MaxWithSelector
        ]);

        __minOverloads = MethodInfoSet.Create(
        [
            EnumerableMethod.Min,
            EnumerableMethod.MinDecimal,
            EnumerableMethod.MinDecimalWithSelector,
            EnumerableMethod.MinDouble,
            EnumerableMethod.MinDoubleWithSelector,
            EnumerableMethod.MinInt32,
            EnumerableMethod.MinInt32WithSelector,
            EnumerableMethod.MinInt64,
            EnumerableMethod.MinInt64WithSelector,
            EnumerableMethod.MinNullableDecimal,
            EnumerableMethod.MinNullableDecimalWithSelector,
            EnumerableMethod.MinNullableDouble,
            EnumerableMethod.MinNullableDoubleWithSelector,
            EnumerableMethod.MinNullableInt32,
            EnumerableMethod.MinNullableInt32WithSelector,
            EnumerableMethod.MinNullableInt64,
            EnumerableMethod.MinNullableInt64WithSelector,
            EnumerableMethod.MinNullableSingle,
            EnumerableMethod.MinNullableSingleWithSelector,
            EnumerableMethod.MinSingle,
            EnumerableMethod.MinSingleWithSelector,
            EnumerableMethod.MinWithSelector,
            QueryableMethod.Min,
            QueryableMethod.MinWithSelector,
        ]);

        __minWithSelectorOverloads = MethodInfoSet.Create(
        [
            EnumerableMethod.MinDecimalWithSelector,
            EnumerableMethod.MinDoubleWithSelector,
            EnumerableMethod.MinInt32WithSelector,
            EnumerableMethod.MinInt64WithSelector,
            EnumerableMethod.MinNullableDecimalWithSelector,
            EnumerableMethod.MinNullableDoubleWithSelector,
            EnumerableMethod.MinNullableInt32WithSelector,
            EnumerableMethod.MinNullableInt64WithSelector,
            EnumerableMethod.MinNullableSingleWithSelector,
            EnumerableMethod.MinSingleWithSelector,
            EnumerableMethod.MinWithSelector,
            QueryableMethod.MinWithSelector
        ]);

        __orderByOverloads = MethodInfoSet.Create(
        [
            __orderBy,
            __orderByDescending
        ]);

        __reverseOverloads = MethodInfoSet.Create(
        [
            __reverse,
            [EnumerableMethod.ReverseWithArray]
        ]);

        __selectManyOverloads = MethodInfoSet.Create(
        [
            __selectManyWithSelector,
            __selectManyWithCollectionSelectorAndResultSelector
        ]);

        __singleOverloads = MethodInfoSet.Create(
        [
            __single,
            __singleOrDefault, // it's convenient to treat SingleOrDefault as if it was an overload
            __singleOrDefaultWithPredicate,
            __singleWithPredicate
        ]);

        __singleWithPredicateOverloads = MethodInfoSet.Create(
        [
            __singleOrDefaultWithPredicate,
            __singleWithPredicate
        ]);

        __skipOverloads = MethodInfoSet.Create(
        [
            __skip,
            __skipWhile, // it's convenient to treat SkipWhile as if it was an overload
            [MongoQueryableMethod.SkipWithLong] // it's convenient to group our custom Skip method with the EnumerableOrQueryable Skip methods
        ]);

        __skipWhileOrTakeWhile = MethodInfoSet.Create(
        [
            __skipWhile,
            __takeWhile
        ]);

        __sumOverloads = MethodInfoSet.Create(
        [
            EnumerableMethod.SumDecimal,
            EnumerableMethod.SumDecimalWithSelector,
            EnumerableMethod.SumDouble,
            EnumerableMethod.SumDoubleWithSelector,
            EnumerableMethod.SumInt32,
            EnumerableMethod.SumInt32WithSelector,
            EnumerableMethod.SumInt64,
            EnumerableMethod.SumInt64WithSelector,
            EnumerableMethod.SumNullableDecimal,
            EnumerableMethod.SumNullableDecimalWithSelector,
            EnumerableMethod.SumNullableDouble,
            EnumerableMethod.SumNullableDoubleWithSelector,
            EnumerableMethod.SumNullableInt32,
            EnumerableMethod.SumNullableInt32WithSelector,
            EnumerableMethod.SumNullableInt64,
            EnumerableMethod.SumNullableInt64WithSelector,
            EnumerableMethod.SumNullableSingle,
            EnumerableMethod.SumNullableSingleWithSelector,
            EnumerableMethod.SumSingle,
            EnumerableMethod.SumSingleWithSelector,
            QueryableMethod.SumDecimal,
            QueryableMethod.SumDecimalWithSelector,
            QueryableMethod.SumDouble,
            QueryableMethod.SumDoubleWithSelector,
            QueryableMethod.SumInt32,
            QueryableMethod.SumInt32WithSelector,
            QueryableMethod.SumInt64,
            QueryableMethod.SumInt64WithSelector,
            QueryableMethod.SumNullableDecimal,
            QueryableMethod.SumNullableDecimalWithSelector,
            QueryableMethod.SumNullableDouble,
            QueryableMethod.SumNullableDoubleWithSelector,
            QueryableMethod.SumNullableInt32,
            QueryableMethod.SumNullableInt32WithSelector,
            QueryableMethod.SumNullableInt64,
            QueryableMethod.SumNullableInt64WithSelector,
            QueryableMethod.SumNullableSingle,
            QueryableMethod.SumNullableSingleWithSelector,
            QueryableMethod.SumSingle,
            QueryableMethod.SumSingleWithSelector
        ]);

        __sumWithSelectorOverloads = MethodInfoSet.Create(
        [
            EnumerableMethod.SumDecimalWithSelector,
            EnumerableMethod.SumDoubleWithSelector,
            EnumerableMethod.SumInt32WithSelector,
            EnumerableMethod.SumInt64WithSelector,
            EnumerableMethod.SumNullableDecimalWithSelector,
            EnumerableMethod.SumNullableDoubleWithSelector,
            EnumerableMethod.SumNullableInt32WithSelector,
            EnumerableMethod.SumNullableInt64WithSelector,
            EnumerableMethod.SumNullableSingleWithSelector,
            EnumerableMethod.SumSingleWithSelector,
            QueryableMethod.SumDecimalWithSelector,
            QueryableMethod.SumDoubleWithSelector,
            QueryableMethod.SumInt32WithSelector,
            QueryableMethod.SumInt64WithSelector,
            QueryableMethod.SumNullableDecimalWithSelector,
            QueryableMethod.SumNullableDoubleWithSelector,
            QueryableMethod.SumNullableInt32WithSelector,
            QueryableMethod.SumNullableInt64WithSelector,
            QueryableMethod.SumNullableSingleWithSelector,
            QueryableMethod.SumSingleWithSelector,
        ]);

      __takeOverloads = MethodInfoSet.Create(
        [
            __take,
            __takeWhile, // it's convenient to treat TakeWhile as if it was an overload of Take
            [MongoQueryableMethod.TakeWithLong] // it's convenient to group our custom Take method with the EnumerableOrQueryable Take methods
        ]);

        __thenByOverloads = MethodInfoSet.Create(
        [
            __thenBy,
            __thenByDescending
        ]);

        // initialize sets that depend on other sets last
        __firstOrLastOrSingleOverloads = MethodInfoSet.Create(
        [
            __firstOverloads,
            __lastOverloads,
            __singleOverloads
        ]);

        __firstOrLastOrSingleWithPredicateOverloads = MethodInfoSet.Create(
        [
            __firstWithPredicateOverloads,
            __lastWithPredicateOverloads,
            __singleWithPredicateOverloads
        ]);

        __firstOrLastOverloads = MethodInfoSet.Create(
        [
            __firstOverloads,
            __lastOverloads
        ]);

        __firstOrLastWithPredicateOverloads = MethodInfoSet.Create(
        [
            __firstWithPredicateOverloads,
            __lastWithPredicateOverloads
        ]);

        __maxOrMinOverloads = MethodInfoSet.Create(
        [
            __maxOverloads,
            __minOverloads
        ]);

        __maxOrMinWithSelectorOverloads = MethodInfoSet.Create(
        [
            __maxWithSelectorOverloads,
            __minWithSelectorOverloads
        ]);

        __orderByOrThenByOverloads = MethodInfoSet.Create(
        [
            __orderByOverloads,
            __thenByOverloads
        ]);

        __skipOrTakeOverloads = MethodInfoSet.Create(
        [
            __skipOverloads,
            __takeOverloads
        ]);
    }

    // methods
    public static IReadOnlyMethodInfoSet AggregateWithFunc => __aggregateWithFunc;
    public static IReadOnlyMethodInfoSet AggregateWithSeedAndFunc => __aggregateWithSeedAndFunc;
    public static IReadOnlyMethodInfoSet AggregateWithSeedFuncAndResultSelector => __aggregateWithSeedFuncAndResultSelector;
    public static IReadOnlyMethodInfoSet All => __all;
    public static IReadOnlyMethodInfoSet Any => __any;
    public static IReadOnlyMethodInfoSet AnyWithPredicate => __anyWithPredicate;
    public static IReadOnlyMethodInfoSet Append => __append;
    public static IReadOnlyMethodInfoSet Concat => __concat;
    public static IReadOnlyMethodInfoSet Count => __count;
    public static IReadOnlyMethodInfoSet CountWithPredicate => __countWithPredicate;
    public static IReadOnlyMethodInfoSet DefaultIfEmpty => __defaultIfEmpty;
    public static IReadOnlyMethodInfoSet DefaultIfEmptyWithDefaultValue => __defaultIfEmptyWithDefaultValue;
    public static IReadOnlyMethodInfoSet Distinct => __distinct;
    public static IReadOnlyMethodInfoSet ElementAt => __elementAt;
    public static IReadOnlyMethodInfoSet ElementAtOrDefault => __elementAtOrDefault;
    public static IReadOnlyMethodInfoSet Except => __except;
    public static IReadOnlyMethodInfoSet First => __first;
    public static IReadOnlyMethodInfoSet FirstOrDefault => __firstOrDefault;
    public static IReadOnlyMethodInfoSet FirstOrDefaultWithPredicate => __firstOrDefaultWithPredicate;
    public static IReadOnlyMethodInfoSet FirstWithPredicate => __firstWithPredicate;
    public static IReadOnlyMethodInfoSet GroupByWithKeySelector => __groupByWithKeySelector;
    public static IReadOnlyMethodInfoSet GroupByWithKeySelectorAndElementSelector => __groupByWithKeySelectorAndElementSelector;
    public static IReadOnlyMethodInfoSet GroupByWithKeySelectorAndResultSelector => __groupByWithKeySelectorAndResultSelector;
    public static IReadOnlyMethodInfoSet GroupByWithKeySelectorElementSelectorAndResultSelector => __groupByWithKeySelectorElementSelectorAndResultSelector;
    public static IReadOnlyMethodInfoSet Intersect => __intersect;
    public static IReadOnlyMethodInfoSet Last => __last;
    public static IReadOnlyMethodInfoSet LastOrDefault => __lastOrDefault;
    public static IReadOnlyMethodInfoSet LastOrDefaultWithPredicate => __lastOrDefaultWithPredicate;
    public static IReadOnlyMethodInfoSet LastWithPredicate => __lastWithPredicate;
    public static IReadOnlyMethodInfoSet LongCount => __longCount;
    public static IReadOnlyMethodInfoSet LongCountWithPredicate => __longCountWithPredicate;
    public static IReadOnlyMethodInfoSet OfType => __ofType;
    public static IReadOnlyMethodInfoSet Reverse => __reverse;
    public static IReadOnlyMethodInfoSet Select => __select;
    public static IReadOnlyMethodInfoSet SelectManyWithCollectionSelectorAndResultSelector => __selectManyWithCollectionSelectorAndResultSelector;
    public static IReadOnlyMethodInfoSet SelectManyWithSelector => __selectManyWithSelector;
    public static IReadOnlyMethodInfoSet Single => __single;
    public static IReadOnlyMethodInfoSet SingleOrDefault => __singleOrDefault;
    public static IReadOnlyMethodInfoSet SingleOrDefaultWithPredicate => __singleOrDefaultWithPredicate;
    public static IReadOnlyMethodInfoSet SingleWithPredicate => __singleWithPredicate;
    public static IReadOnlyMethodInfoSet Skip => __skip;
    public static IReadOnlyMethodInfoSet SkipWhile => __skipWhile;
    public static IReadOnlyMethodInfoSet Take => __take;
    public static IReadOnlyMethodInfoSet TakeWhile => __takeWhile;
    public static IReadOnlyMethodInfoSet Union => __union;
    public static IReadOnlyMethodInfoSet Where => __where;
    public static IReadOnlyMethodInfoSet Zip => __zip;

    // sets of methods
    public static IReadOnlyMethodInfoSet AggregateOverloads => __aggregateOverloads;
    public static IReadOnlyMethodInfoSet AggregateWithSeedOverloads => __aggregateWithSeedOverloads;
    public static IReadOnlyMethodInfoSet AnyOverloads => __anyOverloads;
    public static IReadOnlyMethodInfoSet AppendOrPrepend => __appendOrPrepend;
    public static IReadOnlyMethodInfoSet AverageOverloads => __averageOverloads;
    public static IReadOnlyMethodInfoSet AverageWithSelectorOverloads => __averageWithSelectorOverloads;
    public static IReadOnlyMethodInfoSet CountOverloads => __countOverloads;
    public static IReadOnlyMethodInfoSet CountWithPredicateOverloads => __countWithPredicateOverloads;
    public static IReadOnlyMethodInfoSet DefaultIfEmptyOverloads => __defaultIfEmptyOverloads;
    public static IReadOnlyMethodInfoSet ElementAtOverloads => __elementAtOverloads;
    public static IReadOnlyMethodInfoSet FirstOverloads => __firstOverloads;
    public static IReadOnlyMethodInfoSet FirstOrDefaultOverloads => __firstOrDefaultOverloads;
    public static IReadOnlyMethodInfoSet FirstWithPredicateOverloads => __firstWithPredicateOverloads;
    public static IReadOnlyMethodInfoSet GroupByOverloads => __groupByOverloads;
    public static IReadOnlyMethodInfoSet FirstOrLastOverloads => __firstOrLastOverloads;
    public static IReadOnlyMethodInfoSet FirstOrLastWithPredicateOverloads => __firstOrLastWithPredicateOverloads;
    public static IReadOnlyMethodInfoSet FirstOrLastOrSingleOverloads => __firstOrLastOrSingleOverloads;
    public static IReadOnlyMethodInfoSet FirstOrLastOrSingleWithPredicateOverloads => __firstOrLastOrSingleWithPredicateOverloads;
    public static IReadOnlyMethodInfoSet LastOverloads => __lastOverloads;
    public static IReadOnlyMethodInfoSet LastOrDefaultOverloads => __lastOrDefaultOverloads;
    public static IReadOnlyMethodInfoSet LastWithPredicateOverloads => __lastWithPredicateOverloads;
    public static IReadOnlyMethodInfoSet MaxOverloads => __maxOverloads;
    public static IReadOnlyMethodInfoSet MaxWithSelectorOverloads => __maxWithSelectorOverloads;
    public static IReadOnlyMethodInfoSet MaxOrMinOverloads => __maxOrMinOverloads;
    public static IReadOnlyMethodInfoSet MaxOrMinWithSelectorOverloads => __maxOrMinWithSelectorOverloads;
    public static IReadOnlyMethodInfoSet MinOverloads => __minOverloads;
    public static IReadOnlyMethodInfoSet MinWithSelectorOverloads => __minWithSelectorOverloads;
    public static IReadOnlyMethodInfoSet OrderByOrThenByOverloads => __orderByOrThenByOverloads;
    public static IReadOnlyMethodInfoSet OrderByOverloads => __orderByOverloads;
    public static IReadOnlyMethodInfoSet ReverseOverloads => __reverseOverloads;
    public static IReadOnlyMethodInfoSet SelectManyOverloads => __selectManyOverloads;
    public static IReadOnlyMethodInfoSet SingleOverloads => __singleOverloads;
    public static IReadOnlyMethodInfoSet SingleWithPredicateOverloads => __singleWithPredicateOverloads;
    public static IReadOnlyMethodInfoSet SkipOrTakeOverloads => __skipOrTakeOverloads;
    public static IReadOnlyMethodInfoSet SkipOverloads => __skipOverloads;
    public static IReadOnlyMethodInfoSet SkipWhileOrTakeWhile => __skipWhileOrTakeWhile;
    public static IReadOnlyMethodInfoSet SumOverloads => __sumOverloads;
    public static IReadOnlyMethodInfoSet SumWithSelectorOverloads => __sumWithSelectorOverloads;
    public static IReadOnlyMethodInfoSet TakeOverloads => __takeOverloads;
    public static IReadOnlyMethodInfoSet ThenByOverloads => __thenByOverloads;
}

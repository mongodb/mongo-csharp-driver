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

using System.Collections.Generic;
using System.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection;

internal static class EnumerableOrQueryableMethod
{
    private static readonly HashSet<MethodInfo> __aggregateOverloads;
    private static readonly HashSet<MethodInfo> __aggregateWithFunc;
    private static readonly HashSet<MethodInfo> __aggregateWithSeedOverloads;
    private static readonly HashSet<MethodInfo> __aggregateWithSeedAndFunc;
    private static readonly HashSet<MethodInfo> __aggregateWithSeedFuncAndResultSelector;
    private static readonly HashSet<MethodInfo> __all;
    private static readonly HashSet<MethodInfo> __anyOverloads;
    private static readonly HashSet<MethodInfo> __any;
    private static readonly HashSet<MethodInfo> __anyWithPredicate;
    private static readonly HashSet<MethodInfo> __append;
    private static readonly HashSet<MethodInfo> __appendOrPrepend;
    private static readonly HashSet<MethodInfo> __averageOverloads;
    private static readonly HashSet<MethodInfo> __averageWithSelectorOverloads;
    private static readonly HashSet<MethodInfo> __concat;
    private static readonly HashSet<MethodInfo> __countOverloads;
    private static readonly HashSet<MethodInfo> __countWithPredicateOverloads;
    private static readonly HashSet<MethodInfo> __distinct;
    private static readonly HashSet<MethodInfo> __elementAt;
    private static readonly HashSet<MethodInfo> __elementAtOrDefault;
    private static readonly HashSet<MethodInfo> __elementAtOverloads;
    private static readonly HashSet<MethodInfo> __except;
    private static readonly HashSet<MethodInfo> __firstOverloads;
    private static readonly HashSet<MethodInfo> __firstOrDefaultOverloads;
    private static readonly HashSet<MethodInfo> __firstWithPredicateOverloads;
    private static readonly HashSet<MethodInfo> __groupByOverloads;
    private static readonly HashSet<MethodInfo> __groupByWithKeySelector;
    private static readonly HashSet<MethodInfo> __groupByWithKeySelectorAndElementSelector;
    private static readonly HashSet<MethodInfo> __groupByWithKeySelectorAndResultSelector;
    private static readonly HashSet<MethodInfo> __groupByWithKeySelectorElementSelectorAndResultSelector;
    private static readonly HashSet<MethodInfo> __lastOverloads;
    private static readonly HashSet<MethodInfo> __lastOrDefaultOverloads;
    private static readonly HashSet<MethodInfo> __lastWithPredicateOverloads;
    private static readonly HashSet<MethodInfo> __maxOverloads;
    private static readonly HashSet<MethodInfo> __maxWithSelectorOverloads;
    private static readonly HashSet<MethodInfo> __minOverloads;
    private static readonly HashSet<MethodInfo> __minWithSelectorOverloads;
    private static readonly HashSet<MethodInfo> __selectManyOverloads;
    private static readonly HashSet<MethodInfo> __selectManyWithCollectionSelectorAndResultSelector;
    private static readonly HashSet<MethodInfo> __selectManyWithSelector;
    private static readonly HashSet<MethodInfo> __singleOverloads;
    private static readonly HashSet<MethodInfo> __singleWithPredicateOverloads;
    private static readonly HashSet<MethodInfo> __skipOverloads;
    private static readonly HashSet<MethodInfo> __skipWhile;
    private static readonly HashSet<MethodInfo> __sumOverloads;
    private static readonly HashSet<MethodInfo> __sumWithSelectorOverloads;
    private static readonly HashSet<MethodInfo> __takeOverloads;
    private static readonly HashSet<MethodInfo> __takeWhile;
    private static readonly HashSet<MethodInfo> __where;

    // sets of methods
    private static readonly HashSet<MethodInfo>[] __firstOrLastOverloads;
    private static readonly HashSet<MethodInfo>[] __firstOrLastWithPredicateOverloads;
    private static readonly HashSet<MethodInfo>[] __firstOrLastOrSingleOverloads;
    private static readonly HashSet<MethodInfo>[] __firstOrLastOrSingleWithPredicateOverloads;
    private static readonly HashSet<MethodInfo>[] __maxOrMinOverloads;
    private static readonly HashSet<MethodInfo>[] __maxOrMinWithSelectorOverloads;
    private static readonly HashSet<MethodInfo>[] __skipOrTakeOverloads;
    private static readonly HashSet<MethodInfo>[] __skipOrTakeWhile;

    static EnumerableOrQueryableMethod()
    {
        __aggregateOverloads =
        [
            EnumerableMethod.AggregateWithFunc,
            EnumerableMethod.AggregateWithSeedAndFunc,
            EnumerableMethod.AggregateWithSeedFuncAndResultSelector,
            QueryableMethod.AggregateWithFunc,
            QueryableMethod.AggregateWithSeedAndFunc,
            QueryableMethod.AggregateWithSeedFuncAndResultSelector
        ];

        __aggregateWithFunc =
        [
            EnumerableMethod.AggregateWithFunc,
            QueryableMethod.AggregateWithFunc
        ];

        __aggregateWithSeedOverloads =
        [
            EnumerableMethod.AggregateWithSeedAndFunc,
            EnumerableMethod.AggregateWithSeedFuncAndResultSelector,
            QueryableMethod.AggregateWithSeedAndFunc,
            QueryableMethod.AggregateWithSeedFuncAndResultSelector
        ];

        __aggregateWithSeedAndFunc =
        [
            EnumerableMethod.AggregateWithSeedAndFunc,
            QueryableMethod.AggregateWithSeedAndFunc
        ];

        __aggregateWithSeedFuncAndResultSelector =
        [
            EnumerableMethod.AggregateWithSeedFuncAndResultSelector,
            QueryableMethod.AggregateWithSeedFuncAndResultSelector
        ];

        __all =
        [
            EnumerableMethod.All,
            QueryableMethod.All
        ];

        __any =
        [
            EnumerableMethod.Any,
            QueryableMethod.Any,
        ];

        __anyOverloads =
        [
            EnumerableMethod.Any,
            EnumerableMethod.AnyWithPredicate,
            QueryableMethod.Any,
            QueryableMethod.AnyWithPredicate
        ];

        __anyWithPredicate =
        [
            EnumerableMethod.AnyWithPredicate,
            QueryableMethod.AnyWithPredicate
        ];

        __append =
        [
            EnumerableMethod.Append,
            QueryableMethod.Append
        ];

        __appendOrPrepend =
        [
            EnumerableMethod.Append,
            EnumerableMethod.Prepend,
            QueryableMethod.Append,
            QueryableMethod.Prepend
        ];

        __averageOverloads =
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
        ];

        __averageWithSelectorOverloads =
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
        ];

        __concat =
        [
            EnumerableMethod.Concat,
            QueryableMethod.Concat
        ];

        __countOverloads =
        [
            EnumerableMethod.Count,
            EnumerableMethod.CountWithPredicate,
            EnumerableMethod.LongCount, // it's convenient to treat LongCount as if it was an overload
            EnumerableMethod.LongCountWithPredicate,
            QueryableMethod.Count,
            QueryableMethod.CountWithPredicate,
            QueryableMethod.LongCount,
            QueryableMethod.LongCountWithPredicate
        ];

        __countWithPredicateOverloads =
        [
            EnumerableMethod.CountWithPredicate,
            EnumerableMethod.LongCountWithPredicate,
            QueryableMethod.CountWithPredicate,
            QueryableMethod.LongCountWithPredicate
        ];

        __distinct =
        [
            EnumerableMethod.Distinct,
            QueryableMethod.Distinct
        ];

        __elementAt =
        [
            EnumerableMethod.ElementAt,
            QueryableMethod.ElementAt
        ];

        __elementAtOverloads =
        [
            EnumerableMethod.ElementAt,
            EnumerableMethod.ElementAtOrDefault, // it's convenient to treat ElementAtOrDefault as if it was an overload
            QueryableMethod.ElementAt,
            QueryableMethod.ElementAtOrDefault
        ];

        __elementAtOrDefault =
        [
            EnumerableMethod.ElementAtOrDefault,
            QueryableMethod.ElementAtOrDefault
        ];

        __except =
        [
            EnumerableMethod.Except,
            QueryableMethod.Except
        ];

        __firstOverloads =
        [
            EnumerableMethod.First,
            EnumerableMethod.FirstOrDefault, // it's convenient to treat FirstOrDefault as if it was an overload
            EnumerableMethod.FirstOrDefaultWithPredicate,
            EnumerableMethod.FirstWithPredicate,
            QueryableMethod.First,
            QueryableMethod.FirstOrDefault,
            QueryableMethod.FirstOrDefaultWithPredicate,
            QueryableMethod.FirstWithPredicate
        ];

        __firstOrDefaultOverloads =
        [
            EnumerableMethod.FirstOrDefault,
            EnumerableMethod.FirstOrDefaultWithPredicate,
            QueryableMethod.FirstOrDefault,
            QueryableMethod.FirstOrDefaultWithPredicate
        ];

        __firstWithPredicateOverloads =
        [
            EnumerableMethod.FirstOrDefaultWithPredicate,
            EnumerableMethod.FirstWithPredicate,
            QueryableMethod.FirstOrDefaultWithPredicate,
            QueryableMethod.FirstWithPredicate
        ];

        __groupByOverloads =
        [
            EnumerableMethod.GroupByWithKeySelector,
            EnumerableMethod.GroupByWithKeySelectorAndElementSelector,
            EnumerableMethod.GroupByWithKeySelectorAndResultSelector,
            EnumerableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector,
            QueryableMethod.GroupByWithKeySelector,
            QueryableMethod.GroupByWithKeySelectorAndElementSelector,
            QueryableMethod.GroupByWithKeySelectorAndResultSelector,
            QueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector
        ];

        __groupByWithKeySelector =
        [
            EnumerableMethod.GroupByWithKeySelector,
            QueryableMethod.GroupByWithKeySelector
        ];

        __groupByWithKeySelectorAndElementSelector =
        [
            EnumerableMethod.GroupByWithKeySelectorAndElementSelector,
            QueryableMethod.GroupByWithKeySelectorAndElementSelector
        ];

        __groupByWithKeySelectorAndResultSelector =
        [
            EnumerableMethod.GroupByWithKeySelectorAndResultSelector,
            QueryableMethod.GroupByWithKeySelectorAndResultSelector
        ];

        __groupByWithKeySelectorElementSelectorAndResultSelector =
        [
            EnumerableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector,
            QueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector
        ];

        __lastOverloads =
        [
            EnumerableMethod.Last,
            EnumerableMethod.LastOrDefault, // it's convenient to treat LastOrDefault as if it was an overload
            EnumerableMethod.LastOrDefaultWithPredicate,
            EnumerableMethod.LastWithPredicate,
            QueryableMethod.Last,
            QueryableMethod.LastOrDefault,
            QueryableMethod.LastOrDefaultWithPredicate,
            QueryableMethod.LastWithPredicate
        ];

        __lastOrDefaultOverloads =
        [
            EnumerableMethod.LastOrDefault,
            EnumerableMethod.LastOrDefaultWithPredicate,
            QueryableMethod.LastOrDefault,
            QueryableMethod.LastOrDefaultWithPredicate
        ];

        __lastWithPredicateOverloads =
        [
            EnumerableMethod.LastOrDefaultWithPredicate,
            EnumerableMethod.LastWithPredicate,
            QueryableMethod.LastOrDefaultWithPredicate,
            QueryableMethod.LastWithPredicate
        ];

        __maxOverloads =
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
        ];

        __maxWithSelectorOverloads =
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
            QueryableMethod.MaxWithSelector,
            QueryableMethod.MinWithSelector,
        ];

        __minOverloads =
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
        ];

        __minWithSelectorOverloads =
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
        ];

        __selectManyOverloads =
        [
            EnumerableMethod.SelectManyWithSelector,
            EnumerableMethod.SelectManyWithCollectionSelectorAndResultSelector,
            QueryableMethod.SelectManyWithSelector,
            QueryableMethod.SelectManyWithCollectionSelectorAndResultSelector
        ];

        __selectManyWithCollectionSelectorAndResultSelector =
        [
            EnumerableMethod.SelectManyWithCollectionSelectorAndResultSelector,
            QueryableMethod.SelectManyWithCollectionSelectorAndResultSelector
        ];

        __selectManyWithSelector =
        [
            EnumerableMethod.SelectManyWithSelector,
            QueryableMethod.SelectManyWithSelector
        ];

        __singleOverloads =
        [
            EnumerableMethod.Single,
            EnumerableMethod.SingleOrDefault, // it's convenient to treat SingleOrDefault as if it was an overload
            EnumerableMethod.SingleOrDefaultWithPredicate,
            EnumerableMethod.SingleWithPredicate,
            QueryableMethod.Single,
            QueryableMethod.SingleOrDefault,
            QueryableMethod.SingleOrDefaultWithPredicate,
            QueryableMethod.SingleWithPredicate
        ];

        __singleWithPredicateOverloads =
        [
            EnumerableMethod.SingleOrDefaultWithPredicate,
            EnumerableMethod.SingleWithPredicate,
            QueryableMethod.SingleOrDefaultWithPredicate,
            QueryableMethod.SingleWithPredicate
        ];

        __skipOverloads =
        [
            EnumerableMethod.Skip,
            EnumerableMethod.SkipWhile, // it's convenient to treat SkipWhile as if it was an overload
            QueryableMethod.Skip,
            QueryableMethod.SkipWhile,
            MongoQueryableMethod.SkipWithLong // it's convenient to group our custom Skip method with the EnumerableOrQueryable Skip methods
        ];

        __skipWhile =
        [
            EnumerableMethod.SkipWhile,
            QueryableMethod.SkipWhile
        ];

        __sumOverloads =
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
        ];

        __sumWithSelectorOverloads =
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
        ];

        __takeOverloads =
        [
            EnumerableMethod.Take,
            EnumerableMethod.TakeWhile, // it's convenient to treat TakeWhile as if it was an overload
            QueryableMethod.Take,
            QueryableMethod.TakeWhile,
            MongoQueryableMethod.TakeWithLong // it's convenient to group our custom Take method with the EnumerableOrQueryable Take methods
        ];

        __takeWhile =
        [
            EnumerableMethod.TakeWhile,
            QueryableMethod.TakeWhile
        ];

        __where =
        [
            EnumerableMethod.Where,
            QueryableMethod.Where,
        ];

        // initialize arrays of sets of methods after sets of methods
        __firstOrLastOverloads =
        [
            __firstOverloads,
            __lastOverloads
        ];

        __firstOrLastWithPredicateOverloads =
        [
            __firstWithPredicateOverloads,
            __lastWithPredicateOverloads
        ];

        __firstOrLastOrSingleOverloads =
        [
            __firstOverloads,
            __lastOverloads,
            __singleOverloads
        ];

        __firstOrLastOrSingleWithPredicateOverloads =
        [
            __firstWithPredicateOverloads,
            __lastWithPredicateOverloads,
            __singleWithPredicateOverloads
        ];

        __maxOrMinOverloads =
        [
            __maxOverloads,
            __minOverloads
        ];

        __maxOrMinWithSelectorOverloads =
        [
            __maxWithSelectorOverloads,
            __minWithSelectorOverloads
        ];

        __skipOrTakeOverloads =
        [
            __skipOverloads,
            __takeOverloads
        ];

        __skipOrTakeWhile =
        [
            __skipWhile,
            __takeWhile
        ];
    }

    public static HashSet<MethodInfo> AggregateOverloads => __aggregateOverloads;
    public static HashSet<MethodInfo> AggregateWithFunc => __aggregateWithFunc;
    public static HashSet<MethodInfo> AggregateWithSeedOverloads => __aggregateWithSeedOverloads;
    public static HashSet<MethodInfo> AggregateWithSeedAndFunc => __aggregateWithSeedAndFunc;
    public static HashSet<MethodInfo> AggregateWithSeedFuncAndResultSelector => __aggregateWithSeedFuncAndResultSelector;
    public static HashSet<MethodInfo> All => __all;
    public static HashSet<MethodInfo> AnyOverloads => __anyOverloads;
    public static HashSet<MethodInfo> Any => __any;
    public static HashSet<MethodInfo> AnyWithPredicate => __anyWithPredicate;
    public static HashSet<MethodInfo> Append => __append;
    public static HashSet<MethodInfo> AppendOrPrepend => __appendOrPrepend;
    public static HashSet<MethodInfo> AverageOverloads => __averageOverloads;
    public static HashSet<MethodInfo> AverageWithSelectorOverloads => __averageWithSelectorOverloads;
    public static HashSet<MethodInfo> Concat => __concat;
    public static HashSet<MethodInfo> CountOverloads => __countOverloads;
    public static HashSet<MethodInfo> CountWithPredicateOverloads => __countWithPredicateOverloads;
    public static HashSet<MethodInfo> Distinct => __distinct;
    public static HashSet<MethodInfo> ElementAt => __elementAt;
    public static HashSet<MethodInfo> ElementAtOrDefault => __elementAtOrDefault;
    public static HashSet<MethodInfo> ElementAtOverloads => __elementAtOverloads;
    public static HashSet<MethodInfo> Except => __except;
    public static HashSet<MethodInfo> FirstOverloads => __firstOverloads;
    public static HashSet<MethodInfo> FirstOrDefaultOverloads => __firstOrDefaultOverloads;
    public static HashSet<MethodInfo> FirstWithPredicateOverloads => __firstWithPredicateOverloads;
    public static HashSet<MethodInfo> GroupByOverloads => __groupByOverloads;
    public static HashSet<MethodInfo> GroupByWithKeySelector => __groupByWithKeySelector;
    public static HashSet<MethodInfo> GroupByWithKeySelectorAndElementSelector => __groupByWithKeySelectorAndElementSelector;
    public static HashSet<MethodInfo> GroupByWithKeySelectorAndResultSelector => __groupByWithKeySelectorAndResultSelector;
    public static HashSet<MethodInfo> GroupByWithKeySelectorElementSelectorAndResultSelector => __groupByWithKeySelectorElementSelectorAndResultSelector;
    public static HashSet<MethodInfo> LastOverloads => __lastOverloads;
    public static HashSet<MethodInfo> LastOrDefaultOverloads => __lastOrDefaultOverloads;
    public static HashSet<MethodInfo> LastWithPredicateOverloads => __lastWithPredicateOverloads;
    public static HashSet<MethodInfo> MaxOverloads => __maxOverloads;
    public static HashSet<MethodInfo> MaxWithSelectorOverloads => __maxWithSelectorOverloads;
    public static HashSet<MethodInfo> MinOverloads => __minOverloads;
    public static HashSet<MethodInfo> MinWithSelectorOverloads => __minWithSelectorOverloads;
    public static HashSet<MethodInfo> SelectManyOverloads => __selectManyOverloads;
    public static HashSet<MethodInfo> SelectManyWithCollectionSelectorAndResultSelector => __selectManyWithCollectionSelectorAndResultSelector;
    public static HashSet<MethodInfo> SelectManyWithSelector => __selectManyWithSelector;
    public static HashSet<MethodInfo> SingleOverloads => __singleOverloads;
    public static HashSet<MethodInfo> SingleWithPredicateOverloads => __singleWithPredicateOverloads;
    public static HashSet<MethodInfo> SkipOverloads => __skipOverloads;
    public static HashSet<MethodInfo> SkipWhile => __skipWhile;
    public static HashSet<MethodInfo> SumOverloads => __sumOverloads;
    public static HashSet<MethodInfo> SumWithSelectorOverloads => __sumWithSelectorOverloads;

    public static HashSet<MethodInfo> TakeOverloads => __takeOverloads;
    public static HashSet<MethodInfo> TakeWhile => __takeWhile;
    public static HashSet<MethodInfo> Where => __where;

    // arrays of sets of methods
    public static HashSet<MethodInfo>[] FirstOrLastOverloads => __firstOrLastOverloads;
    public static HashSet<MethodInfo>[] FirstOrLastWithPredicateOverloads => __firstOrLastWithPredicateOverloads;
    public static HashSet<MethodInfo>[] FirstOrLastOrSingleOverloads => __firstOrLastOrSingleOverloads;
    public static HashSet<MethodInfo>[] FirstOrLastOrSingleWithPredicateOverloads => __firstOrLastOrSingleWithPredicateOverloads;
    public static HashSet<MethodInfo>[] MaxOrMinOverloads => __maxOrMinOverloads;
    public static HashSet<MethodInfo>[] MaxOrMinWithSelectorOverloads => __maxOrMinWithSelectorOverloads;
    public static HashSet<MethodInfo>[] SkipOrTakeOverloads => __skipOrTakeOverloads;
    public static HashSet<MethodInfo>[] SkipOrTakeWhile => __skipOrTakeWhile;
}

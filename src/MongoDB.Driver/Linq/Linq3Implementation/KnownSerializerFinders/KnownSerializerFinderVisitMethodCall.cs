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
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.KnownSerializerFinders;

internal partial class KnownSerializerFinderVisitor
{
    private static readonly HashSet<MethodInfo> __absMethods =
    [
        MathMethod.AbsDecimal,
        MathMethod.AbsDouble,
        MathMethod.AbsInt16,
        MathMethod.AbsInt32,
        MathMethod.AbsInt64,
        MathMethod.AbsSByte,
        MathMethod.AbsSingle
    ];

    private static readonly HashSet<MethodInfo> __aggregateMethods =
    [
        EnumerableMethod.AggregateWithFunc,
        EnumerableMethod.AggregateWithSeedAndFunc,
        EnumerableMethod.AggregateWithSeedFuncAndResultSelector,
        QueryableMethod.AggregateWithFunc,
        QueryableMethod.AggregateWithSeedAndFunc,
        QueryableMethod.AggregateWithSeedFuncAndResultSelector
    ];

    private static readonly HashSet<MethodInfo> __aggregateWithFuncMethods =
    [
        EnumerableMethod.AggregateWithFunc,
        QueryableMethod.AggregateWithFunc
    ];

    private static readonly HashSet<MethodInfo> __aggregateWithSeedAndFuncMethods =
    [
        EnumerableMethod.AggregateWithSeedAndFunc,
        QueryableMethod.AggregateWithSeedAndFunc
    ];

    private static readonly HashSet<MethodInfo> __aggregateWithSeedFuncAdResultSelectorMethods =
    [
        EnumerableMethod.AggregateWithSeedFuncAndResultSelector,
        QueryableMethod.AggregateWithSeedFuncAndResultSelector
    ];

    private static readonly HashSet<MethodInfo> __anyMethods =
    [
        EnumerableMethod.Any,
        EnumerableMethod.AnyWithPredicate,
        QueryableMethod.Any,
        QueryableMethod.AnyWithPredicate
    ];

    private static readonly HashSet<MethodInfo> __anyWithPredicateMethods =
    [
        EnumerableMethod.AnyWithPredicate,
        QueryableMethod.AnyWithPredicate
    ];

    private static readonly HashSet<MethodInfo> __appendOrPrependMethods =
    [
        EnumerableMethod.Append,
        EnumerableMethod.Prepend,
        QueryableMethod.Append,
        QueryableMethod.Prepend
    ];

    private static readonly HashSet<MethodInfo> __averageMethods =
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

    private static readonly HashSet<MethodInfo> __averageWithSelectorMethods =
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
        QueryableMethod.AverageSingleWithSelector
    ];

    private static readonly HashSet<MethodInfo> __countMethods =
    [
        EnumerableMethod.Count,
        EnumerableMethod.CountWithPredicate,
        EnumerableMethod.LongCount,
        EnumerableMethod.LongCountWithPredicate,
        QueryableMethod.Count,
        QueryableMethod.CountWithPredicate,
        QueryableMethod.LongCount,
        QueryableMethod.LongCountWithPredicate
    ];

    private static readonly HashSet<MethodInfo> __countWithPredicateMethods =
    [
        EnumerableMethod.CountWithPredicate,
        EnumerableMethod.LongCountWithPredicate,
        QueryableMethod.CountWithPredicate,
        QueryableMethod.LongCountWithPredicate
    ];

    private static readonly HashSet<MethodInfo> __groupByMethods =
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

    private static readonly HashSet<MethodInfo> __indexOfMethods =
    [
        StringMethod.IndexOfAny,
        StringMethod.IndexOfAnyWithStartIndex,
        StringMethod.IndexOfAnyWithStartIndexAndCount,
        StringMethod.IndexOfBytesWithValue,
        StringMethod.IndexOfBytesWithValueAndStartIndex,
        StringMethod.IndexOfBytesWithValueAndStartIndexAndCount,
        StringMethod.IndexOfWithChar,
        StringMethod.IndexOfWithCharAndStartIndex,
        StringMethod.IndexOfWithCharAndStartIndexAndCount,
        StringMethod.IndexOfWithString,
        StringMethod.IndexOfWithStringAndComparisonType,
        StringMethod.IndexOfWithStringAndStartIndex,
        StringMethod.IndexOfWithStringAndStartIndexAndComparisonType,
        StringMethod.IndexOfWithStringAndStartIndexAndCount,
        StringMethod.IndexOfWithStringAndStartIndexAndCountAndComparisonType,
    ];

    private static readonly HashSet<MethodInfo> __tupleOrValueTupleCreateMethods =
    [
        TupleMethod.Create1,
        TupleMethod.Create2,
        TupleMethod.Create3,
        TupleMethod.Create4,
        TupleMethod.Create5,
        TupleMethod.Create6,
        TupleMethod.Create7,
        TupleMethod.Create8,
        ValueTupleMethod.Create1,
        ValueTupleMethod.Create2,
        ValueTupleMethod.Create3,
        ValueTupleMethod.Create4,
        ValueTupleMethod.Create5,
        ValueTupleMethod.Create6,
        ValueTupleMethod.Create7,
        ValueTupleMethod.Create8
    ];

    private static readonly HashSet<MethodInfo> __firstOrLastMethods =
    [
        EnumerableMethod.First,
        EnumerableMethod.FirstOrDefault,
        EnumerableMethod.FirstOrDefaultWithPredicate,
        EnumerableMethod.FirstWithPredicate,
        EnumerableMethod.Last,
        EnumerableMethod.LastOrDefault,
        EnumerableMethod.LastOrDefaultWithPredicate,
        EnumerableMethod.LastWithPredicate,
        EnumerableMethod.Single,
        EnumerableMethod.SingleOrDefault,
        EnumerableMethod.SingleOrDefaultWithPredicate,
        EnumerableMethod.SingleWithPredicate,
        QueryableMethod.First,
        QueryableMethod.FirstOrDefault,
        QueryableMethod.FirstOrDefaultWithPredicate,
        QueryableMethod.FirstWithPredicate,
        QueryableMethod.Last,
        QueryableMethod.LastOrDefault,
        QueryableMethod.LastOrDefaultWithPredicate,
        QueryableMethod.LastWithPredicate,
        QueryableMethod.Single,
        QueryableMethod.SingleOrDefault,
        QueryableMethod.SingleOrDefaultWithPredicate,
        QueryableMethod.SingleWithPredicate
    ];

    private static readonly HashSet<MethodInfo> __firstOrLastWithPredicateMethods =
    [
        EnumerableMethod.FirstOrDefaultWithPredicate,
        EnumerableMethod.FirstWithPredicate,
        EnumerableMethod.LastOrDefaultWithPredicate,
        EnumerableMethod.LastWithPredicate,
        EnumerableMethod.SingleOrDefaultWithPredicate,
        EnumerableMethod.SingleWithPredicate,
        QueryableMethod.LastOrDefaultWithPredicate,
        QueryableMethod.LastWithPredicate,
        QueryableMethod.LastOrDefaultWithPredicate,
        QueryableMethod.LastWithPredicate,
        QueryableMethod.SingleOrDefaultWithPredicate,
        QueryableMethod.SingleWithPredicate
    ];

    private static readonly HashSet<MethodInfo> __logMethods =
    [
        MathMethod.Log,
        MathMethod.Log10,
        MathMethod.LogWithNewBase
    ];

    private static readonly HashSet<MethodInfo> __lookupMethods =
    [
        MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignField,
        MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignFieldAndPipeline,
        MongoQueryableMethod.LookupWithDocumentsAndPipeline,
        MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignField,
        MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignFieldAndPipeline,
        MongoQueryableMethod.LookupWithFromAndPipeline
    ];

    private static readonly HashSet<MethodInfo> __maxOrMinMethods =
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
        QueryableMethod.Max,
        QueryableMethod.MaxWithSelector,
        QueryableMethod.Min,
        QueryableMethod.MinWithSelector,
    ];

    private static readonly HashSet<MethodInfo> __maxOrMinWithSelectorMethods =
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
        QueryableMethod.MaxWithSelector,
        QueryableMethod.MinWithSelector,
    ];

    private static readonly MethodInfo[] __pickMethods = new[]
    {
        EnumerableMethod.Bottom,
        EnumerableMethod.BottomN,
        EnumerableMethod.BottomNWithComputedN,
        EnumerableMethod.FirstN,
        EnumerableMethod.FirstNWithComputedN,
        EnumerableMethod.LastN,
        EnumerableMethod.LastNWithComputedN,
        EnumerableMethod.MaxN,
        EnumerableMethod.MaxNWithComputedN,
        EnumerableMethod.MinN,
        EnumerableMethod.MinNWithComputedN,
        EnumerableMethod.Top,
        EnumerableMethod.TopN,
        EnumerableMethod.TopNWithComputedN
    };

    private static readonly MethodInfo[] __pickWithComputedNMethods = new[]
    {
        EnumerableMethod.BottomNWithComputedN,
        EnumerableMethod.FirstNWithComputedN,
        EnumerableMethod.LastNWithComputedN,
        EnumerableMethod.MaxNWithComputedN,
        EnumerableMethod.MinNWithComputedN,
        EnumerableMethod.TopNWithComputedN
    };

    private static readonly MethodInfo[] __pickWithSortDefinitionMethods = new[]
    {
        EnumerableMethod.Bottom,
        EnumerableMethod.BottomN,
        EnumerableMethod.BottomNWithComputedN,
        EnumerableMethod.Top,
        EnumerableMethod.TopN,
        EnumerableMethod.TopNWithComputedN
    };

    private static readonly HashSet<MethodInfo> __selectManyMethods =
    [
        EnumerableMethod.SelectManyWithSelector,
        EnumerableMethod.SelectManyWithCollectionSelectorAndResultSelector,
        QueryableMethod.SelectManyWithSelector,
        QueryableMethod.SelectManyWithCollectionSelectorAndResultSelector
    ];

    private static readonly HashSet<MethodInfo> __selectManyWithCollectionSelectorAndResultSelectorMethods =
    [
        EnumerableMethod.SelectManyWithCollectionSelectorAndResultSelector,
        QueryableMethod.SelectManyWithCollectionSelectorAndResultSelector
    ];

    private static readonly HashSet<MethodInfo> __selectManyWithResultSelectorMethods =
    [
        EnumerableMethod.SelectManyWithSelector,
        QueryableMethod.SelectManyWithSelector
    ];

    private static readonly HashSet<MethodInfo> __skipOrTakeMethods =
    [
        EnumerableMethod.Skip,
        EnumerableMethod.SkipWhile,
        EnumerableMethod.Take,
        EnumerableMethod.TakeWhile,
        QueryableMethod.Skip,
        QueryableMethod.SkipWhile,
        QueryableMethod.Take,
        QueryableMethod.TakeWhile,
        MongoQueryableMethod.SkipWithLong,
        MongoQueryableMethod.TakeWithLong
    ];

    private static readonly HashSet<MethodInfo> __skipOrTakeWhileMethods =
    [
        EnumerableMethod.SkipWhile,
        EnumerableMethod.TakeWhile,
        QueryableMethod.SkipWhile,
        QueryableMethod.TakeWhile
    ];

    private static readonly HashSet<MethodInfo> __splitMethods =
    [
        StringMethod.SplitWithChars,
        StringMethod.SplitWithCharsAndCount,
        StringMethod.SplitWithCharsAndCountAndOptions,
        StringMethod.SplitWithCharsAndOptions,
        StringMethod.SplitWithStringsAndCountAndOptions,
        StringMethod.SplitWithStringsAndOptions
    ];

    private static readonly HashSet<MethodInfo> __standardDeviationMethods =
    [
        MongoEnumerableMethod.StandardDeviationPopulationDecimal,
        MongoEnumerableMethod.StandardDeviationPopulationDecimalWithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationDouble,
        MongoEnumerableMethod.StandardDeviationPopulationDoubleWithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationInt32,
        MongoEnumerableMethod.StandardDeviationPopulationInt32WithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationInt64,
        MongoEnumerableMethod.StandardDeviationPopulationInt64WithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationNullableDecimal,
        MongoEnumerableMethod.StandardDeviationPopulationNullableDecimalWithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationNullableDouble,
        MongoEnumerableMethod.StandardDeviationPopulationNullableDoubleWithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationNullableInt32,
        MongoEnumerableMethod.StandardDeviationPopulationNullableInt32WithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationNullableInt64,
        MongoEnumerableMethod.StandardDeviationPopulationNullableInt64WithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationNullableSingle,
        MongoEnumerableMethod.StandardDeviationPopulationNullableSingleWithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationSingle,
        MongoEnumerableMethod.StandardDeviationPopulationSingleWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleDecimal,
        MongoEnumerableMethod.StandardDeviationSampleDecimalWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleDouble,
        MongoEnumerableMethod.StandardDeviationSampleDoubleWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleInt32,
        MongoEnumerableMethod.StandardDeviationSampleInt32WithSelector,
        MongoEnumerableMethod.StandardDeviationSampleInt64,
        MongoEnumerableMethod.StandardDeviationSampleInt64WithSelector,
        MongoEnumerableMethod.StandardDeviationSampleNullableDecimal,
        MongoEnumerableMethod.StandardDeviationSampleNullableDecimalWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleNullableDouble,
        MongoEnumerableMethod.StandardDeviationSampleNullableDoubleWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleNullableInt32,
        MongoEnumerableMethod.StandardDeviationSampleNullableInt32WithSelector,
        MongoEnumerableMethod.StandardDeviationSampleNullableInt64,
        MongoEnumerableMethod.StandardDeviationSampleNullableInt64WithSelector,
        MongoEnumerableMethod.StandardDeviationSampleNullableSingle,
        MongoEnumerableMethod.StandardDeviationSampleNullableSingleWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleSingle,
        MongoEnumerableMethod.StandardDeviationSampleSingleWithSelector,
    ];

    private static readonly HashSet<MethodInfo> __standardDeviationWithSelectorMethods =
    [
        MongoEnumerableMethod.StandardDeviationPopulationDecimalWithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationDoubleWithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationInt32WithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationInt64WithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationNullableDecimalWithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationNullableDoubleWithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationNullableInt32WithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationNullableInt64WithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationNullableSingleWithSelector,
        MongoEnumerableMethod.StandardDeviationPopulationSingleWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleDecimalWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleDoubleWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleInt32WithSelector,
        MongoEnumerableMethod.StandardDeviationSampleInt64WithSelector,
        MongoEnumerableMethod.StandardDeviationSampleNullableDecimalWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleNullableDoubleWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleNullableInt32WithSelector,
        MongoEnumerableMethod.StandardDeviationSampleNullableInt64WithSelector,
        MongoEnumerableMethod.StandardDeviationSampleNullableSingleWithSelector,
        MongoEnumerableMethod.StandardDeviationSampleSingleWithSelector,
    ];

    private static readonly HashSet<MethodInfo> __stringContainsMethods =
    [
        StringMethod.ContainsWithChar,
        StringMethod.ContainsWithCharAndComparisonType,
        StringMethod.ContainsWithString,
        StringMethod.ContainsWithStringAndComparisonType
    ];

    private static readonly HashSet<MethodInfo> __stringEndsWithOrStartsWithMethods =
    [
        StringMethod.EndsWithWithChar,
        StringMethod.EndsWithWithString,
        StringMethod.EndsWithWithStringAndComparisonType,
        StringMethod.EndsWithWithStringAndIgnoreCaseAndCulture,
        StringMethod.StartsWithWithChar,
        StringMethod.StartsWithWithString,
        StringMethod.StartsWithWithStringAndComparisonType,
        StringMethod.StartsWithWithStringAndIgnoreCaseAndCulture
    ];

    private static readonly HashSet<MethodInfo> __subtractReturningDateTimeMethods =
    [
        DateTimeMethod.SubtractWithTimeSpan,
        DateTimeMethod.SubtractWithTimeSpanAndTimezone,
        DateTimeMethod.SubtractWithUnit,
        DateTimeMethod.SubtractWithUnitAndTimezone
    ];

    private static readonly HashSet<MethodInfo> __subtractReturningInt64Methods =
    [
        DateTimeMethod.SubtractWithDateTimeAndUnit,
        DateTimeMethod.SubtractWithDateTimeAndUnitAndTimezone
    ];

    private static readonly HashSet<MethodInfo> __subtractReturningTimeSpanWithMillisecondsUnitsMethods =
    [
        DateTimeMethod.SubtractWithDateTime,
        DateTimeMethod.SubtractWithDateTimeAndTimezone
    ];

    private static readonly HashSet<MethodInfo> __sumMethods =
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

    private static readonly HashSet<MethodInfo> __sumWithSelectorMethods =
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

    private static readonly HashSet<MethodInfo> __toLowerOrToUpperMethods =
    [
        StringMethod.ToLower,
        StringMethod.ToLowerInvariant,
        StringMethod.ToLowerWithCulture,
        StringMethod.ToUpper,
        StringMethod.ToUpperInvariant,
        StringMethod.ToUpperWithCulture,
    ];

    private static readonly HashSet<MethodInfo> __whereMethods =
    [
        EnumerableMethod.Where,
        MongoEnumerableMethod.WhereWithLimit,
        QueryableMethod.Where,
    ];

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var method = node.Method;
        var arguments = node.Arguments;

        DeduceMethodCallSerializers();
        if (IsKnown(node, out var knownSerializer) && knownSerializer is IUnknowableSerializer)
        {
            return node; // don't visit node any further
        }
        base.VisitMethodCall(node);
        DeduceMethodCallSerializers();

        return node;

        void DeduceMethodCallSerializers()
        {
            switch (node.Method.Name)
            {
                case "Abs": DeduceAbsMethodSerializers(); break;
                case "Acos": DeduceAcosMethodSerializers(); break;
                case "Acosh": DeduceAcoshMethodSerializers(); break;
                case "Add": DeduceAddMethodSerializers(); break;
                case "AddDays": DeduceAddDaysMethodSerializers(); break;
                case "AddHours": DeduceAddHoursMethodSerializers(); break;
                case "AddMilliseconds": DeduceAddMillisecondsMethodSerializers(); break;
                case "AddMinutes": DeduceAddMinutesMethodSerializers(); break;
                case "AddMonths": DeduceAddMonthsMethodSerializers(); break;
                case "AddQuarters": DeduceAddQuartersMethodSerializers(); break;
                case "AddSeconds": DeduceAddSecondsMethodSerializers(); break;
                case "AddTicks": DeduceAddTicksMethodSerializers(); break;
                case "AddWeeks": DeduceAddWeeksMethodSerializers(); break;
                case "AddYears": DeduceAddYearsMethodSerializers(); break;
                case "Aggregate": DeduceAggregateMethodSerializers(); break;
                case "All": DeduceAllMethodSerializers(); break;
                case "Any": DeduceAnyMethodSerializers(); break;
                case "AppendStage": DeduceAppendStageMethodSerializers(); break;
                case "As": DeduceAsMethodSerializers(); break;
                case "Asin": DeduceAsinMethodSerializers(); break;
                case "Asinh": DeduceAsinhMethodSerializers(); break;
                case "AsQueryable": DeduceAsQueryableMethodSerializers(); break;
                case "Atan": DeduceAtanMethodSerializers(); break;
                case "Atanh": DeduceAtanhMethodSerializers(); break;
                case "Atan2": DeduceAtan2MethodSerializers(); break;
                case "Average": DeduceAverageMethodSerializers(); break;
                case "CompareTo": DeduceCompareToMethodSerializers(); break;
                case "Concat": DeduceConcatMethodSerializers(); break;
                case "Constant": DeduceConstantMethodSerializers(); break;
                case "Contains": DeduceContainsMethodSerializers(); break;
                case "ContainsKey": DeduceContainsKeyMethodSerializers(); break;
                case "ContainsValue": DeduceContainsValueMethodSerializers(); break;
                case "Cos": DeduceCosMethodSerializers(); break;
                case "Cosh": DeduceCoshMethodSerializers(); break;
                case "Create": DeduceCreateMethodSerializers(); break;
                case "DefaultIfEmpty": DeduceDefaultIfEmptyMethodSerializers(); break;
                case "DegreesToRadians": DeduceDegreesToRadiansMethodSerializers(); break;
                case "Distinct": DeduceDistinctMethodSerializers(); break;
                case "Documents": DeduceDocumentsMethodSerializers(); break;
                case "Equals": DeduceEqualsMethodSerializers(); break;
                case "Except": DeduceExceptMethodSerializers(); break;
                case "Exists": DeduceExistsMethodSerializers(); break;
                case "Exp": DeduceExpMethodSerializers(); break;
                case "Field": DeduceFieldMethodSerializers(); break;
                case "get_Item": DeduceGetItemMethodSerializers(); break;
                case "GroupBy": DeduceGroupByMethodSerializers(); break;
                case "GroupJoin": DeduceGroupJoinMethodSerializers(); break;
                case "Inject": DeduceInjectMethodSerializers(); break;
                case "Intersect": DeduceIntersectMethodSerializers(); break;
                case "IsMatch": DeduceIsMatchMethodSerializers(); break;
                case "IsNullOrEmpty": DeduceIsNullOrEmptyMethodSerializers(); break;
                case "IsSubsetOf": DeduceIsSubsetOfMethodSerializers(); break;
                case "Join": DeduceJoinMethodSerializers(); break;
                case "Lookup": DeduceLookupMethodSerializers(); break;
                case "OfType": DeduceOfTypeMethodSerializers(); break;
                case "Parse": DeduceParseMethodSerializers(); break;
                case "Pow": DeducePowMethodSerializers(); break;
                case "RadiansToDegrees": DeduceRadiansToDegreesMethodSerializers(); break;
                case "Range": DeduceRangeMethodSerializers(); break;
                case "Repeat": DeduceRepeatMethodSerializers(); break;
                case "Reverse": DeduceReverseMethodSerializers(); break;
                case "Select": DeduceSelectMethodSerializers(); break;
                case "SelectMany": DeduceSelectManySerializers(); break;
                case "SequenceEqual": DeduceSequenceEqualMethodSerializers(); break;
                case "SetEquals": DeduceSetEqualsMethodSerializers(); break;
                case "Sin": DeduceSinMethodSerializers(); break;
                case "Sinh": DeduceSinhMethodSerializers(); break;
                case "Split": DeduceSplitMethodSerializers(); break;
                case "Sqrt": DeduceSqrtMethodSerializers(); break;
                case "StringIn": DeduceStringInMethodSerializers(); break;
                case "StrLenBytes": DeduceStrLenBytesMethodSerializers(); break;
                case "Subtract": DeduceSubtractMethodSerializers(); break;
                case "Sum": DeduceSumMethodSerializers(); break;
                case "Tan": DeduceTanMethodSerializers(); break;
                case "Tanh": DeduceTanhMethodSerializers(); break;
                case "ToArray": DeduceToArrayMethodSerializers(); break;
                case "ToList": DeduceToListSerializers(); break;
                case "ToString": DeduceToStringSerializers(); break;
                case "Truncate": DeduceTruncateSerializers(); break;
                case "Union": DeduceUnionSerializers(); break;
                case "Week": DeduceWeekSerializers(); break;
                case "Where": DeduceWhereSerializers(); break;
                case "Zip": DeduceZipSerializers(); break;

                case "AllElements":
                case "AllMatchingElements":
                case "FirstMatchingElement":
                    DeduceMatchingElementsMethodSerializers();
                    break;

                case "Append":
                case "Prepend":
                    DeduceAppendOrPrependMethodSerializers();
                    break;

                case "Bottom":
                case "BottomN":
                case "FirstN":
                case "LastN":
                case "MaxN":
                case "MinN":
                case "Top":
                case "TopN":
                    DeducePickMethodSerializers();
                    break;

                case "Ceiling":
                case "Floor":
                    DeduceCeilingOrFloorMethodSerializers();
                    break;

                case "Count":
                case "LongCount":
                    DeduceCountMethodSerializers();
                    break;

                case "ElementAt":
                case "ElementAtOrDefault":
                    DeduceElementAtMethodSerializers();
                    break;

                case "EndsWith":
                case "StartsWith":
                    DeduceEndsWithOrStartsWithMethodSerializers();
                    break;

                case "First":
                case "FirstOrDefault":
                case "Last":
                case "LastOrDefault":
                case "Single":
                case "SingleOrDefault":
                    DeduceFirstOrLastMethodsSerializers();
                    break;

                case "IndexOf":
                case "IndexOfBytes":
                    DeduceIndexOfMethodSerializers();
                    break;

                case "IsMissing":
                case "IsNullOrMissing":
                    DeduceIsMissingOrIsNullOrMissingMethodSerializers();
                    break;

                case "Ln":
                case "Log":
                case "Log10":
                    DeduceLogMethodSerializers();
                    break;

                case "Max":
                case "Min":
                    DeduceMaxOrMinMethodSerializers();
                    break;

                case "OrderBy":
                case "OrderByDescending":
                case "ThenBy":
                case "ThenByDescending":
                    DeduceOrderByMethodSerializers();
                    break;

                case "Skip":
                case "SkipWhile":
                case "Take":
                case "TakeWhile":
                    DeduceSkipOrTakeMethodSerializers();
                    break;

                case "StandardDeviationPopulation":
                case "StandardDeviationSample":
                    DeduceStandardDeviationMethodSerializers();
                    break;

                case "Substring":
                case "SubstrBytes":
                    DeduceSubstringMethodSerializers();
                    break;

                case "ToLower":
                case "ToLowerInvariant":
                case "ToUpper":
                case "ToUpperInvariant":
                    DeduceToLowerOrToUpperSerializers();
                    break;

                default:
                    DeduceUnknownMethodSerializer();
                    break;
            }
        }

        void DeduceAbsMethodSerializers()
        {
            if (method.IsOneOf(__absMethods))
            {
                var valueExpression = arguments[0];
                DeduceSerializers(node, valueExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAcosMethodSerializers()
        {
            if (method.Is(MathMethod.Acos))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAcoshMethodSerializers()
        {
            if (method.Is(MathMethod.Acosh))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddMethodSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.Add, DateTimeMethod.AddWithTimezone, DateTimeMethod.AddWithUnit, DateTimeMethod.AddWithUnitAndTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddDaysMethodSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.AddDays,  DateTimeMethod.AddDaysWithTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddHoursMethodSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.AddHours,  DateTimeMethod.AddHoursWithTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddMillisecondsMethodSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.AddMilliseconds,  DateTimeMethod.AddMillisecondsWithTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddMinutesMethodSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.AddMinutes,  DateTimeMethod.AddMinutesWithTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddMonthsMethodSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.AddMonths,  DateTimeMethod.AddMonthsWithTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddQuartersMethodSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.AddQuarters, DateTimeMethod.AddQuartersWithTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddSecondsMethodSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.AddSeconds, DateTimeMethod.AddSecondsWithTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddTicksMethodSerializers()
        {
            if (method.Is(DateTimeMethod.AddTicks))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddWeeksMethodSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.AddWeeks, DateTimeMethod.AddWeeksWithTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAddYearsMethodSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.AddYears, DateTimeMethod.AddYearsWithTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAggregateMethodSerializers()
        {
            if (method.IsOneOf(__aggregateMethods))
            {
                var sourceExpression = arguments[0];
                _ = IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer);

                if (method.IsOneOf(__aggregateWithFuncMethods))
                {
                    var funcLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var funcAccumulatorParameter = funcLambda.Parameters[0];
                    var funcSourceItemParameter = funcLambda.Parameters[1];

                    DeduceItemAndCollectionSerializers(funcAccumulatorParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(funcSourceItemParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(funcLambda.Body, sourceExpression);
                    DeduceSerializers(node, funcLambda.Body);
                }

                if (method.IsOneOf(__aggregateWithSeedAndFuncMethods))
                {
                    var seedExpression =  arguments[1];
                    var funcLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var funcAccumulatorParameter = funcLambda.Parameters[0];
                    var funcSourceItemParameter = funcLambda.Parameters[1];

                    DeduceSerializers(seedExpression, funcLambda.Body);
                    DeduceSerializers(funcAccumulatorParameter, funcLambda.Body);
                    DeduceItemAndCollectionSerializers(funcSourceItemParameter, sourceExpression);
                    DeduceSerializers(node, funcLambda.Body);
                }

                if (method.IsOneOf(__aggregateWithSeedFuncAdResultSelectorMethods))
                {
                    var seedExpression = arguments[1];
                    var funcLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var funcAccumulatorParameter = funcLambda.Parameters[0];
                    var funcSourceItemParameter = funcLambda.Parameters[1];
                    var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[3]);
                    var resultSelectorAccumulatorParameter = resultSelectorLambda.Parameters[0];

                    DeduceSerializers(seedExpression, funcLambda.Body);
                    DeduceSerializers(funcAccumulatorParameter, funcLambda.Body);
                    DeduceItemAndCollectionSerializers(funcSourceItemParameter, sourceExpression);
                    DeduceSerializers(resultSelectorAccumulatorParameter, funcLambda.Body);
                    DeduceSerializers(node, resultSelectorLambda.Body);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAllMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.AllWithPredicate, QueryableMethod.AllWithPredicate))
            {
                var sourceExpression = arguments[0];
                var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var predicateParameter = predicateLambda.Parameters.Single();

                DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAnyMethodSerializers()
        {
            if (method.IsOneOf(__anyMethods))
            {
                if (method.IsOneOf(__anyWithPredicateMethods))
                {
                    var sourceExpression = arguments[0];
                    var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var predicateParameter = predicateLambda.Parameters[0];

                    DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
                }

                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAppendOrPrependMethodSerializers()
        {
            if (method.IsOneOf(__appendOrPrependMethods))
            {
                var sourceExpression = arguments[0];
                var elementExpression = arguments[1];

                DeduceItemAndCollectionSerializers(elementExpression, sourceExpression);
                DeduceSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAsMethodSerializers()
        {
            if (method.Is(MongoQueryableMethod.As))
            {
                if (IsNotKnown(node))
                {
                    var resultSerializerExpression = arguments[1];
                    if (resultSerializerExpression is not ConstantExpression resultSerializerConstantExpression)
                    {
                        throw new ExpressionNotSupportedException(node, because: "resultSerializer argument must be a constant");
                    }

                    var resultItemSerializer = (IBsonSerializer)resultSerializerConstantExpression.Value;
                    if (resultItemSerializer == null)
                    {
                        var resultItemType = method.GetGenericArguments()[1];
                        resultItemSerializer = BsonSerializer.LookupSerializer(resultItemType);
                    }

                    var resultSerializer = IEnumerableOrIQueryableSerializer.Create(node.Type, resultItemSerializer);
                    AddKnownSerializer(node, resultSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAppendStageMethodSerializers()
        {
            if (method.Is(MongoQueryableMethod.AppendStage))
            {
                if (IsNotKnown(node))
                {
                    var sourceExpression = arguments[0];
                    var stageExpression = arguments[1];
                    var resultSerializerExpression = arguments[2];

                    if (stageExpression is not ConstantExpression stageConstantExpression)
                    {
                        throw new ExpressionNotSupportedException(node, because: "stage argument must be a constant");
                    }
                    var stageDefinition = (IPipelineStageDefinition)stageConstantExpression.Value;

                    if (resultSerializerExpression is not ConstantExpression resultSerializerConstantExpression)
                    {
                        throw new ExpressionNotSupportedException(node, because: "resultSerializer argument must be a constant");
                    }
                    var resultItemSerializer = (IBsonSerializer)resultSerializerConstantExpression.Value;

                    if (resultItemSerializer == null && IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer))
                    {
                        var serializerRegistry = BsonSerializer.SerializerRegistry; // TODO: get correct registry
                        var translationOptions = new ExpressionTranslationOptions(); // TODO: get correct translation options
                        var renderedStage = stageDefinition.Render(sourceItemSerializer, serializerRegistry, translationOptions);
                        resultItemSerializer = renderedStage.OutputSerializer;
                    }

                    if (resultItemSerializer != null)
                    {
                        var resultSerializer = IEnumerableOrIQueryableSerializer.Create(node.Type, resultItemSerializer);
                        AddKnownSerializer(node, resultSerializer);
                    }
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAsinMethodSerializers()
        {
            if (method.Is(MathMethod.Asin))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAsQueryableMethodSerializers()
        {
            if (method.Is(QueryableMethod.AsQueryable))
            {
                var sourceExpression = arguments[0];

                if (IsNotKnown(node) && IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer))
                {
                    var resultSerializer = IQueryableSerializer.Create(sourceItemSerializer);
                    AddKnownSerializer(node, resultSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAsinhMethodSerializers()
        {
            if (method.Is(MathMethod.Asinh))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAtanMethodSerializers()
        {
            if (method.Is(MathMethod.Atan))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAtanhMethodSerializers()
        {
            if (method.Is(MathMethod.Atanh))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAtan2MethodSerializers()
        {
            if (method.Is(MathMethod.Atan2))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAverageMethodSerializers()
        {
            if (method.IsOneOf(__averageMethods))
            {
                if (method.IsOneOf(__averageWithSelectorMethods))
                {
                    var sourceExpression = arguments[0];
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var selectorSourceItemParameter = selectorLambda.Parameters[0];

                    DeduceItemAndCollectionSerializers(selectorSourceItemParameter, sourceExpression);
                }

                DeduceReturnsNumericOrNullableNumericSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceCeilingOrFloorMethodSerializers()
        {
            if (method.IsOneOf(MathMethod.CeilingWithDecimal, MathMethod.CeilingWithDouble, MathMethod.FloorWithDecimal,  MathMethod.FloorWithDouble))
            {
                DeduceReturnsNumericSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceCompareToMethodSerializers()
        {
            if (IsCompareToMethod())
            {
                var objectExpression = node.Object;
                var valueExpression = arguments[0];

                DeduceSerializers(objectExpression, valueExpression);
                DeduceReturnsInt32Serializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }

            bool IsCompareToMethod()
            {
                return
                    method.IsPublic &&
                    method.IsStatic == false &&
                    method.ReturnType == typeof(int) &&
                    method.Name == "CompareTo" &&
                    arguments.Count == 1 &&
                    arguments[0].Type == node.Object.Type;
            }
        }

        void DeduceConcatMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.Concat, QueryableMethod.Concat))
            {
                var firstExpression = arguments[0];
                var secondExpression = arguments[1];

                DeduceCollectionAndCollectionSerializers(firstExpression, secondExpression);
                DeduceCollectionAndCollectionSerializers(node, firstExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceConstantMethodSerializers()
        {
            if (method.IsOneOf(MqlMethod.ConstantWithRepresentation, MqlMethod.ConstantWithSerializer))
            {
                var valueExpression = arguments[0];
                IBsonSerializer serializer = null;

                if (IsNotKnown(node) || IsNotKnown(valueExpression))
                {
                    if (method.Is(MqlMethod.ConstantWithRepresentation))
                    {
                        var representationExpression = arguments[1];

                        var representation = representationExpression.GetConstantValue<BsonType>(node);
                        var defaultSerializer = BsonSerializer.LookupSerializer(valueExpression.Type); // TODO: don't use BsonSerializer
                        if (defaultSerializer is IRepresentationConfigurable representationConfigurableSerializer)
                        {
                            serializer = representationConfigurableSerializer.WithRepresentation(representation);
                        }
                    }
                    else if (method.Is(MqlMethod.ConstantWithSerializer))
                    {
                        var serializerExpression = arguments[1];
                        serializer = serializerExpression.GetConstantValue<IBsonSerializer>(node);
                    }
                }

                DeduceSerializer(valueExpression, serializer);
                DeduceSerializer(node, serializer);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceContainsKeyMethodSerializers()
        {
            if (IsDictionaryContainsKeyMethod(out var keyExpression))
            {
                var dictionaryExpression = node.Object;
                if (IsNotKnown(keyExpression) && IsKnown(dictionaryExpression, out var dictionarySerializer))
                {
                    var keySerializer = (dictionarySerializer as IBsonDictionarySerializer)?.KeySerializer;
                    AddKnownSerializer(keyExpression, keySerializer);
                }

                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceContainsMethodSerializers()
        {
            if (method.IsOneOf(__stringContainsMethods))
            {
                DeduceReturnsBooleanSerializer();
            }
            else if (IsCollectionContainsMethod(out var collectionExpression, out var itemExpression))
            {
                DeduceCollectionAndItemSerializers(collectionExpression, itemExpression);
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }

            bool IsCollectionContainsMethod(out Expression collectionExpression, out Expression itemExpression)
            {
                if (method.IsPublic &&
                    method.ReturnType == typeof(bool) &&
                    method.Name == "Contains" &&
                    method.GetParameters().Length == (method.IsStatic ? 2 : 1))
                {
                    collectionExpression = method.IsStatic ? arguments[0] : node.Object;
                    itemExpression = method.IsStatic ? arguments[1] : arguments[0];
                    return true;
                }

                collectionExpression = null;
                itemExpression = null;
                return false;
            }
        }

        void DeduceContainsValueMethodSerializers()
        {
            if (IsContainsValueInstanceMethod(out var collectionExpression, out var valueExpression))
            {
                if (IsNotKnown(valueExpression) &&
                    IsKnown(collectionExpression, out var collectionSerializer))
                {
                    if (collectionSerializer is IBsonDictionarySerializer dictionarySerializer)
                    {
                        var valueSerializer = dictionarySerializer.ValueSerializer;
                        AddKnownSerializer(valueExpression, valueSerializer);
                    }
                }

                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }

            bool IsContainsValueInstanceMethod(out Expression collectionExpression, out Expression valueExpression)
            {
                if (method.IsPublic &&
                    method.IsStatic == false &&
                    method.ReturnType == typeof(bool) &&
                    method.Name == "ContainsValue" &&
                    method.GetParameters() is var parameters &&
                    parameters.Length == 1)
                {
                    collectionExpression = node.Object;
                    valueExpression = arguments[0];
                    return true;
                }

                collectionExpression = null;
                valueExpression = null;
                return false;
            }
        }

        void DeduceCosMethodSerializers()
        {
            if (method.Is(MathMethod.Cos))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceCoshMethodSerializers()
        {
            if (method.Is(MathMethod.Cosh))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceCreateMethodSerializers()
        {
            if (method.IsOneOf(__tupleOrValueTupleCreateMethods))
            {
                if (AnyAreNotKnown(arguments) && IsKnown(node, out var nodeSerializer))
                {
                    if (nodeSerializer is IBsonTupleSerializer tupleSerializer)
                    {
                        for (var i = 1; i <= arguments.Count; i++)
                        {
                            var argumentExpression = arguments[i];
                            if (IsNotKnown(argumentExpression))
                            {
                                var itemSerializer = tupleSerializer.GetItemSerializer(i);
                                if (i == 8)
                                {
                                    itemSerializer = (itemSerializer as IBsonTupleSerializer)?.GetItemSerializer(1);
                                }
                                AddKnownSerializer(argumentExpression, itemSerializer);
                            }
                        }
                    }
                }

                if (IsNotKnown(node) && AllAreKnown(arguments, out var argumentSerializers))
                {
                    if (arguments.Count == 8)
                    {
                        var tempList = new List<IBsonSerializer>(argumentSerializers);
                        tempList[7] = method.ReturnType.Name.StartsWith("ValueTuple") ?
                            ValueTupleSerializer.Create([argumentSerializers[7]]) :
                            TupleSerializer.Create([argumentSerializers[7]]);
                        argumentSerializers = tempList;
                    }

                    var resultSerializer = method.ReturnType.Name.StartsWith("ValueTuple") ?
                        ValueTupleSerializer.Create(argumentSerializers) :
                        TupleSerializer.Create(argumentSerializers);
                    AddKnownSerializer(node, resultSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceCountMethodSerializers()
        {
            if (method.IsOneOf(__countMethods))
            {
                if (method.IsOneOf(__countWithPredicateMethods))
                {
                    var sourceExpression = arguments[0];
                    var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var predicateParameter = predicateLambda.Parameters.Single();
                    DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
                }

                DeduceReturnsNumericSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceDefaultIfEmptyMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.DefaultIfEmpty, EnumerableMethod.DefaultIfEmptyWithDefaultValue, QueryableMethod.DefaultIfEmpty, QueryableMethod.DefaultIfEmptyWithDefaultValue))
            {
                var sourceExpression = arguments[0];

                if (method.IsOneOf(EnumerableMethod.DefaultIfEmptyWithDefaultValue, QueryableMethod.DefaultIfEmptyWithDefaultValue))
                {
                    var defaultValueExpression = arguments[1];
                    DeduceItemAndCollectionSerializers(defaultValueExpression, sourceExpression);
                }

                DeduceCollectionAndCollectionSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceDegreesToRadiansMethodSerializers()
        {
            if (method.Is(MongoDBMathMethod.DegreesToRadians))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceDistinctMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.Distinct, QueryableMethod.Distinct))
            {
                var sourceExpression = arguments[0];
                DeduceCollectionAndCollectionSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceDocumentsMethodSerializers()
        {
            if (method.IsOneOf(MongoQueryableMethod.Documents, MongoQueryableMethod.DocumentsWithSerializer))
            {
                if (IsNotKnown(node))
                {
                    IBsonSerializer documentSerializer;
                    if (method.Is(MongoQueryableMethod.DocumentsWithSerializer))
                    {
                        var documentSerializerExpression = arguments[2];
                        documentSerializer = documentSerializerExpression.GetConstantValue<IBsonSerializer>(node);
                    }
                    else
                    {
                        var documentsParameter = method.GetParameters()[1];
                        var documentType = documentsParameter.ParameterType.GetElementType();
                        documentSerializer = BsonSerializer.LookupSerializer(documentType); // TODO: don't use static registry
                    }

                    var nodeSerializer = IQueryableSerializer.Create(documentSerializer);
                    AddKnownSerializer(node, nodeSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceElementAtMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.ElementAt, EnumerableMethod.ElementAtOrDefault, QueryableMethod.ElementAt, QueryableMethod.ElementAtOrDefault, QueryableMethod.ElementAtOrDefault))
            {
                var sourceExpression = arguments[0];
                DeduceItemAndCollectionSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceEqualsMethodSerializers()
        {
            if (IsEqualsReturningBooleanMethod(out var expression1, out var expression2))
            {
                DeduceSerializers(expression1, expression2);
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }

            bool IsEqualsReturningBooleanMethod(out Expression expression1, out Expression expression2)
            {
                if (method.Name == "Equals" &&
                    method.ReturnType == typeof(bool) &&
                    method.IsPublic)
                {
                    if (method.IsStatic &&
                        arguments.Count == 2)
                    {
                        expression1 = arguments[0];
                        expression2 = arguments[1];
                        return true;
                    }

                    if (!method.IsStatic &&
                        arguments.Count == 1)
                    {
                        expression1 = node.Object;
                        expression2 = arguments[0];
                        return true;
                    }

                    if (method.Is(StringMethod.EqualsWithComparisonType))
                    {
                        expression1 = node.Object;
                        expression2 = arguments[0];
                        return true;
                    }

                    if (method.Is(StringMethod.StaticEqualsWithComparisonType))
                    {
                        expression1 = arguments[0];
                        expression2 = arguments[1];
                        return true;
                    }
                }

                expression1 = null;
                expression2 = null;
                return false;
            }
        }

        void DeduceExceptMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.Except, QueryableMethod.Except))
            {
                var firstExpression =  arguments[0];
                var secondExpression = arguments[1];
                DeduceCollectionAndCollectionSerializers(secondExpression, firstExpression);
                DeduceCollectionAndCollectionSerializers(node, firstExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceExistsMethodSerializers()
        {
            if (method.Is(ArrayMethod.Exists) || ListMethod.IsExistsMethod(method))
            {
                var collectionExpression = method.IsStatic ? arguments[0] : node.Object;
                var predicateExpression = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, method.IsStatic ? arguments[1] : arguments[0]);
                var predicateParameter = predicateExpression.Parameters.Single();
                DeduceItemAndCollectionSerializers(predicateParameter, collectionExpression);
                DeduceReturnsBooleanSerializer();
            }
            else if (method.Is(MqlMethod.Exists))
            {
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceExpMethodSerializers()
        {
            if (method.IsOneOf(MathMethod.Exp))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceFieldMethodSerializers()
        {
            if (method.Is(MqlMethod.Field))
            {
                if (IsNotKnown(node))
                {
                    var fieldSerializerExpression = arguments[2];
                    var fieldSerializer = fieldSerializerExpression.GetConstantValue<IBsonSerializer>(node);
                    if (fieldSerializer == null)
                    {
                        throw new ExpressionNotSupportedException(node, because: "fieldSerializer is null");
                    }

                    AddKnownSerializer(node, fieldSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceFirstOrLastMethodsSerializers()
        {
            if (method.IsOneOf(__firstOrLastMethods))
            {
                if (method.IsOneOf(__firstOrLastWithPredicateMethods))
                {
                    var sourceExpression = arguments[0];
                    var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var predicateParameter = predicateLambda.Parameters.Single();
                    DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
                }

                DeduceReturnsOneSourceItemSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceGetItemMethodSerializers()
        {
            if (IsNotKnown(node))
            {
                if (BsonValueMethod.IsGetItemWithIntMethod(method) || BsonValueMethod.IsGetItemWithStringMethod(method))
                {
                    AddKnownSerializer(node, BsonValueSerializer.Instance);
                }
                else if (IsInstanceGetItemMethod(out var collectionExpression, out var indexExpression))
                {
                    if (IsKnown(collectionExpression, out var collectionSerializer))
                    {
                        if (collectionSerializer is IBsonArraySerializer arraySerializer &&
                            indexExpression.Type == typeof(int) &&
                            arraySerializer.GetItemSerializer() is var itemSerializer &&
                            itemSerializer.ValueType == method.ReturnType)
                        {
                            AddKnownSerializer(node, itemSerializer);
                        }
                        else if (
                            collectionSerializer is IBsonDictionarySerializer dictionarySerializer &&
                            dictionarySerializer.KeySerializer is var keySerializer &&
                            dictionarySerializer.ValueSerializer is var valueSerializer &&
                            keySerializer.ValueType == indexExpression.Type &&
                            valueSerializer.ValueType == method.ReturnType)
                        {
                            AddKnownSerializer(node, valueSerializer);
                        }
                    }
                }
                else
                {
                    DeduceUnknownMethodSerializer();
                }
            }

            bool IsInstanceGetItemMethod(out Expression collectionExpression, out Expression indexExpression)
            {
                if (method.IsStatic == false &&
                    method.Name == "get_Item")
                {
                    collectionExpression = node.Object;
                    indexExpression = arguments[0];
                    return true;
                }

                collectionExpression = null;
                indexExpression = null;
                return false;
            }
        }

        void DeduceGroupByMethodSerializers()
        {
            if (method.IsOneOf(__groupByMethods))
            {
                var sourceExpression = arguments[0];
                var keySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var keySelectorParameter = keySelectorLambda.Parameters.Single();

                DeduceItemAndCollectionSerializers(keySelectorParameter, sourceExpression);

                if (method.IsOneOf(EnumerableMethod.GroupByWithKeySelector, QueryableMethod.GroupByWithKeySelector))
                {
                    if (IsNotKnown(node) && IsKnown(keySelectorLambda.Body, out var keySerializer) && IsItemSerializerKnown(sourceExpression, out var elementSerializer))
                    {
                        var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);
                        var nodeSerializer = IEnumerableOrIQueryableSerializer.Create(node.Type, groupingSerializer);
                        AddKnownSerializer(node, nodeSerializer);
                    }
                }
                else if (method.IsOneOf(EnumerableMethod.GroupByWithKeySelectorAndElementSelector, QueryableMethod.GroupByWithKeySelectorAndElementSelector))
                {
                    var elementSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var elementSelectorParameter = elementSelectorLambda.Parameters.Single();
                    DeduceItemAndCollectionSerializers(elementSelectorParameter, sourceExpression);
                    if (IsNotKnown(node) && IsKnown(keySelectorLambda.Body, out var keySerializer) && IsKnown(elementSelectorLambda.Body, out var elementSerializer))
                    {
                        var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);
                        var nodeSerializer = IEnumerableOrIQueryableSerializer.Create(node.Type, groupingSerializer);
                        AddKnownSerializer(node, nodeSerializer);
                    }
                }
                else if (method.IsOneOf(EnumerableMethod.GroupByWithKeySelectorAndResultSelector, QueryableMethod.GroupByWithKeySelectorAndResultSelector))
                {
                    var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var resultSelectorKeyParameter = resultSelectorLambda.Parameters[0];
                    var resultSelectorElementsParameter = resultSelectorLambda.Parameters[1];
                    DeduceItemAndCollectionSerializers(keySelectorParameter, sourceExpression);
                    DeduceSerializers(resultSelectorKeyParameter, keySelectorLambda.Body);
                    DeduceCollectionAndCollectionSerializers(resultSelectorElementsParameter, sourceExpression);
                    DeduceResultSerializer(resultSelectorLambda.Body);
                }
                else if (method.IsOneOf(EnumerableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector, QueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector))
                {
                    var elementSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var elementSelectorParameter = elementSelectorLambda.Parameters.Single();
                    var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[3]);
                    var resultSelectorKeyParameter = resultSelectorLambda.Parameters[0];
                    var resultSelectorElementsParameter = resultSelectorLambda.Parameters[1];
                    DeduceItemAndCollectionSerializers(keySelectorParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(elementSelectorParameter, sourceExpression);
                    DeduceSerializers(resultSelectorKeyParameter, keySelectorLambda.Body);
                    DeduceCollectionAndItemSerializers(resultSelectorElementsParameter, elementSelectorLambda.Body);
                    DeduceResultSerializer(resultSelectorLambda.Body);
                }

                void DeduceResultSerializer(Expression resultExpression)
                {
                    if (IsNotKnown(node) && IsKnown(resultExpression, out var resultSerializer))
                    {
                        var nodeSerializer = IEnumerableOrIQueryableSerializer.Create(node.Type, resultSerializer);
                        AddKnownSerializer(node, nodeSerializer);
                    }
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceGroupJoinMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.GroupJoin, QueryableMethod.GroupJoin))
            {
                var outerExpression = arguments[0];
                var innerExpression = arguments[1];
                var outerKeySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                var outerKeySelectorItemParameter = outerKeySelectorLambda.Parameters.Single();
                var innerKeySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[3]);
                var innerKeySelectorItemParameter = innerKeySelectorLambda.Parameters.Single();
                var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[4]);
                var resultSelectorOuterItemParameter = resultSelectorLambda.Parameters[0];
                var resultSelectorInnerItemsParameter = resultSelectorLambda.Parameters[1];

                DeduceItemAndCollectionSerializers(outerKeySelectorItemParameter, outerExpression);
                DeduceItemAndCollectionSerializers(innerKeySelectorItemParameter, innerExpression);
                DeduceItemAndCollectionSerializers(resultSelectorOuterItemParameter, outerExpression);
                DeduceCollectionAndCollectionSerializers(resultSelectorInnerItemsParameter, innerExpression);
                DeduceCollectionAndItemSerializers(node, resultSelectorLambda.Body);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceIndexOfMethodSerializers()
        {
            if (method.IsOneOf(__indexOfMethods))
            {
                DeduceReturnsInt32Serializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceInjectMethodSerializers()
        {
            if (method.Is(LinqExtensionsMethod.Inject))
            {
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceIntersectMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.Intersect, QueryableMethod.Intersect))
            {
                var sourceExpression = arguments[0];
                DeduceCollectionAndCollectionSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceIsMatchMethodSerializers()
        {
            if (method.Is(RegexMethod.StaticIsMatch))
            {
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceIsMissingOrIsNullOrMissingMethodSerializers()
        {
            if (method.IsOneOf(MqlMethod.IsMissing, MqlMethod.IsNullOrMissing))
            {
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceIsSubsetOfMethodSerializers()
        {
            if (IsSubsetOfMethod(method))
            {
                var objectExpression =  node.Object;
                var otherExpression = arguments[0];

                DeduceCollectionAndCollectionSerializers(objectExpression, otherExpression);
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }

            static bool IsSubsetOfMethod(MethodInfo method)
            {
                var declaringType = method.DeclaringType;
                var parameters = method.GetParameters();
                return
                    method.IsPublic &&
                    method.IsStatic == false &&
                    method.ReturnType == typeof(bool) &&
                    method.Name == "IsSubsetOf" &&
                    parameters.Length == 1 &&
                    parameters[0] is var otherParameter &&
                    declaringType.ImplementsIEnumerable(out var declaringTypeItemType) &&
                    otherParameter.ParameterType.ImplementsIEnumerable(out var otherTypeItemType) &&
                    otherTypeItemType == declaringTypeItemType;
            }
        }

        void DeduceJoinMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.Join, QueryableMethod.Join))
            {
                var outerExpression = arguments[0];
                var innerExpression = arguments[1];
                var outerKeySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                var outerKeySelectorItemParameter = outerKeySelectorLambda.Parameters.Single();
                var innerKeySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[3]);
                var innerKeySelectorItemParameter = innerKeySelectorLambda.Parameters.Single();
                var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[4]);
                var resultSelectorOuterItemParameter = resultSelectorLambda.Parameters[0];
                var resultSelectorInnerItemsParameter = resultSelectorLambda.Parameters[1];

                DeduceItemAndCollectionSerializers(outerKeySelectorItemParameter, outerExpression);
                DeduceItemAndCollectionSerializers(innerKeySelectorItemParameter, innerExpression);
                DeduceItemAndCollectionSerializers(resultSelectorOuterItemParameter, outerExpression);
                DeduceItemAndCollectionSerializers(resultSelectorInnerItemsParameter, innerExpression);
                DeduceCollectionAndItemSerializers(node, resultSelectorLambda.Body);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceIsNullOrEmptyMethodSerializers()
        {
            if (method.Is(StringMethod.IsNullOrEmpty))
            {
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceLogMethodSerializers()
        {
            if (method.IsOneOf(__logMethods))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceLookupMethodSerializers()
        {
            if (method.IsOneOf(__lookupMethods))
            {
                var sourceExpression = arguments[0];

                if (method.Is(MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignField))
                {
                    var documentsLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var documentsLambdaParameter = documentsLambda.Parameters.Single();
                    var localFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var localFieldLambdaParameter = localFieldLambda.Parameters.Single();
                    var foreignFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[3]);
                    var foreignFieldLambdaParameter = foreignFieldLambda.Parameters.Single();

                    DeduceItemAndCollectionSerializers(documentsLambdaParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(localFieldLambdaParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(foreignFieldLambdaParameter, documentsLambda.Body);

                    if (IsNotKnown(node) &&
                        IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                        IsItemSerializerKnown(documentsLambda.Body, out var documentSerializer))
                    {
                        var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, documentSerializer);
                        AddKnownSerializer(node, IQueryableSerializer.Create(lookupResultSerializer));
                    }
                }
                else if (method.Is(MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignFieldAndPipeline))
                {
                    var documentsLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var documentsLambdaParameter = documentsLambda.Parameters.Single();
                    var localFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var localFieldLambdaParameter = localFieldLambda.Parameters.Single();
                    var foreignFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[3]);
                    var foreignFieldLambdaParameter = foreignFieldLambda.Parameters.Single();
                    var pipelineLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[4]);
                    var pipelineLambdaLocalParameter = pipelineLambda.Parameters[0];
                    var pipelineLambdaForeignQueryableParameter = pipelineLambda.Parameters[1];

                    DeduceItemAndCollectionSerializers(documentsLambdaParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(localFieldLambdaParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(foreignFieldLambdaParameter, documentsLambda.Body);
                    DeduceItemAndCollectionSerializers(pipelineLambdaLocalParameter, sourceExpression);
                    DeduceCollectionAndCollectionSerializers(pipelineLambdaForeignQueryableParameter, documentsLambda.Body);

                    if (IsNotKnown(node) &&
                        IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                        IsItemSerializerKnown(pipelineLambda.Body, out var pipelineDocumentSerializer))
                    {
                        var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, pipelineDocumentSerializer);
                        AddKnownSerializer(node, IQueryableSerializer.Create(lookupResultSerializer));
                    }
                }
                else if (method.Is(MongoQueryableMethod.LookupWithDocumentsAndPipeline))
                {
                    var documentsLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var documentsLambdaParameter = documentsLambda.Parameters.Single();
                    var pipelineLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var pipelineLambdaSourceParameter = pipelineLambda.Parameters[0];
                    var pipelineLambdaQueryableDocumentParameter = pipelineLambda.Parameters[1];

                    DeduceItemAndCollectionSerializers(documentsLambdaParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(pipelineLambdaSourceParameter, sourceExpression);
                    DeduceCollectionAndCollectionSerializers(pipelineLambdaQueryableDocumentParameter, documentsLambda.Body);

                    if (IsNotKnown(node) &&
                        IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                        IsItemSerializerKnown(pipelineLambda.Body, out var pipelineItemSerializer))
                    {
                        var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, pipelineItemSerializer);
                        AddKnownSerializer(node, IQueryableSerializer.Create(lookupResultSerializer));
                    }
                }

                if (method.Is(MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignField))
                {
                    var fromExpression = arguments[1];
                    var fromCollection = fromExpression.GetConstantValue<IMongoCollection>(node);
                    var foreignDocumentSerializer = fromCollection.DocumentSerializer;
                    var localFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var localFieldLambdaParameter = localFieldLambda.Parameters.Single();
                    var foreignFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[3]);
                    var foreignFieldLambdaParameter = foreignFieldLambda.Parameters.Single();

                    DeduceItemAndCollectionSerializers(localFieldLambdaParameter, sourceExpression);
                    DeduceSerializer(foreignFieldLambdaParameter, foreignDocumentSerializer);

                    if (IsNotKnown(node) &&
                        IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer))
                    {
                        var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, foreignDocumentSerializer);
                        AddKnownSerializer(node, IQueryableSerializer.Create(lookupResultSerializer));
                    }
                }
                else if (method.Is(MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignFieldAndPipeline))
                {
                    var fromExpression = arguments[1];
                    var fromCollection = fromExpression.GetConstantValue<IMongoCollection>(node);
                    var foreignDocumentSerializer = fromCollection.DocumentSerializer;
                    var localFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var localFieldLambdaParameter = localFieldLambda.Parameters.Single();
                    var foreignFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[3]);
                    var foreignFieldLambdaParameter = foreignFieldLambda.Parameters.Single();
                    var pipelineLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[4]);
                    var pipelineLambdaLocalParameter = pipelineLambda.Parameters[0];
                    var pipelineLamdbaForeignQueryableParameter = pipelineLambda.Parameters[1];

                    DeduceItemAndCollectionSerializers(localFieldLambdaParameter, sourceExpression);
                    DeduceSerializer(foreignFieldLambdaParameter, foreignDocumentSerializer);
                    DeduceItemAndCollectionSerializers(pipelineLambdaLocalParameter, sourceExpression);

                    if (IsNotKnown(pipelineLamdbaForeignQueryableParameter))
                    {
                        var foreignQueryableSerializer = IQueryableSerializer.Create(foreignDocumentSerializer);
                        AddKnownSerializer(pipelineLamdbaForeignQueryableParameter, foreignQueryableSerializer);
                    }

                    if (IsNotKnown(node) &&
                        IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                        IsItemSerializerKnown(pipelineLambda.Body, out var pipelineItemSerializer))
                    {
                        var lookupResultsSerializer = LookupResultSerializer.Create(sourceItemSerializer, pipelineItemSerializer);
                        AddKnownSerializer(node, IQueryableSerializer.Create(lookupResultsSerializer));
                    }
                }
                else if (method.Is(MongoQueryableMethod.LookupWithFromAndPipeline))
                {
                    var fromCollection = arguments[1].GetConstantValue<IMongoCollection>(node);
                    var foreignDocumentSerializer = fromCollection.DocumentSerializer;
                    var pipelineLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var pipelineLambdaLocalParameter = pipelineLambda.Parameters[0];
                    var pipelineLamdbaForeignQueryableParameter = pipelineLambda.Parameters[1];

                    DeduceItemAndCollectionSerializers(pipelineLambdaLocalParameter, sourceExpression);

                    if (IsNotKnown(pipelineLamdbaForeignQueryableParameter))
                    {
                        var foreignQueryableSerializer = IQueryableSerializer.Create(foreignDocumentSerializer);
                        AddKnownSerializer(pipelineLamdbaForeignQueryableParameter, foreignQueryableSerializer);
                    }

                    if (IsNotKnown(node) &&
                        IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                        IsItemSerializerKnown(pipelineLambda.Body, out var pipelineItemSerializer))
                    {
                        var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, pipelineItemSerializer);
                        AddKnownSerializer(node, IQueryableSerializer.Create(lookupResultSerializer));
                    }
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceMatchingElementsMethodSerializers()
        {
            if (method.IsOneOf(MongoEnumerableMethod.AllElements, MongoEnumerableMethod.AllMatchingElements, MongoEnumerableMethod.FirstMatchingElement))
            {
                DeduceReturnsOneSourceItemSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceMaxOrMinMethodSerializers()
        {
            if (method.IsOneOf(__maxOrMinMethods))
            {
                if (method.IsOneOf(__maxOrMinWithSelectorMethods))
                {
                    var sourceExpression = arguments[0];
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var selectorItemParameter = selectorLambda.Parameters.Single();

                    DeduceItemAndCollectionSerializers(selectorItemParameter, sourceExpression);
                    DeduceSerializers(node, selectorLambda.Body);
                }
                else
                {
                    DeduceReturnsOneSourceItemSerializer();
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceOfTypeMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.OfType, QueryableMethod.OfType))
            {
                var sourceExpression = arguments[0];
                var resultType = method.GetGenericArguments()[0];

                if (IsNotKnown(node) && IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer))
                {
                    var resultItemSerializer = sourceItemSerializer.GetDerivedTypeSerializer(resultType);
                    var resultSerializer = IEnumerableOrIQueryableSerializer.Create(node.Type, resultItemSerializer);
                    AddKnownSerializer(node, resultSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceOrderByMethodSerializers()
        {
            if (method.IsOneOf(
                    EnumerableMethod.OrderBy,
                    EnumerableMethod.OrderByDescending,
                    EnumerableMethod.ThenBy,
                    EnumerableMethod.ThenByDescending,
                    QueryableMethod.OrderBy,
                    QueryableMethod.OrderByDescending,
                    QueryableMethod.ThenBy,
                    QueryableMethod.ThenByDescending))
            {
                var sourceExpression = arguments[0];
                var keySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var keySelectorParameter = keySelectorLambda.Parameters.Single();

                DeduceItemAndCollectionSerializers(keySelectorParameter, sourceExpression);
                DeduceCollectionAndCollectionSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeducePickMethodSerializers()
        {
            if (method.IsOneOf(__pickMethods))
            {
                if (method.IsOneOf(__pickWithSortDefinitionMethods))
                {
                    var sortByExpression = arguments[1];
                    if (IsNotKnown(sortByExpression))
                    {
                        var ignoreSubTreeSerializer = IgnoreSubtreeSerializer.Create(sortByExpression.Type);
                        AddKnownSerializer(sortByExpression, ignoreSubTreeSerializer);
                    }
                }

                var sourceExpression = arguments[0];
                if (IsKnown(sourceExpression, out var sourceSerializer))
                {
                    var sourceItemSerializer =  ArraySerializerHelper.GetItemSerializer(sourceSerializer);

                    var selectorExpression = arguments[method.IsOneOf(__pickWithSortDefinitionMethods) ? 2 : 1];
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, selectorExpression);
                    var selectorSourceItemParameter = selectorLambda.Parameters.Single();
                    if (IsNotKnown(selectorSourceItemParameter))
                    {
                        AddKnownSerializer(selectorSourceItemParameter, sourceItemSerializer);
                    }
                }

                if (method.IsOneOf(__pickWithComputedNMethods))
                {
                    var keyExpression = arguments[method.IsOneOf(__pickWithSortDefinitionMethods) ? 3 : 2];
                    if (IsKnown(keyExpression, out var keySerializer))
                    {
                        var nExpression = arguments[method.IsOneOf(__pickWithSortDefinitionMethods) ? 4 : 3];
                        var nLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, nExpression);
                        var nLambdaKeyParameter = nLambda.Parameters.Single();

                        if (IsNotKnown(nLambdaKeyParameter))
                        {
                            AddKnownSerializer(nLambdaKeyParameter, keySerializer);
                        }
                    }
                }

                if (IsNotKnown(node))
                {
                    var selectorExpressionIndex = method switch
                    {
                        _ when method.Is(EnumerableMethod.Bottom) => 2,
                        _ when method.Is(EnumerableMethod.BottomN) => 2,
                        _ when method.Is(EnumerableMethod.BottomNWithComputedN) => 2,
                        _ when method.Is(EnumerableMethod.FirstN) => 1,
                        _ when method.Is(EnumerableMethod.FirstNWithComputedN) => 1,
                        _ when method.Is(EnumerableMethod.LastN) => 1,
                        _ when method.Is(EnumerableMethod.LastNWithComputedN) => 1,
                        _ when method.Is(EnumerableMethod.MaxN) => 1,
                        _ when method.Is(EnumerableMethod.MaxNWithComputedN) => 1,
                        _ when method.Is(EnumerableMethod.MinN) => 1,
                        _ when method.Is(EnumerableMethod.MinNWithComputedN) => 1,
                        _ when method.Is(EnumerableMethod.Top) => 2,
                        _ when method.Is(EnumerableMethod.TopN) => 2,
                        _ when method.Is(EnumerableMethod.TopNWithComputedN) => 2,
                        _ => throw new ArgumentException($"Unrecognized method: {method.Name}.")
                    };
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[selectorExpressionIndex]);

                    if (IsKnown(selectorLambda.Body, out var selectorItemSerializer))
                    {
                        var nodeSerializer = method.IsOneOf(EnumerableMethod.Bottom, EnumerableMethod.Top) ?
                            selectorItemSerializer :
                            IEnumerableSerializer.Create(selectorItemSerializer);
                        AddKnownSerializer(node, nodeSerializer);
                    }
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceParseMethodSerializers()
        {
            if (IsNotKnown(node))
            {
                if (IsParseMethod(method))
                {
                    var nodeSerializer = GetParseResultSerializer(method.DeclaringType);
                    AddKnownSerializer(node, nodeSerializer);
                }
                else
                {
                    DeduceUnknownMethodSerializer();
                }
            }

            static bool IsParseMethod(MethodInfo method)
            {
                var parameters = method.GetParameters();
                return
                    method.IsPublic &&
                    method.IsStatic &&
                    method.ReturnType == method.DeclaringType &&
                    parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(string);
            }

            static IBsonSerializer GetParseResultSerializer(Type declaringType)
            {
                return declaringType switch
                {
                    _ when declaringType == typeof(DateTime) => DateTimeSerializer.Instance,
                    _ when declaringType == typeof(decimal) => DecimalSerializer.Instance,
                    _ when declaringType == typeof(double) => DoubleSerializer.Instance,
                    _ when declaringType == typeof(int) => Int32Serializer.Instance,
                    _ when declaringType == typeof(short) => Int64Serializer.Instance,
                    _ => UnknowableSerializer.Create(declaringType)
                };
            }
        }

        void DeducePowMethodSerializers()
        {
            if (method.IsOneOf(MathMethod.Pow))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceRadiansToDegreesMethodSerializers()
        {
            if (method.Is(MongoDBMathMethod.RadiansToDegrees))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceReturnsBooleanSerializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, BooleanSerializer.Instance);
            }
        }

        void DeduceReturnsDateTimeSerializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, DateTimeSerializer.UtcInstance);
            }
        }

        void DeduceReturnsDecimalSerializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, DecimalSerializer.Instance);
            }
        }

        void DeduceReturnsDoubleSerializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, DoubleSerializer.Instance);
            }
        }

        void DeduceReturnsInt32Serializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, Int32Serializer.Instance);
            }
        }

        void DeduceReturnsInt64Serializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, Int64Serializer.Instance);
            }
        }

        void DeduceReturnsNullableDecimalSerializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, NullableSerializer.NullableDecimalInstance);
            }
        }

        void DeduceReturnsNullableDoubleSerializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, NullableSerializer.NullableDoubleInstance);
            }
        }

        void DeduceReturnsNullableInt32Serializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, NullableSerializer.NullableInt32Instance);
            }
        }

        void DeduceReturnsNullableInt64Serializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, NullableSerializer.NullableInt64Instance);
            }
        }

        void DeduceReturnsNullableSingleSerializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, NullableSerializer.NullableSingleInstance);
            }
        }

        void DeduceReturnsNumericSerializer()
        {
            if (IsNotKnown(node) && node.Type.IsNumeric())
            {
                var numericSerializer = StandardSerializers.GetSerializer(node.Type);
                AddKnownSerializer(node, numericSerializer);
            }
        }

        void DeduceReturnsNumericOrNullableNumericSerializer()
        {
            if (IsNotKnown(node) && node.Type.IsNumericOrNullableNumeric())
            {
                var numericSerializer = StandardSerializers.GetSerializer(node.Type);
                AddKnownSerializer(node, numericSerializer);
            }
        }

        void DeduceReturnsOneSourceItemSerializer()
        {
            var sourceExpression = arguments[0];

            if (IsNotKnown(node) && IsKnown(sourceExpression, out var sourceSerializer))
            {
                var nodeSerializer = sourceSerializer is IUnknowableSerializer ?
                    UnknowableSerializer.Create(node.Type) :
                    ArraySerializerHelper.GetItemSerializer(sourceSerializer);
                AddKnownSerializer(node, nodeSerializer);
            }
        }

        void DeduceReturnsSingleSerializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, DoubleSerializer.Instance);
            }
        }

        void DeduceReturnsStringSerializer()
        {
            if (IsNotKnown(node))
            {
                AddKnownSerializer(node, StringSerializer.Instance);
            }
        }

        void DeduceReturnsTimeSpanSerializer(TimeSpanUnits units)
        {
            if (IsNotKnown(node))
            {
                var resultSerializer = new TimeSpanSerializer(BsonType.Int64, units);
                AddKnownSerializer(node, resultSerializer);
            }
        }

        void DeduceRangeMethodSerializers()
        {
            if (method.Is(EnumerableMethod.Range))
            {
                var elementExpression = arguments[0];
                DeduceCollectionAndItemSerializers(node, elementExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceRepeatMethodSerializers()
        {
            if (method.Is(EnumerableMethod.Repeat))
            {
                var elementExpression = arguments[0];
                DeduceCollectionAndItemSerializers(node, elementExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceReverseMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.Reverse, QueryableMethod.Reverse))
            {
                var sourceExpression = arguments[0];
                DeduceCollectionAndCollectionSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSelectMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.Select, QueryableMethod.Select))
            {
                var sourceExpression = arguments[0];
                var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var selectorParameter = selectorLambda.Parameters.Single();
                DeduceItemAndCollectionSerializers(selectorParameter, sourceExpression);
                DeduceCollectionAndItemSerializers(node, selectorLambda.Body);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSelectManySerializers()
        {
            if (method.IsOneOf(__selectManyMethods))
            {
                var sourceExpression = arguments[0];

                if (method.IsOneOf(__selectManyWithResultSelectorMethods))
                {
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var selectorSourceParameter = selectorLambda.Parameters.Single();

                    DeduceItemAndCollectionSerializers(selectorSourceParameter, sourceExpression);
                    DeduceCollectionAndCollectionSerializers(node, selectorLambda.Body);
                }

                if (method.IsOneOf(__selectManyWithCollectionSelectorAndResultSelectorMethods))
                {
                    var collectionSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var resultSelectorLambda =  ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);

                    var collectionSelectorSourceItemParameter = collectionSelectorLambda.Parameters.Single();
                    var resultSelectorSourceItemParameter = resultSelectorLambda.Parameters[0];
                    var resultSelectorCollectionItemParameter = resultSelectorLambda.Parameters[1];

                    DeduceItemAndCollectionSerializers(collectionSelectorSourceItemParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(resultSelectorSourceItemParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(resultSelectorCollectionItemParameter, collectionSelectorLambda.Body);
                    DeduceCollectionAndItemSerializers(node, resultSelectorLambda.Body);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSequenceEqualMethodSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.SequenceEqual, QueryableMethod.SequenceEqual))
            {
                var source1Expression =  arguments[0];
                var source2Expression = arguments[1];

                DeduceCollectionAndCollectionSerializers(source1Expression, source2Expression);
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSetEqualsMethodSerializers()
        {
            if (IsSetEqualsMethod(method))
            {
                var objectExpression =  node.Object;
                var otherExpression = arguments[0];

                DeduceCollectionAndCollectionSerializers(objectExpression, otherExpression);
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }

            static bool IsSetEqualsMethod(MethodInfo method)
            {
                var declaringType = method.DeclaringType;
                var parameters = method.GetParameters();
                return
                    method.IsPublic &&
                    method.IsStatic == false &&
                    method.ReturnType == typeof(bool) &&
                    method.Name == "SetEquals" &&
                    parameters.Length == 1 &&
                    parameters[0] is var otherParameter &&
                    declaringType.ImplementsIEnumerable(out var declaringTypeItemType) &&
                    otherParameter.ParameterType.ImplementsIEnumerable(out var otherTypeItemType) &&
                    otherTypeItemType == declaringTypeItemType;
            }
        }

        void DeduceSinMethodSerializers()
        {
            if (method.Is(MathMethod.Sin))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSinhMethodSerializers()
        {
            if (method.Is(MathMethod.Sinh))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSplitMethodSerializers()
        {
            if (method.IsOneOf(__splitMethods))
            {
                if (IsNotKnown(node))
                {
                    var nodeSerializer = ArraySerializer.Create(StringSerializer.Instance);
                    AddKnownSerializer(node, nodeSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSqrtMethodSerializers()
        {
            if (method.Is(MathMethod.Sqrt))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceStandardDeviationMethodSerializers()
        {
            if (method.IsOneOf(__standardDeviationMethods))
            {
                if (method.IsOneOf(__standardDeviationWithSelectorMethods))
                {
                    var sourceExpression = arguments[0];
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var selectorItemParameter = selectorLambda.Parameters.Single();
                    DeduceItemAndCollectionSerializers(selectorItemParameter, sourceExpression);
                }

                DeduceReturnsNumericOrNullableNumericSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceEndsWithOrStartsWithMethodSerializers()
        {
            if (method.IsOneOf(__stringEndsWithOrStartsWithMethods))
            {
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceStringInMethodSerializers()
        {
            if (method.IsOneOf(StringMethod.StringInWithEnumerable, StringMethod.StringInWithParams))
            {
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceStrLenBytesMethodSerializers()
        {
            if (method.Is(StringMethod.StrLenBytes))
            {
                DeduceReturnsInt32Serializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSubstringMethodSerializers()
        {
            if (method.IsOneOf(StringMethod.Substring, StringMethod.SubstringWithLength, StringMethod.SubstrBytes))
            {
                DeduceReturnsStringSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSubtractMethodSerializers()
        {
            if (method.IsOneOf(__subtractReturningDateTimeMethods))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else if (method.IsOneOf(__subtractReturningInt64Methods))
            {
                DeduceReturnsInt64Serializer();
            }
            else if (method.IsOneOf(__subtractReturningTimeSpanWithMillisecondsUnitsMethods))
            {
                var units = TimeSpanUnits.Milliseconds;
                DeduceReturnsTimeSpanSerializer(units);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSumMethodSerializers()
        {
            if (method.IsOneOf(__sumMethods))
            {
                if (method.IsOneOf(__sumWithSelectorMethods))
                {
                    var sourceExpression = arguments[0];
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var selectorParameter = selectorLambda.Parameters.Single();
                    DeduceItemAndCollectionSerializers(selectorParameter, sourceExpression);
                }

                var returnType = node.Type;
                switch (returnType)
                {
                    case not null when returnType == typeof(decimal): DeduceReturnsDecimalSerializer(); break;
                    case not null when returnType == typeof(double): DeduceReturnsDoubleSerializer(); break;
                    case not null when returnType == typeof(int): DeduceReturnsInt32Serializer(); break;
                    case not null when returnType == typeof(long): DeduceReturnsInt64Serializer(); break;
                    case not null when returnType == typeof(float): DeduceReturnsSingleSerializer(); break;
                    case not null when returnType == typeof(decimal?): DeduceReturnsNullableDecimalSerializer(); break;
                    case not null when returnType == typeof(double?): DeduceReturnsNullableDoubleSerializer(); break;
                    case not null when returnType == typeof(int?): DeduceReturnsNullableInt32Serializer(); break;
                    case not null when returnType == typeof(long?): DeduceReturnsNullableInt64Serializer(); break;
                    case not null when returnType == typeof(float?): DeduceReturnsNullableSingleSerializer(); break;

                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSkipOrTakeMethodSerializers()
        {
            if (method.IsOneOf(__skipOrTakeMethods))
            {
                var sourceExpression = arguments[0];

                if (method.IsOneOf(__skipOrTakeWhileMethods))
                {
                    var predicateLambda =  ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var predicateParameter = predicateLambda.Parameters.Single();
                    DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
                }

                DeduceCollectionAndCollectionSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceTanMethodSerializers()
        {
            if (method.Is(MathMethod.Tan))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceTanhMethodSerializers()
        {
            if (method.Is(MathMethod.Tanh))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceToArrayMethodSerializers()
        {
            if (IsToArrayMethod(out var sourceExpression))
            {
                DeduceCollectionAndCollectionSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }

            bool IsToArrayMethod(out Expression sourceExpression)
            {
                if (method.IsPublic &&
                    method.Name == "ToArray" &&
                    method.GetParameters().Length == (method.IsStatic ? 1 : 0))
                {
                    sourceExpression = method.IsStatic ? arguments[0] : node.Object;
                    return true;
                }

                sourceExpression = null;
                return false;
            }
        }

        void DeduceToListSerializers()
        {
            if (IsNotKnown(node))
            {
                var source = method.IsStatic ? arguments[0] : node.Object;
                if (IsKnown(source, out var sourceSerializer))
                {
                    var sourceItemSerializer = ArraySerializerHelper.GetItemSerializer(sourceSerializer);
                    var resultSerializer = ListSerializer.Create(sourceItemSerializer);
                    AddKnownSerializer(node, resultSerializer);
                }
            }
        }

        void DeduceToLowerOrToUpperSerializers()
        {
            if (method.IsOneOf(__toLowerOrToUpperMethods))
            {
                DeduceReturnsStringSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceToStringSerializers()
        {
            DeduceReturnsStringSerializer();
        }

        void DeduceTruncateSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.Truncate, DateTimeMethod.TruncateWithBinSize, DateTimeMethod.TruncateWithBinSizeAndTimezone))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else if (method.IsOneOf(MathMethod.TruncateDecimal, MathMethod.TruncateDouble))
            {
                DeduceReturnsNumericSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceUnionSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.Union, QueryableMethod.Union))
            {
                var sourceExpression = arguments[0];
                DeduceCollectionAndCollectionSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceUnknownMethodSerializer()
        {
            DeduceUnknowableSerializer(node);
        }

        void DeduceWeekSerializers()
        {
            if (method.IsOneOf(DateTimeMethod.Week, DateTimeMethod.WeekWithTimezone))
            {
                if (IsNotKnown(node))
                {
                    AddKnownSerializer(node, Int32Serializer.Instance);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceWhereSerializers()
        {
            if (method.IsOneOf(__whereMethods))
            {
                var sourceExpression = arguments[0];
                var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var predicateParameter =  predicateLambda.Parameters.Single();
                DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
                DeduceCollectionAndCollectionSerializers(node, sourceExpression);
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceZipSerializers()
        {
            if (method.IsOneOf(EnumerableMethod.Zip, QueryableMethod.Zip))
            {
                var firstExpression = arguments[0];
                var secondExpression = arguments[1];
                var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                var resultSelectorFirstParameter = resultSelectorLambda.Parameters[0];
                var resultSelectorSecondParameter =  resultSelectorLambda.Parameters[1];

                if (IsNotKnown(resultSelectorFirstParameter) && IsKnown(firstExpression, out var firstSerializer))
                {
                    var firstItemSerializer =  ArraySerializerHelper.GetItemSerializer(firstSerializer);
                    AddKnownSerializer(resultSelectorFirstParameter, firstItemSerializer);
                }

                if (IsNotKnown(resultSelectorSecondParameter) && IsKnown(secondExpression, out var secondSerializer))
                {
                    var secondItemSerializer =  ArraySerializerHelper.GetItemSerializer(secondSerializer);
                    AddKnownSerializer(resultSelectorSecondParameter, secondItemSerializer);
                }

                if (IsNotKnown(node) && IsKnown(resultSelectorLambda.Body, out var resultItemSerializer))
                {
                    var resultSerializer = IEnumerableOrIQueryableSerializer.Create(node.Type, resultItemSerializer);
                    AddKnownSerializer(node, resultSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        bool IsDictionaryContainsKeyMethod(out Expression keyExpression)
        {
            if (method.DeclaringType.Name.Contains("Dictionary") &&
                method.IsPublic &&
                method.IsStatic == false &&
                method.ReturnType == typeof(bool) &&
                method.Name == "ContainsKey" &&
                method.GetParameters().Length == 1)
            {
                keyExpression = arguments[0];
                return true;
            }

            keyExpression = null;
            return false;
        }
    }
}

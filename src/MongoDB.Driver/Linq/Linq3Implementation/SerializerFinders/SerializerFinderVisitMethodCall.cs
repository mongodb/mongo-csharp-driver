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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var methodInfo = node.Method.IsGenericMethod && !node.Method.ContainsGenericParameters ? node.Method.GetGenericMethodDefinition() : node.Method;
        var deducer = MethodCallSerializerDeducers.GetSerializerDeducer(methodInfo);

        DeduceMethodCallSerializers();
        base.VisitMethodCall(node);
        DeduceMethodCallSerializers();

        return node;

        void DeduceMethodCallSerializers()
        {
            if (deducer == null)
            {
                DeduceUnknowableSerializer(node);
            }
            else
            {
                deducer(this, node);
            }
        }
    }

    private static class MethodCallSerializerDeducers
    {
        private static readonly Dictionary<MethodInfo, Action<SerializerFinderVisitor, MethodCallExpression>> __serializerDeducers = new();
        private static readonly ReaderWriterLockSlim __serializerDeducersLock = new();
        private static readonly IBsonSerializer __binarySubTypeSerializer = NullableSerializer.Create(new EnumSerializer<BsonBinarySubType>());

        private static readonly IReadOnlyMethodInfoSet __averageOrMedianOrPercentileOverloads = MethodInfoSet.Create(
        [
            EnumerableOrQueryableMethod.AverageOverloads,
            MongoEnumerableMethod.MedianOverloads,
            MongoEnumerableMethod.PercentileOverloads
        ]);

        private static readonly IReadOnlyMethodInfoSet __averageOrMedianOrPercentileWindowMethodOverloads = MethodInfoSet.Create(
        [
            WindowMethod.AverageOverloads,
            WindowMethod.MedianOverloads,
            WindowMethod.PercentileOverloads
        ]);

        private static readonly IReadOnlyMethodInfoSet __averageOrMedianOrPercentileWithSelectorOverloads = MethodInfoSet.Create(
        [
            EnumerableOrQueryableMethod.AverageWithSelectorOverloads,
            MongoEnumerableMethod.MedianWithSelectorOverloads,
            MongoEnumerableMethod.PercentileWithSelectorOverloads
        ]);

        private static readonly IReadOnlyMethodInfoSet __whereOverloads = MethodInfoSet.Create(
        [
            EnumerableOrQueryableMethod.Where,
            [MongoEnumerableMethod.WhereWithLimit]
        ]);

        static MethodCallSerializerDeducers()
        {
            // DateTimeMethod
            RegisterSerializerDeducers(
                [
                    DateTimeMethod.Add, DateTimeMethod.AddWithTimezone, DateTimeMethod.AddWithUnit, DateTimeMethod.AddWithUnitAndTimezone,
                    DateTimeMethod.AddDays, DateTimeMethod.AddDaysWithTimezone, DateTimeMethod.AddHours, DateTimeMethod.AddHoursWithTimezone,
                    DateTimeMethod.AddMilliseconds, DateTimeMethod.AddMillisecondsWithTimezone, DateTimeMethod.AddMinutes, DateTimeMethod.AddMinutesWithTimezone,
                    DateTimeMethod.AddMonths, DateTimeMethod.AddMonthsWithTimezone, DateTimeMethod.AddQuarters, DateTimeMethod.AddQuartersWithTimezone,
                    DateTimeMethod.AddSeconds, DateTimeMethod.AddSecondsWithTimezone, DateTimeMethod.AddTicks, DateTimeMethod.AddWeeks,
                    DateTimeMethod.AddWeeksWithTimezone, DateTimeMethod.AddYears, DateTimeMethod.AddYearsWithTimezone
                ],
                (visitor, expression) => visitor.DeduceSerializer(expression, DateTimeSerializer.UtcInstance));
            RegisterSerializerDeducers(DateTimeMethod.SubtractReturningDateTimeOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, DateTimeSerializer.UtcInstance));
            RegisterSerializerDeducers(DateTimeMethod.SubtractReturningInt64Overloads, (visitor, expression) => visitor.DeduceSerializer(expression, Int64Serializer.Instance));
            RegisterSerializerDeducers(DateTimeMethod.SubtractReturningTimeSpanWithMillisecondsUnitsOverloads, (visitor, expression) => DeduceReturnsTimeSpanSerializer(visitor, expression, TimeSpanUnits.Milliseconds));
            RegisterSerializerDeducers([DateTimeMethod.Truncate, DateTimeMethod.TruncateWithBinSize, DateTimeMethod.TruncateWithBinSizeAndTimezone], (visitor, expression) => visitor.DeduceSerializer(expression, DateTimeSerializer.UtcInstance));
            RegisterSerializerDeducers([DateTimeMethod.Week, DateTimeMethod.WeekWithTimezone], (visitor, expression) => visitor.DeduceSerializer(expression, Int32Serializer.Instance));

            // EnumMethod
            RegisterSerializerDeducer(EnumMethod.HasFlag, DeduceHasFlagMethodSerializers);

            // EnumerableOrQueryableMethod
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.AggregateWithFunc, DeduceAggregateWithFuncMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.AggregateWithSeedAndFunc, DeduceAggregateWithSeedAndFuncMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.AggregateWithSeedFuncAndResultSelector, DeduceAggregateWithSeedFuncAndResultSelectorMethodSerializers);
            RegisterSerializerDeducers([MongoEnumerableMethod.AllElements, MongoEnumerableMethod.AllMatchingElements, MongoEnumerableMethod.FirstMatchingElement], DeduceReturnsOneSourceItemSerializer);
            RegisterSerializerDeducers([EnumerableMethod.AllWithPredicate, QueryableMethod.AllWithPredicate], DeduceAllMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.Any, (visitor, expression) => visitor.DeduceSerializer(expression, BooleanSerializer.Instance));
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.AnyWithPredicate, DeduceAnyWithPredicateMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.AppendOrPrepend, DeduceAppendOrPrependMethodSerializers);
            RegisterSerializerDeducer(MongoQueryableMethod.AppendStage, DeduceAppendStageMethodSerializers);
            RegisterSerializerDeducer(MongoQueryableMethod.As, DeduceAsMethodSerializers);
            RegisterSerializerDeducer(QueryableMethod.AsQueryable, DeduceAsQueryableMethodSerializers);
            RegisterSerializerDeducers(__averageOrMedianOrPercentileOverloads, DeduceAverageOrMedianOrPercentileMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.Concat, DeduceEnumerableConcatMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.CountOverloads, DeduceEnumerableCountMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.DefaultIfEmptyOverloads, DeduceDefaultIfEmptyMethodSerializers);
            RegisterSerializerDeducer(MongoQueryableMethod.DensifyWithArrayPartitionByFields, DeduceDensifyMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.Distinct, (visitor, expression) => visitor.DeduceCollectionAndCollectionSerializers(expression, expression.Arguments[0]));
            RegisterSerializerDeducers([MongoQueryableMethod.Documents, MongoQueryableMethod.DocumentsWithSerializer], DeduceDocumentsMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.ElementAtOverloads, (visitor, expression) => visitor.DeduceItemAndCollectionSerializers(expression, expression.Arguments[0]));
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.Except, DeduceExceptMethodSerializers);
            // TODO: Definitely wrong registration copy-pasted from prev code, investigate before merge to main
            // RegisterSerializerResolver(EnumerableMethod.First, DeduceSetWindowFieldsMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.FirstOrLastOrSingleOverloads, DeduceFirstOrLastOrSingleMethodsSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.GroupByOverloads, DeduceGroupByMethodSerializers);
            RegisterSerializerDeducers([EnumerableMethod.GroupJoin, QueryableMethod.GroupJoin], DeduceGroupJoinMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.Intersect, (visitor, expression) => visitor.DeduceCollectionAndCollectionSerializers(expression, expression.Arguments[0]));
            RegisterSerializerDeducers([EnumerableMethod.Join, QueryableMethod.Join], DeduceJoinMethodSerializers);
            RegisterSerializerDeducer(MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignField, DeduceLookupWithDocumentsAndLocalFieldAndForeignFieldMethodSerializers);
            RegisterSerializerDeducer(MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignFieldAndPipeline, DeduceLookupWithDocumentsAndLocalFieldAndForeignFieldAndPipelineMethodSerializers);
            RegisterSerializerDeducer(MongoQueryableMethod.LookupWithDocumentsAndPipeline, DeduceLookupWithDocumentsAndPipelineMethodSerializers);
            RegisterSerializerDeducer(MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignField, DeduceLookupWithFromAndLocalFieldAndForeignFieldMethodSerializers);
            RegisterSerializerDeducer(MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignFieldAndPipeline, DeduceLookupWithFromAndLocalFieldAndForeignFieldAndPipelineMethodSerializers);
            RegisterSerializerDeducer(MongoQueryableMethod.LookupWithFromAndPipeline, DeduceLookupWithFromAndPipelineMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.MaxOrMinOverloads, DeduceMaxOrMinMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.OfType, DeduceOfTypeMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.OrderByOrThenByOverloads, DeduceOrderByMethodSerializers);
            RegisterSerializerDeducers(EnumerableMethod.PickOverloads, DeducePickMethodSerializers);
            RegisterSerializerDeducer(EnumerableMethod.Range, (visitor, expression) => visitor.DeduceCollectionAndItemSerializers(expression, expression.Arguments[0]));
            RegisterSerializerDeducer(EnumerableMethod.Repeat, (visitor, expression) => visitor.DeduceCollectionAndItemSerializers(expression, expression.Arguments[0]));
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.ReverseOverloads, (visitor, expression) => visitor.DeduceCollectionAndCollectionSerializers(expression, expression.Arguments[0]));
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.Select, DeduceSelectMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.SelectManyWithCollectionSelectorAndResultSelector, DeduceSelectManyWithCollectionSelectorAndResultSelectorMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.SelectManyWithSelector, DeduceSelectManyWithSelectorMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.SelectManyWithSelectorTakingIndex, SelectManyWithSelectorTakingIndexMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.SelectWithSelectorTakingIndex, DeduceSelectWithSelectorTakingIndexMethodSerializers);
            RegisterSerializerDeducers([EnumerableMethod.SequenceEqual, QueryableMethod.SequenceEqual], DeduceSequenceEqualMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.SkipOrTakeOverloads, DeduceSkipOrTakeMethodSerializers);
            RegisterSerializerDeducers(MongoEnumerableMethod.StandardDeviationOverloads, DeduceStandardDeviationMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.SumOverloads, DeduceEnumerableSumMethodSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.Union, (visitor, expression) => visitor.DeduceCollectionAndCollectionSerializers(expression, expression.Arguments[0]));
            RegisterSerializerDeducers(__whereOverloads, DeduceWhereSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.WhereWithPredicateTakingIndex, DeduceWhereWithPredicateTakingIndexSerializers);
            RegisterSerializerDeducers(EnumerableOrQueryableMethod.Zip, DeduceZipSerializers);

            // KeyValuePairMethod
    #if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            RegisterSerializerDeducer(KeyValuePairMethod.Create, DeduceKeyValuePairCreateMethodSerializers);
    #endif

            // LinqExtensionsMethod
            RegisterSerializerDeducer(LinqExtensionsMethod.Inject, (visitor, expression) => visitor.DeduceSerializer(expression, BooleanSerializer.Instance));

            // MathMethod
            RegisterSerializerDeducers(MathMethod.AbsOverloads, (visitor, expression) => visitor.DeduceSerializers(expression, expression.Arguments[0]));
            RegisterSerializerDeducers([MathMethod.CeilingWithDecimal, MathMethod.CeilingWithDouble, MathMethod.FloorWithDecimal, MathMethod.FloorWithDouble], DeduceReturnsNumericSerializer);
            RegisterSerializerDeducer(MathMethod.Exp, (visitor, expression) => visitor.DeduceSerializer(expression, DoubleSerializer.Instance));
            RegisterSerializerDeducers(MathMethod.LogOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, DoubleSerializer.Instance));
            RegisterSerializerDeducer(MathMethod.Pow, (visitor, expression) => visitor.DeduceSerializer(expression, DoubleSerializer.Instance));
            RegisterSerializerDeducers([MathMethod.RoundWithDecimal, MathMethod.RoundWithDecimalAndDecimals, MathMethod.RoundWithDouble, MathMethod.RoundWithDoubleAndDigits], DeduceRoundMethodSerializers);
            RegisterSerializerDeducer(MathMethod.Sqrt, (visitor, expression) => visitor.DeduceSerializer(expression, DoubleSerializer.Instance));
            RegisterSerializerDeducers(MathMethod.TrigonometricMethods, (visitor, expression) => visitor.DeduceSerializer(expression, DoubleSerializer.Instance));
            RegisterSerializerDeducers([MathMethod.TruncateDecimal, MathMethod.TruncateDouble], DeduceReturnsNumericSerializer);

            // MongoDBMathMethod
            RegisterSerializerDeducer(MongoDBMathMethod.DegreesToRadians, (visitor, method) => visitor.DeduceSerializer(method, DoubleSerializer.Instance));
            RegisterSerializerDeducer(MongoDBMathMethod.RadiansToDegrees, (visitor, expression) => visitor.DeduceSerializer(expression, DoubleSerializer.Instance));

            // MqlMethod
            RegisterSerializerDeducers([MqlMethod.ConstantWithRepresentation, MqlMethod.ConstantWithSerializer], DeduceConstantMethodSerializers);
            RegisterSerializerDeducer(MqlMethod.Convert, DeduceConvertMethodSerializers);
            RegisterSerializerDeducer(MqlMethod.CreateObjectId, (visitor, expression) => visitor.DeduceSerializer(expression, ObjectIdSerializer.Instance));
            RegisterSerializerDeducers(MqlMethod.DateFromStringOverloads, DeduceDateFromStringMethodSerializers);
            RegisterSerializerDeducer(MqlMethod.DeserializeEJson, DeduceDeserializeEJsonMethodSerializers);
            RegisterSerializerDeducers([MqlMethod.Exists, MqlMethod.IsMissing, MqlMethod.IsNullOrMissing], (visitor, expression) => visitor.DeduceSerializer(expression, BooleanSerializer.Instance));
            RegisterSerializerDeducer(MqlMethod.Field, DeduceFieldMethodSerializers);
            RegisterSerializerDeducer(MqlMethod.Hash, (visitor, expression) => visitor.DeduceSerializer(expression, BsonBinaryDataSerializer.Instance));
            RegisterSerializerDeducer(MqlMethod.HexHash, (visitor, expression) => visitor.DeduceSerializer(expression, StringSerializer.Instance));
            RegisterSerializerDeducer(MqlMethod.SerializeEJson, DeduceSerializeEJsonMethodSerializers);
            RegisterSerializerDeducer(MqlMethod.Sigmoid, (visitor, expression) => visitor.DeduceSerializer(expression, DoubleSerializer.Instance));
            RegisterSerializerDeducers(MqlMethod.SimilarityFunctionOverloads, DeduceReturnsNumericSerializer);
            RegisterSerializerDeducer(MqlMethod.Subtype, (visitor, expression) => visitor.DeduceSerializer(expression, __binarySubTypeSerializer));
            RegisterSerializerDeducer(MqlMethod.ToHashedIndexKey, (visitor, expression) => visitor.DeduceSerializer(expression, Int64Serializer.Instance));

            // RegexMethod
            RegisterSerializerDeducers(RegexMethod.IsMatchOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, BooleanSerializer.Instance));
            RegisterSerializerDeducers(RegexMethod.ReplaceOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, StringSerializer.Instance));
            RegisterSerializerDeducers(RegexMethod.SplitOverloads, DeduceSplitMethodSerializers);

            // StringMethod
            RegisterSerializerDeducers(StringMethod.AnyStringInOrNinOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, BooleanSerializer.Instance));
            RegisterSerializerDeducers(StringMethod.ConcatOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, StringSerializer.Instance));
            RegisterSerializerDeducers(StringMethod.ContainsOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, BooleanSerializer.Instance));
            RegisterSerializerDeducers(StringMethod.EndsWithOrStartsWithOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, BooleanSerializer.Instance));
            RegisterSerializerDeducers([StringMethod.EqualsInstanceMethod, StringMethod.EqualsWithComparisonType, StringMethod.StaticEqualsWithComparisonType, StringMethod.StaticEquals], DeduceStringEqualsMethodSerializers);
            RegisterSerializerDeducer(StringMethod.GetChars, (visitor, expression) => visitor.DeduceSerializer(expression, CharSerializer.Instance));
            RegisterSerializerDeducers(StringMethod.IndexOfOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, Int32Serializer.Instance));
            RegisterSerializerDeducers([StringMethod.IsNullOrEmpty, StringMethod.IsNullOrWhiteSpace], (visitor, expression) => visitor.DeduceSerializer(expression, BooleanSerializer.Instance));
            RegisterSerializerDeducers(StringMethod.ReplaceOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, StringSerializer.Instance));
            RegisterSerializerDeducers(StringMethod.SplitOverloads, DeduceSplitMethodSerializers);
            RegisterSerializerDeducer(StringMethod.StrLenBytes, (visitor, expression) => visitor.DeduceSerializer(expression, Int32Serializer.Instance));
            RegisterSerializerDeducers(StringMethod.StringInOrNinOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, BooleanSerializer.Instance));
            RegisterSerializerDeducers([StringMethod.Substring, StringMethod.SubstringWithLength, StringMethod.SubstrBytes], (visitor, expression) => visitor.DeduceSerializer(expression, StringSerializer.Instance));
            RegisterSerializerDeducers(StringMethod.ToLowerOrToUpperOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, StringSerializer.Instance));
            RegisterSerializerDeducers(StringMethod.TrimOverloads, (visitor, expression) => visitor.DeduceSerializer(expression, StringSerializer.Instance));

            // TupleOrValueTupleMethod
            RegisterSerializerDeducers(TupleOrValueTupleMethod.CreateOverloads, DeduceTupleOrValueTupleCreateMethodSerializers);

            // WindowMethod
            RegisterSerializerDeducer(WindowMethod.AddToSet, DeduceAddToSetMethodSerializers);
            RegisterSerializerDeducers(__averageOrMedianOrPercentileWindowMethodOverloads, DeduceAverageOrMedianOrPercentileWindowMethodSerializers);
            RegisterSerializerDeducer(WindowMethod.Count, (visitor, expression) => visitor.DeduceSerializer(expression, Int64Serializer.Instance));
            RegisterSerializerDeducers(WindowMethod.CovarianceOverloads, DeduceCovarianceMethodSerializers);
            RegisterSerializerDeducers([WindowMethod.DenseRank, WindowMethod.Rank], (visitor, expression) => visitor.DeduceStandardSerializer(expression)); // currently decimal, but might be long in the future
            RegisterSerializerDeducers(WindowMethod.DerivativeOrIntegralOverloads, DeduceDerivativeOrIntegralMethodSerializers);
            RegisterSerializerDeducer(WindowMethod.DocumentNumber, (visitor, expression) => visitor.DeduceStandardSerializer(expression)); // currently decimal, but might be long in the future
            RegisterSerializerDeducers(WindowMethod.ExponentialMovingAverageOverloads, DeduceExponentialMovingAverageMethodSerializers);
            RegisterSerializerDeducers([WindowMethod.First, WindowMethod.Last], DeduceFirstOrLastWindowMethodsSerializers);
            RegisterSerializerDeducer(WindowMethod.Locf, DeduceLocfMethodSerializers);
            RegisterSerializerDeducers([WindowMethod.Max, WindowMethod.Min], DeduceWindowMaxOrMinMethodSerializers);
            RegisterSerializerDeducer(WindowMethod.Push, DeducePushMethodSerializers);
            RegisterSerializerDeducers([WindowMethod.Shift, WindowMethod.ShiftWithDefaultValue], DeduceShiftMethodSerializers);
            RegisterSerializerDeducers(WindowMethod.StandardDeviationOverloads, DeduceWindowStandardDeviationMethodSerializers);
            RegisterSerializerDeducers(WindowMethod.SumOverloads, DeduceWindowSumMethodSerializers);

            void RegisterSerializerDeducer(MethodInfo method, Action<SerializerFinderVisitor, MethodCallExpression> resolver)
            {
                Ensure.IsNotNull(method, nameof(method));
                Ensure.IsNotNull(resolver, nameof(resolver));

                __serializerDeducers.Add(method, resolver);
            }

            void RegisterSerializerDeducers(IEnumerable<MethodInfo> methods, Action<SerializerFinderVisitor, MethodCallExpression> resolver)
            {
                foreach (var method in methods)
                {
                    RegisterSerializerDeducer(method, resolver);
                }
            }
        }
        // DeduceAddToSetMethodSerializers
        private static void DeduceAddToSetMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = (LambdaExpression)expression.Arguments[1];
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selectorLambda);
            visitor.DeduceCollectionAndItemSerializers(expression, selectorLambda.Body);
        }

        // DeduceAggregateMethodSerializers + EnumerableOrQueryableMethod.AggregateWithFunc branch
        private static void DeduceAggregateWithFuncMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var funcLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var funcAccumulatorParameter = funcLambda.Parameters[0];
            var funcSourceItemParameter = funcLambda.Parameters[1];

            visitor.DeduceItemAndCollectionSerializers(funcAccumulatorParameter, sourceExpression);
            visitor.DeduceItemAndCollectionSerializers(funcSourceItemParameter, sourceExpression);
            visitor.DeduceItemAndCollectionSerializers(funcLambda.Body, sourceExpression);
            visitor.DeduceSerializers(expression, funcLambda.Body);
        }

        // DeduceAggregateMethodSerializers + EnumerableOrQueryableMethod.AggregateWithSeedAndFunc branch
        private static void DeduceAggregateWithSeedAndFuncMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var seedExpression =  expression.Arguments[1];
            var funcLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var funcAccumulatorParameter = funcLambda.Parameters[0];
            var funcSourceItemParameter = funcLambda.Parameters[1];

            visitor.DeduceSerializers(seedExpression, funcLambda.Body);
            visitor.DeduceSerializers(funcAccumulatorParameter, funcLambda.Body);
            visitor.DeduceItemAndCollectionSerializers(funcSourceItemParameter, sourceExpression);
            visitor.DeduceSerializers(expression, funcLambda.Body);
        }

        // DeduceAggregateMethodSerializers + EnumerableOrQueryableMethod.AggregateWithSeedFuncAndResultSelector branch
        private static void DeduceAggregateWithSeedFuncAndResultSelectorMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var seedExpression = expression.Arguments[1];
            var funcLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var funcAccumulatorParameter = funcLambda.Parameters[0];
            var funcSourceItemParameter = funcLambda.Parameters[1];
            var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[3]);
            var resultSelectorAccumulatorParameter = resultSelectorLambda.Parameters[0];

            visitor.DeduceSerializers(seedExpression, funcLambda.Body);
            visitor.DeduceSerializers(funcAccumulatorParameter, funcLambda.Body);
            visitor.DeduceItemAndCollectionSerializers(funcSourceItemParameter, sourceExpression);
            visitor.DeduceSerializers(resultSelectorAccumulatorParameter, funcLambda.Body);
            visitor.DeduceSerializers(expression, resultSelectorLambda.Body);
        }

        // DeduceAllMethodSerializers
        private static void DeduceAllMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var predicateParameter = predicateLambda.Parameters.Single();

            visitor.DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
            visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
        }

        // DeduceAnyMethodSerializers + EnumerableOrQueryableMethod.AnyWithPredicate branch
        private static void DeduceAnyWithPredicateMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var predicateParameter = predicateLambda.Parameters[0];

            visitor.DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
            visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
        }

        // DeduceAppendStageMethodSerializers
        private static void DeduceAppendStageMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsNotKnown(expression))
            {
                var sourceExpression = expression.Arguments[0];
                var stageExpression = expression.Arguments[1];
                var resultSerializerExpression = expression.Arguments[2];

                if (stageExpression is not ConstantExpression stageConstantExpression)
                {
                    throw new ExpressionNotSupportedException(expression, because: "stage argument must be a constant");
                }
                var stageDefinition = (IPipelineStageDefinition)stageConstantExpression.Value;

                if (resultSerializerExpression is not ConstantExpression resultSerializerConstantExpression)
                {
                    throw new ExpressionNotSupportedException(expression, because: "resultSerializer argument must be a constant");
                }
                var resultItemSerializer = (IBsonSerializer)resultSerializerConstantExpression.Value;

                if (resultItemSerializer == null && visitor.IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer))
                {
                    var serializerRegistry = BsonSerializer.SerializerRegistry; // TODO: get correct registry
                    var translationOptions = new ExpressionTranslationOptions(); // TODO: get correct translation options
                    var renderedStage = stageDefinition.Render(sourceItemSerializer, serializerRegistry, translationOptions);
                    resultItemSerializer = renderedStage.OutputSerializer;
                }

                if (resultItemSerializer != null)
                {
                    var resultSerializer = IEnumerableOrIQueryableSerializer.Create(expression.Type, resultItemSerializer);
                    visitor.AddNodeSerializer(expression, resultSerializer);
                }
            }
        }

        // DeduceAsMethodSerializers
        private static void DeduceAsMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsNotKnown(expression))
            {
                var resultSerializerExpression = expression.Arguments[1];
                if (resultSerializerExpression is not ConstantExpression resultSerializerConstantExpression)
                {
                    throw new ExpressionNotSupportedException(expression, because: "resultSerializer argument must be a constant");
                }

                var resultItemSerializer = (IBsonSerializer)resultSerializerConstantExpression.Value;
                if (resultItemSerializer == null)
                {
                    var resultItemType = expression.Method.GetGenericArguments()[1];
                    resultItemSerializer = BsonSerializer.LookupSerializer(resultItemType);
                }

                var resultSerializer = IEnumerableOrIQueryableSerializer.Create(expression.Type, resultItemSerializer);
                visitor.AddNodeSerializer(expression, resultSerializer);
            }
        }

        // DeduceAsQueryableMethodSerializers
        private static void DeduceAsQueryableMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];

            if (visitor.IsNotKnown(expression) && visitor.IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer))
            {
                var resultSerializer = NestedAsQueryableSerializer.Create(sourceItemSerializer);
                visitor.AddNodeSerializer(expression, resultSerializer);
            }
        }

        // DeduceConcatMethodSerializers + EnumerableMethod.Concat, QueryableMethod.Concat branch
        private static void DeduceEnumerableConcatMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var firstExpression = expression.Arguments[0];
            var secondExpression = expression.Arguments[1];

            visitor.DeduceCollectionAndCollectionSerializers(firstExpression, secondExpression);
            visitor.DeduceCollectionAndCollectionSerializers(expression, firstExpression);
        }

        // DeduceConstantMethodSerializers
        private static void DeduceConstantMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var valueExpression = expression.Arguments[0];
            IBsonSerializer serializer = null;

            if (visitor.IsNotKnown(expression) || visitor.IsNotKnown(valueExpression))
            {
                if (expression.Method.Is(MqlMethod.ConstantWithRepresentation))
                {
                    var representationExpression = expression.Arguments[1];

                    var representation = representationExpression.GetConstantValue<BsonType>(expression);
                    var defaultSerializer = BsonSerializer.LookupSerializer(valueExpression.Type); // TODO: don't use BsonSerializer
                    if (defaultSerializer is IRepresentationConfigurable representationConfigurableSerializer)
                    {
                        serializer = representationConfigurableSerializer.WithRepresentation(representation);
                    }
                }
                else if (expression.Method.Is(MqlMethod.ConstantWithSerializer))
                {
                    var serializerExpression = expression.Arguments[1];
                    serializer = serializerExpression.GetConstantValue<IBsonSerializer>(expression);
                }
            }

            visitor.DeduceSerializer(valueExpression, serializer);
            visitor.DeduceSerializer(expression, serializer);
        }

        // DeduceConvertMethodSerializers
        private static void DeduceConvertMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsNotKnown(expression))
            {
                var toType = expression.Method.GetGenericArguments()[1];
                var resultSerializer = GetResultSerializer(expression, toType);
                visitor.AddNodeSerializer(expression, resultSerializer);
            }

            static IBsonSerializer GetResultSerializer(Expression expression, Type toType)
            {
                // TODO: should we use StandardSerializers at least for the subset of types where it would return the correct serializer?
                var isNullable = toType.IsNullable();
                var valueType = isNullable ? Nullable.GetUnderlyingType(toType) : toType;

                var valueSerializer = (IBsonSerializer)(Type.GetTypeCode(valueType) switch
                {
                    TypeCode.Boolean => BooleanSerializer.Instance,
                    TypeCode.Byte => ByteSerializer.Instance,
                    TypeCode.Char => StringSerializer.Instance,
                    TypeCode.DateTime => DateTimeSerializer.Instance,
                    TypeCode.Decimal => DecimalSerializer.Instance,
                    TypeCode.Double => DoubleSerializer.Instance,
                    TypeCode.Int16 => Int16Serializer.Instance,
                    TypeCode.Int32 => Int32Serializer.Instance,
                    TypeCode.Int64 => Int64Serializer.Instance,
                    TypeCode.SByte => SByteSerializer.Instance,
                    TypeCode.Single => SingleSerializer.Instance,
                    TypeCode.String => StringSerializer.Instance,
                    TypeCode.UInt16 => UInt16Serializer.Instance,
                    TypeCode.UInt32 => Int32Serializer.Instance,
                    TypeCode.UInt64 => UInt64Serializer.Instance,

                    _ when valueType == typeof(BsonArray) => BsonArraySerializer.Instance,
                    _ when valueType == typeof(BsonDocument) => BsonDocumentSerializer.Instance,
                    _ when valueType == typeof(byte[]) => ByteArraySerializer.Instance,
                    _ when valueType == typeof(BsonBinaryData) => BsonBinaryDataSerializer.Instance,
                    _ when valueType == typeof(Decimal128) => Decimal128Serializer.Instance,
                    _ when valueType == typeof(Guid) => GuidSerializer.StandardInstance,
                    _ when valueType == typeof(ObjectId) => ObjectIdSerializer.Instance,

                    _ => throw new ExpressionNotSupportedException(expression, because: $"{toType} is not a valid TTo for Convert")
                });

                return isNullable ? NullableSerializer.Create(valueSerializer) : valueSerializer;
            }
        }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        // DeduceCreateMethodSerializers + KeyValuePairMethod.Create branch
        private static void DeduceKeyValuePairCreateMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsAnyNotKnown(expression.Arguments) && visitor.IsKnown(expression, out var nodeSerializer))
            {
                var keyExpression = expression.Arguments[0];
                var valueExpression = expression.Arguments[1];

                if (nodeSerializer.IsKeyValuePairSerializer(out _, out _, out var keySerializer, out var valueSerializer))
                {
                    visitor.DeduceSerializer(keyExpression, keySerializer);
                    visitor.DeduceSerializer(valueExpression, valueSerializer);
                }
            }

            if (visitor.IsNotKnown(expression) && visitor.AreAllKnown(expression.Arguments, out var argumentSerializers))
            {
                var keySerializer = argumentSerializers[0];
                var valueSerializer = argumentSerializers[1];
                var keyValuePairSerializer = KeyValuePairSerializer.Create(BsonType.Document, keySerializer, valueSerializer);
                visitor.AddNodeSerializer(expression, keyValuePairSerializer);
            }
        }
#endif

        // DeduceCreateMethodSerializers + TupleOrValueTupleMethod.CreateOverloads
        private static void DeduceTupleOrValueTupleCreateMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsAnyNotKnown(expression.Arguments) && visitor.IsKnown(expression, out var nodeSerializer))
            {
                if (nodeSerializer is IBsonTupleSerializer tupleSerializer)
                {
                    for (var i = 1; i <= expression.Arguments.Count; i++)
                    {
                        var argumentExpression = expression.Arguments[i];
                        if (visitor.IsNotKnown(argumentExpression))
                        {
                            var itemSerializer = tupleSerializer.GetItemSerializer(i);
                            if (i == 8)
                            {
                                itemSerializer = (itemSerializer as IBsonTupleSerializer)?.GetItemSerializer(1);
                            }
                            visitor.AddNodeSerializer(argumentExpression, itemSerializer);
                        }
                    }
                }
            }

            if (visitor.IsNotKnown(expression) && visitor.AreAllKnown(expression.Arguments, out var argumentSerializers))
            {
                var tupleType = expression.Method.ReturnType;

                if (expression.Arguments.Count == 8)
                {
                    var item8Expression = expression.Arguments[7];
                    var item8Type = item8Expression.Type;
                    var item8Serializer = argumentSerializers[7];
                    var restTupleType = (tupleType.IsTuple() ? typeof(Tuple<>) : typeof(ValueTuple<>)).MakeGenericType(item8Type);
                    var restSerializer = TupleOrValueTupleSerializer.Create(restTupleType, [item8Serializer]);
                    argumentSerializers = argumentSerializers.Take(7).Append(restSerializer).ToArray();
                }

                var tupleSerializer = TupleOrValueTupleSerializer.Create(tupleType, argumentSerializers);
                visitor.AddNodeSerializer(expression, tupleSerializer);
            }
        }

        // DeduceDateFromStringMethodSerializers
        private static void DeduceDateFromStringMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var resultSerializer = expression.Method.Is(MqlMethod.DateFromStringWithFormatAndTimezoneAndOnErrorAndOnNull) ? NullableSerializer.NullableUtcDateTimeInstance : DateTimeSerializer.UtcInstance;
            visitor.DeduceSerializer(expression, resultSerializer);
        }

        // DeduceDefaultIfEmptyMethodSerializers
        private static void DeduceDefaultIfEmptyMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];

            if (expression.Method.IsOneOf(EnumerableMethod.DefaultIfEmptyWithDefaultValue, QueryableMethod.DefaultIfEmptyWithDefaultValue))
            {
                var defaultValueExpression = expression.Arguments[1];
                visitor.DeduceItemAndCollectionSerializers(defaultValueExpression, sourceExpression);
            }

            visitor.DeduceCollectionAndCollectionSerializers(expression, sourceExpression);
        }

        // DeduceDensifyMethodSerializers
        private static void DeduceDensifyMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var fieldLambda = ExpressionHelper.UnquoteLambda(expression.Arguments[1]);
            var fieldLambdaSourceParameter = fieldLambda.Parameters.Single();
            var rangeExpression = expression.Arguments[2];
            var partitionByFieldsExpression = expression.Arguments[3];

            visitor.DeduceItemAndCollectionSerializers(fieldLambdaSourceParameter, sourceExpression);
            visitor.DeduceIgnoreSubtreeSerializer(rangeExpression);
            if (partitionByFieldsExpression is NewArrayExpression newArrayExpression)
            {
                foreach (var arrayItemExpression in newArrayExpression.Expressions)
                {
                    var partitionByFieldLambda = ExpressionHelper.UnquoteLambda(arrayItemExpression);
                    var partitionByFieldLambdaSourceParameter = partitionByFieldLambda.Parameters.Single();
                    visitor.DeduceItemAndCollectionSerializers(partitionByFieldLambdaSourceParameter, sourceExpression);
                }
            }
            visitor.DeduceCollectionAndCollectionSerializers(expression, sourceExpression);
        }

        // DeduceDocumentsMethodSerializers
        private static void DeduceDocumentsMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsNotKnown(expression))
            {
                IBsonSerializer documentSerializer;
                if (expression.Method.Is(MongoQueryableMethod.DocumentsWithSerializer))
                {
                    var documentSerializerExpression = expression.Arguments[2];
                    documentSerializer = documentSerializerExpression.GetConstantValue<IBsonSerializer>(expression);
                }
                else
                {
                    var documentsParameter = expression.Method.GetParameters()[1];
                    var documentType = documentsParameter.ParameterType.GetElementType();
                    documentSerializer = BsonSerializer.LookupSerializer(documentType); // TODO: don't use static registry
                }

                var nodeSerializer = IQueryableSerializer.Create(documentSerializer);
                visitor.AddNodeSerializer(expression, nodeSerializer);
            }
        }

        // DeduceExceptMethodSerializers
        private static void DeduceExceptMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var firstExpression = expression.Arguments[0];
            var secondExpression = expression.Arguments[1];
            visitor.DeduceCollectionAndCollectionSerializers(secondExpression, firstExpression);
            visitor.DeduceCollectionAndCollectionSerializers(expression, firstExpression);
        }

        // DeduceExponentialMovingAverageMethodSerializers
        private static void DeduceExponentialMovingAverageMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = (LambdaExpression)expression.Arguments[1];
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selectorLambda);
            visitor.DeduceStandardSerializer(expression);
        }

        // DeduceFieldMethodSerializers
        private static void DeduceFieldMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsNotKnown(expression))
            {
                var fieldSerializerExpression = expression.Arguments[2];
                var fieldSerializer = fieldSerializerExpression.GetConstantValue<IBsonSerializer>(expression);
                if (fieldSerializer == null)
                {
                    fieldSerializer = BsonSerializer.LookupSerializer(expression.Method.GetGenericArguments()[1]);
                }

                visitor.AddNodeSerializer(expression, fieldSerializer);
            }
        }

        // DeduceGroupByMethodSerializers
        private static void DeduceGroupByMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var keySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var keySelectorParameter = keySelectorLambda.Parameters.Single();

            visitor.DeduceItemAndCollectionSerializers(keySelectorParameter, sourceExpression);

            if (expression.Method.IsOneOf(EnumerableOrQueryableMethod.GroupByWithKeySelector))
            {
                if (visitor.IsNotKnown(expression) && visitor.IsKnown(keySelectorLambda.Body, out var keySerializer) && visitor.IsItemSerializerKnown(sourceExpression, out var elementSerializer))
                {
                    var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);
                    var nodeSerializer = IEnumerableOrIQueryableSerializer.Create(expression.Type, groupingSerializer);
                    visitor.AddNodeSerializer(expression, nodeSerializer);
                }
            }
            else if (expression.Method.IsOneOf(EnumerableOrQueryableMethod.GroupByWithKeySelectorAndElementSelector))
            {
                var elementSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
                var elementSelectorParameter = elementSelectorLambda.Parameters.Single();
                visitor.DeduceItemAndCollectionSerializers(elementSelectorParameter, sourceExpression);
                if (visitor.IsNotKnown(expression) && visitor.IsKnown(keySelectorLambda.Body, out var keySerializer) && visitor.IsKnown(elementSelectorLambda.Body, out var elementSerializer))
                {
                    var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);
                    var nodeSerializer = IEnumerableOrIQueryableSerializer.Create(expression.Type, groupingSerializer);
                    visitor.AddNodeSerializer(expression, nodeSerializer);
                }
            }
            else if (expression.Method.IsOneOf(EnumerableOrQueryableMethod.GroupByWithKeySelectorAndResultSelector))
            {
                var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
                var resultSelectorKeyParameter = resultSelectorLambda.Parameters[0];
                var resultSelectorElementsParameter = resultSelectorLambda.Parameters[1];
                visitor.DeduceItemAndCollectionSerializers(keySelectorParameter, sourceExpression);
                visitor.DeduceSerializers(resultSelectorKeyParameter, keySelectorLambda.Body);
                visitor.DeduceCollectionAndCollectionSerializers(resultSelectorElementsParameter, sourceExpression);
                DeduceResultSerializer(resultSelectorLambda.Body);
            }
            else if (expression.Method.IsOneOf(EnumerableOrQueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector))
            {
                var elementSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
                var elementSelectorParameter = elementSelectorLambda.Parameters.Single();
                var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[3]);
                var resultSelectorKeyParameter = resultSelectorLambda.Parameters[0];
                var resultSelectorElementsParameter = resultSelectorLambda.Parameters[1];
                visitor.DeduceItemAndCollectionSerializers(keySelectorParameter, sourceExpression);
                visitor.DeduceItemAndCollectionSerializers(elementSelectorParameter, sourceExpression);
                visitor.DeduceSerializers(resultSelectorKeyParameter, keySelectorLambda.Body);
                visitor.DeduceCollectionAndItemSerializers(resultSelectorElementsParameter, elementSelectorLambda.Body);
                DeduceResultSerializer(resultSelectorLambda.Body);
            }

            void DeduceResultSerializer(Expression resultExpression)
            {
                if (visitor.IsNotKnown(expression) && visitor.IsKnown(resultExpression, out var resultSerializer))
                {
                    var nodeSerializer = IEnumerableOrIQueryableSerializer.Create(expression.Type, resultSerializer);
                    visitor.AddNodeSerializer(expression, nodeSerializer);
                }
            }
        }

        // DeduceGroupJoinMethodSerializers
        private static void DeduceGroupJoinMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var outerExpression = expression.Arguments[0];
            var innerExpression = expression.Arguments[1];
            var outerKeySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var outerKeySelectorItemParameter = outerKeySelectorLambda.Parameters.Single();
            var innerKeySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[3]);
            var innerKeySelectorItemParameter = innerKeySelectorLambda.Parameters.Single();
            var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[4]);
            var resultSelectorOuterItemParameter = resultSelectorLambda.Parameters[0];
            var resultSelectorInnerItemsParameter = resultSelectorLambda.Parameters[1];

            visitor.DeduceItemAndCollectionSerializers(outerKeySelectorItemParameter, outerExpression);
            visitor.DeduceItemAndCollectionSerializers(innerKeySelectorItemParameter, innerExpression);
            visitor.DeduceItemAndCollectionSerializers(resultSelectorOuterItemParameter, outerExpression);
            visitor.DeduceCollectionAndCollectionSerializers(resultSelectorInnerItemsParameter, innerExpression);
            visitor.DeduceCollectionAndItemSerializers(expression, resultSelectorLambda.Body);
        }

        // DeduceHasFlagMethodSerializers
        private static void DeduceHasFlagMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var objectExpression = expression.Object;
            var flagExpression = expression.Arguments[0];
            if (visitor.IsNotKnown(flagExpression) && visitor.IsKnown(objectExpression, out var enumSerializer))
            {
                visitor.AddNodeSerializer(flagExpression, enumSerializer);
            }
            visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
        }

        // DeduceJoinMethodSerializers
        private static void DeduceJoinMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var outerExpression = expression.Arguments[0];
            var innerExpression = expression.Arguments[1];
            var outerKeySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var outerKeySelectorItemParameter = outerKeySelectorLambda.Parameters.Single();
            var innerKeySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[3]);
            var innerKeySelectorItemParameter = innerKeySelectorLambda.Parameters.Single();
            var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[4]);
            var resultSelectorOuterItemParameter = resultSelectorLambda.Parameters[0];
            var resultSelectorInnerItemsParameter = resultSelectorLambda.Parameters[1];

            visitor.DeduceItemAndCollectionSerializers(outerKeySelectorItemParameter, outerExpression);
            visitor.DeduceItemAndCollectionSerializers(innerKeySelectorItemParameter, innerExpression);
            visitor.DeduceItemAndCollectionSerializers(resultSelectorOuterItemParameter, outerExpression);
            visitor.DeduceItemAndCollectionSerializers(resultSelectorInnerItemsParameter, innerExpression);
            visitor.DeduceCollectionAndItemSerializers(expression, resultSelectorLambda.Body);
        }

        // DeduceLocfMethodSerializers
        private static void DeduceLocfMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = (LambdaExpression)expression.Arguments[1];
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selectorLambda);
            visitor.DeduceSerializers(expression, selectorLambda.Body);
        }

        // DeduceLookupMethodSerializers + MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignField branch
        private static void DeduceLookupWithDocumentsAndLocalFieldAndForeignFieldMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var documentsLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var documentsLambdaParameter = documentsLambda.Parameters.Single();
            var localFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var localFieldLambdaParameter = localFieldLambda.Parameters.Single();
            var foreignFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[3]);
            var foreignFieldLambdaParameter = foreignFieldLambda.Parameters.Single();

            visitor.DeduceItemAndCollectionSerializers(documentsLambdaParameter, sourceExpression);
            visitor.DeduceItemAndCollectionSerializers(localFieldLambdaParameter, sourceExpression);
            visitor.DeduceItemAndCollectionSerializers(foreignFieldLambdaParameter, documentsLambda.Body);

            if (visitor.IsNotKnown(expression) &&
                visitor.IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                visitor.IsItemSerializerKnown(documentsLambda.Body, out var documentSerializer))
            {
                var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, documentSerializer);
                visitor.AddNodeSerializer(expression, IQueryableSerializer.Create(lookupResultSerializer));
            }
        }
        // DeduceLookupMethodSerializers + LookupWithDocumentsAndLocalFieldAndForeignFieldAndPipeline branch
        private static void DeduceLookupWithDocumentsAndLocalFieldAndForeignFieldAndPipelineMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var documentsLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var documentsLambdaParameter = documentsLambda.Parameters.Single();
            var localFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var localFieldLambdaParameter = localFieldLambda.Parameters.Single();
            var foreignFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[3]);
            var foreignFieldLambdaParameter = foreignFieldLambda.Parameters.Single();
            var pipelineLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[4]);
            var pipelineLambdaLocalParameter = pipelineLambda.Parameters[0];
            var pipelineLambdaForeignQueryableParameter = pipelineLambda.Parameters[1];

            visitor.DeduceItemAndCollectionSerializers(documentsLambdaParameter, sourceExpression);
            visitor.DeduceItemAndCollectionSerializers(localFieldLambdaParameter, sourceExpression);
            visitor.DeduceItemAndCollectionSerializers(foreignFieldLambdaParameter, documentsLambda.Body);
            visitor.DeduceItemAndCollectionSerializers(pipelineLambdaLocalParameter, sourceExpression);
            visitor.DeduceCollectionAndCollectionSerializers(pipelineLambdaForeignQueryableParameter, documentsLambda.Body);

            if (visitor.IsNotKnown(expression) &&
                visitor.IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                visitor.IsItemSerializerKnown(pipelineLambda.Body, out var pipelineDocumentSerializer))
            {
                var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, pipelineDocumentSerializer);
                visitor.AddNodeSerializer(expression, IQueryableSerializer.Create(lookupResultSerializer));
            }
        }

        // DeduceLookupMethodSerializers + LookupWithDocumentsAndPipeline branch
        private static void DeduceLookupWithDocumentsAndPipelineMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var documentsLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var documentsLambdaParameter = documentsLambda.Parameters.Single();
            var pipelineLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var pipelineLambdaSourceParameter = pipelineLambda.Parameters[0];
            var pipelineLambdaQueryableDocumentParameter = pipelineLambda.Parameters[1];

            visitor.DeduceItemAndCollectionSerializers(documentsLambdaParameter, sourceExpression);
            visitor.DeduceItemAndCollectionSerializers(pipelineLambdaSourceParameter, sourceExpression);
            visitor.DeduceCollectionAndCollectionSerializers(pipelineLambdaQueryableDocumentParameter, documentsLambda.Body);

            if (visitor.IsNotKnown(expression) &&
                visitor.IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                visitor.IsItemSerializerKnown(pipelineLambda.Body, out var pipelineItemSerializer))
            {
                var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, pipelineItemSerializer);
                visitor.AddNodeSerializer(expression, IQueryableSerializer.Create(lookupResultSerializer));
            }
        }

        // DeduceLookupMethodSerializers + LookupWithFromAndLocalFieldAndForeignField branch
        private static void DeduceLookupWithFromAndLocalFieldAndForeignFieldMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var fromExpression = expression.Arguments[1];
            var fromCollection = fromExpression.GetConstantValue<IMongoCollection>(expression);
            var foreignDocumentSerializer = fromCollection.DocumentSerializer;
            var localFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var localFieldLambdaParameter = localFieldLambda.Parameters.Single();
            var foreignFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[3]);
            var foreignFieldLambdaParameter = foreignFieldLambda.Parameters.Single();

            visitor.DeduceItemAndCollectionSerializers(localFieldLambdaParameter, sourceExpression);
            visitor.DeduceSerializer(foreignFieldLambdaParameter, foreignDocumentSerializer);

            if (visitor.IsNotKnown(expression) &&
                visitor.IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer))
            {
                var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, foreignDocumentSerializer);
                visitor.AddNodeSerializer(expression, IQueryableSerializer.Create(lookupResultSerializer));
            }
        }

        // DeduceLookupMethodSerializers + LookupWithFromAndLocalFieldAndForeignFieldAndPipeline branch
        private static void DeduceLookupWithFromAndLocalFieldAndForeignFieldAndPipelineMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var fromExpression = expression.Arguments[1];
            var fromCollection = fromExpression.GetConstantValue<IMongoCollection>(expression);
            var foreignDocumentSerializer = fromCollection.DocumentSerializer;
            var localFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var localFieldLambdaParameter = localFieldLambda.Parameters.Single();
            var foreignFieldLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[3]);
            var foreignFieldLambdaParameter = foreignFieldLambda.Parameters.Single();
            var pipelineLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[4]);
            var pipelineLambdaLocalParameter = pipelineLambda.Parameters[0];
            var pipelineLamdbaForeignQueryableParameter = pipelineLambda.Parameters[1];

            visitor.DeduceItemAndCollectionSerializers(localFieldLambdaParameter, sourceExpression);
            visitor.DeduceSerializer(foreignFieldLambdaParameter, foreignDocumentSerializer);
            visitor.DeduceItemAndCollectionSerializers(pipelineLambdaLocalParameter, sourceExpression);

            if (visitor.IsNotKnown(pipelineLamdbaForeignQueryableParameter))
            {
                var foreignQueryableSerializer = IQueryableSerializer.Create(foreignDocumentSerializer);
                visitor.AddNodeSerializer(pipelineLamdbaForeignQueryableParameter, foreignQueryableSerializer);
            }

            if (visitor.IsNotKnown(expression) &&
                visitor.IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                visitor.IsItemSerializerKnown(pipelineLambda.Body, out var pipelineItemSerializer))
            {
                var lookupResultsSerializer = LookupResultSerializer.Create(sourceItemSerializer, pipelineItemSerializer);
                visitor.AddNodeSerializer(expression, IQueryableSerializer.Create(lookupResultsSerializer));
            }
        }

        // DeduceLookupMethodSerializers + LookupWithFromAndPipeline branch
        private static void DeduceLookupWithFromAndPipelineMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var fromCollection = expression.Arguments[1].GetConstantValue<IMongoCollection>(expression);
            var foreignDocumentSerializer = fromCollection.DocumentSerializer;
            var pipelineLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var pipelineLambdaLocalParameter = pipelineLambda.Parameters[0];
            var pipelineLamdbaForeignQueryableParameter = pipelineLambda.Parameters[1];

            visitor.DeduceItemAndCollectionSerializers(pipelineLambdaLocalParameter, sourceExpression);

            if (visitor.IsNotKnown(pipelineLamdbaForeignQueryableParameter))
            {
                var foreignQueryableSerializer = IQueryableSerializer.Create(foreignDocumentSerializer);
                visitor.AddNodeSerializer(pipelineLamdbaForeignQueryableParameter, foreignQueryableSerializer);
            }

            if (visitor.IsNotKnown(expression) &&
                visitor.IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                visitor.IsItemSerializerKnown(pipelineLambda.Body, out var pipelineItemSerializer))
            {
                var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, pipelineItemSerializer);
                visitor.AddNodeSerializer(expression, IQueryableSerializer.Create(lookupResultSerializer));
            }
        }

        // DeduceOfTypeMethodSerializers
        private static void DeduceOfTypeMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var resultType = expression.Method.GetGenericArguments()[0];

            if (visitor.IsNotKnown(expression) && visitor.IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer))
            {
                var resultItemSerializer = sourceItemSerializer.GetDerivedTypeSerializer(resultType);
                var resultSerializer = IEnumerableOrIQueryableSerializer.Create(expression.Type, resultItemSerializer);
                visitor.AddNodeSerializer(expression, resultSerializer);
            }
        }

        // DeducePushMethodSerializers
        private static void DeducePushMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = (LambdaExpression)expression.Arguments[1];
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selectorLambda);
            visitor.DeduceCollectionAndItemSerializers(expression, selectorLambda.Body);
        }

        // DeduceRoundMethodSerializers
        private static void DeduceRoundMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsNotKnown(expression))
            {
                var resultSerializer = StandardSerializers.GetSerializer(expression.Type);
                visitor.AddNodeSerializer(expression, resultSerializer);
            }
        }

        // DeduceSelectMethodSerializers
        private static void DeduceSelectMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var selectorParameter = selectorLambda.Parameters.Single();
            visitor.DeduceItemAndCollectionSerializers(selectorParameter, sourceExpression);
            visitor.DeduceCollectionAndItemSerializers(expression, selectorLambda.Body);
        }

        private static void DeduceSelectWithSelectorTakingIndexMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var itemParameter = selectorLambda.Parameters[0];
            var indexParameter = selectorLambda.Parameters[1];
            visitor.DeduceItemAndCollectionSerializers(itemParameter, sourceExpression);
            if (visitor.IsNotKnown(indexParameter))
            {
                visitor.AddNodeSerializer(indexParameter, Int32Serializer.Instance);
            }
            visitor.DeduceCollectionAndItemSerializers(expression, selectorLambda.Body);
        }

        // DeduceSelectManySerializers + EnumerableOrQueryableMethod.SelectManyWithSelector branch
        private static void DeduceSelectManyWithSelectorMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var selectorSourceParameter = selectorLambda.Parameters.Single();

            visitor.DeduceItemAndCollectionSerializers(selectorSourceParameter, sourceExpression);
            visitor.DeduceCollectionAndCollectionSerializers(expression, selectorLambda.Body);
        }

        // DeduceSelectManySerializers + EnumerableOrQueryableMethod.SelectManyWithCollectionSelectorAndResultSelector branch
        private static void DeduceSelectManyWithCollectionSelectorAndResultSelectorMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var collectionSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var resultSelectorLambda =  ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);

            var collectionSelectorSourceItemParameter = collectionSelectorLambda.Parameters.Single();
            var resultSelectorSourceItemParameter = resultSelectorLambda.Parameters[0];
            var resultSelectorCollectionItemParameter = resultSelectorLambda.Parameters[1];

            visitor.DeduceItemAndCollectionSerializers(collectionSelectorSourceItemParameter, sourceExpression);
            visitor.DeduceItemAndCollectionSerializers(resultSelectorSourceItemParameter, sourceExpression);
            visitor.DeduceItemAndCollectionSerializers(resultSelectorCollectionItemParameter, collectionSelectorLambda.Body);
            visitor.DeduceCollectionAndItemSerializers(expression, resultSelectorLambda.Body);
        }

        // DeduceSelectManySerializers + EnumerableOrQueryableMethod.SelectManyWithSelectorTakingIndex branch
        private static void SelectManyWithSelectorTakingIndexMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var itemParameter = selectorLambda.Parameters[0];
            var indexParameter = selectorLambda.Parameters[1];
            visitor.DeduceItemAndCollectionSerializers(itemParameter, sourceExpression);
            if (visitor.IsNotKnown(indexParameter))
            {
                visitor.AddNodeSerializer(indexParameter, Int32Serializer.Instance);
            }
            visitor.DeduceCollectionAndCollectionSerializers(expression, selectorLambda.Body);
        }

        // DeduceSequenceEqualMethodSerializers
        private static void DeduceSequenceEqualMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            visitor.DeduceCollectionAndCollectionSerializers(expression.Arguments[0], expression.Arguments[1]);
            visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
        }

        // DeduceSetWindowFieldsMethodSerializers
        // static void DeduceSetWindowFieldsMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        // {
        //     visitor.DeduceCollectionAndCollectionSerializers(expression.Object, expression.Arguments[0]);
        //     visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
        // }

        // DeduceShiftMethodSerializers
        private static void DeduceShiftMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selectorLambda);
            visitor.DeduceSerializers(expression, selectorLambda.Body);
        }

        // DeduceSplitMethodSerializers
        private static void DeduceSplitMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsNotKnown(expression))
            {
                var nodeSerializer = ArraySerializer.Create(StringSerializer.Instance);
                visitor.AddNodeSerializer(expression, nodeSerializer);
            }
        }

        // DeduceSumMethodSerializers + EnumerableOrQueryableMethod.SumOverloads branch
        private static void DeduceEnumerableSumMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(EnumerableOrQueryableMethod.SumWithSelectorOverloads))
            {
                var sourceExpression = expression.Arguments[0];
                var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
                var selectorParameter = selectorLambda.Parameters.Single();
                visitor.DeduceItemAndCollectionSerializers(selectorParameter, sourceExpression);
            }

            var returnType = expression.Type;
            // TODO: check if can replace with StandardSerializers.
            var serializer = returnType switch
            {
                not null when returnType == typeof(decimal) => DecimalSerializer.Instance,
                not null when returnType == typeof(double) => DoubleSerializer.Instance,
                not null when returnType == typeof(int) => Int32Serializer.Instance,
                not null when returnType == typeof(long) => Int64Serializer.Instance,
                not null when returnType == typeof(float) => SingleSerializer.Instance,
                not null when returnType == typeof(decimal?) => NullableSerializer.NullableDecimalInstance,
                not null when returnType == typeof(double?) => NullableSerializer.NullableDoubleInstance,
                not null when returnType == typeof(int?) => NullableSerializer.NullableInt32Instance,
                not null when returnType == typeof(long?) => NullableSerializer.NullableInt64Instance,
                not null when returnType == typeof(float?) => NullableSerializer.NullableSingleInstance,
                _ => throw new NotImplementedException($"Cannot resolver serializer for {returnType}")
            };

            visitor.DeduceSerializer(expression, serializer);
        }

        // DeduceSumMethodSerializers + WindowMethod.SumOverloads branch
        private static void DeduceWindowSumMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = (LambdaExpression)expression.Arguments[1];
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selectorLambda);
            visitor.DeduceStandardSerializer(expression);
        }

        // DeduceWhereSerializers
        private static void DeduceWhereSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var predicateParameter =  predicateLambda.Parameters.Single();
            visitor.DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
            visitor.DeduceCollectionAndCollectionSerializers(expression, sourceExpression);
        }

        private static void DeduceWhereWithPredicateTakingIndexSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var itemParameter = predicateLambda.Parameters[0];
            var indexParameter = predicateLambda.Parameters[1];
            visitor.DeduceItemAndCollectionSerializers(itemParameter, sourceExpression);
            if (visitor.IsNotKnown(indexParameter))
            {
                visitor.AddNodeSerializer(indexParameter, Int32Serializer.Instance);
            }
            visitor.DeduceCollectionAndCollectionSerializers(expression, sourceExpression);
        }

        // DeduceZipSerializers
        private static void DeduceZipSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var firstExpression = expression.Arguments[0];
            var secondExpression = expression.Arguments[1];
            var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[2]);
            var resultSelectorFirstParameter = resultSelectorLambda.Parameters[0];
            var resultSelectorSecondParameter =  resultSelectorLambda.Parameters[1];

            if (visitor.IsNotKnown(resultSelectorFirstParameter) && visitor.IsKnown(firstExpression, out var firstSerializer))
            {
                var firstItemSerializer =  ArraySerializerHelper.GetItemSerializer(firstSerializer);
                visitor.AddNodeSerializer(resultSelectorFirstParameter, firstItemSerializer);
            }

            if (visitor.IsNotKnown(resultSelectorSecondParameter) && visitor.IsKnown(secondExpression, out var secondSerializer))
            {
                var secondItemSerializer =  ArraySerializerHelper.GetItemSerializer(secondSerializer);
                visitor.AddNodeSerializer(resultSelectorSecondParameter, secondItemSerializer);
            }

            if (visitor.IsNotKnown(expression) && visitor.IsKnown(resultSelectorLambda.Body, out var resultItemSerializer))
            {
                var resultSerializer = IEnumerableOrIQueryableSerializer.Create(expression.Type, resultItemSerializer);
                visitor.AddNodeSerializer(expression, resultSerializer);
            }
        }

        // DeduceAppendOrPrependMethodSerializers
        private static void DeduceAppendOrPrependMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var elementExpression = expression.Arguments[1];

            visitor.DeduceItemAndCollectionSerializers(elementExpression, sourceExpression);
            visitor.DeduceCollectionAndCollectionSerializers(expression, sourceExpression);
        }

        // DeduceAverageOrMedianOrPercentileMethodSerializers + __averageOrMedianOrPercentileOverloads branch
        private static void DeduceAverageOrMedianOrPercentileMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(__averageOrMedianOrPercentileWithSelectorOverloads))
            {
                var sourceExpression = expression.Arguments[0];
                var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
                var selectorSourceItemParameter = selectorLambda.Parameters[0];

                visitor.DeduceItemAndCollectionSerializers(selectorSourceItemParameter, sourceExpression);
            }

            if (visitor.IsNotKnown(expression))
            {
                var nodeSerializer = StandardSerializers.GetSerializer(expression.Type);
                visitor.AddNodeSerializer(expression, nodeSerializer);
            }
        }

        // DeduceAverageOrMedianOrPercentileMethodSerializers + __averageOrMedianOrPercentileWindowMethodOverloads branch
        private static void DeduceAverageOrMedianOrPercentileWindowMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = (LambdaExpression)expression.Arguments[1];
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selectorLambda);
            visitor.DeduceStandardSerializer(expression);
        }

        private static void DeduceDeserializeEJsonMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsNotKnown(expression))
            {
                var outputType = expression.Method.GetGenericArguments()[1];
                var outputSerializer = BsonSerializer.LookupSerializer(outputType);
                visitor.AddNodeSerializer(expression, outputSerializer);
            }
        }

        private static void DeduceSerializeEJsonMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsNotKnown(expression))
            {
                var outputType = expression.Method.GetGenericArguments()[1];
                var outputSerializer = BsonSerializer.LookupSerializer(outputType);
                visitor.AddNodeSerializer(expression, outputSerializer);
            }
        }

        // DeducePickMethodSerializers
        private static void DeducePickMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var method = expression.Method;
            if (method.IsOneOf(EnumerableMethod.PickWithSortByOverloads))
            {
                var sortByExpression = expression.Arguments[1];
                if (visitor.IsNotKnown(sortByExpression))
                {
                    var ignoreSubTreeSerializer = IgnoreSubtreeSerializer.Create(sortByExpression.Type);
                    visitor.AddNodeSerializer(sortByExpression, ignoreSubTreeSerializer);
                }
            }

            var sourceExpression = expression.Arguments[0];
            if (visitor.IsKnown(sourceExpression, out var sourceSerializer))
            {
                var sourceItemSerializer = ArraySerializerHelper.GetItemSerializer(sourceSerializer);

                var selectorExpression = expression.Arguments[method.IsOneOf(EnumerableMethod.PickWithSortByOverloads) ? 2 : 1];
                var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, selectorExpression);
                var selectorSourceItemParameter = selectorLambda.Parameters.Single();
                if (visitor.IsNotKnown(selectorSourceItemParameter))
                {
                    visitor.AddNodeSerializer(selectorSourceItemParameter, sourceItemSerializer);
                }
            }

            if (method.IsOneOf(EnumerableMethod.PickWithComputedNOverloads))
            {
                var keyExpression = expression.Arguments[method.IsOneOf(EnumerableMethod.PickWithSortByOverloads) ? 3 : 2];
                if (visitor.IsKnown(keyExpression, out var keySerializer))
                {
                    var nExpression = expression.Arguments[method.IsOneOf(EnumerableMethod.PickWithSortByOverloads) ? 4 : 3];
                    var nLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, nExpression);
                    var nLambdaKeyParameter = nLambda.Parameters.Single();

                    if (visitor.IsNotKnown(nLambdaKeyParameter))
                    {
                        visitor.AddNodeSerializer(nLambdaKeyParameter, keySerializer);
                    }
                }
            }

            if (visitor.IsNotKnown(expression))
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
                var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, expression.Arguments[selectorExpressionIndex]);

                if (visitor.IsKnown(selectorLambda.Body, out var selectorItemSerializer))
                {
                    var nodeSerializer = method.IsOneOf(EnumerableMethod.Bottom, EnumerableMethod.Top) ?
                        selectorItemSerializer :
                        IEnumerableSerializer.Create(selectorItemSerializer);
                    visitor.AddNodeSerializer(expression, nodeSerializer);
                }
            }
        }

        // DeduceCountMethodSerializers + EnumerableOrQueryableMethod.CountOverloads branch
        private static void DeduceEnumerableCountMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(EnumerableOrQueryableMethod.CountWithPredicateOverloads))
            {
                var sourceExpression = expression.Arguments[0];
                var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
                var predicateParameter = predicateLambda.Parameters.Single();
                visitor.DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
            }

            DeduceReturnsNumericSerializer(visitor, expression);
        }

        // DeduceCovarianceMethodSerializers
        private static void DeduceCovarianceMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selector1Lambda = (LambdaExpression)expression.Arguments[1];
            var selector2Lambda = (LambdaExpression)expression.Arguments[2];
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selector1Lambda);
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selector2Lambda);
            visitor.DeduceStandardSerializer(expression);
        }

        // DeduceDerivativeOrIntegralMethodSerializers
        private static void DeduceDerivativeOrIntegralMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = (LambdaExpression)expression.Arguments[1];
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selectorLambda);
            visitor.DeduceStandardSerializer(expression);
        }

        // DeduceFirstOrLastOrSingleMethodsSerializers + EnumerableOrQueryableMethod.FirstOrLastOrSingleOverloads
        private static void DeduceFirstOrLastOrSingleMethodsSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(EnumerableOrQueryableMethod.FirstOrLastOrSingleWithPredicateOverloads))
            {
                var sourceExpression = expression.Arguments[0];
                var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
                var predicateParameter = predicateLambda.Parameters.Single();
                visitor.DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);
            }

            DeduceReturnsOneSourceItemSerializer(visitor, expression);
        }

        // DeduceFirstOrLastOrSingleMethodsSerializers + WindowMethod.First, WindowMethod.Last branch
        private static void DeduceFirstOrLastWindowMethodsSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = (LambdaExpression)expression.Arguments[1];
            DeduceWindowMethodSelectorParameterSerializer(visitor,partitionExpression, selectorLambda);
            visitor.DeduceSerializers(expression, selectorLambda.Body);
        }

        // DeduceMaxOrMinMethodSerializers + EnumerableOrQueryableMethod.MaxOrMinOverloads branch
        private static void DeduceMaxOrMinMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(EnumerableOrQueryableMethod.MaxOrMinWithSelectorOverloads))
            {
                var sourceExpression = expression.Arguments[0];
                var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
                var selectorItemParameter = selectorLambda.Parameters.Single();

                visitor.DeduceItemAndCollectionSerializers(selectorItemParameter, sourceExpression);
                visitor.DeduceSerializers(expression, selectorLambda.Body);
            }
            else
            {
                DeduceReturnsOneSourceItemSerializer(visitor, expression);
            }
        }

        // DeduceMaxOrMinMethodSerializers + WindowMethod.Max, WindowMethod.Min branch
        private static void DeduceWindowMaxOrMinMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = (LambdaExpression)expression.Arguments[1];
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selectorLambda);
            visitor.DeduceSerializers(expression, selectorLambda.Body);
        }

        // DeduceOrderByMethodSerializers
        private static void DeduceOrderByMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];
            var keySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
            var keySelectorParameter = keySelectorLambda.Parameters.Single();

            visitor.DeduceItemAndCollectionSerializers(keySelectorParameter, sourceExpression);
            visitor.DeduceCollectionAndCollectionSerializers(expression, sourceExpression);
        }

        // DeduceSkipOrTakeMethodSerializers
        private static void DeduceSkipOrTakeMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];

            if (expression.Method.IsOneOf(EnumerableOrQueryableMethod.SkipWhileOrTakeWhile))
            {
                var predicateLambda =  ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
                var predicateParameter = predicateLambda.Parameters[0];
                visitor.DeduceItemAndCollectionSerializers(predicateParameter, sourceExpression);

                if (expression.Method.IsOneOf(EnumerableOrQueryableMethod.SkipWhileWithPredicateTakingIndexOrTakeWhileWithPredicateTakingIndex))
                {
                    var indexParameter = predicateLambda.Parameters[1];
                    if (visitor.IsNotKnown(indexParameter))
                    {
                        visitor.AddNodeSerializer(indexParameter, Int32Serializer.Instance);
                    }
                }
            }

            visitor.DeduceCollectionAndCollectionSerializers(expression, sourceExpression);
        }

        // DeduceStandardDeviationMethodSerializers + MongoEnumerableMethod.StandardDeviationOverloads branch
        private static void DeduceStandardDeviationMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(MongoEnumerableMethod.StandardDeviationWithSelectorOverloads, MongoQueryableMethod.StandardDeviationWithSelectorOverloads))
            {
                var sourceExpression = expression.Arguments[0];
                var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Arguments[1]);
                var selectorItemParameter = selectorLambda.Parameters.Single();
                visitor.DeduceCollectionAndItemSerializers(sourceExpression, selectorItemParameter);
            }

            visitor.DeduceStandardSerializer(expression);
        }

        private static void DeduceStringEqualsMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var expression1 = expression.Method.IsStatic ? expression.Arguments[0] : expression.Object;
            var expression2 = expression.Method.IsStatic ? expression.Arguments[1] : expression.Arguments[0];

            visitor.DeduceSerializers(expression1, expression2);
            visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
        }

        // DeduceStandardDeviationMethodSerializers + WindowMethod.StandardDeviationOverloads branch
        private static void DeduceWindowStandardDeviationMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var partitionExpression = expression.Arguments[0];
            var selectorLambda = (LambdaExpression)expression.Arguments[1];
            DeduceWindowMethodSelectorParameterSerializer(visitor, partitionExpression, selectorLambda);
            visitor.DeduceStandardSerializer(expression);
        }

        private static void DeduceReturnsNumericSerializer(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            if (visitor.IsNotKnown(expression) && expression.Type.IsNumeric())
            {
                var numericSerializer = StandardSerializers.GetSerializer(expression.Type);
                visitor.AddNodeSerializer(expression, numericSerializer);
            }
        }

        private static void DeduceReturnsTimeSpanSerializer(SerializerFinderVisitor visitor, MethodCallExpression expression, TimeSpanUnits units)
        {
            if (visitor.IsNotKnown(expression))
            {
                var resultSerializer = new TimeSpanSerializer(BsonType.Int64, units);
                visitor.AddNodeSerializer(expression, resultSerializer);
            }
        }

        private static void DeduceReturnsOneSourceItemSerializer(SerializerFinderVisitor visitor, MethodCallExpression expression)
        {
            var sourceExpression = expression.Arguments[0];

            if (visitor.IsNotKnown(expression) && visitor.IsKnown(sourceExpression, out var sourceSerializer))
            {
                var nodeSerializer = sourceSerializer is IUnknowableSerializer ?
                    UnknowableSerializer.Create(expression.Type) :
                    ArraySerializerHelper.GetItemSerializer(sourceSerializer);
                visitor.AddNodeSerializer(expression, nodeSerializer);
            }
        }

        private static void DeduceWindowMethodSelectorParameterSerializer(SerializerFinderVisitor visitor, Expression partitionExpression, LambdaExpression selectorLambda)
        {
            var inputParameter = selectorLambda.Parameters.Single();
            if (visitor.IsNotKnown(inputParameter) && visitor.IsKnown(partitionExpression, out var partitionSerializer))
            {
                var inputSerializer = ((ISetWindowFieldsPartitionSerializer)partitionSerializer).InputSerializer;
                visitor.AddNodeSerializer(inputParameter, inputSerializer);
            }
        }

        public static Action<SerializerFinderVisitor,MethodCallExpression> GetSerializerDeducer(MethodInfo methodInfo)
        {
            __serializerDeducersLock.EnterReadLock();
            try
            {
                if (__serializerDeducers.TryGetValue(methodInfo, out var deducer))
                {
                    return deducer;
                }
            }
            finally
            {
                __serializerDeducersLock.ExitReadLock();
            }

            __serializerDeducersLock.EnterWriteLock();
            try
            {
                if (__serializerDeducers.TryGetValue(methodInfo, out var deducer))
                {
                    return deducer;
                }

                deducer = CreateDynamicSerializerDeducer(methodInfo);
                __serializerDeducers.Add(methodInfo, deducer);
                return deducer;
            }
            finally
            {
                __serializerDeducersLock.ExitWriteLock();
            }
        }

        // Processes special cases where we do not have fixed list of methodInfos that we support,
        // but trying to deduce serializers based on method name and arguments
        static Action<SerializerFinderVisitor, MethodCallExpression> CreateDynamicSerializerDeducer(MethodInfo method)
        {
            return method.Name switch
            {
                "Contains" when EnumerableMethod.IsContainsMethod(method) => DeduceContainsMethodSerializers,
                "ContainsKey" when DictionaryMethod.IsContainsKeyMethod(method) => DeduceDictionaryContainsKeyMethodSerializers,
                "ContainsValue" when IsContainsValueMethod(method) => DeduceContainsValueMethodSerializers,
                "Equals" when IsEqualsReturningBooleanMethod(method) => DeduceEqualsMethodSerializers,
                "Exists" when method.Is(ArrayMethod.Exists) || ListMethod.IsExistsMethod(method) => DeduceExistsMethodSerializers,
                "get_Item" when BsonValueMethod.IsGetItemWithIntMethod(method) || BsonValueMethod.IsGetItemWithStringMethod(method) =>
                    (visitor, expression) => visitor.DeduceSerializer(expression, BsonValueSerializer.Instance),
                "get_Item" when !method.IsStatic => DeduceGetItemInstanceMethodSerializers,
                "IsSubsetOf" when IsSubsetOfMethod(method) => DeduceIsSubsetOfMethodSerializers,
                "Parse" when IsParseMethod(method) => DeduceParseMethodSerializers,
                "SetEquals" when ISetMethod.IsSetEqualsMethod(method) => DeduceSetEqualsMethodSerializers,
                "ToArray" when IsToArrayMethod(method) => DeduceToArrayMethodSerializers,
                "ToList" => DeduceToListSerializers,
                "ToString" => (visitor, expression) => visitor.DeduceSerializer(expression, StringSerializer.Instance),
                "Compare" or "CompareTo" when IsCompareMethod(method) => DeduceCompareOrCompareToMethodSerializers,
                _ => null
            };

            static bool IsCompareMethod(MethodInfo method) =>
                method.IsStaticCompareMethod() ||
                method.IsInstanceCompareToMethod() ||
                method.IsOneOf(StringMethod.CompareOverloads);

            static void DeduceCompareOrCompareToMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                var valueExpression = expression.Method.IsStatic ? expression.Arguments[0] : expression.Object;
                var comparandExpression = expression.Method.IsStatic ? expression.Arguments[1] : expression.Arguments[0];
                visitor.DeduceSerializers(valueExpression, comparandExpression);
                visitor.DeduceSerializer(expression, Int32Serializer.Instance);
            }

            static void DeduceContainsMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                if (EnumerableMethod.IsContainsMethod(expression, out var collectionExpression, out var itemExpression))
                {
                    visitor.DeduceCollectionAndItemSerializers(collectionExpression, itemExpression);
                    visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
                }
            }

            static bool IsContainsValueMethod(MethodInfo method) =>
                method.IsPublic &&
                method.IsStatic == false &&
                method.ReturnType == typeof(bool) &&
                method.Name == "ContainsValue" &&
                method.GetParameters() is var parameters &&
                parameters.Length == 1;

            void DeduceContainsValueMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                var collectionExpression = expression.Object;
                var valueExpression = expression.Arguments[0];

                if (visitor.IsNotKnown(valueExpression) &&
                    visitor.IsKnown(collectionExpression, out var collectionSerializer))
                {
                    if (collectionSerializer is IBsonDictionarySerializer dictionarySerializer)
                    {
                        var valueSerializer = dictionarySerializer.ValueSerializer;
                        visitor.AddNodeSerializer(valueExpression, valueSerializer);
                    }
                }

                visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
            }

            void DeduceDictionaryContainsKeyMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                var dictionaryExpression = expression.Object;
                var keyExpression = expression.Arguments[0];
                if (visitor.IsNotKnown(keyExpression) && visitor.IsKnown(dictionaryExpression, out var dictionarySerializer))
                {
                    var keySerializer = (dictionarySerializer as IBsonDictionarySerializer)?.KeySerializer;
                    visitor.AddNodeSerializer(keyExpression, keySerializer);
                }

                visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
            }

            static bool IsEqualsReturningBooleanMethod(MethodInfo method)
            {
                var arguments = method.GetParameters();
                return method.Name == "Equals" &&
                       method.ReturnType == typeof(bool) &&
                       method.IsPublic &&
                       (
                           (method.IsStatic && arguments.Length == 2) ||
                           (!method.IsStatic && arguments.Length == 1)
                       );
            }

            void DeduceEqualsMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                var expression1 = expression.Method.IsStatic ? expression.Arguments[0] : expression.Object;
                var expression2 = expression.Method.IsStatic ? expression.Arguments[1] : expression.Arguments[0];

                visitor.DeduceSerializers(expression1, expression2);
                visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
            }

            static void DeduceExistsMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                var collectionExpression = expression.Method.IsStatic ? expression.Arguments[0] : expression.Object;
                var predicateExpression = ExpressionHelper.UnquoteLambdaIfQueryableMethod(expression.Method, expression.Method.IsStatic ? expression.Arguments[1] : expression.Arguments[0]);
                var predicateParameter = predicateExpression.Parameters.Single();
                visitor.DeduceItemAndCollectionSerializers(predicateParameter, collectionExpression);
                visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
            }

            static void DeduceGetItemInstanceMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                if (visitor.IsNotKnown(expression))
                {
                    var collectionExpression = expression.Object;
                    var indexExpression = expression.Arguments[0];

                    if (visitor.IsKnown(collectionExpression, out var collectionSerializer))
                    {
                        if (collectionSerializer is IBsonArraySerializer arraySerializer &&
                            indexExpression.Type == typeof(int) &&
                            arraySerializer.GetItemSerializer() is var itemSerializer &&
                            itemSerializer.ValueType == expression.Method.ReturnType)
                        {
                            visitor.AddNodeSerializer(expression, itemSerializer);
                        }
                        else if (
                            collectionSerializer is IBsonDictionarySerializer dictionarySerializer &&
                            dictionarySerializer.KeySerializer is var keySerializer &&
                            dictionarySerializer.ValueSerializer is var valueSerializer &&
                            keySerializer.ValueType == indexExpression.Type &&
                            valueSerializer.ValueType == expression.Method.ReturnType)
                        {
                            visitor.AddNodeSerializer(expression, valueSerializer);
                        }
                    }
                }
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

            static void DeduceIsSubsetOfMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                var objectExpression = expression.Object;
                var otherExpression = expression.Arguments[0];

                visitor.DeduceCollectionAndCollectionSerializers(objectExpression, otherExpression);
                visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
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

            static void DeduceParseMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                if (visitor.IsNotKnown(expression))
                {
                    var declaringType = expression.Method.DeclaringType;
                    var nodeSerializer = declaringType switch
                    {
                        _ when declaringType == typeof(DateTime) => DateTimeSerializer.Instance,
                        _ when declaringType == typeof(decimal) => DecimalSerializer.Instance,
                        _ when declaringType == typeof(double) => DoubleSerializer.Instance,
                        _ when declaringType == typeof(int) => Int32Serializer.Instance,
                        _ when declaringType == typeof(short) => Int16Serializer.Instance,
                        _ when declaringType == typeof(long) => Int64Serializer.Instance,
                        _ when declaringType == typeof(float) => SingleSerializer.Instance,
                        _ when declaringType == typeof(byte) => ByteSerializer.Instance,
                        _ when declaringType == typeof(sbyte) => SByteSerializer.Instance,
                        _ => UnknowableSerializer.Create(declaringType)
                    };

                    visitor.AddNodeSerializer(expression, nodeSerializer);
                }
            }

            static void DeduceSetEqualsMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                var objectExpression = expression.Object;
                var otherExpression = expression.Arguments[0];

                visitor.DeduceCollectionAndCollectionSerializers(objectExpression, otherExpression);
                visitor.DeduceSerializer(expression, BooleanSerializer.Instance);
            }

            static bool IsToArrayMethod(MethodInfo method) =>
                method.IsPublic &&
                method.Name == "ToArray" &&
                method.GetParameters().Length == (method.IsStatic ? 1 : 0);

            static void DeduceToArrayMethodSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                var sourceExpression = expression.Method.IsStatic ? expression.Arguments[0] : expression.Object;
                visitor.DeduceCollectionAndCollectionSerializers(expression, sourceExpression);
            }

            static void DeduceToListSerializers(SerializerFinderVisitor visitor, MethodCallExpression expression)
            {
                if (visitor.IsNotKnown(expression))
                {
                    var source = expression.Method.IsStatic ? expression.Arguments[0] : expression.Object;
                    if (visitor.IsKnown(source, out var sourceSerializer))
                    {
                        var sourceItemSerializer = ArraySerializerHelper.GetItemSerializer(sourceSerializer);
                        var resultSerializer = ListSerializer.Create(sourceItemSerializer);
                        visitor.AddNodeSerializer(expression, resultSerializer);
                    }
                }
            }
        }
    }
}

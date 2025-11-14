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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    private static readonly IReadOnlyMethodInfoSet __averageOrMedianOrPercentileOverloads = MethodInfoSet.Create(
    [
        EnumerableOrQueryableMethod.AverageOverloads,
        MongoEnumerableMethod.MedianOverloads,
        MongoEnumerableMethod.PercentileOverloads,
        WindowMethod.PercentileOverloads
    ]);

    private static readonly IReadOnlyMethodInfoSet __averageOrMedianOrPercentileWithSelectorOverloads = MethodInfoSet.Create(
    [
        EnumerableOrQueryableMethod.AverageWithSelectorOverloads,
        MongoEnumerableMethod.MedianWithSelectorOverloads,
        MongoEnumerableMethod.PercentileWithSelectorOverloads,
        WindowMethod.PercentileOverloads
    ]);

    private static readonly IReadOnlyMethodInfoSet __whereOverloads = MethodInfoSet.Create(
    [
        EnumerableOrQueryableMethod.Where,
        [MongoEnumerableMethod.WhereWithLimit]
    ]);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var method = node.Method;
        var arguments = node.Arguments;

        DeduceMethodCallSerializers();
        base.VisitMethodCall(node);
        DeduceMethodCallSerializers();

        return node;

        void DeduceMethodCallSerializers()
        {
            switch (node.Method.Name)
            {
                case "Abs": DeduceAbsMethodSerializers(); break;
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
                case "AsQueryable": DeduceAsQueryableMethodSerializers(); break;
                case "Concat": DeduceConcatMethodSerializers(); break;
                case "Constant": DeduceConstantMethodSerializers(); break;
                case "Contains": DeduceContainsMethodSerializers(); break;
                case "ContainsKey": DeduceContainsKeyMethodSerializers(); break;
                case "ContainsValue": DeduceContainsValueMethodSerializers(); break;
                case "Convert": DeduceConvertMethodSerializers(); break;
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
                case "get_Chars": DeduceGetCharsMethodSerializers(); break;
                case "GroupBy": DeduceGroupByMethodSerializers(); break;
                case "GroupJoin": DeduceGroupJoinMethodSerializers(); break;
                case "Inject": DeduceInjectMethodSerializers(); break;
                case "Intersect": DeduceIntersectMethodSerializers(); break;
                case "IsMatch": DeduceIsMatchMethodSerializers(); break;
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
                case "Round": DeduceRoundMethodSerializers(); break;
                case "Select": DeduceSelectMethodSerializers(); break;
                case "SelectMany": DeduceSelectManySerializers(); break;
                case "SequenceEqual": DeduceSequenceEqualMethodSerializers(); break;
                case "SetEquals": DeduceSetEqualsMethodSerializers(); break;
                case "SetWindowFields": DeduceSetWindowFieldsMethodSerializers(); break;
                case "Shift": DeduceShiftMethodSerializers(); break;
                case "Split": DeduceSplitMethodSerializers(); break;
                case "Sqrt": DeduceSqrtMethodSerializers(); break;
                case "StringIn": DeduceStringInMethodSerializers(); break;
                case "StrLenBytes": DeduceStrLenBytesMethodSerializers(); break;
                case "Subtract": DeduceSubtractMethodSerializers(); break;
                case "Sum": DeduceSumMethodSerializers(); break;
                case "ToArray": DeduceToArrayMethodSerializers(); break;
                case "ToList": DeduceToListSerializers(); break;
                case "ToString": DeduceToStringSerializers(); break;
                case "Truncate": DeduceTruncateSerializers(); break;
                case "Union": DeduceUnionSerializers(); break;
                case "Week": DeduceWeekSerializers(); break;
                case "Where": DeduceWhereSerializers(); break;
                case "Zip": DeduceZipSerializers(); break;

                case "Acos":
                case "Acosh":
                case "Asin":
                case "Asinh":
                case "Atan":
                case "Atanh":
                case "Atan2":
                case "Cos":
                case "Cosh":
                case "Sin":
                case "Sinh":
                case "Tan":
                case "Tanh":
                    DeduceTrigonometricMethodSerializers();
                    break;

                case "AllElements":
                case "AllMatchingElements":
                case "FirstMatchingElement":
                    DeduceMatchingElementsMethodSerializers();
                    break;

                case "Append":
                case "Prepend":
                    DeduceAppendOrPrependMethodSerializers();
                    break;

                case "Average":
                case "Median":
                case "Percentile":
                    DeduceAverageOrMedianOrPercentileMethodSerializers();
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

                case "Compare":
                case "CompareTo":
                    DeduceCompareOrCompareToMethodSerializers();
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
                    DeduceFirstOrLastOrSingleMethodsSerializers();
                    break;

                case "IndexOf":
                case "IndexOfBytes":
                    DeduceIndexOfMethodSerializers();
                    break;

                case "IsMissing":
                case "IsNullOrMissing":
                    DeduceIsMissingOrIsNullOrMissingMethodSerializers();
                    break;

                case "IsNullOrEmpty":
                case "IsNullOrWhiteSpace":
                    DeduceIsNullOrEmptyOrIsNullOrWhiteSpaceMethodSerializers();
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
            if (method.IsOneOf(MathMethod.AbsOverloads))
            {
                var valueExpression = arguments[0];
                DeduceSerializers(node, valueExpression);
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
            if (method.IsOneOf(EnumerableOrQueryableMethod.AggregateOverloads))
            {
                var sourceExpression = arguments[0];

                if (method.IsOneOf(EnumerableOrQueryableMethod.AggregateWithFunc))
                {
                    var funcLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var funcAccumulatorParameter = funcLambda.Parameters[0];
                    var funcSourceItemParameter = funcLambda.Parameters[1];

                    DeduceItemAndCollectionSerializers(funcAccumulatorParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(funcSourceItemParameter, sourceExpression);
                    DeduceItemAndCollectionSerializers(funcLambda.Body, sourceExpression);
                    DeduceSerializers(node, funcLambda.Body);
                }

                if (method.IsOneOf(EnumerableOrQueryableMethod.AggregateWithSeedAndFunc))
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

                if (method.IsOneOf(EnumerableOrQueryableMethod.AggregateWithSeedFuncAndResultSelector))
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
            if (method.IsOneOf(EnumerableOrQueryableMethod.AnyOverloads))
            {
                if (method.IsOneOf(EnumerableOrQueryableMethod.AnyWithPredicate))
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
            if (method.IsOneOf(EnumerableOrQueryableMethod.AppendOrPrepend))
            {
                var sourceExpression = arguments[0];
                var elementExpression = arguments[1];

                DeduceItemAndCollectionSerializers(elementExpression, sourceExpression);
                DeduceCollectionAndCollectionSerializers(node, sourceExpression);
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
                    AddNodeSerializer(node, resultSerializer);
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
                        AddNodeSerializer(node, resultSerializer);
                    }
                }
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
                    var resultSerializer = NestedAsQueryableSerializer.Create(sourceItemSerializer);
                    AddNodeSerializer(node, resultSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceAverageOrMedianOrPercentileMethodSerializers()
        {
            if (method.IsOneOf(__averageOrMedianOrPercentileOverloads))
            {
                if (method.IsOneOf(__averageOrMedianOrPercentileWithSelectorOverloads))
                {
                    var sourceExpression = arguments[0];
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var selectorSourceItemParameter = selectorLambda.Parameters[0];

                    DeduceItemAndCollectionSerializers(selectorSourceItemParameter, sourceExpression);
                }

                if (IsNotKnown(node))
                {
                    var nodeSerializer = StandardSerializers.GetSerializer(node.Type);
                    AddNodeSerializer(node, nodeSerializer);
                }
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

        void DeduceCompareOrCompareToMethodSerializers()
        {
            if (method.IsStaticCompareMethod() ||
                method.IsInstanceCompareToMethod() ||
                method.IsOneOf(StringMethod.CompareOverloads))
            {
                var valueExpression = method.IsStatic ? arguments[0] : node.Object;
                var comparandExpression = method.IsStatic ? arguments[1] : arguments[0];
                DeduceSerializers(valueExpression, comparandExpression);
                DeduceReturnsInt32Serializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
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
            else if (method.IsOneOf(StringMethod.ConcatOverloads))
            {
                DeduceReturnsStringSerializer();
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
            if (IsDictionaryContainsKeyExpression(out var keyExpression))
            {
                var dictionaryExpression = node.Object;
                if (IsNotKnown(keyExpression) && IsKnown(dictionaryExpression, out var dictionarySerializer))
                {
                    var keySerializer = (dictionarySerializer as IBsonDictionarySerializer)?.KeySerializer;
                    AddNodeSerializer(keyExpression, keySerializer);
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
            if (method.IsOneOf(StringMethod.ContainsOverloads))
            {
                DeduceReturnsBooleanSerializer();
            }
            else if (EnumerableMethod.IsContainsMethod(node, out var collectionExpression, out var itemExpression))
            {
                DeduceCollectionAndItemSerializers(collectionExpression, itemExpression);
                DeduceReturnsBooleanSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
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
                        AddNodeSerializer(valueExpression, valueSerializer);
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

        void DeduceConvertMethodSerializers()
        {
            if (method.Is(MqlMethod.Convert))
            {
                if (IsNotKnown(node))
                {
                    var toType = method.GetGenericArguments()[1];
                    var resultSerializer = GetResultSerializer(node, toType);
                    AddNodeSerializer(node, resultSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
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

        void DeduceCreateMethodSerializers()
        {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (method.Is(KeyValuePairMethod.Create))
            {
                if (IsAnyNotKnown(arguments) && IsKnown(node, out var nodeSerializer))
                {
                    var keyExpression = arguments[0];
                    var valueExpression = arguments[1];

                    if (nodeSerializer.IsKeyValuePairSerializer(out _, out _, out var keySerializer, out var valueSerializer))
                    {
                        DeduceSerializer(keyExpression, keySerializer);
                        DeduceSerializer(valueExpression, valueSerializer);
                    }
                }

                if (IsNotKnown(node) && AreAllKnown(arguments, out var argumentSerializers))
                {
                    var keySerializer = argumentSerializers[0];
                    var valueSerializer = argumentSerializers[1];
                    var keyValuePairSerializer = KeyValuePairSerializer.Create(BsonType.Document, keySerializer, valueSerializer);
                    AddNodeSerializer(node, keyValuePairSerializer);
                }
            }
            else
 #endif
            if (method.IsOneOf(TupleOrValueTupleMethod.CreateOverloads))
            {
                if (IsAnyNotKnown(arguments) && IsKnown(node, out var nodeSerializer))
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
                                AddNodeSerializer(argumentExpression, itemSerializer);
                            }
                        }
                    }
                }

                if (IsNotKnown(node) && AreAllKnown(arguments, out var argumentSerializers))
                {
                    var tupleType = method.ReturnType;

                    if (arguments.Count == 8)
                    {
                        var item8Expression = arguments[7];
                        var item8Type = item8Expression.Type;
                        var item8Serializer = argumentSerializers[7];
                        var restTupleType = (tupleType.IsTuple() ? typeof(Tuple<>) : typeof(ValueTuple<>)).MakeGenericType(item8Type);
                        var restSerializer = TupleOrValueTupleSerializer.Create(restTupleType, [item8Serializer]);
                        argumentSerializers = argumentSerializers.Take(7).Append(restSerializer).ToArray();
                    }

                    var tupleSerializer = TupleOrValueTupleSerializer.Create(tupleType, argumentSerializers);
                    AddNodeSerializer(node, tupleSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceCountMethodSerializers()
        {
            if (method.IsOneOf(EnumerableOrQueryableMethod.CountOverloads))
            {
                if (method.IsOneOf(EnumerableOrQueryableMethod.CountWithPredicateOverloads))
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
                    AddNodeSerializer(node, nodeSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceElementAtMethodSerializers()
        {
            if (method.IsOneOf(EnumerableOrQueryableMethod.ElementAtOverloads))
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
            if (method.Is(MathMethod.Exp))
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

                    AddNodeSerializer(node, fieldSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceFirstOrLastOrSingleMethodsSerializers()
        {
            if (method.IsOneOf(EnumerableOrQueryableMethod.FirstOrLastOrSingleOverloads))
            {
                if (method.IsOneOf(EnumerableOrQueryableMethod.FirstOrLastOrSingleWithPredicateOverloads))
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
                    AddNodeSerializer(node, BsonValueSerializer.Instance);
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
                            AddNodeSerializer(node, itemSerializer);
                        }
                        else if (
                            collectionSerializer is IBsonDictionarySerializer dictionarySerializer &&
                            dictionarySerializer.KeySerializer is var keySerializer &&
                            dictionarySerializer.ValueSerializer is var valueSerializer &&
                            keySerializer.ValueType == indexExpression.Type &&
                            valueSerializer.ValueType == method.ReturnType)
                        {
                            AddNodeSerializer(node, valueSerializer);
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

        void DeduceGetCharsMethodSerializers()
        {
            if (method.Is(StringMethod.GetChars))
            {
                DeduceCharSerializer(node);
            }

            DeduceUnknowableSerializer(node);
        }

        void DeduceGroupByMethodSerializers()
        {
            if (method.IsOneOf(EnumerableOrQueryableMethod.GroupByOverloads))
            {
                var sourceExpression = arguments[0];
                var keySelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var keySelectorParameter = keySelectorLambda.Parameters.Single();

                DeduceItemAndCollectionSerializers(keySelectorParameter, sourceExpression);

                if (method.IsOneOf(EnumerableOrQueryableMethod.GroupByWithKeySelector))
                {
                    if (IsNotKnown(node) && IsKnown(keySelectorLambda.Body, out var keySerializer) && IsItemSerializerKnown(sourceExpression, out var elementSerializer))
                    {
                        var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);
                        var nodeSerializer = IEnumerableOrIQueryableSerializer.Create(node.Type, groupingSerializer);
                        AddNodeSerializer(node, nodeSerializer);
                    }
                }
                else if (method.IsOneOf(EnumerableOrQueryableMethod.GroupByWithKeySelectorAndElementSelector))
                {
                    var elementSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var elementSelectorParameter = elementSelectorLambda.Parameters.Single();
                    DeduceItemAndCollectionSerializers(elementSelectorParameter, sourceExpression);
                    if (IsNotKnown(node) && IsKnown(keySelectorLambda.Body, out var keySerializer) && IsKnown(elementSelectorLambda.Body, out var elementSerializer))
                    {
                        var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);
                        var nodeSerializer = IEnumerableOrIQueryableSerializer.Create(node.Type, groupingSerializer);
                        AddNodeSerializer(node, nodeSerializer);
                    }
                }
                else if (method.IsOneOf(EnumerableOrQueryableMethod.GroupByWithKeySelectorAndResultSelector))
                {
                    var resultSelectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[2]);
                    var resultSelectorKeyParameter = resultSelectorLambda.Parameters[0];
                    var resultSelectorElementsParameter = resultSelectorLambda.Parameters[1];
                    DeduceItemAndCollectionSerializers(keySelectorParameter, sourceExpression);
                    DeduceSerializers(resultSelectorKeyParameter, keySelectorLambda.Body);
                    DeduceCollectionAndCollectionSerializers(resultSelectorElementsParameter, sourceExpression);
                    DeduceResultSerializer(resultSelectorLambda.Body);
                }
                else if (method.IsOneOf(EnumerableOrQueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector))
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
                        AddNodeSerializer(node, nodeSerializer);
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
            if (method.IsOneOf(StringMethod.IndexOfOverloads))
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

        void DeduceIsNullOrEmptyOrIsNullOrWhiteSpaceMethodSerializers()
        {
            if (method.IsOneOf(StringMethod.IsNullOrEmpty, StringMethod.IsNullOrWhiteSpace))
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
            if (method.IsOneOf(MathMethod.LogOverloads))
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
            if (method.IsOneOf(MongoQueryableMethod.LookupOverloads))
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
                        AddNodeSerializer(node, IQueryableSerializer.Create(lookupResultSerializer));
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
                        AddNodeSerializer(node, IQueryableSerializer.Create(lookupResultSerializer));
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
                        AddNodeSerializer(node, IQueryableSerializer.Create(lookupResultSerializer));
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
                        AddNodeSerializer(node, IQueryableSerializer.Create(lookupResultSerializer));
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
                        AddNodeSerializer(pipelineLamdbaForeignQueryableParameter, foreignQueryableSerializer);
                    }

                    if (IsNotKnown(node) &&
                        IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                        IsItemSerializerKnown(pipelineLambda.Body, out var pipelineItemSerializer))
                    {
                        var lookupResultsSerializer = LookupResultSerializer.Create(sourceItemSerializer, pipelineItemSerializer);
                        AddNodeSerializer(node, IQueryableSerializer.Create(lookupResultsSerializer));
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
                        AddNodeSerializer(pipelineLamdbaForeignQueryableParameter, foreignQueryableSerializer);
                    }

                    if (IsNotKnown(node) &&
                        IsItemSerializerKnown(sourceExpression, out var sourceItemSerializer) &&
                        IsItemSerializerKnown(pipelineLambda.Body, out var pipelineItemSerializer))
                    {
                        var lookupResultSerializer = LookupResultSerializer.Create(sourceItemSerializer, pipelineItemSerializer);
                        AddNodeSerializer(node, IQueryableSerializer.Create(lookupResultSerializer));
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
            if (method.IsOneOf(EnumerableOrQueryableMethod.MaxOrMinOverloads))
            {
                if (method.IsOneOf(EnumerableOrQueryableMethod.MaxOrMinWithSelectorOverloads))
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
                    AddNodeSerializer(node, resultSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceOrderByMethodSerializers()
        {
            if (method.IsOneOf(EnumerableOrQueryableMethod.OrderByOrThenByOverloads))
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
            if (method.IsOneOf(EnumerableMethod.PickOverloads))
            {
                if (method.IsOneOf(EnumerableMethod.PickWithSortByOverloads))
                {
                    var sortByExpression = arguments[1];
                    if (IsNotKnown(sortByExpression))
                    {
                        var ignoreSubTreeSerializer = IgnoreSubtreeSerializer.Create(sortByExpression.Type);
                        AddNodeSerializer(sortByExpression, ignoreSubTreeSerializer);
                    }
                }

                var sourceExpression = arguments[0];
                if (IsKnown(sourceExpression, out var sourceSerializer))
                {
                    var sourceItemSerializer =  ArraySerializerHelper.GetItemSerializer(sourceSerializer);

                    var selectorExpression = arguments[method.IsOneOf(EnumerableMethod.PickWithSortByOverloads) ? 2 : 1];
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, selectorExpression);
                    var selectorSourceItemParameter = selectorLambda.Parameters.Single();
                    if (IsNotKnown(selectorSourceItemParameter))
                    {
                        AddNodeSerializer(selectorSourceItemParameter, sourceItemSerializer);
                    }
                }

                if (method.IsOneOf(EnumerableMethod.PickWithComputedNOverloads))
                {
                    var keyExpression = arguments[method.IsOneOf(EnumerableMethod.PickWithSortByOverloads) ? 3 : 2];
                    if (IsKnown(keyExpression, out var keySerializer))
                    {
                        var nExpression = arguments[method.IsOneOf(EnumerableMethod.PickWithSortByOverloads) ? 4 : 3];
                        var nLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, nExpression);
                        var nLambdaKeyParameter = nLambda.Parameters.Single();

                        if (IsNotKnown(nLambdaKeyParameter))
                        {
                            AddNodeSerializer(nLambdaKeyParameter, keySerializer);
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
                        AddNodeSerializer(node, nodeSerializer);
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
                    AddNodeSerializer(node, nodeSerializer);
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
            if (method.Is(MathMethod.Pow))
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
                AddNodeSerializer(node, BooleanSerializer.Instance);
            }
        }

        void DeduceReturnsDateTimeSerializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, DateTimeSerializer.UtcInstance);
            }
        }

        void DeduceReturnsDecimalSerializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, DecimalSerializer.Instance);
            }
        }

        void DeduceReturnsDoubleSerializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, DoubleSerializer.Instance);
            }
        }

        void DeduceReturnsInt32Serializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, Int32Serializer.Instance);
            }
        }

        void DeduceReturnsInt64Serializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, Int64Serializer.Instance);
            }
        }

        void DeduceReturnsNullableDecimalSerializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, NullableSerializer.NullableDecimalInstance);
            }
        }

        void DeduceReturnsNullableDoubleSerializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, NullableSerializer.NullableDoubleInstance);
            }
        }

        void DeduceReturnsNullableInt32Serializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, NullableSerializer.NullableInt32Instance);
            }
        }

        void DeduceReturnsNullableInt64Serializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, NullableSerializer.NullableInt64Instance);
            }
        }

        void DeduceReturnsNullableSingleSerializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, NullableSerializer.NullableSingleInstance);
            }
        }

        void DeduceReturnsNumericSerializer()
        {
            if (IsNotKnown(node) && node.Type.IsNumeric())
            {
                var numericSerializer = StandardSerializers.GetSerializer(node.Type);
                AddNodeSerializer(node, numericSerializer);
            }
        }

        void DeduceReturnsNumericOrNullableNumericSerializer()
        {
            if (IsNotKnown(node) && node.Type.IsNumericOrNullableNumeric())
            {
                var numericSerializer = StandardSerializers.GetSerializer(node.Type);
                AddNodeSerializer(node, numericSerializer);
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
                AddNodeSerializer(node, nodeSerializer);
            }
        }

        void DeduceReturnsSingleSerializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, SingleSerializer.Instance);
            }
        }

        void DeduceReturnsStringSerializer()
        {
            if (IsNotKnown(node))
            {
                AddNodeSerializer(node, StringSerializer.Instance);
            }
        }

        void DeduceReturnsTimeSpanSerializer(TimeSpanUnits units)
        {
            if (IsNotKnown(node))
            {
                var resultSerializer = new TimeSpanSerializer(BsonType.Int64, units);
                AddNodeSerializer(node, resultSerializer);
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

        void DeduceRoundMethodSerializers()
        {
            if (method.IsOneOf(MathMethod.RoundWithDecimal, MathMethod.RoundWithDecimalAndDecimals, MathMethod.RoundWithDouble, MathMethod.RoundWithDoubleAndDigits))
            {
                if (IsNotKnown(node))
                {
                    var resultSerializer = StandardSerializers.GetSerializer(node.Type);
                    AddNodeSerializer(node, resultSerializer);
                }
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
            if (method.IsOneOf(EnumerableOrQueryableMethod.SelectManyOverloads))
            {
                var sourceExpression = arguments[0];

                if (method.IsOneOf(EnumerableOrQueryableMethod.SelectManyWithSelector))
                {
                    var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                    var selectorSourceParameter = selectorLambda.Parameters.Single();

                    DeduceItemAndCollectionSerializers(selectorSourceParameter, sourceExpression);
                    DeduceCollectionAndCollectionSerializers(node, selectorLambda.Body);
                }

                if (method.IsOneOf(EnumerableOrQueryableMethod.SelectManyWithCollectionSelectorAndResultSelector))
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
            if (ISetMethod.IsSetEqualsMethod(method))
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
        }

        void DeduceSetWindowFieldsMethodSerializers()
        {
            if (method.Is(EnumerableMethod.First))
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
        }

        void DeduceShiftMethodSerializers()
        {
            if (method.IsOneOf(WindowMethod.Shift, WindowMethod.ShiftWithDefaultValue))
            {
                var sourceExpression = arguments[0];
                var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var selectorSourceItemParameter = selectorLambda.Parameters[0];

                DeduceItemAndCollectionSerializers(selectorSourceItemParameter, sourceExpression);

                if (IsNotKnown(node) && IsKnown(selectorLambda.Body, out var resultSerializer))
                {
                    AddNodeSerializer(node, resultSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceSplitMethodSerializers()
        {
            if (method.IsOneOf(StringMethod.SplitOverloads))
            {
                if (IsNotKnown(node))
                {
                    var nodeSerializer = ArraySerializer.Create(StringSerializer.Instance);
                    AddNodeSerializer(node, nodeSerializer);
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
            if (method.IsOneOf(MongoEnumerableMethod.StandardDeviationOverloads))
            {
                if (method.IsOneOf(MongoEnumerableMethod.StandardDeviationWithSelectorOverloads))
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
            if (method.IsOneOf(StringMethod.EndsWithOrStartsWithOverloads))
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
            if (method.IsOneOf(DateTimeMethod.SubtractReturningDateTimeOverloads))
            {
                DeduceReturnsDateTimeSerializer();
            }
            else if (method.IsOneOf(DateTimeMethod.SubtractReturningInt64Overloads))
            {
                DeduceReturnsInt64Serializer();
            }
            else if (method.IsOneOf(DateTimeMethod.SubtractReturningTimeSpanWithMillisecondsUnitsOverloads))
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
            if (method.IsOneOf(EnumerableOrQueryableMethod.SumOverloads))
            {
                if (method.IsOneOf(EnumerableOrQueryableMethod.SumWithSelectorOverloads))
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
            if (method.IsOneOf(EnumerableOrQueryableMethod.SkipOrTakeOverloads))
            {
                var sourceExpression = arguments[0];

                if (method.IsOneOf(EnumerableOrQueryableMethod.SkipWhileOrTakeWhile))
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
                    AddNodeSerializer(node, resultSerializer);
                }
            }
        }

        void DeduceToLowerOrToUpperSerializers()
        {
            if (method.IsOneOf(StringMethod.ToLowerOrToUpperOverloads))
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

        void DeduceTrigonometricMethodSerializers()
        {
            if (method.IsOneOf(MathMethod.TrigonometricMethods))
            {
                DeduceReturnsDoubleSerializer();
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
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
                    AddNodeSerializer(node, Int32Serializer.Instance);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        void DeduceWhereSerializers()
        {
            if (method.IsOneOf(__whereOverloads))
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
                    AddNodeSerializer(resultSelectorFirstParameter, firstItemSerializer);
                }

                if (IsNotKnown(resultSelectorSecondParameter) && IsKnown(secondExpression, out var secondSerializer))
                {
                    var secondItemSerializer =  ArraySerializerHelper.GetItemSerializer(secondSerializer);
                    AddNodeSerializer(resultSelectorSecondParameter, secondItemSerializer);
                }

                if (IsNotKnown(node) && IsKnown(resultSelectorLambda.Body, out var resultItemSerializer))
                {
                    var resultSerializer = IEnumerableOrIQueryableSerializer.Create(node.Type, resultItemSerializer);
                    AddNodeSerializer(node, resultSerializer);
                }
            }
            else
            {
                DeduceUnknownMethodSerializer();
            }
        }

        bool IsDictionaryContainsKeyExpression(out Expression keyExpression)
        {
            if (DictionaryMethod.IsContainsKeyMethod(method))
            {
                keyExpression = arguments[0];
                return true;
            }

            keyExpression = null;
            return false;
        }
    }
}

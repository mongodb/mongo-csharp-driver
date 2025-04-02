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

using System.Linq.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class MethodCallExpressionToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case "Abs": return AbsMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Add": return AddMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "AddToSet": return AddToSetMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Aggregate": return AggregateMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "All": return AllMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Any": return AnyMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "AsQueryable": return AsQueryableMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Average": return AverageMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Ceiling": return CeilingMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "CompareTo": return CompareToMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Concat": return ConcatMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Constant": return ConstantMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Contains": return ContainsMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "ContainsKey": return ContainsKeyMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "ContainsValue": return ContainsValueMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Convert": return ConvertMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "CovariancePopulation": return CovariancePopulationMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "CovarianceSample": return CovarianceSampleMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Create": return CreateMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "DateFromString": return DateFromStringMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "DefaultIfEmpty": return DefaultIfEmptyMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "DenseRank": return DenseRankMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Derivative": return DerivativeMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Distinct": return DistinctMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "DocumentNumber": return DocumentNumberMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Equals": return EqualsMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Except": return ExceptMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Exists": return ExistsMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Exp": return ExpMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "ExponentialMovingAverage": return ExponentialMovingAverageMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Field": return FieldMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Floor": return FloorMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "get_Item": return GetItemMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Integral": return IntegralMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Intersect": return IntersectMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "IsMatch": return IsMatchMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "IsNullOrEmpty": return IsNullOrEmptyMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "IsNullOrWhiteSpace": return IsNullOrWhiteSpaceMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "IsSubsetOf": return IsSubsetOfMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Locf": return LocfMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "OfType": return OfTypeMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Parse": return ParseMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Pow": return PowMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Push": return PushMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Range": return RangeMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Rank": return RankMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Repeat": return RepeatMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Reverse": return ReverseMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Round": return RoundMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Select": return SelectMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "SelectMany": return SelectManyMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "SequenceEqual": return SequenceEqualMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "SetEquals": return SetEqualsMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Shift": return ShiftMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Split": return SplitMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Sqrt": return SqrtMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "StrLenBytes": return StrLenBytesMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Subtract": return SubtractMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Sum": return SumMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "ToArray": return ToArrayMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "ToList": return ToListMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "ToString": return ToStringMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Truncate": return TruncateMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Week": return WeekMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Where": return WhereMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Union": return UnionMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Zip": return ZipMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Acos":
                case "Acosh":
                case "Asin":
                case "Asinh":
                case "Atan":
                case "Atan2":
                case "Atanh":
                case "Cos":
                case "Cosh":
                case "DegreesToRadians":
                case "RadiansToDegrees":
                case "Sin":
                case "Sinh":
                case "Tan":
                case "Tanh":
                    return TrigMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "AddDays":
                case "AddHours":
                case "AddMilliseconds":
                case "AddMinutes":
                case "AddMonths":
                case "AddQuarters":
                case "AddSeconds":
                case "AddTicks":
                case "AddWeeks":
                case "AddYears":
                    return DateTimeAddOrSubtractMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Append":
                case "Prepend":
                    return AppendOrPrependMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Bottom":
                case "BottomN":
                case "FirstN":
                case "LastN":
                case "MaxN":
                case "MinN":
                case "Top":
                case "TopN":
                    return PickMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Count":
                case "LongCount":
                    return CountMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "ElementAt":
                case "ElementAtOrDefault":
                    return ElementAtMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "First":
                case "FirstOrDefault":
                case "Last":
                case "LastOrDefault":
                    return FirstOrLastMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "IndexOf":
                case "IndexOfBytes":
                    return IndexOfMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "IndexOfAny":
                    return IndexOfAnyMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "IsMissing":
                case "IsNullOrMissing":
                    return IsMissingMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Log":
                case "Log10":
                    return LogMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Max":
                case "Min":
                    return MaxOrMinMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "OrderBy":
                case "OrderByDescending":
                case "ThenBy":
                case "ThenByDescending":
                    return OrderByMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Skip":
                case "Take":
                    return SkipOrTakeMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "SkipWhile":
                case "TakeWhile":
                    return SkipWhileOrTakeWhileMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "StandardDeviationPopulation":
                case "StandardDeviationSample":
                    return StandardDeviationMethodsToAggregationExpressionTranslator.Translate(context, expression);

                case "StartsWith":
                case "EndsWith":
                    return StartsWithContainsOrEndsWithMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Substring":
                case "SubstrBytes":
                    return SubstringMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "ToLower":
                case "ToLowerInvariant":
                case "ToUpper":
                case "ToUpperInvariant":
                    return ToLowerOrToUpperMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Trim":
                case "TrimEnd":
                case "TrimStart":
                    return TrimMethodToAggregationExpressionTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}

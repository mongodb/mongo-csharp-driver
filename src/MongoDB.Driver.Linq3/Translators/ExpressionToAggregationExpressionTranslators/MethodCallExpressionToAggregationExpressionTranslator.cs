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
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators
{
    public static class MethodCallExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case "Abs": return AbsMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Aggregate": return AggregateMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "All": return AllMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Any": return AnyMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Average": return AverageMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Ceiling": return CeilingMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Concat": return ConcatMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "CompareTo": return CompareToMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Contains": return ContainsMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Distinct": return DistinctMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "ElementAt": return ElementAtMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Equals": return EqualsMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Except": return ExceptMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Exp": return ExpMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Floor": return FloorMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "get_Item": return GetItemMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Intersect": return IntersectMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "IsNullOrEmpty": return IsNullOrEmptyMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "IsSubsetOf": return IsSubsetOfMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Parse": return ParseMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Pow": return PowMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Range": return RangeMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Reverse": return ReverseMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Select": return SelectMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "SetEquals": return SetEqualsMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Split": return SplitMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Sqrt": return SqrtMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "StrLenBytes": return StrLenBytesMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Sum": return SumMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Take": return TakeMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "ToList": return ToListMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "ToString": return ToStringMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Truncate": return TruncateMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Where": return WhereMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Union": return UnionMethodToAggregationExpressionTranslator.Translate(context, expression);
                case "Zip": return ZipMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Count":
                case "LongCount":
                    return CountMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "First":
                case "Last":
                    return FirstOrLastMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "IndexOf":
                case "IndexOfBytes":
                    return IndexOfMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Log":
                case "Log10":
                    return LogMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "Max":
                case "Min":
                    return MaxOrMinMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "StandardDeviationPopulation":
                case "StandardDeviationSample":
                    return StandardDeviationMethodsToAggregationExpressionTranslator.Translate(context, expression);

                case "Substring":
                case "SubstrBytes":
                    return SubstringMethodToAggregationExpressionTranslator.Translate(context, expression);

                case "ToLower":
                case "ToLowerInvariant":
                case "ToUpper":
                case "ToUpperInvariant":
                    return ToLowerOrToUpperMethodToAggregationExpressionTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}

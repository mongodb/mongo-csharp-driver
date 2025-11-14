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
using System.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class MongoEnumerableMethod
    {
        // private static fields
        private static readonly MethodInfo __allElements;
        private static readonly MethodInfo __allMatchingElements;
        private static readonly MethodInfo __firstMatchingElement;
        private static readonly MethodInfo __medianDecimal;
        private static readonly MethodInfo __medianDecimalWithSelector;
        private static readonly MethodInfo __medianDouble;
        private static readonly MethodInfo __medianDoubleWithSelector;
        private static readonly MethodInfo __medianInt32;
        private static readonly MethodInfo __medianInt32WithSelector;
        private static readonly MethodInfo __medianInt64;
        private static readonly MethodInfo __medianInt64WithSelector;
        private static readonly MethodInfo __medianNullableDecimal;
        private static readonly MethodInfo __medianNullableDecimalWithSelector;
        private static readonly MethodInfo __medianNullableDouble;
        private static readonly MethodInfo __medianNullableDoubleWithSelector;
        private static readonly MethodInfo __medianNullableInt32;
        private static readonly MethodInfo __medianNullableInt32WithSelector;
        private static readonly MethodInfo __medianNullableInt64;
        private static readonly MethodInfo __medianNullableInt64WithSelector;
        private static readonly MethodInfo __medianNullableSingle;
        private static readonly MethodInfo __medianNullableSingleWithSelector;
        private static readonly MethodInfo __medianSingle;
        private static readonly MethodInfo __medianSingleWithSelector;
        private static readonly MethodInfo __percentileDecimal;
        private static readonly MethodInfo __percentileDecimalWithSelector;
        private static readonly MethodInfo __percentileDouble;
        private static readonly MethodInfo __percentileDoubleWithSelector;
        private static readonly MethodInfo __percentileInt32;
        private static readonly MethodInfo __percentileInt32WithSelector;
        private static readonly MethodInfo __percentileInt64;
        private static readonly MethodInfo __percentileInt64WithSelector;
        private static readonly MethodInfo __percentileNullableDecimal;
        private static readonly MethodInfo __percentileNullableDecimalWithSelector;
        private static readonly MethodInfo __percentileNullableDouble;
        private static readonly MethodInfo __percentileNullableDoubleWithSelector;
        private static readonly MethodInfo __percentileNullableInt32;
        private static readonly MethodInfo __percentileNullableInt32WithSelector;
        private static readonly MethodInfo __percentileNullableInt64;
        private static readonly MethodInfo __percentileNullableInt64WithSelector;
        private static readonly MethodInfo __percentileNullableSingle;
        private static readonly MethodInfo __percentileNullableSingleWithSelector;
        private static readonly MethodInfo __percentileSingle;
        private static readonly MethodInfo __percentileSingleWithSelector;
        private static readonly MethodInfo __standardDeviationPopulationDecimal;
        private static readonly MethodInfo __standardDeviationPopulationDecimalWithSelector;
        private static readonly MethodInfo __standardDeviationPopulationDouble;
        private static readonly MethodInfo __standardDeviationPopulationDoubleWithSelector;
        private static readonly MethodInfo __standardDeviationPopulationInt32;
        private static readonly MethodInfo __standardDeviationPopulationInt32WithSelector;
        private static readonly MethodInfo __standardDeviationPopulationInt64;
        private static readonly MethodInfo __standardDeviationPopulationInt64WithSelector;
        private static readonly MethodInfo __standardDeviationPopulationNullableDecimal;
        private static readonly MethodInfo __standardDeviationPopulationNullableDecimalWithSelector;
        private static readonly MethodInfo __standardDeviationPopulationNullableDouble;
        private static readonly MethodInfo __standardDeviationPopulationNullableDoubleWithSelector;
        private static readonly MethodInfo __standardDeviationPopulationNullableInt32;
        private static readonly MethodInfo __standardDeviationPopulationNullableInt32WithSelector;
        private static readonly MethodInfo __standardDeviationPopulationNullableInt64;
        private static readonly MethodInfo __standardDeviationPopulationNullableInt64WithSelector;
        private static readonly MethodInfo __standardDeviationPopulationNullableSingle;
        private static readonly MethodInfo __standardDeviationPopulationNullableSingleWithSelector;
        private static readonly MethodInfo __standardDeviationPopulationSingle;
        private static readonly MethodInfo __standardDeviationPopulationSingleWithSelector;
        private static readonly MethodInfo __standardDeviationSampleDecimal;
        private static readonly MethodInfo __standardDeviationSampleDecimalWithSelector;
        private static readonly MethodInfo __standardDeviationSampleDouble;
        private static readonly MethodInfo __standardDeviationSampleDoubleWithSelector;
        private static readonly MethodInfo __standardDeviationSampleInt32;
        private static readonly MethodInfo __standardDeviationSampleInt32WithSelector;
        private static readonly MethodInfo __standardDeviationSampleInt64;
        private static readonly MethodInfo __standardDeviationSampleInt64WithSelector;
        private static readonly MethodInfo __standardDeviationSampleNullableDecimal;
        private static readonly MethodInfo __standardDeviationSampleNullableDecimalWithSelector;
        private static readonly MethodInfo __standardDeviationSampleNullableDouble;
        private static readonly MethodInfo __standardDeviationSampleNullableDoubleWithSelector;
        private static readonly MethodInfo __standardDeviationSampleNullableInt32;
        private static readonly MethodInfo __standardDeviationSampleNullableInt32WithSelector;
        private static readonly MethodInfo __standardDeviationSampleNullableInt64;
        private static readonly MethodInfo __standardDeviationSampleNullableInt64WithSelector;
        private static readonly MethodInfo __standardDeviationSampleNullableSingle;
        private static readonly MethodInfo __standardDeviationSampleNullableSingleWithSelector;
        private static readonly MethodInfo __standardDeviationSampleSingle;
        private static readonly MethodInfo __standardDeviationSampleSingleWithSelector;
        private static readonly MethodInfo __whereWithLimit;

        // sets of methods
        private static readonly IReadOnlyMethodInfoSet __medianOverloads;
        private static readonly IReadOnlyMethodInfoSet __medianWithSelectorOverloads;
        private static readonly IReadOnlyMethodInfoSet __percentileOverloads;
        private static readonly IReadOnlyMethodInfoSet __percentileWithSelectorOverloads;
        private static readonly IReadOnlyMethodInfoSet __standardDeviationOverloads;
        private static readonly IReadOnlyMethodInfoSet __standardDeviationWithSelectorOverloads;

        // static constructor
        static MongoEnumerableMethod()
        {
            // initialize methods before sets of methods
            __allElements = ReflectionInfo.Method((IEnumerable<object> source) => source.AllElements());
            __allMatchingElements = ReflectionInfo.Method((IEnumerable<object> source, string identifier) => source.AllMatchingElements(identifier));
            __firstMatchingElement = ReflectionInfo.Method((IEnumerable<object> source) => source.FirstMatchingElement());
            __medianDecimal = ReflectionInfo.Method((IEnumerable<decimal> source) => source.Median());
            __medianDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal> selector) => source.Median(selector));
            __medianDouble = ReflectionInfo.Method((IEnumerable<double> source) => source.Median());
            __medianDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double> selector) => source.Median(selector));
            __medianInt32 = ReflectionInfo.Method((IEnumerable<int> source) => source.Median());
            __medianInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int> selector) => source.Median(selector));
            __medianInt64 = ReflectionInfo.Method((IEnumerable<long> source) => source.Median());
            __medianInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long> selector) => source.Median(selector));
            __medianNullableDecimal = ReflectionInfo.Method((IEnumerable<decimal?> source) => source.Median());
            __medianNullableDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal?> selector) => source.Median(selector));
            __medianNullableDouble = ReflectionInfo.Method((IEnumerable<double?> source) => source.Median());
            __medianNullableDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double?> selector) => source.Median(selector));
            __medianNullableInt32 = ReflectionInfo.Method((IEnumerable<int?> source) => source.Median());
            __medianNullableInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int?> selector) => source.Median(selector));
            __medianNullableInt64 = ReflectionInfo.Method((IEnumerable<long?> source) => source.Median());
            __medianNullableInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long?> selector) => source.Median(selector));
            __medianNullableSingle = ReflectionInfo.Method((IEnumerable<float?> source) => source.Median());
            __medianNullableSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float?> selector) => source.Median(selector));
            __medianSingle = ReflectionInfo.Method((IEnumerable<float> source) => source.Median());
            __medianSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float> selector) => source.Median(selector));
            __percentileDecimal = ReflectionInfo.Method((IEnumerable<decimal> source, IEnumerable<double> percentiles) => source.Percentile(percentiles));
            __percentileDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal> selector, IEnumerable<double> percentiles) => source.Percentile(selector, percentiles));
            __percentileDouble = ReflectionInfo.Method((IEnumerable<double> source, IEnumerable<double> percentiles) => source.Percentile(percentiles));
            __percentileDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double> selector, IEnumerable<double> percentiles) => source.Percentile(selector, percentiles));
            __percentileInt32 = ReflectionInfo.Method((IEnumerable<int> source, IEnumerable<double> percentiles) => source.Percentile(percentiles));
            __percentileInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int> selector, IEnumerable<double> percentiles) => source.Percentile(selector, percentiles));
            __percentileInt64 = ReflectionInfo.Method((IEnumerable<long> source, IEnumerable<double> percentiles) => source.Percentile(percentiles));
            __percentileInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long> selector, IEnumerable<double> percentiles) => source.Percentile(selector, percentiles));
            __percentileNullableDecimal = ReflectionInfo.Method((IEnumerable<decimal?> source, IEnumerable<double> percentiles) => source.Percentile(percentiles));
            __percentileNullableDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal?> selector, IEnumerable<double> percentiles) => source.Percentile(selector, percentiles));
            __percentileNullableDouble = ReflectionInfo.Method((IEnumerable<double?> source, IEnumerable<double> percentiles) => source.Percentile(percentiles));
            __percentileNullableDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double?> selector, IEnumerable<double> percentiles) => source.Percentile(selector, percentiles));
            __percentileNullableInt32 = ReflectionInfo.Method((IEnumerable<int?> source, IEnumerable<double> percentiles) => source.Percentile(percentiles));
            __percentileNullableInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int?> selector, IEnumerable<double> percentiles) => source.Percentile(selector, percentiles));
            __percentileNullableInt64 = ReflectionInfo.Method((IEnumerable<long?> source, IEnumerable<double> percentiles) => source.Percentile(percentiles));
            __percentileNullableInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long?> selector, IEnumerable<double> percentiles) => source.Percentile(selector, percentiles));
            __percentileNullableSingle = ReflectionInfo.Method((IEnumerable<float?> source, IEnumerable<double> percentiles) => source.Percentile(percentiles));
            __percentileNullableSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float?> selector, IEnumerable<double> percentiles) => source.Percentile(selector, percentiles));
            __percentileSingle = ReflectionInfo.Method((IEnumerable<float> source, IEnumerable<double> percentiles) => source.Percentile(percentiles));
            __percentileSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float> selector, IEnumerable<double> percentiles) => source.Percentile(selector, percentiles));
            __standardDeviationPopulationDecimal = ReflectionInfo.Method((IEnumerable<decimal> source) => source.StandardDeviationPopulation());
            __standardDeviationPopulationDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal> selector) => source.StandardDeviationPopulation(selector));
            __standardDeviationPopulationDouble = ReflectionInfo.Method((IEnumerable<double> source) => source.StandardDeviationPopulation());
            __standardDeviationPopulationDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double> selector) => source.StandardDeviationPopulation(selector));
            __standardDeviationPopulationInt32 = ReflectionInfo.Method((IEnumerable<int> source) => source.StandardDeviationPopulation());
            __standardDeviationPopulationInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int> selector) => source.StandardDeviationPopulation(selector));
            __standardDeviationPopulationInt64 = ReflectionInfo.Method((IEnumerable<long> source) => source.StandardDeviationPopulation());
            __standardDeviationPopulationInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long> selector) => source.StandardDeviationPopulation(selector));
            __standardDeviationPopulationNullableDecimal = ReflectionInfo.Method((IEnumerable<decimal?> source) => source.StandardDeviationPopulation());
            __standardDeviationPopulationNullableDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal?> selector) => source.StandardDeviationPopulation(selector));
            __standardDeviationPopulationNullableDouble = ReflectionInfo.Method((IEnumerable<double?> source) => source.StandardDeviationPopulation());
            __standardDeviationPopulationNullableDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double?> selector) => source.StandardDeviationPopulation(selector));
            __standardDeviationPopulationNullableInt32 = ReflectionInfo.Method((IEnumerable<int?> source) => source.StandardDeviationPopulation());
            __standardDeviationPopulationNullableInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int?> selector) => source.StandardDeviationPopulation(selector));
            __standardDeviationPopulationNullableInt64 = ReflectionInfo.Method((IEnumerable<long?> source) => source.StandardDeviationPopulation());
            __standardDeviationPopulationNullableInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long?> selector) => source.StandardDeviationPopulation(selector));
            __standardDeviationPopulationNullableSingle = ReflectionInfo.Method((IEnumerable<float?> source) => source.StandardDeviationPopulation());
            __standardDeviationPopulationNullableSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float?> selector) => source.StandardDeviationPopulation(selector));
            __standardDeviationPopulationSingle = ReflectionInfo.Method((IEnumerable<float> source) => source.StandardDeviationPopulation());
            __standardDeviationPopulationSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float> selector) => source.StandardDeviationPopulation(selector));
            __standardDeviationSampleDecimal = ReflectionInfo.Method((IEnumerable<decimal> source) => source.StandardDeviationSample());
            __standardDeviationSampleDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal> selector) => source.StandardDeviationSample(selector));
            __standardDeviationSampleDouble = ReflectionInfo.Method((IEnumerable<double> source) => source.StandardDeviationSample());
            __standardDeviationSampleDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double> selector) => source.StandardDeviationSample(selector));
            __standardDeviationSampleInt32 = ReflectionInfo.Method((IEnumerable<int> source) => source.StandardDeviationSample());
            __standardDeviationSampleInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int> selector) => source.StandardDeviationSample(selector));
            __standardDeviationSampleInt64 = ReflectionInfo.Method((IEnumerable<long> source) => source.StandardDeviationSample());
            __standardDeviationSampleInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long> selector) => source.StandardDeviationSample(selector));
            __standardDeviationSampleNullableDecimal = ReflectionInfo.Method((IEnumerable<decimal?> source) => source.StandardDeviationSample());
            __standardDeviationSampleNullableDecimalWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, decimal?> selector) => source.StandardDeviationSample(selector));
            __standardDeviationSampleNullableDouble = ReflectionInfo.Method((IEnumerable<double?> source) => source.StandardDeviationSample());
            __standardDeviationSampleNullableDoubleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, double?> selector) => source.StandardDeviationSample(selector));
            __standardDeviationSampleNullableInt32 = ReflectionInfo.Method((IEnumerable<int?> source) => source.StandardDeviationSample());
            __standardDeviationSampleNullableInt32WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, int?> selector) => source.StandardDeviationSample(selector));
            __standardDeviationSampleNullableInt64 = ReflectionInfo.Method((IEnumerable<long?> source) => source.StandardDeviationSample());
            __standardDeviationSampleNullableInt64WithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, long?> selector) => source.StandardDeviationSample(selector));
            __standardDeviationSampleNullableSingle = ReflectionInfo.Method((IEnumerable<float?> source) => source.StandardDeviationSample());
            __standardDeviationSampleNullableSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float?> selector) => source.StandardDeviationSample(selector));
            __standardDeviationSampleSingle = ReflectionInfo.Method((IEnumerable<float> source) => source.StandardDeviationSample());
            __standardDeviationSampleSingleWithSelector = ReflectionInfo.Method((IEnumerable<object> source, Func<object, float> selector) => source.StandardDeviationSample(selector));
            __whereWithLimit = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate, int limit) => source.Where(predicate, limit));

            // initialize sets of methods after methods
            __medianOverloads = MethodInfoSet.Create(
            [
                __medianDecimal,
                __medianDecimalWithSelector,
                __medianDouble,
                __medianDoubleWithSelector,
                __medianInt32,
                __medianInt32WithSelector,
                __medianInt64,
                __medianInt64WithSelector,
                __medianNullableDecimal,
                __medianNullableDecimalWithSelector,
                __medianNullableDouble,
                __medianNullableDoubleWithSelector,
                __medianNullableInt32,
                __medianNullableInt32WithSelector,
                __medianNullableInt64,
                __medianNullableInt64WithSelector,
                __medianNullableSingle,
                __medianNullableSingleWithSelector,
                __medianSingle,
                __medianSingleWithSelector
            ]);

            __medianWithSelectorOverloads = MethodInfoSet.Create(
            [
                __medianDecimalWithSelector,
                __medianDoubleWithSelector,
                __medianInt32WithSelector,
                __medianInt64WithSelector,
                __medianNullableDecimalWithSelector,
                __medianNullableDoubleWithSelector,
                __medianNullableInt32WithSelector,
                __medianNullableInt64WithSelector,
                __medianNullableSingleWithSelector,
                __medianSingleWithSelector
            ]);

            __percentileOverloads = MethodInfoSet.Create(
            [
                __percentileDecimal,
                __percentileDecimalWithSelector,
                __percentileDouble,
                __percentileDoubleWithSelector,
                __percentileInt32,
                __percentileInt32WithSelector,
                __percentileInt64,
                __percentileInt64WithSelector,
                __percentileNullableDecimal,
                __percentileNullableDecimalWithSelector,
                __percentileNullableDouble,
                __percentileNullableDoubleWithSelector,
                __percentileNullableInt32,
                __percentileNullableInt32WithSelector,
                __percentileNullableInt64,
                __percentileNullableInt64WithSelector,
                __percentileNullableSingle,
                __percentileNullableSingleWithSelector,
                __percentileSingle,
                __percentileSingleWithSelector
            ]);

            __percentileWithSelectorOverloads = MethodInfoSet.Create(
            [
                __percentileDecimalWithSelector,
                __percentileDoubleWithSelector,
                __percentileInt32WithSelector,
                __percentileInt64WithSelector,
                __percentileNullableDecimalWithSelector,
                __percentileNullableDoubleWithSelector,
                __percentileNullableInt32WithSelector,
                __percentileNullableInt64WithSelector,
                __percentileNullableSingleWithSelector,
                __percentileSingleWithSelector
            ]);

            __standardDeviationOverloads = MethodInfoSet.Create(
            [
                __standardDeviationPopulationDecimal,
                __standardDeviationPopulationDecimalWithSelector,
                __standardDeviationPopulationDouble,
                __standardDeviationPopulationDoubleWithSelector,
                __standardDeviationPopulationInt32,
                __standardDeviationPopulationInt32WithSelector,
                __standardDeviationPopulationInt64,
                __standardDeviationPopulationInt64WithSelector,
                __standardDeviationPopulationNullableDecimal,
                __standardDeviationPopulationNullableDecimalWithSelector,
                __standardDeviationPopulationNullableDouble,
                __standardDeviationPopulationNullableDoubleWithSelector,
                __standardDeviationPopulationNullableInt32,
                __standardDeviationPopulationNullableInt32WithSelector,
                __standardDeviationPopulationNullableInt64,
                __standardDeviationPopulationNullableInt64WithSelector,
                __standardDeviationPopulationNullableSingle,
                __standardDeviationPopulationNullableSingleWithSelector,
                __standardDeviationPopulationSingle,
                __standardDeviationPopulationSingleWithSelector,
                __standardDeviationSampleDecimal,
                __standardDeviationSampleDecimalWithSelector,
                __standardDeviationSampleDouble,
                __standardDeviationSampleDoubleWithSelector,
                __standardDeviationSampleInt32,
                __standardDeviationSampleInt32WithSelector,
                __standardDeviationSampleInt64,
                __standardDeviationSampleInt64WithSelector,
                __standardDeviationSampleNullableDecimal,
                __standardDeviationSampleNullableDecimalWithSelector,
                __standardDeviationSampleNullableDouble,
                __standardDeviationSampleNullableDoubleWithSelector,
                __standardDeviationSampleNullableInt32,
                __standardDeviationSampleNullableInt32WithSelector,
                __standardDeviationSampleNullableInt64,
                __standardDeviationSampleNullableInt64WithSelector,
                __standardDeviationSampleNullableSingle,
                __standardDeviationSampleNullableSingleWithSelector,
                __standardDeviationSampleSingle,
                __standardDeviationSampleSingleWithSelector,
            ]);

            __standardDeviationWithSelectorOverloads = MethodInfoSet.Create(
            [
                __standardDeviationPopulationDecimalWithSelector,
                __standardDeviationPopulationDoubleWithSelector,
                __standardDeviationPopulationInt32WithSelector,
                __standardDeviationPopulationInt64WithSelector,
                __standardDeviationPopulationNullableDecimalWithSelector,
                __standardDeviationPopulationNullableDoubleWithSelector,
                __standardDeviationPopulationNullableInt32WithSelector,
                __standardDeviationPopulationNullableInt64WithSelector,
                __standardDeviationPopulationNullableSingleWithSelector,
                __standardDeviationPopulationSingleWithSelector,
                __standardDeviationSampleDecimalWithSelector,
                __standardDeviationSampleDoubleWithSelector,
                __standardDeviationSampleInt32WithSelector,
                __standardDeviationSampleInt64WithSelector,
                __standardDeviationSampleNullableDecimalWithSelector,
                __standardDeviationSampleNullableDoubleWithSelector,
                __standardDeviationSampleNullableInt32WithSelector,
                __standardDeviationSampleNullableInt64WithSelector,
                __standardDeviationSampleNullableSingleWithSelector,
                __standardDeviationSampleSingleWithSelector,
            ]);
        }

        // public properties
        public static MethodInfo AllElements => __allElements;
        public static MethodInfo AllMatchingElements => __allMatchingElements;
        public static MethodInfo FirstMatchingElement => __firstMatchingElement;
        public static MethodInfo MedianDecimal => __medianDecimal;
        public static MethodInfo MedianDecimalWithSelector => __medianDecimalWithSelector;
        public static MethodInfo MedianDouble => __medianDouble;
        public static MethodInfo MedianDoubleWithSelector => __medianDoubleWithSelector;
        public static MethodInfo MedianInt32 => __medianInt32;
        public static MethodInfo MedianInt32WithSelector => __medianInt32WithSelector;
        public static MethodInfo MedianInt64 => __medianInt64;
        public static MethodInfo MedianInt64WithSelector => __medianInt64WithSelector;
        public static MethodInfo MedianNullableDecimal => __medianNullableDecimal;
        public static MethodInfo MedianNullableDecimalWithSelector => __medianNullableDecimalWithSelector;
        public static MethodInfo MedianNullableDouble => __medianNullableDouble;
        public static MethodInfo MedianNullableDoubleWithSelector => __medianNullableDoubleWithSelector;
        public static MethodInfo MedianNullableInt32 => __medianNullableInt32;
        public static MethodInfo MedianNullableInt32WithSelector => __medianNullableInt32WithSelector;
        public static MethodInfo MedianNullableInt64 => __medianNullableInt64;
        public static MethodInfo MedianNullableInt64WithSelector => __medianNullableInt64WithSelector;
        public static MethodInfo MedianNullableSingle => __medianNullableSingle;
        public static MethodInfo MedianNullableSingleWithSelector => __medianNullableSingleWithSelector;
        public static MethodInfo MedianSingle => __medianSingle;
        public static MethodInfo MedianSingleWithSelector => __medianSingleWithSelector;
        public static MethodInfo PercentileDecimal => __percentileDecimal;
        public static MethodInfo PercentileDecimalWithSelector => __percentileDecimalWithSelector;
        public static MethodInfo PercentileDouble => __percentileDouble;
        public static MethodInfo PercentileDoubleWithSelector => __percentileDoubleWithSelector;
        public static MethodInfo PercentileInt32 => __percentileInt32;
        public static MethodInfo PercentileInt32WithSelector => __percentileInt32WithSelector;
        public static MethodInfo PercentileInt64 => __percentileInt64;
        public static MethodInfo PercentileInt64WithSelector => __percentileInt64WithSelector;
        public static MethodInfo PercentileNullableDecimal => __percentileNullableDecimal;
        public static MethodInfo PercentileNullableDecimalWithSelector => __percentileNullableDecimalWithSelector;
        public static MethodInfo PercentileNullableDouble => __percentileNullableDouble;
        public static MethodInfo PercentileNullableDoubleWithSelector => __percentileNullableDoubleWithSelector;
        public static MethodInfo PercentileNullableInt32 => __percentileNullableInt32;
        public static MethodInfo PercentileNullableInt32WithSelector => __percentileNullableInt32WithSelector;
        public static MethodInfo PercentileNullableInt64 => __percentileNullableInt64;
        public static MethodInfo PercentileNullableInt64WithSelector => __percentileNullableInt64WithSelector;
        public static MethodInfo PercentileNullableSingle => __percentileNullableSingle;
        public static MethodInfo PercentileNullableSingleWithSelector => __percentileNullableSingleWithSelector;
        public static MethodInfo PercentileSingle => __percentileSingle;
        public static MethodInfo PercentileSingleWithSelector => __percentileSingleWithSelector;
        public static MethodInfo StandardDeviationPopulationDecimal => __standardDeviationPopulationDecimal;
        public static MethodInfo StandardDeviationPopulationDecimalWithSelector => __standardDeviationPopulationDecimalWithSelector;
        public static MethodInfo StandardDeviationPopulationDouble => __standardDeviationPopulationDouble;
        public static MethodInfo StandardDeviationPopulationDoubleWithSelector => __standardDeviationPopulationDoubleWithSelector;
        public static MethodInfo StandardDeviationPopulationInt32 => __standardDeviationPopulationInt32;
        public static MethodInfo StandardDeviationPopulationInt32WithSelector => __standardDeviationPopulationInt32WithSelector;
        public static MethodInfo StandardDeviationPopulationInt64 => __standardDeviationPopulationInt64;
        public static MethodInfo StandardDeviationPopulationInt64WithSelector => __standardDeviationPopulationInt64WithSelector;
        public static MethodInfo StandardDeviationPopulationNullableDecimal => __standardDeviationPopulationNullableDecimal;
        public static MethodInfo StandardDeviationPopulationNullableDecimalWithSelector => __standardDeviationPopulationNullableDecimalWithSelector;
        public static MethodInfo StandardDeviationPopulationNullableDouble => __standardDeviationPopulationNullableDouble;
        public static MethodInfo StandardDeviationPopulationNullableDoubleWithSelector => __standardDeviationPopulationNullableDoubleWithSelector;
        public static MethodInfo StandardDeviationPopulationNullableInt32 => __standardDeviationPopulationNullableInt32;
        public static MethodInfo StandardDeviationPopulationNullableInt32WithSelector => __standardDeviationPopulationNullableInt32WithSelector;
        public static MethodInfo StandardDeviationPopulationNullableInt64 => __standardDeviationPopulationNullableInt64;
        public static MethodInfo StandardDeviationPopulationNullableInt64WithSelector => __standardDeviationPopulationNullableInt64WithSelector;
        public static MethodInfo StandardDeviationPopulationNullableSingle => __standardDeviationPopulationNullableSingle;
        public static MethodInfo StandardDeviationPopulationNullableSingleWithSelector => __standardDeviationPopulationNullableSingleWithSelector;
        public static MethodInfo StandardDeviationPopulationSingle => __standardDeviationPopulationSingle;
        public static MethodInfo StandardDeviationPopulationSingleWithSelector => __standardDeviationPopulationSingleWithSelector;
        public static MethodInfo StandardDeviationSampleDecimal => __standardDeviationSampleDecimal;
        public static MethodInfo StandardDeviationSampleDecimalWithSelector => __standardDeviationSampleDecimalWithSelector;
        public static MethodInfo StandardDeviationSampleDouble => __standardDeviationSampleDouble;
        public static MethodInfo StandardDeviationSampleDoubleWithSelector => __standardDeviationSampleDoubleWithSelector;
        public static MethodInfo StandardDeviationSampleInt32 => __standardDeviationSampleInt32;
        public static MethodInfo StandardDeviationSampleInt32WithSelector => __standardDeviationSampleInt32WithSelector;
        public static MethodInfo StandardDeviationSampleInt64 => __standardDeviationSampleInt64;
        public static MethodInfo StandardDeviationSampleInt64WithSelector => __standardDeviationSampleInt64WithSelector;
        public static MethodInfo StandardDeviationSampleNullableDecimal => __standardDeviationSampleNullableDecimal;
        public static MethodInfo StandardDeviationSampleNullableDecimalWithSelector => __standardDeviationSampleNullableDecimalWithSelector;
        public static MethodInfo StandardDeviationSampleNullableDouble => __standardDeviationSampleNullableDouble;
        public static MethodInfo StandardDeviationSampleNullableDoubleWithSelector => __standardDeviationSampleNullableDoubleWithSelector;
        public static MethodInfo StandardDeviationSampleNullableInt32 => __standardDeviationSampleNullableInt32;
        public static MethodInfo StandardDeviationSampleNullableInt32WithSelector => __standardDeviationSampleNullableInt32WithSelector;
        public static MethodInfo StandardDeviationSampleNullableInt64 => __standardDeviationSampleNullableInt64;
        public static MethodInfo StandardDeviationSampleNullableInt64WithSelector => __standardDeviationSampleNullableInt64WithSelector;
        public static MethodInfo StandardDeviationSampleNullableSingle => __standardDeviationSampleNullableSingle;
        public static MethodInfo StandardDeviationSampleNullableSingleWithSelector => __standardDeviationSampleNullableSingleWithSelector;
        public static MethodInfo StandardDeviationSampleSingle => __standardDeviationSampleSingle;
        public static MethodInfo StandardDeviationSampleSingleWithSelector => __standardDeviationSampleSingleWithSelector;
        public static MethodInfo WhereWithLimit => __whereWithLimit;

        // sets of methods
        public static IReadOnlyMethodInfoSet MedianOverloads => __medianOverloads;
        public static IReadOnlyMethodInfoSet MedianWithSelectorOverloads => __medianWithSelectorOverloads;
        public static IReadOnlyMethodInfoSet PercentileOverloads => __percentileOverloads;
        public static IReadOnlyMethodInfoSet PercentileWithSelectorOverloads => __percentileWithSelectorOverloads;
        public static IReadOnlyMethodInfoSet StandardDeviationOverloads => __standardDeviationOverloads;
        public static IReadOnlyMethodInfoSet StandardDeviationWithSelectorOverloads => __standardDeviationWithSelectorOverloads;
    }
}

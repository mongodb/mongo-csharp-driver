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
        private static readonly MethodInfo __whereWithLimit;

        // static constructor
        static MongoEnumerableMethod()
        {
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
            __whereWithLimit = ReflectionInfo.Method((IEnumerable<object> source, Func<object, bool> predicate, int limit) => source.Where(predicate, limit));
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
        public static MethodInfo WhereWithLimit => __whereWithLimit;
    }
}

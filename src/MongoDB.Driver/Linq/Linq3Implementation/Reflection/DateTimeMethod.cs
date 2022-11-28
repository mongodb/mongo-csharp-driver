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
using System.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class DateTimeMethod
    {
        // private static fields
        private static readonly MethodInfo __add;
        private static readonly MethodInfo __addDays;
        private static readonly MethodInfo __addDaysWithTimezone;
        private static readonly MethodInfo __addHours;
        private static readonly MethodInfo __addHoursWithTimezone;
        private static readonly MethodInfo __addMilliseconds;
        private static readonly MethodInfo __addMillisecondsWithTimezone;
        private static readonly MethodInfo __addMinutes;
        private static readonly MethodInfo __addMinutesWithTimezone;
        private static readonly MethodInfo __addMonths;
        private static readonly MethodInfo __addMonthsWithTimezone;
        private static readonly MethodInfo __addQuarters;
        private static readonly MethodInfo __addQuartersWithTimezone;
        private static readonly MethodInfo __addSeconds;
        private static readonly MethodInfo __addSecondsWithTimezone;
        private static readonly MethodInfo __addTicks;
        private static readonly MethodInfo __addWeeks;
        private static readonly MethodInfo __addWeeksWithTimezone;
        private static readonly MethodInfo __addWithTimezone;
        private static readonly MethodInfo __addWithUnit;
        private static readonly MethodInfo __addWithUnitAndTimezone;
        private static readonly MethodInfo __addYears;
        private static readonly MethodInfo __addYearsWithTimezone;
        private static readonly MethodInfo __parse;
        private static readonly MethodInfo __subtractWithDateTime;
        private static readonly MethodInfo __subtractWithDateTimeAndTimezone;
        private static readonly MethodInfo __subtractWithDateTimeAndUnit;
        private static readonly MethodInfo __subtractWithDateTimeAndUnitAndTimezone;
        private static readonly MethodInfo __subtractWithTimeSpan;
        private static readonly MethodInfo __subtractWithTimeSpanAndTimezone;
        private static readonly MethodInfo __subtractWithUnit;
        private static readonly MethodInfo __subtractWithUnitAndTimezone;
        private static readonly MethodInfo __toStringWithFormat;
        private static readonly MethodInfo __toStringWithFormatAndTimezone;
        private static readonly MethodInfo __truncate;
        private static readonly MethodInfo __truncateWithBinSize;
        private static readonly MethodInfo __truncateWithBinSizeAndTimezone;
        private static readonly MethodInfo __week;
        private static readonly MethodInfo __weekWithTimezone;

        // static constructor
        static DateTimeMethod()
        {
            __add = ReflectionInfo.Method((DateTime @this, TimeSpan value) => @this.Add(value));
            __addDays = ReflectionInfo.Method((DateTime @this, double value) => @this.AddDays(value));
            __addDaysWithTimezone = ReflectionInfo.Method((DateTime @this, double value, string timezone) => @this.AddDays(value, timezone));
            __addHours = ReflectionInfo.Method((DateTime @this, double value) => @this.AddHours(value));
            __addHoursWithTimezone = ReflectionInfo.Method((DateTime @this, double value, string timezone) => @this.AddHours(value, timezone));
            __addMilliseconds = ReflectionInfo.Method((DateTime @this, double value) => @this.AddMilliseconds(value));
            __addMillisecondsWithTimezone = ReflectionInfo.Method((DateTime @this, double value, string timezone) => @this.AddMilliseconds(value, timezone));
            __addMinutes = ReflectionInfo.Method((DateTime @this, double value) => @this.AddMinutes(value));
            __addMinutesWithTimezone = ReflectionInfo.Method((DateTime @this, double value, string timezone) => @this.AddMinutes(value, timezone));
            __addMonths = ReflectionInfo.Method((DateTime @this, int value) => @this.AddMonths(value));
            __addMonthsWithTimezone = ReflectionInfo.Method((DateTime @this, int value, string timezone) => @this.AddMonths(value, timezone));
            __addQuarters = ReflectionInfo.Method((DateTime @this, int value) => @this.AddQuarters(value));
            __addQuartersWithTimezone = ReflectionInfo.Method((DateTime @this, int value, string timezone) => @this.AddQuarters(value, timezone));
            __addSeconds = ReflectionInfo.Method((DateTime @this, double value) => @this.AddSeconds(value));
            __addSecondsWithTimezone = ReflectionInfo.Method((DateTime @this, double value, string timezone) => @this.AddSeconds(value, timezone));
            __addTicks = ReflectionInfo.Method((DateTime @this, long value) => @this.AddTicks(value));
            __addWeeks = ReflectionInfo.Method((DateTime @this, int value) => @this.AddWeeks(value));
            __addWeeksWithTimezone = ReflectionInfo.Method((DateTime @this, int value, string timezone) => @this.AddWeeks(value, timezone));
            __addWithTimezone = ReflectionInfo.Method((DateTime @this, TimeSpan value, string timezone) => @this.Add(value, timezone));
            __addWithUnit = ReflectionInfo.Method((DateTime @this, long value, DateTimeUnit unit) => @this.Add(value, unit));
            __addWithUnitAndTimezone = ReflectionInfo.Method((DateTime @this, long value, DateTimeUnit unit, string timezone) => @this.Add(value, unit, timezone));
            __addYears = ReflectionInfo.Method((DateTime @this, int value) => @this.AddYears(value));
            __addYearsWithTimezone = ReflectionInfo.Method((DateTime @this, int value, string timezone) => @this.AddYears(value, timezone));
            __parse = ReflectionInfo.Method((string s) => DateTime.Parse(s));
            __subtractWithDateTime = ReflectionInfo.Method((DateTime @this, DateTime value) => @this.Subtract(value));
            __subtractWithDateTimeAndTimezone = ReflectionInfo.Method((DateTime @this, DateTime value, string timezone) => @this.Subtract(value, timezone));
            __subtractWithDateTimeAndUnit = ReflectionInfo.Method((DateTime @this, DateTime value, DateTimeUnit unit) => @this.Subtract(value, unit));
            __subtractWithDateTimeAndUnitAndTimezone = ReflectionInfo.Method((DateTime @this, DateTime value, DateTimeUnit unit, string timezone) => @this.Subtract(value, unit, timezone));
            __subtractWithTimeSpan = ReflectionInfo.Method((DateTime @this, TimeSpan value) => @this.Subtract(value));
            __subtractWithTimeSpanAndTimezone = ReflectionInfo.Method((DateTime @this, TimeSpan value, string timezone) => @this.Subtract(value, timezone));
            __subtractWithUnit = ReflectionInfo.Method((DateTime @this, long value, DateTimeUnit unit) => @this.Subtract(value, unit));
            __subtractWithUnitAndTimezone = ReflectionInfo.Method((DateTime @this, long value, DateTimeUnit unit, string timezone) => @this.Subtract(value, unit, timezone));
            __toStringWithFormat = ReflectionInfo.Method((DateTime @this, string format) => @this.ToString(format));
            __toStringWithFormatAndTimezone = ReflectionInfo.Method((DateTime @this, string format, string timezone) => @this.ToString(format, timezone));
            __truncate = ReflectionInfo.Method((DateTime @this, DateTimeUnit unit) => @this.Truncate(unit));
            __truncateWithBinSize = ReflectionInfo.Method((DateTime @this, DateTimeUnit unit, long binSize) => @this.Truncate(unit, binSize));
            __truncateWithBinSizeAndTimezone = ReflectionInfo.Method((DateTime @this, DateTimeUnit unit, long binSize, string timezone) => @this.Truncate(unit, binSize, timezone));
            __week = ReflectionInfo.Method((DateTime @this) => @this.Week());
            __weekWithTimezone = ReflectionInfo.Method((DateTime @this, string timezone) => @this.Week(timezone));
        }

        // public properties
        public static MethodInfo Add => __add;
        public static MethodInfo AddDays => __addDays;
        public static MethodInfo AddDaysWithTimezone => __addDaysWithTimezone;
        public static MethodInfo AddHours => __addHours;
        public static MethodInfo AddHoursWithTimezone => __addHoursWithTimezone;
        public static MethodInfo AddMilliseconds => __addMilliseconds;
        public static MethodInfo AddMillisecondsWithTimezone => __addMillisecondsWithTimezone;
        public static MethodInfo AddMinutes => __addMinutes;
        public static MethodInfo AddMinutesWithTimezone => __addMinutesWithTimezone;
        public static MethodInfo AddMonths => __addMonths;
        public static MethodInfo AddMonthsWithTimezone => __addMonthsWithTimezone;
        public static MethodInfo AddQuarters => __addQuarters;
        public static MethodInfo AddQuartersWithTimezone => __addQuartersWithTimezone;
        public static MethodInfo AddSeconds => __addSeconds;
        public static MethodInfo AddSecondsWithTimezone => __addSecondsWithTimezone;
        public static MethodInfo AddTicks => __addTicks;
        public static MethodInfo AddWeeks => __addWeeks;
        public static MethodInfo AddWeeksWithTimezone => __addWeeksWithTimezone;
        public static MethodInfo AddWithTimezone => __addWithTimezone;
        public static MethodInfo AddWithUnit => __addWithUnit;
        public static MethodInfo AddWithUnitAndTimezone => __addWithUnitAndTimezone;
        public static MethodInfo AddYears => __addYears;
        public static MethodInfo AddYearsWithTimezone => __addYearsWithTimezone;
        public static MethodInfo Parse => __parse;
        public static MethodInfo SubtractWithDateTime => __subtractWithDateTime;
        public static MethodInfo SubtractWithDateTimeAndTimezone => __subtractWithDateTimeAndTimezone;
        public static MethodInfo SubtractWithDateTimeAndUnit => __subtractWithDateTimeAndUnit;
        public static MethodInfo SubtractWithDateTimeAndUnitAndTimezone => __subtractWithDateTimeAndUnitAndTimezone;
        public static MethodInfo SubtractWithTimeSpan => __subtractWithTimeSpan;
        public static MethodInfo SubtractWithTimeSpanAndTimezone => __subtractWithTimeSpanAndTimezone;
        public static MethodInfo SubtractWithUnit => __subtractWithUnit;
        public static MethodInfo SubtractWithUnitAndTimezone => __subtractWithUnitAndTimezone;
        public static MethodInfo ToStringWithFormat => __toStringWithFormat;
        public static MethodInfo ToStringWithFormatAndTimezone => __toStringWithFormatAndTimezone;
        public static MethodInfo Truncate => __truncate;
        public static MethodInfo TruncateWithBinSize => __truncateWithBinSize;
        public static MethodInfo TruncateWithBinSizeAndTimezone => __truncateWithBinSizeAndTimezone;
        public static MethodInfo Week => __week;
        public static MethodInfo WeekWithTimezone => __weekWithTimezone;
    }
}

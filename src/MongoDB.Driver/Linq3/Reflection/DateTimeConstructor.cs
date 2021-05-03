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

namespace MongoDB.Driver.Linq3.Reflection
{
    internal static class DateTimeConstructor
    {
        private static readonly ConstructorInfo __withYearMonthDay;
        private static readonly ConstructorInfo __withYearMonthDayHourMinuteSecond;
        private static readonly ConstructorInfo __withYearMonthDayHourMinuteSecondMillisecond;

        static DateTimeConstructor()
        {
            __withYearMonthDay = ReflectionInfo.Constructor((int year, int month, int day) => new DateTime(year, month, day));
            __withYearMonthDayHourMinuteSecond = ReflectionInfo.Constructor((int year, int month, int day, int hour, int minute, int second) => new DateTime(year, month, day, hour, minute, second));
            __withYearMonthDayHourMinuteSecondMillisecond = ReflectionInfo.Constructor((int year, int month, int day, int hour, int minute, int second, int millisecond) => new DateTime(year, month, day, hour, minute, second, millisecond));
        }

        public static ConstructorInfo WithYearMonthDay=> __withYearMonthDay;
        public static ConstructorInfo WithYearMonthDayHourMinuteSecond => __withYearMonthDayHourMinuteSecond;
        public static ConstructorInfo WithYearMonthDayHourMinuteSecondMillisecond => __withYearMonthDayHourMinuteSecondMillisecond;
    }
}

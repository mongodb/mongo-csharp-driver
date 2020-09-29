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
using System.Linq.Expressions;
using System.Reflection;

namespace MongoDB.Driver.Linq3.Constructors
{
    public static class DateTimeConstructor
    {
        private static readonly ConstructorInfo __withYearMonthDay;
        private static readonly ConstructorInfo __withYearMonthDayHourMinuteSecond;
        private static readonly ConstructorInfo __withYearMonthDayHourMinuteSecondMillisecond;

        static DateTimeConstructor()
        {
            __withYearMonthDay = GetConstructor(() => new DateTime(0, 0, 0));
            __withYearMonthDayHourMinuteSecond = GetConstructor(() => new DateTime(0, 0, 0, 0, 0, 0 ));
            __withYearMonthDayHourMinuteSecondMillisecond = GetConstructor(() => new DateTime(0, 0, 0, 0, 0, 0, 0));
        }

        public static ConstructorInfo WithYearMonthDay=> __withYearMonthDay;
        public static ConstructorInfo WithYearMonthDayHourMinuteSecond => __withYearMonthDayHourMinuteSecond;
        public static ConstructorInfo WithYearMonthDayHourMinuteSecondMillisecond => __withYearMonthDayHourMinuteSecondMillisecond;

        private static ConstructorInfo GetConstructor(Expression<Func<DateTime>> lambda)
        {
            var body = (NewExpression)lambda.Body;
            return body.Constructor;
        }
    }
}

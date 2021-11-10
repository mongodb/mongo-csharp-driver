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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents the unit for various DateTime operations.
    /// </summary>
    public abstract class DateTimeUnit
    {
        #region static
        /// <summary>
        /// Day unit.
        /// </summary>
        public static DateTimeUnit Day { get; } = new UnitOnlyDayTimeUnit("day");

        /// <summary>
        /// Hour unit.
        /// </summary>
        public static DateTimeUnit Hour { get; } = new UnitOnlyDayTimeUnit("hour");

        /// <summary>
        /// Millisecond unit.
        /// </summary>
        public static DateTimeUnit Millisecond { get; } = new UnitOnlyDayTimeUnit("millisecond");

        /// <summary>
        /// Minute unit.
        /// </summary>
        public static DateTimeUnit Minute { get; } = new UnitOnlyDayTimeUnit("minute");

        /// <summary>
        /// Month unit.
        /// </summary>
        public static DateTimeUnit Month { get; } = new UnitOnlyDayTimeUnit("month");

        /// <summary>
        /// Quarter unit.
        /// </summary>
        public static DateTimeUnit Quarter { get; } = new UnitOnlyDayTimeUnit("quarter");

        /// <summary>
        /// Second unit.
        /// </summary>
        public static DateTimeUnit Second { get; } = new UnitOnlyDayTimeUnit("second");

        /// <summary>
        /// Week unit.
        /// </summary>
        public static DateTimeUnit Week { get; } = new UnitOnlyDayTimeUnit("week");

        /// <summary>
        /// Week starting on Friday unit.
        /// </summary>
        public static DateTimeUnit WeekStartingFriday { get; } = new WeekWithStartOfWeekDayTimeUnit("friday");

        /// <summary>
        /// Week starting on Monday unit.
        /// </summary>
        public static DateTimeUnit WeekStartingMonday { get; } = new WeekWithStartOfWeekDayTimeUnit("monday");

        /// <summary>
        /// Week starting on Saturday unit.
        /// </summary>
        public static DateTimeUnit WeekStartingSaturday { get; } = new WeekWithStartOfWeekDayTimeUnit("saturday");

        /// <summary>
        /// Week starting on Sunday unit.
        /// </summary>
        public static DateTimeUnit WeekStartingSunday { get; } = new WeekWithStartOfWeekDayTimeUnit("sunday");

        /// <summary>
        /// Week starting on Thursday unit.
        /// </summary>
        public static DateTimeUnit WeekStartingThursday { get; } = new WeekWithStartOfWeekDayTimeUnit("thursday");

        /// <summary>
        /// Week starting on Tuesday unit.
        /// </summary>
        public static DateTimeUnit WeekStartingTuesday { get; } = new WeekWithStartOfWeekDayTimeUnit("tuesday");

        /// <summary>
        /// Week starting on Wednesday unit.
        /// </summary>
        public static DateTimeUnit WeekStartingWednesday { get; } = new WeekWithStartOfWeekDayTimeUnit("wednesday");

        /// <summary>
        /// Year unit.
        /// </summary>
        public static DateTimeUnit Year { get; } = new UnitOnlyDayTimeUnit("year");
        #endregion

        /// <summary>
        /// The day of the start of the week (only valid with Week units that specify a start of the week).
        /// </summary>
        public abstract string StartOfWeek { get; }

        /// <summary>
        /// The unit.
        /// </summary>
        public abstract string Unit { get; }
    }

    internal sealed class UnitOnlyDayTimeUnit : DateTimeUnit
    {
        private readonly string _unit;

        public UnitOnlyDayTimeUnit(string unit)
        {
            _unit = Ensure.IsNotNullOrEmpty(unit, nameof(unit));
        }

        public override string StartOfWeek => throw new InvalidOperationException($"StartOfWeek is not valid for {nameof(UnitOnlyDayTimeUnit)}.");

        public override string Unit => _unit;

        public override string ToString() => _unit;
    }

    internal sealed class WeekWithStartOfWeekDayTimeUnit : DateTimeUnit
    {
        private readonly string _startOfWeek;

        public WeekWithStartOfWeekDayTimeUnit(string startOfWeek)
        {
            _startOfWeek = startOfWeek;
        }

        public override string StartOfWeek => _startOfWeek;

        public override string Unit => "week";

        public override string ToString() => $"week:{_startOfWeek}";
    }
}

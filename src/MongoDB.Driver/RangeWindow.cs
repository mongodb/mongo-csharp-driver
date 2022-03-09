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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a range window for a SetWindowFields window method.
    /// </summary>
    public sealed class RangeWindow : SetWindowFieldsWindow
    {
        #region static
        private static readonly KeywordRangeWindowBoundary __current = new KeywordRangeWindowBoundary("current");
        private static readonly KeywordRangeWindowBoundary __unbounded = new KeywordRangeWindowBoundary("unbounded");

        /// <summary>
        /// Returns a "current" range window boundary.
        /// </summary>
        public static KeywordRangeWindowBoundary Current => __current;

        /// <summary>
        /// Returns an "unbounded" range window boundary.
        /// </summary>
        public static KeywordRangeWindowBoundary Unbounded => __unbounded;

        /// <summary>
        /// Creates a range window.
        /// </summary>
        /// <typeparam name="TValue">The type of the boundary conditions.</typeparam>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A range window.</returns>
        public static RangeWindow Create<TValue>(TValue lowerBoundary, TValue upperBoundary)
        {
            return new RangeWindow(new ValueRangeWindowBoundary<TValue>(lowerBoundary), new ValueRangeWindowBoundary<TValue>(upperBoundary));
        }

        /// <summary>
        /// Creates a range window.
        /// </summary>
        /// <typeparam name="TValue">The type of the lower boundary condition.</typeparam>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A range window.</returns>
        public static RangeWindow Create<TValue>(TValue lowerBoundary, KeywordRangeWindowBoundary upperBoundary)
        {
            return new RangeWindow(new ValueRangeWindowBoundary<TValue>(lowerBoundary), upperBoundary);
        }

        /// <summary>
        /// Creates a range window.
        /// </summary>
        /// <typeparam name="TValue">The type of the upper boundary condition.</typeparam>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A range window.</returns>
        public static RangeWindow Create<TValue>(KeywordRangeWindowBoundary lowerBoundary, TValue upperBoundary)
        {
            return new RangeWindow(lowerBoundary, new ValueRangeWindowBoundary<TValue>(upperBoundary));
        }

        /// <summary>
        /// Creates a range window.
        /// </summary>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A range window.</returns>
        public static RangeWindow Create(TimeRangeWindowBoundary lowerBoundary, TimeRangeWindowBoundary upperBoundary)
        {
            return new RangeWindow(lowerBoundary, upperBoundary);
        }

        /// <summary>
        /// Creates a range window.
        /// </summary>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A range window.</returns>
        public static RangeWindow Create(TimeRangeWindowBoundary lowerBoundary, KeywordRangeWindowBoundary upperBoundary)
        {
            return new RangeWindow(lowerBoundary, upperBoundary);
        }

        /// <summary>
        /// Creates a range window.
        /// </summary>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A range window.</returns>
        public static RangeWindow Create(KeywordRangeWindowBoundary lowerBoundary, TimeRangeWindowBoundary upperBoundary)
        {
            return new RangeWindow(lowerBoundary, upperBoundary);
        }

        /// <summary>
        /// Creates a range window.
        /// </summary>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A range window.</returns>
        public static RangeWindow Create(KeywordRangeWindowBoundary lowerBoundary, KeywordRangeWindowBoundary upperBoundary)
        {
            return new RangeWindow(lowerBoundary, upperBoundary);
        }

        /// <summary>
        /// Returns a time range in days.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A time range.</returns>
        public static TimeRangeWindowBoundary Days(int value) => new TimeRangeWindowBoundary(value, unit: "day");

        /// <summary>
        /// Returns a time range in hours.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A time range.</returns>
        public static TimeRangeWindowBoundary Hours(int value) => new TimeRangeWindowBoundary(value, unit: "hour");

        /// <summary>
        /// Returns a time range in milliseconds.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A time range.</returns>
        public static TimeRangeWindowBoundary Milliseconds(int value) => new TimeRangeWindowBoundary(value, unit: "millisecond");

        /// <summary>
        /// Returns a time range in minutes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A time range.</returns>
        public static TimeRangeWindowBoundary Minutes(int value) => new TimeRangeWindowBoundary(value, unit: "minute");

        /// <summary>
        /// Returns a time range in months.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A time range.</returns>
        public static TimeRangeWindowBoundary Months(int value) => new TimeRangeWindowBoundary(value, unit: "month");

        /// <summary>
        /// Returns a time range in quarters.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A time range.</returns>
        public static TimeRangeWindowBoundary Quarters(int value) => new TimeRangeWindowBoundary(value, unit: "quarter");

        /// <summary>
        /// Returns a time range in seconds.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A time range.</returns>
        public static TimeRangeWindowBoundary Seconds(int value) => new TimeRangeWindowBoundary(value, unit: "second");

        /// <summary>
        /// Returns a time range in weeks.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A time range.</returns>
        public static TimeRangeWindowBoundary Weeks(int value) => new TimeRangeWindowBoundary(value, unit: "week");

        /// <summary>
        /// Returns a time range in years.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A time range.</returns>
        public static TimeRangeWindowBoundary Years(int value) => new TimeRangeWindowBoundary(value, unit: "year");
        #endregion

        private readonly RangeWindowBoundary _lowerBoundary;
        private readonly RangeWindowBoundary _upperBoundary;
        private readonly string _unit;

        /// <summary>
        /// Initializes an instance of RangeWindow.
        /// </summary>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        internal RangeWindow(RangeWindowBoundary lowerBoundary, RangeWindowBoundary upperBoundary)
        {
            _lowerBoundary = Ensure.IsNotNull(lowerBoundary, nameof(lowerBoundary));
            _upperBoundary = Ensure.IsNotNull(upperBoundary, nameof(upperBoundary));

            if (_lowerBoundary is TimeRangeWindowBoundary timeLowerBoundary &&
                _upperBoundary is TimeRangeWindowBoundary timeUpperBoundary)
            {
                if (timeLowerBoundary.Unit != timeUpperBoundary.Unit)
                {
                    throw new ArgumentException("Lower and upper time-based boundaries must use the same units.");
                }

                _unit = timeLowerBoundary.Unit;
            }
        }

        /// <summary>
        /// The lower boundary.
        /// </summary>
        public RangeWindowBoundary LowerBoundary => _lowerBoundary;

        /// <summary>
        /// The upper boundary.
        /// </summary>
        public RangeWindowBoundary UpperBoundary => _upperBoundary;

        /// <inheritdoc/>
        public override string ToString()
        {
            var unit = (_lowerBoundary as TimeRangeWindowBoundary)?.Unit ?? (_upperBoundary as TimeRangeWindowBoundary)?.Unit;
            if (unit != null)
            {
                return $"range : [{_lowerBoundary}, {_upperBoundary}], unit : \"{unit}\"";
            }
            else
            {
                return $"range : [{_lowerBoundary}, {_upperBoundary}]";
            }
        }
    }
}

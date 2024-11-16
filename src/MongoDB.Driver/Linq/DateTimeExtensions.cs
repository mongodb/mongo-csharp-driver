
/* Copyright 2016-present MongoDB Inc.
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
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// This static class holds methods that can be used to express MongoDB specific operations in LINQ queries.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Adds a value of the specified unit to a DateTime.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>The resulting DateTime.</returns>
        public static DateTime Add(this DateTime @this, long value, DateTimeUnit unit)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value of the specified unit to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime Add(this DateTime @this, long value, DateTimeUnit unit, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a TimeSpan value to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime Add(this DateTime @this, TimeSpan value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in days to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime AddDays(this DateTime @this, double value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in hours to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime AddHours(this DateTime @this, double value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in milliseconds to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime AddMilliseconds(this DateTime @this, double value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in minutes to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime AddMinutes(this DateTime @this, double value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in months to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime AddMonths(this DateTime @this, int value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in quarters to a DateTime.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <returns>The resulting DateTime.</returns>
        public static DateTime AddQuarters(this DateTime @this, int value)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in quarters to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime AddQuarters(this DateTime @this, int value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in seconds to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime AddSeconds(this DateTime @this, double value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in weeks to a DateTime taking.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <returns>The resulting DateTime.</returns>
        public static DateTime AddWeeks(this DateTime @this, int value)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in weeks to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime AddWeeks(this DateTime @this, int value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Adds a value in years to a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateAdd for information on timezones in MongoDB.</remarks>
        public static DateTime AddYears(this DateTime @this, int value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Subtracts the start date from the end date returning the result in the specified unit.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>The result.</returns>
        public static long Subtract(this DateTime @this, DateTime startDate, DateTimeUnit unit)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Subtracts the start date from the end date returning the result in the specified unit taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The result.</returns>
        /// <remarks>See the server documentation for $dateDiff for information on timezones in MongoDB.</remarks>
        public static long Subtract(this DateTime @this, DateTime startDate, DateTimeUnit unit, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Subtracts the start date from the end date returning a TimeSpan taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateDiff for information on timezones in MongoDB.</remarks>
        public static TimeSpan Subtract(this DateTime @this, DateTime value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Subtracts a TimeSpan from a date taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateSubtract for information on timezones in MongoDB.</remarks>
        public static DateTime Subtract(this DateTime @this, TimeSpan value, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Subtracts a value of the specified unit from a DateTime.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>The resulting DateTime.</returns>
        public static DateTime Subtract(this DateTime @this, long value, DateTimeUnit unit)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Subtracts a value of the specified unit from a DateTime taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="value">The value to be added.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateSubtract for information on timezones in MongoDB.</remarks>
        public static DateTime Subtract(this DateTime @this, long value, DateTimeUnit unit, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a DateTime value to a string.
        /// </summary>
        /// <param name="this">The DateTime value.</param>
        /// <param name="format">The format string (optional, can be null).</param>
        /// <param name="timezone">The timezone to use in the returned string (optional, can be null).</param>
        /// <returns>The DateTime value converted to a string.</returns>
        public static string ToString(this DateTime @this, string format, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Truncates a DateTime value to the specified unit.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>The resulting DateTime.</returns>
        public static DateTime Truncate(this DateTime @this, DateTimeUnit unit)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Truncates a DateTime value to the specified unit and bin size.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="binSize">The bin size.</param>
        /// <returns>The resulting DateTime.</returns>
        public static DateTime Truncate(this DateTime @this, DateTimeUnit unit, long binSize)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Truncates a DateTime value to the specified unit and bin size taking a timezone into consideration.
        /// </summary>
        /// <param name="this">The original DateTime.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="binSize">The bin size.</param>
        /// <param name="timezone">The timezone.</param>
        /// <returns>The resulting DateTime.</returns>
        /// <remarks>See the server documentation for $dateTrunc for information on timezones in MongoDB.</remarks>
        public static DateTime Truncate(this DateTime @this, DateTimeUnit unit, long binSize, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the week number of a specified DateTime value.
        /// </summary>
        /// <param name="this">The DateTime value.</param>
        /// <returns>The week number of a specified DateTime value.</returns>
        public static int Week(this DateTime @this)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the week number of a specified DateTime value.
        /// </summary>
        /// <param name="this">The DateTime value.</param>
        /// <param name="timezone">The timezone to use (optional, can be null).</param>
        /// <returns>The week number of a specified DateTime value.</returns>
        public static int Week(this DateTime @this, string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }
    }
}

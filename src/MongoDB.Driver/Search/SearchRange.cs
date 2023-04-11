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

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Object that specifies range of scalar and DateTime values.
    /// </summary>
    /// <typeparam name="TValue">The type of the range value.</typeparam>
    public struct SearchRange<TValue> where TValue : struct, IComparable<TValue>
    {
        #region static
        /// <summary>Empty range.</summary>
        public static SearchRange<TValue> Empty { get; } = new(default, default, default, default);
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchRange{TValue}"/> class.
        /// </summary>
        /// <param name="min">The lower bound of the range.</param>
        /// <param name="max">The upper bound of the range</param>
        /// <param name="isMinInclusive">Indicates whether the lower bound of the range is inclusive.</param>
        /// <param name="isMaxInclusive">Indicates whether the upper bound of the range is inclusive.</param>
        public SearchRange(TValue? min, TValue? max, bool isMinInclusive, bool isMaxInclusive)
        {
            if (min != null && max != null)
            {
                Ensure.IsGreaterThanOrEqualTo(max.Value, min.Value, nameof(max));
            }

            Min = min;
            Max = max;
            IsMinInclusive = isMinInclusive;
            IsMaxInclusive = isMaxInclusive;
        }

        /// <summary>Gets the value that indicates whether the upper bound of the range is inclusive.</summary>
        public bool IsMaxInclusive { get; }

        /// <summary>Gets the value that indicates whether the lower bound of the range is inclusive.</summary>
        public bool IsMinInclusive { get; }

        /// <summary>Gets the lower bound of the range.</summary>
        public TValue? Max { get; }

        /// <summary>Gets the lower bound of the range.</summary>
        public TValue? Min { get; }
    }

    /// <summary>
    /// A builder for a SearchRange.
    /// </summary>
    public static class SearchRangeBuilder
    {
        /// <summary>
        /// Creates a greater than search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Gt<TValue>(TValue value) where TValue : struct, IComparable<TValue>
            => SearchRange<TValue>.Empty.Gt(value);

        /// <summary>
        /// Adds a greater than value to a search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Gt<TValue>(this SearchRange<TValue> searchRange, TValue value) where TValue : struct, IComparable<TValue>
            => new(min: value, searchRange.Max, isMinInclusive: false, searchRange.IsMaxInclusive);

        /// <summary>
        /// Creates a greater or equal than search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Gte<TValue>(TValue value) where TValue : struct, IComparable<TValue>
            => SearchRange<TValue>.Empty.Gte(value);

        /// <summary>
        /// Adds a greater or equal than value to a search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Gte<TValue>(this SearchRange<TValue> searchRange, TValue value) where TValue : struct, IComparable<TValue>
            => new(min: value, searchRange.Max, isMinInclusive: true, searchRange.IsMaxInclusive);

        /// <summary>
        /// Creates a less than search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Lt<TValue>(TValue value) where TValue : struct, IComparable<TValue>
            => SearchRange<TValue>.Empty.Lt(value);

        /// <summary>
        /// Adds a less than value to a search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Lt<TValue>(this SearchRange<TValue> searchRange, TValue value) where TValue : struct, IComparable<TValue>
            => new(searchRange.Min, max: value, searchRange.IsMinInclusive, isMaxInclusive: false);

        /// <summary>
        /// Creates a less than or equal search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>search range.</returns>
        public static SearchRange<TValue> Lte<TValue>(TValue value) where TValue : struct, IComparable<TValue>
            => SearchRange<TValue>.Empty.Lte(value);

        /// <summary>
        /// Adds a less than or equal value to a search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>search range.</returns>
        public static SearchRange<TValue> Lte<TValue>(this SearchRange<TValue> searchRange, TValue value) where TValue : struct, IComparable<TValue>
            => new(searchRange.Min, max: value, searchRange.IsMinInclusive, isMaxInclusive: true);
    }
}

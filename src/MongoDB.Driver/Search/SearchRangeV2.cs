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

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Object that specifies the boundaries for a range query. 
    /// </summary>
    /// <typeparam name="TValue">The type of the range value.</typeparam>
    public struct SearchRangeV2<TValue>
    {
        #region static
        /// <summary>Empty range.</summary>
        internal static SearchRangeV2<TValue> Empty { get; } = new(null, null);
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchRangeV2{TValue}"/> class.
        /// </summary>
        /// <param name="min">The lower bound of the range.</param>
        /// <param name="max">The upper bound of the range.</param>
        public SearchRangeV2(Bound<TValue> min, Bound<TValue> max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>Gets the upper bound of the range.</summary>
        public Bound<TValue> Max { get; }

        /// <summary>Gets the lower bound of the range.</summary>
        public Bound<TValue> Min { get; }
    }
    
    /// <summary>
    /// Represents a bound value.
    /// </summary>
    /// <typeparam name="TValue">The type of the bound value.</typeparam>
    public sealed class Bound<TValue>
    {
        /// <summary>
        /// Gets the bound value.
        /// </summary>
        public TValue Value { get; }
        
        /// <summary>
        /// Gets whether the bound is inclusive or not.
        /// </summary>
        public bool Inclusive { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bound{TValue}"/> class.
        /// </summary>
        /// <param name="value">The value of the bound.</param>
        /// <param name="inclusive">Indicates whether the bound is inclusive or not.</param>
        public Bound(TValue value, bool inclusive = false)
        {
            Value = value;
            Inclusive = inclusive;
        }
    }
    
    /// <summary>
    /// A builder for a SearchRangeV2.
    /// </summary>
    public static class SearchRangeV2Builder
    {
        /// <summary>
        /// Creates a greater than search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRangeV2<TValue> Gt<TValue>(TValue value) => SearchRangeV2<TValue>.Empty.Gt(value);

        /// <summary>
        /// Adds a greater than value to a search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRangeV2<TValue> Gt<TValue>(this SearchRangeV2<TValue> searchRange, TValue value)
            => new(min: new (value), searchRange.Max);

        /// <summary>
        /// Creates a greater or equal than search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRangeV2<TValue> Gte<TValue>(TValue value)
            => SearchRangeV2<TValue>.Empty.Gte(value);

        /// <summary>
        /// Adds a greater or equal than value to a search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRangeV2<TValue> Gte<TValue>(this SearchRangeV2<TValue> searchRange, TValue value)
            => new(min: new(value, inclusive: true), searchRange.Max);

        /// <summary>
        /// Creates a less than search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRangeV2<TValue> Lt<TValue>(TValue value)
            => SearchRangeV2<TValue>.Empty.Lt(value);

        /// <summary>
        /// Adds a less than value to a search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRangeV2<TValue> Lt<TValue>(this SearchRangeV2<TValue> searchRange, TValue value)
            => new(searchRange.Min, max: new(value));

        /// <summary>
        /// Creates a less than or equal search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>search range.</returns>
        public static SearchRangeV2<TValue> Lte<TValue>(TValue value)
            => SearchRangeV2<TValue>.Empty.Lte(value);

        /// <summary>
        /// Adds a less than or equal value to a search range.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>search range.</returns>
        public static SearchRangeV2<TValue> Lte<TValue>(this SearchRangeV2<TValue> searchRange, TValue value)
            => new(searchRange.Min, max: new(value, inclusive: true));
    }
}

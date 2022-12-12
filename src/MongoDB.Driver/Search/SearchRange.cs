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
        /// <summary>Empty range.</summary>
        public static SearchRange<TValue> Empty { get; } = new(default, default, default, default);

        /// <summary>
        /// Initializes a new instance of the <see cref="Range{TValue}"/> class.
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

        /// <summary>Gets the lower bound of the range.</summary>
        public TValue? Min { get; }
        /// <summary>Gets the lower bound of the range.</summary>
        public TValue? Max { get; }
        /// <summary>Gets the value that indicates whether the lower bound of the range is inclusive.</summary>
        public bool IsMinInclusive { get; }
        /// <summary>Gets the value that indicates whether the upper bound of the range is inclusive.</summary>
        public bool IsMaxInclusive { get; }
    }

    /// <summary>
    /// A builder for a SearchRange.
    /// </summary>
    public static class SearchRangeBuilder
    {
        /// <summary>
        /// Creates a greater than search range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Gt<TValue>(TValue value) where TValue : struct, IComparable<TValue>
            => SearchRange<TValue>.Empty.Gt(value);

        /// <summary>
        /// Creates a greater or equal than search range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Gte<TValue>(TValue value) where TValue : struct, IComparable<TValue>
            => SearchRange<TValue>.Empty.Gte(value);

        /// <summary>
        /// Creates a less than search range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Lt<TValue>(TValue value) where TValue : struct, IComparable<TValue>
            => SearchRange<TValue>.Empty.Lt(value);

        /// <summary>
        /// Creates a less than or equal search range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>search range.</returns>
        public static SearchRange<TValue> Lte<TValue>(TValue value) where TValue : struct, IComparable<TValue>
            => SearchRange<TValue>.Empty.Lte(value);

        /// <summary>
        /// Adds a greater than value to a search range.
        /// </summary>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Gt<TValue>(this SearchRange<TValue> searchRange, TValue value) where TValue : struct, IComparable<TValue>
            => new(value, searchRange.Max, false, searchRange.IsMaxInclusive);

        /// <summary>
        /// Adds a greater or equal than value to a search range.
        /// </summary>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Gte<TValue>(this SearchRange<TValue> searchRange, TValue value) where TValue : struct, IComparable<TValue>
            => new(value, searchRange.Max, true, searchRange.IsMaxInclusive);

        /// <summary>
        /// Adds a less than value to a search range.
        /// </summary>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>Search range.</returns>
        public static SearchRange<TValue> Lt<TValue>(this SearchRange<TValue> searchRange, TValue value) where TValue : struct, IComparable<TValue>
            => new(searchRange.Min, value, searchRange.IsMinInclusive, false);

        /// <summary>
        /// Adds a less than or equal value to a search range.
        /// </summary>
        /// <param name="searchRange">Search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>search range.</returns>
        public static SearchRange<TValue> Lte<TValue>(this SearchRange<TValue> searchRange, TValue value) where TValue : struct, IComparable<TValue>
            => new(searchRange.Min, value, searchRange.IsMinInclusive, true);
    }
}

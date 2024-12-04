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
    /// Object that specifies range of string values.
    /// </summary>
    public struct StringSearchRange
    {
        #region static
        /// <summary>Empty range.</summary>
        public static StringSearchRange Empty { get; } = new(default, default, default, default);
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="StringSearchRange"/> class.
        /// </summary>
        /// <param name="min">The lower bound of the range.</param>
        /// <param name="max">The upper bound of the range</param>
        /// <param name="isMinInclusive">Indicates whether the lower bound of the range is inclusive.</param>
        /// <param name="isMaxInclusive">Indicates whether the upper bound of the range is inclusive.</param>
        public StringSearchRange(string min, string max, bool isMinInclusive, bool isMaxInclusive)
        {
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
        public string Max { get; }

        /// <summary>Gets the lower bound of the range.</summary>
        public string Min { get; }
    }

    /// <summary>
    /// A builder for a StringSearchRange.
    /// </summary>
    public static class StringSearchRangeBuilder
    {
        /// <summary>
        /// Creates a greater than string search range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>StringSearchRange.</returns>
        public static StringSearchRange Gt(string value)
            => StringSearchRange.Empty.Gt(value);
        
        /// <summary>
        /// Adds a greater than value to a string search range.
        /// </summary>
        /// <param name="stringSearchRange">The string search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>StringSearchRange.</returns>
        public static StringSearchRange Gt(this StringSearchRange stringSearchRange, string value)
            => new(min: value, stringSearchRange.Max, isMinInclusive: false, stringSearchRange.IsMaxInclusive);
        
        /// <summary>
        /// Creates a greater than or equal to string search range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>StringSearchRange.</returns>
        public static StringSearchRange Gte(string value)
            => StringSearchRange.Empty.Gte(value);
        
        /// <summary>
        /// Adds a greater than or equal to value to a string search range.
        /// </summary>
        /// <param name="stringSearchRange">The string search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>StringSearchRange.</returns>
        public static StringSearchRange Gte(this StringSearchRange stringSearchRange, string value)
            => new(min: value, stringSearchRange.Max, isMinInclusive: true, stringSearchRange.IsMaxInclusive);
        
        /// <summary>
        /// Creates a less than string search range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>StringSearchRange.</returns>
        public static StringSearchRange Lt(string value)
            => StringSearchRange.Empty.Lt(value);
        
        /// <summary>
        /// Adds a less than value to a string search range.
        /// </summary>
        /// <param name="stringSearchRange">The string search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>StringSearchRange.</returns>
        public static StringSearchRange Lt(this StringSearchRange stringSearchRange, string value)
            => new(stringSearchRange.Min, max: value, stringSearchRange.IsMinInclusive, false);
        
        /// <summary>
        /// Creates a less than or equal to string search range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>StringSearchRange.</returns>
        public static StringSearchRange Lte(string value)
            => StringSearchRange.Empty.Lte(value);
        
        /// <summary>
        /// Adds a less than or equal to value to a string search range.
        /// </summary>
        /// <param name="stringSearchRange">The string search range.</param>
        /// <param name="value">The value.</param>
        /// <returns>StringSearchRange.</returns>
        public static StringSearchRange Lte(this StringSearchRange stringSearchRange, string value)
            => new(stringSearchRange.Min, max: value, stringSearchRange.IsMinInclusive, true);
    }
}
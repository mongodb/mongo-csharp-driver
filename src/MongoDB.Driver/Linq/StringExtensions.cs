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
using System.Collections.Generic;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// This static class holds methods that can be used to express MongoDB specific operations in LINQ queries.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Returns true if any value in s is present in values (corresponds to the $in filter operator).
        /// </summary>
        /// <param name="s">The values to test.</param>
        /// <param name="values">The values to test against.</param>
        /// <returns>True if any value in s is present in values.</returns>
        public static bool AnyStringIn(this IEnumerable<string> s, IEnumerable<StringOrRegularExpression> values)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns true if any value in s is present in values (corresponds to the $in filter operator).
        /// </summary>
        /// <param name="s">The values to test.</param>
        /// <param name="values">The values to test against.</param>
        /// <returns>True if any value in s is present in values.</returns>
        public static bool AnyStringIn(this IEnumerable<string> s, params StringOrRegularExpression[] values)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns true if any value in s is not present in values (corresponds to the $nin filter operator).
        /// </summary>
        /// <param name="s">The values to test.</param>
        /// <param name="values">The values to test against.</param>
        /// <returns>True if any value in s is not present in values.</returns>
        public static bool AnyStringNin(this IEnumerable<string> s, IEnumerable<StringOrRegularExpression> values)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns true if any value in s is not present in values (corresponds to the $nin filter operator).
        /// </summary>
        /// <param name="s">The values to test.</param>
        /// <param name="values">The values to test against.</param>
        /// <returns>True if any value in s is not present in values.</returns>
        public static bool AnyStringNin(this IEnumerable<string> s, params StringOrRegularExpression[] values)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Searches a string for an occurrence of a substring and returns the UTF-8 byte index (zero-based) of the first occurrence.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The value.</param>
        /// <returns>The byte index of the first occurrence, or -1 if not found.</returns>
        public static int IndexOfBytes(this string s, string value)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Searches a string for an occurrence of a substring and returns the UTF-8 byte index (zero-based) of the first occurrence.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns>The byte index of the first occurrence, or -1 if not found.</returns>
        public static int IndexOfBytes(this string s, string value, int startIndex)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Searches a string for an occurrence of a substring and returns the UTF-8 byte index (zero-based) of the first occurrence.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The count.</param>
        /// <returns>The byte index of the first occurrence, or -1 if not found.</returns>
        public static int IndexOfBytes(this string s, string value, int startIndex, int count)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns true if s is present in values (corresponds to the $in filter operator).
        /// </summary>
        /// <param name="s">The value to test.</param>
        /// <param name="values">The values to test against.</param>
        /// <returns>True if s is present in values.</returns>
        public static bool StringIn(this string s, IEnumerable<StringOrRegularExpression> values)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns true if s is present in values (corresponds to the $in filter operator).
        /// </summary>
        /// <param name="s">The value to test.</param>
        /// <param name="values">The values to test against.</param>
        /// <returns>True if s is present in values.</returns>
        public static bool StringIn(this string s, params StringOrRegularExpression[] values)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns true if s is not present in values (corresponds to the $nin filter operator).
        /// </summary>
        /// <param name="s">The value to test.</param>
        /// <param name="values">The values to test against.</param>
        /// <returns>True if s is not present in values.</returns>
        public static bool StringNin(this string s, IEnumerable<StringOrRegularExpression> values)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns true if s is not present in values (corresponds to the $nin filter operator).
        /// </summary>
        /// <param name="s">The value to test.</param>
        /// <param name="values">The values to test against.</param>
        /// <returns>True if s is not present in values.</returns>
        public static bool StringNin(this string s, params StringOrRegularExpression[] values)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the number of UTF-8 encoded bytes in the specified string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The number of UTF-8 bytes.</returns>
        public static int StrLenBytes(this string s)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Returns the number of UTF-8 encoded bytes in the specified string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="length">The length.</param>
        /// <returns>The number of UTF-8 bytes.</returns>
        public static string SubstrBytes(this string s, int startIndex, int length)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }
    }
}

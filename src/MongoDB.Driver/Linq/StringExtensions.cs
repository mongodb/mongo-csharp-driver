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

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// This static class holds methods that can be used to express MongoDB specific operations in LINQ queries.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Searches a string for an occurrence of a substring and returns the UTF-8 byte index (zero-based) of the first occurrence.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The value.</param>
        /// <returns>The byte index of the first occurrence, or -1 if not found.</returns>
        public static int IndexOfBytes(this string s, string value)
        {
            throw new InvalidOperationException("This String.IndexOfBytes method is only intended to be used in LINQ queries.");
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
            throw new InvalidOperationException("This String.IndexOfBytes method is only intended to be used in LINQ queries.");
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
            throw new InvalidOperationException("This String.IndexOfBytes method is only intended to be used in LINQ queries.");
        }

        /// <summary>
        /// Returns the number of UTF-8 encoded bytes in the specified string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The number of UTF-8 bytes.</returns>
        public static int StrLenBytes(this string s)
        {
            throw new InvalidOperationException("This String.StrLenBytes method is only intended to be used in LINQ queries.");
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
            throw new InvalidOperationException("This String.SubstrBytes method is only intended to be used in LINQ queries.");
        }
    }
}


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
    public static class NullableDateTimeExtensions
    {
        /// <summary>
        /// Converts a NullableDateTime value to a string.
        /// </summary>
        /// <param name="this">The NullableDateTime value.</param>
        /// <param name="format">The format string (optional, can be null).</param>
        /// <param name="timezone">The timezone to use in the returned string (optional, can be null).</param>
        /// <param name="onNull">The string to return if the NullableDateTime value is null.</param>
        /// <returns>The NullableDateTime value converted to a string.</returns>
        public static string ToString(this DateTime? @this, string format, string timezone, string onNull)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }
    }
}

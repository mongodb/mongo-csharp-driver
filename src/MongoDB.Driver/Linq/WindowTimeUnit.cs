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
* 
*/

using System;
using MongoDB.Bson;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// The time unit to use in DateTime SetWindowFields windows.
    /// </summary>
    public enum WindowTimeUnit
    {
        /// <summary>
        /// Weeks.
        /// </summary>
        Week,

        /// <summary>
        /// Days.
        /// </summary>
        Day,

        /// <summary>
        /// Hours.
        /// </summary>
        Hour,

        /// <summary>
        /// Minutes.
        /// </summary>
        Minute,

        /// <summary>
        /// Seconds.
        /// </summary>
        Second,

        /// <summary>
        /// Milliseconds.
        /// </summary>
        Millisecond
    }

    internal static class WindowTimeUnitExtensions
    {
        public static BsonValue Render(this WindowTimeUnit unit)
        {
            return unit switch
            {
                WindowTimeUnit.Week => "week",
                WindowTimeUnit.Day => "day",
                WindowTimeUnit.Hour => "hour",
                WindowTimeUnit.Minute => "minute",
                WindowTimeUnit.Second => "second",
                WindowTimeUnit.Millisecond => "millisecond",
                _ => throw new ArgumentException($"Invalid WindowTimeUnit : {unit}.", nameof(unit))
            };
        }
    }
}

/* Copyright 2021-present MongoDB Inc.
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
    /// Temporary change.
    /// </summary>
    public static class MongoInternalDefaults
    {
        private static TimeSpan? __rttTimeout;
        private static TimeSpan? __rttReadTimeout;

        /// <summary>
        /// Temporary change.
        /// </summary>
        public static class ConnectionPool
        {
            /// <summary>
            /// Temporary change.
            /// </summary>
            public const int MaxConnecting = 2;
        }

        /// <summary>
        /// Temporary change.
        /// </summary>
        public static TimeSpan? RttInterval
        {
            get { return __rttTimeout; }
            set { __rttTimeout = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        /// <summary>
        /// Temporary change.
        /// </summary>
        public static TimeSpan? RttReadTimeout
        {
            get { return __rttReadTimeout; }
            set { __rttReadTimeout = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }
    }
}

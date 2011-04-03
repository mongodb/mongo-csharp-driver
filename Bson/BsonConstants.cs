/* Copyright 2010-2011 10gen Inc.
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
using System.Linq;
using System.Text;

namespace MongoDB.Bson {
    /// <summary>
    /// A static class containing BSON constants.
    /// </summary>
    public static class BsonConstants {
        #region private static fields
        private static readonly long dateTimeMaxValueMillisecondsSinceEpoch;
        private static readonly long dateTimeMinValueMillisecondsSinceEpoch;
        private static readonly DateTime unixEpoch;
        #endregion

        #region static constructor
        static BsonConstants() {
            // unixEpoch has to be initialized first
            unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dateTimeMaxValueMillisecondsSinceEpoch = (DateTime.MaxValue - unixEpoch).Ticks / 10000;
            dateTimeMinValueMillisecondsSinceEpoch = (DateTime.MinValue - unixEpoch).Ticks / 10000;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets the number of milliseconds since the Unix epoch for DateTime.MaxValue.
        /// </summary>
        public static long DateTimeMaxValueMillisecondsSinceEpoch {
            get { return dateTimeMaxValueMillisecondsSinceEpoch; }
        }

        /// <summary>
        /// Gets the number of milliseconds since the Unix epoch for DateTime.MinValue.
        /// </summary>
        public static long DateTimeMinValueMillisecondsSinceEpoch {
            get { return dateTimeMinValueMillisecondsSinceEpoch; }
        }

        /// <summary>
        /// Gets the Unix Epoch for BSON DateTimes (1970-01-01).
        /// </summary>
        public static DateTime UnixEpoch { get { return unixEpoch; } }
        #endregion
    }
}

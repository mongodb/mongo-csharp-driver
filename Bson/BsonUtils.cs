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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson {
    /// <summary>
    /// A static class containing BSON utility methods.
    /// </summary>
    public static class BsonUtils {
        #region public static methods
        /// <summary>
        /// Parses a hex string to a byte array.
        /// </summary>
        /// <param name="s">The hex string.</param>
        /// <returns>A byte array.</returns>
        public static byte[] ParseHexString(
            string s
        ) {
            byte[] bytes;
            if (!TryParseHexString(s, out bytes)) {
                var message = string.Format("'{0}' is not a valid hex string.", s);
                throw new FormatException(message);
            }
            return bytes;
        }

        /// <summary>
        /// Converts from number of milliseconds since Unix epoch to DateTime.
        /// </summary>
        /// <param name="millisecondsSinceEpoch">The number of milliseconds since Unix epoch.</param>
        /// <returns>A DateTime.</returns>
        public static DateTime ToDateTimeFromMillisecondsSinceEpoch(
            long millisecondsSinceEpoch
        ) {
            // MaxValue has to be handled specially to avoid rounding errors
            if (millisecondsSinceEpoch == BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch) {
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            } else {
                return BsonConstants.UnixEpoch.AddTicks(millisecondsSinceEpoch * 10000);
            }
        }

        /// <summary>
        /// Converts a byte array to a hex string.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>A hex string.</returns>
        public static string ToHexString(
            byte[] bytes
        ) {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts a DateTime to local time (with special handling for MinValue and MaxValue).
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <param name="kind">A DateTimeKind.</param>
        /// <returns>The DateTime in local time.</returns>
        public static DateTime ToLocalTime(
            DateTime dateTime,
            DateTimeKind kind
        ) {
            if (dateTime.Kind == kind) {
                return dateTime;
            } else {
                if (dateTime == DateTime.MinValue) {
                    return DateTime.SpecifyKind(DateTime.MinValue, kind);
                } else if (dateTime == DateTime.MaxValue) {
                    return DateTime.SpecifyKind(DateTime.MaxValue, kind);
                } else {
                    return DateTime.SpecifyKind(dateTime.ToLocalTime(), kind);
                }
            }
        }

        /// <summary>
        /// Converts a DateTime to number of milliseconds since Unix epoch.
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>Number of seconds since Unix epoch.</returns>
        public static long ToMillisecondsSinceEpoch(
            DateTime dateTime
        ) {
            var utcDateTime = ToUniversalTime(dateTime);
            return (utcDateTime - BsonConstants.UnixEpoch).Ticks / 10000;
        }

        /// <summary>
        /// Converts a DateTime to UTC (with special handling for MinValue and MaxValue).
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>The DateTime in UTC.</returns>
        public static DateTime ToUniversalTime(
            DateTime dateTime
        ) {
            if (dateTime.Kind == DateTimeKind.Utc) {
                return dateTime;
            } else {
                if (dateTime == DateTime.MinValue) {
                    return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
                } else if (dateTime == DateTime.MaxValue) {
                    return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
                } else {
                    return dateTime.ToUniversalTime();
                }
            }
        }

        /// <summary>
        /// Tries to parse a hex string to a byte array.
        /// </summary>
        /// <param name="s">The hex string.</param>
        /// <param name="bytes">A byte array.</param>
        /// <returns>True if the hex string was successfully parsed.</returns>
        public static bool TryParseHexString(
            string s,
            out byte[] bytes
        ) {
            if (s != null) {
                if ((s.Length & 1) != 0) { s = "0" + s; } // make length of s even
                bytes = new byte[s.Length / 2];
                for (int i = 0; i < bytes.Length; i++) {
                    string hex = s.Substring(2 * i, 2);
                    try {
                        byte b = Convert.ToByte(hex, 16);
                        bytes[i] = b;
                    } catch (FormatException) {
                        bytes = null;
                        return false;
                    }
                }
                return true;
            }

            bytes = null;
            return false;
        }
        #endregion
    }
}
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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MongoDB.Bson
{

    /// <summary>
    /// A static class containing BSON utility methods.
    /// </summary>
    public static class BsonUtils
    {
        // public static methods
        /// <summary>
        /// Gets a friendly class name suitable for use in error messages.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A friendly class name.</returns>
        public static string GetFriendlyTypeName(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericType)
            {
                return type.Name;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("{0}<", Regex.Replace(type.Name, @"\`\d+$", ""));
            foreach (var typeParameter in type.GetTypeInfo().GetGenericArguments())
            {
                sb.AppendFormat("{0}, ", GetFriendlyTypeName(typeParameter));
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append(">");
            return sb.ToString();
        }

        /// <summary>
        /// Get's the hex string's equivalent byte array size
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int GetHexStringBinaryLength(string s)
        {
            return (s.Length + 1) / 2;
        }

        /// <summary>
        /// Parses a hex string into its equivalent byte array.
        /// </summary>
        /// <param name="s">The hex string to parse.</param>
        /// <returns>The byte equivalent of the hex string.</returns>
        public static byte[] ParseHexString(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            byte[] bytes;
            if (!TryParseHexString(s, out bytes))
            {
                throw new FormatException("String should contain only hexadecimal digits.");
            }

            return bytes;
        }

        /// <summary>
        /// Parses a hex string into its equivalent span of bytes
        /// </summary>
        /// <param name="s">The hex string to parse.</param>
        /// <param name="span">The byte sequivalent of the hex string.</param>
        public static void ParseHexString(string s, Span<byte> span)
        {
            if (s == null)
            {
                throw new ArgumentException(nameof(s));
            }

            if (span == null)
            {
                throw new ArgumentException(nameof(span));
            }

            var length = GetHexStringBinaryLength(s);
            if (span.Length != length)
            {
                throw new ArgumentException(nameof(span), $"Span does not match the length of the equivalent binary size of the hex string (expected : {length})");
            }

            if (!TryParseHexString(s, span))
            {
                throw new FormatException("String should contain only hexadecimal digits.");
            }
        }

        /// <summary>
        /// Converts from number of milliseconds since Unix epoch to DateTime.
        /// </summary>
        /// <param name="millisecondsSinceEpoch">The number of milliseconds since Unix epoch.</param>
        /// <returns>A DateTime.</returns>
        public static DateTime ToDateTimeFromMillisecondsSinceEpoch(long millisecondsSinceEpoch)
        {
            if (millisecondsSinceEpoch < BsonConstants.DateTimeMinValueMillisecondsSinceEpoch ||
                millisecondsSinceEpoch > BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch)
            {
                var message = string.Format(
                    "The value {0} for the BsonDateTime MillisecondsSinceEpoch is outside the" +
                    "range that can be converted to a .NET DateTime.",
                    millisecondsSinceEpoch);
                throw new ArgumentOutOfRangeException("millisecondsSinceEpoch", message);
            }

            // MaxValue has to be handled specially to avoid rounding errors
            if (millisecondsSinceEpoch == BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch)
            {
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            }
            else
            {
                return BsonConstants.UnixEpoch.AddTicks(millisecondsSinceEpoch * 10000);
            }
        }

        /// <summary>
        /// Converts a value to a hex character.
        /// </summary>
        /// <param name="value">The value (assumed to be between 0 and 15).</param>
        /// <returns>The hex character.</returns>
        public static char ToHexChar(int value)
        {
            return (char)(value + (value < 10 ? '0' : 'a' - 10));
        }

        /// <summary>
        /// Converts a byte array to a hex string.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>A hex string.</returns>
        public static string ToHexString(byte[] bytes)
        {
            return ToHexString(bytes.AsSpan());
        }

        /// <summary>
        /// Converts a span of bytes to a hex string.
        /// </summary>
        /// <param name="bytes">The span of bytes.</param>
        /// <returns>A hex string.</returns>
        public static string ToHexString(ReadOnlySpan<byte> bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentException(nameof(bytes));
            }

            var length = bytes.Length;
            Span<char> c = stackalloc char[length * 2];
            for (int i = 0, j = 0; i < length; i++)
            {
                var b = bytes[i];
                c[j++] = ToHexChar(b >> 4);
                c[j++] = ToHexChar(b & 0x0f);
            }
            return c.ToString();
        }

        /// <summary>
        /// Converts a DateTime to local time (with special handling for MinValue and MaxValue).
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>The DateTime in local time.</returns>
        public static DateTime ToLocalTime(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local);
            }
            else if (dateTime == DateTime.MaxValue)
            {
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local);
            }
            else
            {
                return dateTime.ToLocalTime();
            }
        }

        /// <summary>
        /// Converts a DateTime to number of milliseconds since Unix epoch.
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>Number of seconds since Unix epoch.</returns>
        public static long ToMillisecondsSinceEpoch(DateTime dateTime)
        {
            var utcDateTime = ToUniversalTime(dateTime);
            return (utcDateTime - BsonConstants.UnixEpoch).Ticks / 10000;
        }

        /// <summary>
        /// Converts a DateTime to UTC (with special handling for MinValue and MaxValue).
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>The DateTime in UTC.</returns>
        public static DateTime ToUniversalTime(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            }
            else if (dateTime == DateTime.MaxValue)
            {
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            }
            else
            {
                return dateTime.ToUniversalTime();
            }
        }

        /// <summary>
        /// Tries to parse a hex string to a pre-allocated span of bytes
        /// </summary>
        /// <param name="s">The hex string.</param>
        /// <param name="buffer">A pre-allocated span of bytes</param>
        /// <returns>True if the hex string was successfully parsed, and the allocated span of bytes has the exact length</returns>
        public static bool TryParseHexString(string s, Span<byte> buffer)
        {
            if (s == null || buffer.Length != GetHexStringBinaryLength(s)) {
                return false;
            }

            var i = 0;
            var j = 0;

            if ((s.Length % 2) == 1)
            {
                // if s has an odd length assume an implied leading "0"
                int y;
                if (!TryParseHexChar(s[i++], out y))
                {
                    return false;
                }
                buffer[j++] = (byte)y;
            }

            while (i < s.Length)
            {
                int x, y;
                if (!TryParseHexChar(s[i++], out x))
                {
                    return false;
                }
                if (!TryParseHexChar(s[i++], out y))
                {
                    return false;
                }
                buffer[j++] = (byte)((x << 4) | y);
            }
            return true;
        }

        /// <summary>
        /// Tries to parse a hex string to a byte array.
        /// </summary>
        /// <param name="s">The hex string.</param>
        /// <param name="bytes">A byte array.</param>
        /// <returns>True if the hex string was successfully parsed.</returns>
        public static bool TryParseHexString(string s, out byte[] bytes)
        {
            bytes = null;

            if (s == null)
            {
                return false;
            }

            var buffer = new byte[GetHexStringBinaryLength(s)];
            var result = TryParseHexString(s, buffer);

            if (result)
            {
                bytes = buffer;
            }

            return result;
        }

        // private static methods
        private static bool TryParseHexChar(char c, out int value)
        {
            if (c >= '0' && c <= '9')
            {
                value = c - '0';
                return true;
            }

            if (c >= 'a' && c <= 'f')
            {
                value = 10 + (c - 'a');
                return true;
            }

            if (c >= 'A' && c <= 'F')
            {
                value = 10 + (c - 'A');
                return true;
            }

            value = 0;
            return false;
        }
    }
}

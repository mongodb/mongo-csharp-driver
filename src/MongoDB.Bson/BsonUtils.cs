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
            foreach (var typeParameter in typeInfo.GetGenericArguments())
            {
                sb.AppendFormat("{0}, ", GetFriendlyTypeName(typeParameter));
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append(">");
            return sb.ToString();
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
        /// Parses a hex string into its equivalent byte array.
        /// </summary>
        /// <param name="s">The hex string to parse.</param>
        /// <param name="bytes">The output buffer containing the byte equivalent of the hex string.</param>
        public static void ParseHexChars(ReadOnlySpan<char> s, Span<byte> bytes)
        {
            if (!TryParseHexChars(s, bytes))
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
                    "The value {0} for the BsonDateTime MillisecondsSinceEpoch is outside the " +
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
#if NET5_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bytes);
#else
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
#endif
            return ToHexString(bytes.AsMemory());
        }

        /// <summary>
        /// Converts a memory of bytes to a hex string.
        /// </summary>
        /// <param name="bytes">The memory of bytes.</param>
        /// <returns>A hex string.</returns>
        public static string ToHexString(ReadOnlyMemory<byte> bytes)
        {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return string.Create(bytes.Length * 2, bytes, static (chars, bytes) =>
            {
                ToHexChars(bytes.Span, chars);
            });
#else
            return new string(ToHexChars(bytes.Span));
#endif
        }

        /// <summary>
        /// Converts a span of byte to a span of hex characters.
        /// </summary>
        /// <param name="bytes">The input span of bytes.</param>
        /// <returns>An array of hex characters.</returns>
        public static char[] ToHexChars(ReadOnlySpan<byte> bytes)
        {
            var length = bytes.Length;
            var c = new char[length * 2];
            ToHexChars(bytes, c.AsSpan());
            return c;
        }

        /// <summary>
        /// Converts a span of bytes to a span of hex characters.
        /// </summary>
        /// <param name="bytes">The input span of bytes.</param>
        /// <param name="chars">The result span of characters.</param>
        public static void ToHexChars(ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            if (chars.Length != bytes.Length * 2)
            {
                throw new ArgumentException("Length of character span should be 2 times the length of byte span");
            }
            int length = bytes.Length;
            for (int i = 0, j = 0; i < length; i++)
            {
                var b = bytes[i];
                chars[j++] = ToHexChar(b >> 4);
                chars[j++] = ToHexChar(b & 0x0f);
            }
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
        /// <returns>Number of milliseconds since Unix epoch.</returns>
        public static long ToMillisecondsSinceEpoch(DateTime dateTime)
        {
            var utcDateTime = ToUniversalTime(dateTime);
            return (utcDateTime - BsonConstants.UnixEpoch).Ticks / 10000;
        }

        /// <summary>
        /// Converts a DateTime to number of seconds since Unix epoch.
        /// </summary>
        /// <param name="dateTime">A DateTime.</param>
        /// <returns>Number of seconds since Unix epoch.</returns>
        public static long ToSecondsSinceEpoch(DateTime dateTime)
        {
            var utcDateTime = ToUniversalTime(dateTime);
            return (utcDateTime - BsonConstants.UnixEpoch).Ticks / TimeSpan.TicksPerSecond;
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

            var buffer = new byte[(s.Length + 1) / 2];
            if (!TryParseHexChars(s.AsSpan(), buffer.AsSpan()))
            {
                return false;
            }
            bytes = buffer;
            return true;
        }

        /// <summary>
        /// Tries to parse hex characters into a span of bytes.
        /// </summary>
        /// <param name="s">The span containing hex characters.</param>
        /// <param name="bytes">The result byte span.</param>
        /// <returns>True if the hex string was successfully parsed.</returns>
        public static bool TryParseHexChars(ReadOnlySpan<char> s, Span<byte> bytes)
        {
            return HexParser.TryParse(s, bytes);
        }

        private static class HexParser
        {
            private static readonly int s_min = Math.Min(Math.Min('a', 'A'), '0');
            private static readonly int s_max = Math.Max(Math.Max('f', 'F'), '9');

            private static readonly byte[] s_lookup = CreateLookup();

            private static byte[] CreateLookup()
            {
                var result = new byte[s_max - s_min + 1];
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = HexToByte((char)(i + s_min));
                }
                return result;
                static byte HexToByte(char ch)
                {
                    if (char.IsDigit(ch))
                    {
                        return (byte)(ch - '0');
                    }
                    else if ('A' <= ch && ch <= 'F')
                    {
                        return (byte)(10 + ch - 'A');
                    }
                    else if ('a' <= ch && ch <= 'f')
                    {
                        return (byte)(10 + ch - 'a');
                    }
                    else
                    {
                        return byte.MaxValue;
                    }
                }
            }

            public static bool TryParse(ReadOnlySpan<char> chars, Span<byte> bytes)
            {
                if (bytes.Length != (chars.Length + 1) / 2)
                    return false;
                int j = 0;
                if ((chars.Length & 1) == 1)
                {
                    // if chars has an odd length assume an implied leading "0"
                    if (!TryParseChar(chars[0], out byte b))
                        return false;
                    bytes[j++] = b;
                    chars = chars.Slice(1);
                }
                for (int i = 0; i < chars.Length; i += 2)
                {
                    if (!TryParseChars(chars.Slice(i, 2), out byte b))
                        return false;
                    bytes[j++] = b;
                }
                return true;
            }

            public static bool TryParseChars(ReadOnlySpan<char> chars, out byte value)
            {
                if (chars.Length == 1)
                {
                    return TryParseChar(chars[0], out value);
                }
                if (chars.Length >= 2
                    && TryParseChar(chars[0], out byte upper)
                    && TryParseChar(chars[1], out byte lower))
                {
                    value = (byte)((upper << 4) | lower);
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            public static bool TryParseChar(char ch, out byte result)
            {
                int index = ch - s_min;
                if (0 <= index && index < s_lookup.Length && s_lookup[index] != byte.MaxValue)
                {
                    result = s_lookup[index];
                    return true;
                }
                else
                {
                    result = default;
                    return false;
                }
            }
        }
    }
}

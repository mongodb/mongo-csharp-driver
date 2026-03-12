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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
            int x = value + '0';
            return (char)(x + (((9 - value) >> 31) & 39));
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
        /// Get the 2-character hex value of the byte, combined into a single uint
        /// </summary>
        /// <param name="byteValue">The byte to convert</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint GetHexChars(byte byteValue)
        {
            return HexConverter.GetHexChars(byteValue);
        }

        /// <summary>
        /// Converts a span of bytes to a span of hex characters.
        /// </summary>
        /// <param name="bytes">The input span of bytes.</param>
        /// <param name="chars">The result span of characters.</param>
        public static void ToHexChars(ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            HexConverter.ToHexChars(bytes, chars);
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

        /// <summary>
        /// Tries to parse 2 hex characters and combine them into a single byte
        /// </summary>
        /// <param name="c1">The first character</param>
        /// <param name="c2">The second character</param>
        /// <param name="value">The combined byte value</param>
        /// <returns>True if the hex characters were successfully parsed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseByte(char c1, char c2, out byte value)
        {
            return HexParser.TryParseByte(c1, c2, out value);
        }

        private static class HexParser
        {
            private static readonly int[] s_lookup = CreateLookup();

            private static int[] CreateLookup()
            {
                var table = new int[128];
                table.AsSpan().Fill(0xFF);
                for (int i = 0; i < 10; i++)
                {
                    table[i + '0'] = (byte)i;
                }
                for (int i = 0; i < 6; i++)
                {
                    table[i + 'a'] = (byte)(i + 10);
                    table[i + 'A'] = (byte)(i + 10);
                }
                return table;
            }

            public static bool TryParse(ReadOnlySpan<char> chars, Span<byte> bytes)
            {
                if (bytes.Length != (chars.Length + 1) / 2)
                    return false;

                int j = 0;
                int i = 0;

                if ((chars.Length & 1) != 0)
                {
                    if (!TryParseByte('0', chars[0], out byte b))
                        return false;

                    bytes[j++] = b;
                    i = 1;
                }

                for (; i < chars.Length; i += 2)
                {
                    if (!TryParseByte(chars[i], chars[i + 1], out byte b))
                        return false;

                    bytes[j++] = b;
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryParseByte(char c1, char c2, out byte value)
            {
                int n1 = c1 < 128 ? s_lookup[c1] : 0xFF;
                int n2 = c2 < 128 ? s_lookup[c2] : 0xFF;

                if ((n1 | n2) == 0xFF)
                {
                    value = default;
                    return false;
                }

                value = (byte)((n1 << 4) | n2);
                return true;
            }
        }

        private static class HexConverter
        {
            private static readonly uint[] s_hexLookup = CreateLookup();

            private static uint[] CreateLookup()
            {
                var result = new uint[256];

                for (int i = 0; i < 256; i++)
                {
                    int hi = i >> 4;
                    int lo = i & 0xF;

                    uint c1 = (uint)(hi + (hi < 10 ? '0' : 'a' - 10));
                    uint c2 = (uint)(lo + (lo < 10 ? '0' : 'a' - 10));

                    result[i] = c1 | (c2 << 16);
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint GetHexChars(byte byteValue)
            {
                return s_hexLookup[byteValue];
            }

            public static void ToHexChars(ReadOnlySpan<byte> bytes, Span<char> chars)
            {
                if (chars.Length != bytes.Length * 2)
                    throw new ArgumentException("Length of character span should be 2x byte span");

                var uintSpan = MemoryMarshal.Cast<char, uint>(chars);
                for (int i = 0; i < bytes.Length; i++)
                {
                    uintSpan[i] = s_hexLookup[bytes[i]];
                }
            }
        }
    }
}

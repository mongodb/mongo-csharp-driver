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
using System.Linq;
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
        // private static fields
        private static readonly uint[] __lookupByteToHex = CreateLookupByteToHex();
        private static readonly byte[] __lookupCharToByte = CreateLookupHexCharToByte();
        private static readonly char[] __lookupNibbleToHexChar = CreateLookupNibbleToHexChar();

        private const byte InvalidByte = byte.MaxValue;

        // private static methods
        /// <summary>
        /// Precalculates a lookup array that will help the performance of any byte to hex char conversion.
        /// </summary>
        /// <returns>An array that maps all potential byte values (0-255) to a uint that represents the two hex chars for each byte key.</returns>
        private static uint[] CreateLookupByteToHex()
        {
            var result = new uint[256];
            for (var b = 0; b < 256; b++)
            {
                // first we convert each byte into a hex string...
                var s = b.ToString("x2");
                // ...then we store the hex representation of the byte in a uint such that
                // every uint in our array consists of 2x2 bytes
                // each of which will represent a single char, e.g.:
                //         b: 181
                //         s: "b5" --> two chars: 'b': 98 (01100010) and '5': 53 (00110101)
                // result[i]:  3473506 (00000000 01100010 00000000 00110101) (little endian notation)
                //               this is 'b' ----^     and '5' ----^
                result[b] = ((uint)s[0] << 16) | s[1];
            }
            return result;
        }

        /// <summary>
        /// Precalculates a lookup array that will help the performance of any hex char to byte conversion.
        /// </summary>
        /// <returns>An array that maps all potential hex char values (0-F) to their respective byte representation or <see cref="InvalidByte"/> for invalid hex chars.</returns>
        private static byte[] CreateLookupHexCharToByte()
        {
            // initialization: all bytes invalid
            var result = Enumerable.Repeat(InvalidByte, byte.MaxValue + 1).ToArray();
            
            const string hexChars = "0123456789abcdefABCDEF";

            // path valid bytes
            foreach (var c in hexChars)
            {
                result[c] = Convert.ToByte(c.ToString(), 16);
            }
            return result;
        }

        /// <summary>
        /// Precalculates a lookup array that will help the performance of any nibble to char conversion.
        /// </summary>
        /// <returns>An array that maps all potential hex characters (0-F) to the byte values (0-255) to a uint that represents the two hex chars for each byte key.</returns>
        private static char[] CreateLookupNibbleToHexChar()
        {
            var lookup = new char[15];
            for (var i = 0; i < 15; i++)
            {
                lookup[i] = (char)(i + (i < 10 ? '0' : 'a' - 10));
            }

            return lookup;
        }

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

        private static bool TryParseHexChars(char left, char right, out byte b)
        {
            var l = __lookupCharToByte[left];
            var r = __lookupCharToByte[right];
            if (l == InvalidByte || r == InvalidByte)
            {
                b = 0;
                return false;
            }
            b = (byte) ((l << 4) | r);
            return true;
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
                    "The value {0} for the BsonDateTime MillisecondsSinceEpoch is outside the"+
                    "range that can be converted to a .NET DateTime.",
                    millisecondsSinceEpoch);
                throw new ArgumentOutOfRangeException(nameof(millisecondsSinceEpoch), message);
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
            return __lookupNibbleToHexChar[value];
        }

        /// <summary>
        /// Converts a byte array to a hex string.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>A hex string.</returns>
        public static string ToHexString(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var length = bytes.Length;
            var c = new char[length * 2];

            for (int i = 0; i < length; i++)
            {
                var val = __lookupByteToHex[bytes[i]];
                c[2 * i] = (char)(val >> 16);
                c[2 * i + 1] = (char)val;
            }

            return new string(c);
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

            // some magic in order to avoid creating a new string with a "0" glued to the start in case of an odd string
            var stringLength = s.Length;
            var offset = stringLength & 1; // the offset will hold either 1 or zero depending on if we need to shift character processing by one or not
            var buffer = new byte[(stringLength + offset) / 2];
            if (offset == 1)
            {
                // process first character separately, prefixed with a '0'
                if (!TryParseHexChars('0', s[0], out buffer[0]))
                {
                    return false;
                }
            }
            for (var i = offset; i < buffer.Length; i++)
            {
                var startIndex = 2 * i - offset;
                if(!TryParseHexChars(s[startIndex], s[startIndex + 1], out buffer[i]))
                {
                    return false;
                }
            }

            bytes = buffer;
            return true;
        }
    }
}
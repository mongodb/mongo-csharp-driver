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
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents an ObjectId (see also BsonObjectId).
    /// </summary>
    public struct ObjectId : IComparable<ObjectId>, IEquatable<ObjectId>, IConvertible
    {
        // private static fields
        private static readonly ObjectId __emptyInstance = default(ObjectId);
        private static readonly long __random = CalculateRandomValue();
        private static int __staticIncrement = (new Random()).Next();

        // private fields
        private readonly int _a;
        private readonly int _b;
        private readonly int _c;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public ObjectId(byte[] bytes)
            : this(bytes.AsSpan())
        {
        }
        // constructors
        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public ObjectId(ReadOnlySpan<byte> bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (bytes.Length != 12)
            {
                throw new ArgumentException("Byte array must be 12 bytes long", "bytes");
            }

            FromSpan(bytes, out _a, out _b, out _c);
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="index">The index into the byte array where the ObjectId starts.</param>
        internal ObjectId(ReadOnlySpan<byte> bytes, int index)
        {
            FromSpan(bytes.Slice(index), out _a, out _b, out _c);
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ObjectId(string value)
            : this(value is null ? throw new ArgumentNullException("value") :value.AsSpan())
        {
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ObjectId(ReadOnlySpan<char> value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            Span<byte> bytes = stackalloc byte[12];
            BsonUtils.ParseHexChars(value, bytes);
            FromSpan(bytes, out _a, out _b, out _c);
        }

        private ObjectId(int a, int b, int c)
        {
            _a = a;
            _b = b;
            _c = c;
        }

        // public static properties
        /// <summary>
        /// Gets an instance of ObjectId where the value is empty.
        /// </summary>
        public static ObjectId Empty
        {
            get { return __emptyInstance; }
        }

        // public properties
        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public int Timestamp
        {
            get { return _a; }
        }

        /// <summary>
        /// Gets the creation time (derived from the timestamp).
        /// </summary>
        public DateTime CreationTime
        {
            get { return BsonConstants.UnixEpoch.AddSeconds((uint)Timestamp); }
        }

        // public operators
        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is less than the second ObjectId.</returns>
        public static bool operator <(ObjectId lhs, ObjectId rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is less than or equal to the second ObjectId.</returns>
        public static bool operator <=(ObjectId lhs, ObjectId rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are equal.</returns>
        public static bool operator ==(ObjectId lhs, ObjectId rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are not equal.</returns>
        public static bool operator !=(ObjectId lhs, ObjectId rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is greather than or equal to the second ObjectId.</returns>
        public static bool operator >=(ObjectId lhs, ObjectId rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is greather than the second ObjectId.</returns>
        public static bool operator >(ObjectId lhs, ObjectId rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        // public static methods
        /// <summary>
        /// Generates a new ObjectId with a unique value.
        /// </summary>
        /// <returns>An ObjectId.</returns>
        public static ObjectId GenerateNewId()
        {
            return GenerateNewId(GetTimestampFromDateTime(DateTime.UtcNow));
        }

        /// <summary>
        /// Generates a new ObjectId with a unique value (with the timestamp component based on a given DateTime).
        /// </summary>
        /// <param name="timestamp">The timestamp component (expressed as a DateTime).</param>
        /// <returns>An ObjectId.</returns>
        public static ObjectId GenerateNewId(DateTime timestamp)
        {
            return GenerateNewId(GetTimestampFromDateTime(timestamp));
        }

        /// <summary>
        /// Generates a new ObjectId with a unique value (with the given timestamp).
        /// </summary>
        /// <param name="timestamp">The timestamp component.</param>
        /// <returns>An ObjectId.</returns>
        public static ObjectId GenerateNewId(int timestamp)
        {
            int increment = Interlocked.Increment(ref __staticIncrement) & 0x00ffffff; // only use low order 3 bytes
            return Create(timestamp, __random, increment);
        }

        /// <summary>
        /// Parses a string and creates a new ObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <returns>A ObjectId.</returns>
        public static ObjectId Parse(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            ObjectId objectId;
            if (TryParse(s, out objectId))
            {
                return objectId;
            }
            else
            {
                var message = string.Format("'{0}' is not a valid 24 digit hex string.", s);
                throw new FormatException(message);
            }
        }

        /// <summary>
        /// Tries to parse a string and create a new ObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <param name="objectId">The new ObjectId.</param>
        /// <returns>True if the string was parsed successfully.</returns>
        public static bool TryParse(string s, out ObjectId objectId)
        {
            // don't throw ArgumentNullException if s is null
            if (s == null)
            {
                objectId = default;
                return false;
            }
            return TryParse(s.AsSpan(), out objectId);
        }

        /// <summary>
        /// Tries to parse a string and create a new ObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <param name="objectId">The new ObjectId.</param>
        /// <returns>True if the string was parsed successfully.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out ObjectId objectId)
        {
            if (s.Length == 24)
            {
                Span<byte> bytes = stackalloc byte[12];
                if (BsonUtils.TryParseHexChars(s, bytes))
                {
                    objectId = new ObjectId(bytes);
                    return true;
                }
            }

            objectId = default(ObjectId);
            return false;
        }

        // internal static methods
        internal static long CalculateRandomValue()
        {
#if NET472_OR_GREATER
            var seed = (int)DateTime.UtcNow.Ticks ^ GetMachineHash() ^ GetPid();
            var random = new Random(seed);
#else
            var random = new Random();
#endif
            var high = random.Next();
            var low = random.Next();
            var combined = (long)((ulong)(uint)high << 32 | (ulong)(uint)low);
            return combined & 0xffffffffff; // low order 5 bytes
        }

        // private static methods
        private static ObjectId Create(int timestamp, long random, int increment)
        {
            if (random < 0 || random > 0xffffffffff)
            {
                throw new ArgumentOutOfRangeException(nameof(random), "The random value must be between 0 and 1099511627775 (it must fit in 5 bytes).");
            }
            if (increment < 0 || increment > 0xffffff)
            {
                throw new ArgumentOutOfRangeException(nameof(increment), "The increment value must be between 0 and 16777215 (it must fit in 3 bytes).");
            }

            var a = timestamp;
            var b = (int)(random >> 8); // first 4 bytes of random
            var c = (int)(random << 24) | increment; // 5th byte of random and 3 byte increment
            return new ObjectId(a, b, c);
        }

        /// <summary>
        /// Gets the current process id.  This method exists because of how CAS operates on the call stack, checking
        /// for permissions before executing the method.  Hence, if we inlined this call, the calling method would not execute
        /// before throwing an exception requiring the try/catch at an even higher level that we don't necessarily control.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetCurrentProcessId()
        {
            using var process = Process.GetCurrentProcess();
            return process.Id;
        }

        private static int GetMachineHash()
        {
            // use instead of Dns.HostName so it will work offline
            var machineName = GetMachineName();
            return 0x00ffffff & machineName.GetHashCode(); // use first 3 bytes of hash
        }

        private static string GetMachineName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return "";
            }
        }

        private static short GetPid()
        {
            try
            {
                return (short)GetCurrentProcessId(); // use low order two bytes only
            }
            catch
            {
                return 0;
            }
        }

        private static int GetTimestampFromDateTime(DateTime timestamp)
        {
            var secondsSinceEpoch = (long)Math.Floor((BsonUtils.ToUniversalTime(timestamp) - BsonConstants.UnixEpoch).TotalSeconds);
            if (secondsSinceEpoch < uint.MinValue || secondsSinceEpoch > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timestamp");
            }
            return (int)(uint)secondsSinceEpoch;
        }

        private static void FromSpan(ReadOnlySpan<byte> bytes, out int a, out int b, out int c)
        {
            var ints = MemoryMarshal.Cast<byte, int>(bytes);
            a = BinaryPrimitives.ReverseEndianness(ints[0]);
            b = BinaryPrimitives.ReverseEndianness(ints[1]);
            c = BinaryPrimitives.ReverseEndianness(ints[2]);
        }

        // public methods
        /// <summary>
        /// Compares this ObjectId to another ObjectId.
        /// </summary>
        /// <param name="other">The other ObjectId.</param>
        /// <returns>A 32-bit signed integer that indicates whether this ObjectId is less than, equal to, or greather than the other.</returns>
        public int CompareTo(ObjectId other)
        {
            int result = ((uint)_a).CompareTo((uint)other._a);
            if (result != 0) { return result; }
            result = ((uint)_b).CompareTo((uint)other._b);
            if (result != 0) { return result; }
            return ((uint)_c).CompareTo((uint)other._c);
        }

        /// <summary>
        /// Compares this ObjectId to another ObjectId.
        /// </summary>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are equal.</returns>
        public bool Equals(ObjectId rhs)
        {
            return
                _a == rhs._a &&
                _b == rhs._b &&
                _c == rhs._c;
        }

        /// <summary>
        /// Compares this ObjectId to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is an ObjectId and equal to this one.</returns>
        public override bool Equals(object obj)
        {
            if (obj is ObjectId)
            {
                return Equals((ObjectId)obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = 37 * hash + _a.GetHashCode();
            hash = 37 * hash + _b.GetHashCode();
            hash = 37 * hash + _c.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Converts the ObjectId to a byte array.
        /// </summary>
        /// <returns>A byte array.</returns>
        public byte[] ToByteArray()
        {
            var bytes = new byte[12];
            ToByteArray(bytes, 0);
            return bytes;
        }

        /// <summary>
        /// Converts the ObjectId to a byte array.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="offset">The offset.</param>
        public void ToByteArray(byte[] destination, int offset)
        {
            ToByteSpan(destination.AsSpan().Slice(offset));
        }

        /// <summary>
        /// Writes the ObjectId into a byte span.
        /// </summary>
        /// <param name="destination">The destination.</param>
        public void ToByteSpan(Span<byte> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if (12 > destination.Length)
            {
                throw new ArgumentException("Not enough room in destination buffer.", "offset");
            }
            var intValues = MemoryMarshal.Cast<byte, int>(destination);
            intValues[0] = BinaryPrimitives.ReverseEndianness(_a);
            intValues[1] = BinaryPrimitives.ReverseEndianness(_b);
            intValues[2] = BinaryPrimitives.ReverseEndianness(_c);
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString()
        {
#if NETFRAMEWORK
            var chars = new char[24];
#else
            Span<char> chars = stackalloc char[24];
#endif
            WriteToSpan(chars);
            return new string(chars);
        }

        /// <summary>
        /// Writes the string representation of the value into the target span
        /// </summary>
        /// <param name="span">the target span</param>
        public void WriteToSpan(Span<char> span)
        {
            if (span.Length < 24)
            {
                throw new ArgumentException("Not enough room in destination span.", "offset");
            }
            Span<int> intValues = stackalloc int[3];
            intValues[0] = BinaryPrimitives.ReverseEndianness(_a);
            intValues[1] = BinaryPrimitives.ReverseEndianness(_b);
            intValues[2] = BinaryPrimitives.ReverseEndianness(_c);
            BsonUtils.ToHexChars(MemoryMarshal.AsBytes(intValues), span);
        }

        // explicit IConvertible implementation
        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            switch (Type.GetTypeCode(conversionType))
            {
                case TypeCode.String:
                    return ((IConvertible)this).ToString(provider);
                case TypeCode.Object:
                    if (conversionType == typeof(object) || conversionType == typeof(ObjectId))
                    {
                        return this;
                    }
                    if (conversionType == typeof(BsonObjectId))
                    {
                        return new BsonObjectId(this);
                    }
                    if (conversionType == typeof(BsonString))
                    {
                        return new BsonString(((IConvertible)this).ToString(provider));
                    }
                    break;
            }

            throw new InvalidCastException();
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }
    }
}

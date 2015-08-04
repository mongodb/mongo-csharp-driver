/* Copyright 2010-2015 MongoDB Inc.
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
using System.Globalization;

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents a BSON double value.
    /// </summary>
    [Serializable]
    public class BsonDouble : BsonValue, IComparable<BsonDouble>, IEquatable<BsonDouble>
    {
         #region static
        const int __minPrecreatedValue = -100;
        const int __maxPrecreatedValue = 100;
        private static readonly BsonDouble[] __precreatedInstances = new BsonDouble[__maxPrecreatedValue - __minPrecreatedValue + 1];

        static BsonDouble()
        {
            for (var i = __minPrecreatedValue; i <= __maxPrecreatedValue; i++)
            {
                var precreatedInstance = new BsonDouble(i);
                var index = i - __minPrecreatedValue;
                __precreatedInstances[index] = precreatedInstance;
            }
        }
        #endregion

        // private fields
        private double _value;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonDouble class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BsonDouble(double value)
        {
            _value = value;
        }

        // public properties
        /// <summary>
        /// Gets the BsonType of this BsonValue.
        /// </summary>
        public override BsonType BsonType
        {
            get { return BsonType.Double; }
        }

        /// <summary>
        /// Gets the BsonDouble as a double.
        /// </summary>
        [Obsolete("Use Value instead.")]
        public override object RawValue
        {
            get { return _value; }
        }

        /// <summary>
        /// Gets the value of this BsonDouble.
        /// </summary>
        public double Value
        {
            get { return _value; }
        }

        // public operators
        /// <summary>
        /// Converts a double to a BsonDouble.
        /// </summary>
        /// <param name="value">A double.</param>
        /// <returns>A BsonDouble.</returns>
        public static implicit operator BsonDouble(double value)
        {
            var intValue = (int)value;
            if (intValue == value && intValue >= __minPrecreatedValue && intValue <= __maxPrecreatedValue)
            {
                var index = intValue - __minPrecreatedValue;
                return __precreatedInstances[index];
            }
            return new BsonDouble(value);
        }

        /// <summary>
        /// Compares two BsonDouble values.
        /// </summary>
        /// <param name="lhs">The first BsonDouble.</param>
        /// <param name="rhs">The other BsonDouble.</param>
        /// <returns>True if the two BsonDouble values are not equal according to ==.</returns>
        public static bool operator !=(BsonDouble lhs, BsonDouble rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two BsonDouble values.
        /// </summary>
        /// <param name="lhs">The first BsonDouble.</param>
        /// <param name="rhs">The other BsonDouble.</param>
        /// <returns>True if the two BsonDouble values are equal according to ==.</returns>
        public static bool operator ==(BsonDouble lhs, BsonDouble rhs)
        {
            if (object.ReferenceEquals(lhs, null)) { return object.ReferenceEquals(rhs, null); }
            return lhs.OperatorEqualsImplementation(rhs);
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of the BsonDouble class.
        /// </summary>
        /// <param name="value">An object to be mapped to a BsonDouble.</param>
        /// <returns>A BsonDouble.</returns>
        public new static BsonDouble Create(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
        }

        // public methods
        /// <summary>
        /// Compares this BsonDouble to another BsonDouble.
        /// </summary>
        /// <param name="other">The other BsonDouble.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonDouble is less than, equal to, or greather than the other.</returns>
        public int CompareTo(BsonDouble other)
        {
            if (other == null) { return 1; }
            return _value.CompareTo(other._value);
        }

        /// <summary>
        /// Compares the BsonDouble to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this BsonDouble is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(BsonValue other)
        {
            if (other == null) { return 1; }
            var otherDouble = other as BsonDouble;
            if (otherDouble != null)
            {
                return _value.CompareTo(otherDouble._value);
            }
            var otherInt32 = other as BsonInt32;
            if (otherInt32 != null)
            {
                return _value.CompareTo((double)otherInt32.Value);
            }
            var otherInt64 = other as BsonInt64;
            if (otherInt64 != null)
            {
                return _value.CompareTo((double)otherInt64.Value);
            }
            return CompareTypeTo(other);
        }

        /// <summary>
        /// Compares this BsonDouble to another BsonDouble.
        /// </summary>
        /// <param name="rhs">The other BsonDouble.</param>
        /// <returns>True if the two BsonDouble values are equal.</returns>
        public bool Equals(BsonDouble rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return _value.Equals(rhs._value); // use Equals instead of == so NaN is handled correctly
        }

        /// <summary>
        /// Compares this BsonDouble to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a BsonDouble and equal to this one.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as BsonDouble); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + BsonType.GetHashCode();
            hash = 37 * hash + _value.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Converts this BsonValue to a Boolean (using the JavaScript definition of truthiness).
        /// </summary>
        /// <returns>A Boolean.</returns>
        public override bool ToBoolean()
        {
            return !(double.IsNaN(_value) || _value == 0.0);
        }

        /// <summary>
        /// Converts this BsonValue to a Double.
        /// </summary>
        /// <returns>A Double.</returns>
        public override double ToDouble()
        {
            return _value;
        }

        /// <summary>
        /// Converts this BsonValue to an Int32.
        /// </summary>
        /// <returns>An Int32.</returns>
        public override int ToInt32()
        {
            return (int)_value;
        }

        /// <summary>
        /// Converts this BsonValue to an Int64.
        /// </summary>
        /// <returns>An Int32.</returns>
        public override long ToInt64()
        {
            return (long)_value;
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString()
        {
            return _value.ToString("R", NumberFormatInfo.InvariantInfo);
        }

        // protected methods
        /// <inheritdoc/>
        protected override TypeCode IConvertibleGetTypeCodeImplementation()
        {
            return TypeCode.Double;
        }

        /// <inheritdoc/>
        protected override bool IConvertibleToBooleanImplementation(IFormatProvider provider)
        {
            return Convert.ToBoolean(_value, provider);
        }

        /// <inheritdoc/>
        protected override byte IConvertibleToByteImplementation(IFormatProvider provider)
        {
            return Convert.ToByte(_value, provider);
        }

        /// <inheritdoc/>
        protected override decimal IConvertibleToDecimalImplementation(IFormatProvider provider)
        {
            return Convert.ToDecimal(_value, provider);
        }

        /// <inheritdoc/>
        protected override double IConvertibleToDoubleImplementation(IFormatProvider provider)
        {
            return _value;
        }

        /// <inheritdoc/>
        protected override short IConvertibleToInt16Implementation(IFormatProvider provider)
        {
            return Convert.ToInt16(_value, provider);
        }

        /// <inheritdoc/>
        protected override int IConvertibleToInt32Implementation(IFormatProvider provider)
        {
            return Convert.ToInt32(_value, provider);
        }

        /// <inheritdoc/>
        protected override long IConvertibleToInt64Implementation(IFormatProvider provider)
        {
            return Convert.ToInt64(_value, provider);
        }

        /// <inheritdoc/>
#pragma warning disable 3002
        protected override sbyte IConvertibleToSByteImplementation(IFormatProvider provider)
        {
            return Convert.ToSByte(_value, provider);
        }
#pragma warning restore

        /// <inheritdoc/>
        protected override float IConvertibleToSingleImplementation(IFormatProvider provider)
        {
            return Convert.ToSingle(_value, provider);
        }

        /// <inheritdoc/>
        protected override string IConvertibleToStringImplementation(IFormatProvider provider)
        {
            return Convert.ToString(_value, provider);
        }

        /// <inheritdoc/>
#pragma warning disable 3002
        protected override ushort IConvertibleToUInt16Implementation(IFormatProvider provider)
        {
            return Convert.ToUInt16(_value, provider);
        }
#pragma warning restore

        /// <inheritdoc/>
#pragma warning disable 3002
        protected override uint IConvertibleToUInt32Implementation(IFormatProvider provider)
        {
            return Convert.ToUInt32(_value, provider);
        }
#pragma warning restore

        /// <inheritdoc/>
#pragma warning disable 3002
        protected override ulong IConvertibleToUInt64Implementation(IFormatProvider provider)
        {
            return Convert.ToUInt64(_value, provider);
        }
#pragma warning restore

        /// <summary>
        /// Compares this BsonDouble against another BsonValue.
        /// </summary>
        /// <param name="rhs">The other BsonValue.</param>
        /// <returns>True if this BsonDouble and the other BsonValue are equal according to ==.</returns>
        protected override bool OperatorEqualsImplementation(BsonValue rhs)
        {
            var rhsDouble = rhs as BsonDouble;
            if (rhsDouble != null)
            {
                return _value == rhsDouble._value; // use == instead of Equals so NaN is handled correctly
            }

            var rhsInt32 = rhs as BsonInt32;
            if (rhsInt32 != null)
            {
                return _value == (double)rhsInt32.Value;
            }

            var rhsInt64 = rhs as BsonInt64;
            if (rhsInt64 != null)
            {
                return _value == (double)rhsInt64.Value;
            }

            return this.Equals(rhs);
        }
    }
}

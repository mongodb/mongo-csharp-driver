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

using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization {
    /// <summary>
    /// Represents the external representation of a field or property.
    /// </summary>
    public class RepresentationSerializationOptions : IBsonSerializationOptions {
        #region private fields
        private BsonType representation;
        private bool allowOverflow;
        private bool allowTruncation;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the RepresentationSerializationOptions class.
        /// </summary>
        /// <param name="representation">The external representation.</param>
        public RepresentationSerializationOptions(
            BsonType representation
        ) {
            this.representation = representation;
        }

        /// <summary>
        /// Initializes a new instance of the RepresentationSerializationOptions class.
        /// </summary>
        /// <param name="representation">The external representation.</param>
        /// <param name="allowOverflow">Whether to allow overflow.</param>
        /// <param name="allowTruncation">Whether to allow truncation.</param>
        public RepresentationSerializationOptions(
            BsonType representation,
            bool allowOverflow,
            bool allowTruncation
        ) {
            this.representation = representation;
            this.allowOverflow = allowOverflow;
            this.allowTruncation = allowTruncation;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the external representation.
        /// </summary>
        public BsonType Representation {
            get { return representation; }
        }

        /// <summary>
        /// Gets whether to allow overflow.
        /// </summary>
        public bool AllowOverflow {
            get { return allowOverflow; }
        }

        /// <summary>
        /// Gets whether to allow truncation.
        /// </summary>
        public bool AllowTruncation {
            get { return allowTruncation; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Converts a Double to a Decimal.
        /// </summary>
        /// <param name="value">A Double.</param>
        /// <returns>A Decimal.</returns>
        public decimal ToDecimal(
            double value
        ) {
            if (value == double.MinValue) {
                return decimal.MinValue;
            } else if (value == double.MaxValue) {
                return decimal.MaxValue;
            }
            if (value < (double) decimal.MinValue || value > (double) decimal.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (double) (decimal) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (decimal) value;
        }

        /// <summary>
        /// Converts an Int32 to a Decimal.
        /// </summary>
        /// <param name="value">An Int32.</param>
        /// <returns>A Decimal.</returns>
        public decimal ToDecimal(
            int value
        ) {
            return (decimal) value;
        }

        /// <summary>
        /// Converts an Int64 to a Decimal.
        /// </summary>
        /// <param name="value">An Int64.</param>
        /// <returns>A Decimal.</returns>
        public decimal ToDecimal(
            long value
        ) {
            return (decimal) value;
        }

        /// <summary>
        /// Converts a Decimal to a Double.
        /// </summary>
        /// <param name="value">A Decimal.</param>
        /// <returns>A Double.</returns>
        public double ToDouble(
            decimal value
        ) {
            if (value == decimal.MinValue) {
                return double.MinValue;
            } else if (value == decimal.MaxValue) {
                return double.MaxValue;
            }
            if (value != (decimal) (double) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (double) value;
        }

        /// <summary>
        /// Converts a Double to a Double.
        /// </summary>
        /// <param name="value">A Double.</param>
        /// <returns>A Double.</returns>
        public double ToDouble(
            double value
        ) {
            return value;
        }

        /// <summary>
        /// Converts a Single to a Double.
        /// </summary>
        /// <param name="value">A Single.</param>
        /// <returns>A Double.</returns>
        public double ToDouble(
            float value
        ) {
            if (value == float.MinValue) {
                return double.MinValue;
            } else if (value == float.MaxValue) {
                return double.MaxValue;
            } else if (float.IsNegativeInfinity(value)) {
                return double.NegativeInfinity;
            } else if (float.IsPositiveInfinity(value)) {
                return double.PositiveInfinity;
            } else if (float.IsNaN(value)) {
                return double.NaN;
            }
            return value;
        }

        /// <summary>
        /// Converts an Int32 to a Double.
        /// </summary>
        /// <param name="value">An Int32.</param>
        /// <returns>A Double.</returns>
        public double ToDouble(
            int value
        ) {
            return value;
        }

        /// <summary>
        /// Converts an Int64 to a Double.
        /// </summary>
        /// <param name="value">An Int64.</param>
        /// <returns>A Double.</returns>
        public double ToDouble(
            long value
        ) {
            if (value != (long) (double) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return value;
        }

        /// <summary>
        /// Converts an Int16 to a Double.
        /// </summary>
        /// <param name="value">An Int16.</param>
        /// <returns>A Double.</returns>
        public double ToDouble(
            short value
        ) {
            return value;
        }

        /// <summary>
        /// Converts a UInt32 to a Double.
        /// </summary>
        /// <param name="value">A UInt32.</param>
        /// <returns>A Double.</returns>
        public double ToDouble(
            uint value
        ) {
            return value;
        }

        /// <summary>
        /// Converts a UInt64 to a Double.
        /// </summary>
        /// <param name="value">A UInt64.</param>
        /// <returns>A Double.</returns>
        public double ToDouble(
            ulong value
        ) {
            if (value != (ulong) (double) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return value;
        }

        /// <summary>
        /// Converts a UInt16 to a Double.
        /// </summary>
        /// <param name="value">A UInt16.</param>
        /// <returns>A Double.</returns>
        public double ToDouble(
            ushort value
        ) {
            return value;
        }

        /// <summary>
        /// Converts a Double to an Int16.
        /// </summary>
        /// <param name="value">A Double.</param>
        /// <returns>An Int16.</returns>
        public short ToInt16(
            double value
        ) {
            if (value < short.MinValue || value > short.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (double) (short) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (short) value;
        }

        /// <summary>
        /// Converts an Int32 to an Int16.
        /// </summary>
        /// <param name="value">An Int32.</param>
        /// <returns>An Int16.</returns>
        public short ToInt16(
            int value
        ) {
            if (value < short.MinValue || value > short.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (short) value;
        }

        /// <summary>
        /// Converts an Int64 to an Int16.
        /// </summary>
        /// <param name="value">An Int64.</param>
        /// <returns>An Int16.</returns>
        public short ToInt16(
            long value
        ) {
            if (value < short.MinValue || value > short.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (short) value;
        }

        /// <summary>
        /// Converts a Decimal to an Int32.
        /// </summary>
        /// <param name="value">A Decimal.</param>
        /// <returns>An Int32.</returns>
        public int ToInt32(
            decimal value
        ) {
            if (value == decimal.MinValue) {
                return int.MinValue;
            } else if (value == decimal.MaxValue) {
                return int.MaxValue;
            }
            if (value < int.MinValue || value > int.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (decimal) (int) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (int) value;
        }

        /// <summary>
        /// Converts a Double to an Int32.
        /// </summary>
        /// <param name="value">A Double.</param>
        /// <returns>An Int32.</returns>
        public int ToInt32(
            double value
        ) {
            if (value < int.MinValue || value > int.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (double) (int) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (int) value;
        }

        /// <summary>
        /// Converts a Single to an Int32.
        /// </summary>
        /// <param name="value">A Single.</param>
        /// <returns>An Int32.</returns>
        public int ToInt32(
            float value
        ) {
            if (value < int.MinValue || value > int.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (float) (int) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (int) value;
        }

        /// <summary>
        /// Converts an Int32 to an Int32.
        /// </summary>
        /// <param name="value">An Int32.</param>
        /// <returns>An Int32.</returns>
        public int ToInt32(
            int value
        ) {
            return value;
        }

        /// <summary>
        /// Converts an Int64 to an Int32.
        /// </summary>
        /// <param name="value">An Int64.</param>
        /// <returns>An Int32.</returns>
        public int ToInt32(
            long value
        ) {
            if (value < int.MinValue || value > int.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (int) value;
        }

        /// <summary>
        /// Converts an Int16 to an Int32.
        /// </summary>
        /// <param name="value">An Int16.</param>
        /// <returns>An Int32.</returns>
        public int ToInt32(
            short value
        ) {
            return value;
        }

        /// <summary>
        /// Converts a UInt32 to an Int32.
        /// </summary>
        /// <param name="value">A UInt32.</param>
        /// <returns>An Int32.</returns>
        public int ToInt32(
            uint value
        ) {
            if (value > (uint) int.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (int) value;
        }

        /// <summary>
        /// Converts a UInt64 to an Int32.
        /// </summary>
        /// <param name="value">A UInt64.</param>
        /// <returns>An Int32.</returns>
        public int ToInt32(
            ulong value
        ) {
            if (value > (ulong) int.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (int) value;
        }

        /// <summary>
        /// Converts a UInt16 to an Int32.
        /// </summary>
        /// <param name="value">A UInt16.</param>
        /// <returns>An Int32.</returns>
        public int ToInt32(
            ushort value
        ) {
            return value;
        }

        /// <summary>
        /// Converts a Decimal to an Int64.
        /// </summary>
        /// <param name="value">A Decimal.</param>
        /// <returns>An Int64.</returns>
        public long ToInt64(
            decimal value
        ) {
            if (value == decimal.MinValue) {
                return long.MinValue;
            } else if (value == decimal.MaxValue) {
                return long.MaxValue;
            }
            if (value < long.MinValue || value > long.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (decimal) (long) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (long) value;
        }

        /// <summary>
        /// Converts a Double to an Int64.
        /// </summary>
        /// <param name="value">A Double.</param>
        /// <returns>An Int64.</returns>
        public long ToInt64(
            double value
        ) {
            if (value < long.MinValue || value > long.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (double) (long) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (long) value;
        }

        /// <summary>
        /// Converts a Single to an Int64.
        /// </summary>
        /// <param name="value">A Single.</param>
        /// <returns>An Int64.</returns>
        public long ToInt64(
            float value
        ) {
            if (value < long.MinValue || value > long.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (float) (long) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (long) value;
        }

        /// <summary>
        /// Converts an Int32 to an Int64.
        /// </summary>
        /// <param name="value">An Int32.</param>
        /// <returns>An Int64.</returns>
        public long ToInt64(
            int value
        ) {
            return value;
        }

        /// <summary>
        /// Converts an Int64 to an Int64.
        /// </summary>
        /// <param name="value">An Int64.</param>
        /// <returns>An Int64.</returns>
        public long ToInt64(
            long value
        ) {
            return value;
        }

        /// <summary>
        /// Converts an Int16 to an Int64.
        /// </summary>
        /// <param name="value">An Int16.</param>
        /// <returns>An Int64.</returns>
        public long ToInt64(
            short value
        ) {
            return value;
        }

        /// <summary>
        /// Converts a UInt32 to an Int64.
        /// </summary>
        /// <param name="value">A UInt32.</param>
        /// <returns>An Int64.</returns>
        public long ToInt64(
            uint value
        ) {
            return (long) value;
        }

        /// <summary>
        /// Converts a UInt64 to an Int64.
        /// </summary>
        /// <param name="value">A UInt64.</param>
        /// <returns>An Int64.</returns>
        public long ToInt64(
            ulong value
        ) {
            if (value > (ulong) long.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (long) value;
        }

        /// <summary>
        /// Converts a UInt16 to an Int64.
        /// </summary>
        /// <param name="value">A UInt16.</param>
        /// <returns>An Int64.</returns>
        public long ToInt64(
            ushort value
        ) {
            return value;
        }

        /// <summary>
        /// Converts a Double to a Single.
        /// </summary>
        /// <param name="value">A Double.</param>
        /// <returns>A Single.</returns>
        public float ToSingle(
            double value
        ) {
            if (value == double.MinValue) {
                return float.MinValue;
            } else if (value == double.MaxValue) {
                return float.MaxValue;
            } else if (double.IsNegativeInfinity(value)) {
                return float.NegativeInfinity;
            } else if (double.IsPositiveInfinity(value)) {
                return float.PositiveInfinity;
            } else if (double.IsNaN(value)) {
                return float.NaN;
            }

            if (value < float.MinValue || value > float.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (double) (float) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (float) value;
        }

        /// <summary>
        /// Converts an Int32 to a Single.
        /// </summary>
        /// <param name="value">An Int32.</param>
        /// <returns>A Single.</returns>
        public float ToSingle(
            int value
        ) {
            if (value != (int) (float) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return value;
        }

        /// <summary>
        /// Converts an Int64 to a Single.
        /// </summary>
        /// <param name="value">An Int64.</param>
        /// <returns>A Single.</returns>
        public float ToSingle(
            long value
        ) {
            if (value != (long) (float) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return value;
        }

        /// <summary>
        /// Converts a Double to a UInt16.
        /// </summary>
        /// <param name="value">A Double.</param>
        /// <returns>A UInt16.</returns>
        public ushort ToUInt16(
            double value
        ) {
            if (value < ushort.MinValue || value > ushort.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (double) (ushort) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (ushort) value;
        }

        /// <summary>
        /// Converts an Int32 to a UInt16.
        /// </summary>
        /// <param name="value">An Int32.</param>
        /// <returns>A UInt16.</returns>
        public ushort ToUInt16(
            int value
        ) {
            if (value < ushort.MinValue || value > ushort.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (ushort) value;
        }

        /// <summary>
        /// Converts an Int64 to a UInt16.
        /// </summary>
        /// <param name="value">An Int64.</param>
        /// <returns>A UInt16.</returns>
        public ushort ToUInt16(
            long value
        ) {
            if (value < ushort.MinValue || value > ushort.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (ushort) value;
        }

        /// <summary>
        /// Converts a Double to a UInt32.
        /// </summary>
        /// <param name="value">A Double.</param>
        /// <returns>A UInt32.</returns>
        public uint ToUInt32(
            double value
        ) {
            if (value < uint.MinValue || value > uint.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (double) (uint) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (uint) value;
        }

        /// <summary>
        /// Converts an Int32 to a UInt32.
        /// </summary>
        /// <param name="value">An Int32.</param>
        /// <returns>A UInt32.</returns>
        public uint ToUInt32(
            int value
        ) {
            if (value < uint.MinValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (uint) value;
        }

        /// <summary>
        /// Converts an Int64 to a UInt32.
        /// </summary>
        /// <param name="value">An Int64.</param>
        /// <returns>A UInt32.</returns>
        public uint ToUInt32(
            long value
        ) {
            if (value < uint.MinValue || value > uint.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (uint) value;
        }

        /// <summary>
        /// Converts a Double to a UInt64.
        /// </summary>
        /// <param name="value">A Double.</param>
        /// <returns>A UInt64.</returns>
        public ulong ToUInt64(
            double value
        ) {
            if (value < ulong.MinValue || value > ulong.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            } else if (value != (double) (ulong) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return (ulong) value;
        }

        /// <summary>
        /// Converts an Int32 to a UInt64.
        /// </summary>
        /// <param name="value">An Int32.</param>
        /// <returns>A UInt64.</returns>
        public ulong ToUInt64(
            int value
        ) {
            if (value < (int) ulong.MinValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (ulong) value;
        }

        /// <summary>
        /// Converts an Int64 to a UInt64.
        /// </summary>
        /// <param name="value">An Int64.</param>
        /// <returns>A UInt64.</returns>
        public ulong ToUInt64(
            long value
        ) {
            if (value < (int) ulong.MinValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (ulong) value;
        }
        #endregion
    }
}

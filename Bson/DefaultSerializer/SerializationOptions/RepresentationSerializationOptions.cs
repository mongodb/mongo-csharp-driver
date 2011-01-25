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
    public class RepresentationSerializationOptions : IBsonSerializationOptions {
        #region private fields
        private BsonType representation;
        private bool allowOverflow;
        private bool allowTruncation;
        #endregion

        #region constructors
        public RepresentationSerializationOptions(
            BsonType representation
        ) {
            this.representation = representation;
        }

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
        public BsonType Representation {
            get { return representation; }
        }

        public bool AllowOverflow {
            get { return allowOverflow; }
        }

        public bool AllowTruncation {
            get { return allowTruncation; }
        }
        #endregion

        #region public methods
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

        public decimal ToDecimal(
            int value
        ) {
            return (decimal) value;
        }

        public decimal ToDecimal(
            long value
        ) {
            return (decimal) value;
        }

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

        public double ToDouble(
            double value
        ) {
            return value;
        }

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

        public double ToDouble(
            int value
        ) {
            return value;
        }

        public double ToDouble(
            long value
        ) {
            if (value != (long) (double) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return value;
        }

        public double ToDouble(
            short value
        ) {
            return value;
        }

        public double ToDouble(
            uint value
        ) {
            return value;
        }

        public double ToDouble(
            ulong value
        ) {
            if (value != (ulong) (double) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return value;
        }

        public double ToDouble(
            ushort value
        ) {
            return value;
        }

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

        public short ToInt16(
            int value
        ) {
            if (value < short.MinValue || value > short.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (short) value;
        }

        public short ToInt16(
            long value
        ) {
            if (value < short.MinValue || value > short.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (short) value;
        }

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

        public int ToInt32(
            int value
        ) {
            return value;
        }

        public int ToInt32(
            long value
        ) {
            if (value < int.MinValue || value > int.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (int) value;
        }

        public int ToInt32(
            short value
        ) {
            return value;
        }

        public int ToInt32(
            uint value
        ) {
            if (value > (uint) int.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (int) value;
        }

        public int ToInt32(
            ulong value
        ) {
            if (value > (ulong) int.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (int) value;
        }

        public int ToInt32(
            ushort value
        ) {
            return value;
        }

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

        public long ToInt64(
            int value
        ) {
            return value;
        }

        public long ToInt64(
            long value
        ) {
            return value;
        }

        public long ToInt64(
            short value
        ) {
            return value;
        }

        public long ToInt64(
            uint value
        ) {
            return (long) value;
        }

        public long ToInt64(
            ulong value
        ) {
            if (value > (ulong) long.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (long) value;
        }

        public long ToInt64(
            ushort value
        ) {
            return value;
        }

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

        public float ToSingle(
            int value
        ) {
            if (value != (int) (float) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return value;
        }

        public float ToSingle(
            long value
        ) {
            if (value != (long) (float) value) {
                if (!allowTruncation) { throw new TruncationException(); }
            }
            return value;
        }

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

        public ushort ToUInt16(
            int value
        ) {
            if (value < ushort.MinValue || value > ushort.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (ushort) value;
        }

        public ushort ToUInt16(
            long value
        ) {
            if (value < ushort.MinValue || value > ushort.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (ushort) value;
        }

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

        public uint ToUInt32(
            int value
        ) {
            if (value < uint.MinValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (uint) value;
        }

        public uint ToUInt32(
            long value
        ) {
            if (value < uint.MinValue || value > uint.MaxValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (uint) value;
        }

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

        public ulong ToUInt64(
            int value
        ) {
            if (value < (int) ulong.MinValue) {
                if (!allowOverflow) { throw new OverflowException(); }
            }
            return (ulong) value;
        }

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

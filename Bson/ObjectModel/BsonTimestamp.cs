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
using System.Xml;

namespace MongoDB.Bson {
    [Serializable]
    public class BsonTimestamp : BsonValue, IComparable<BsonTimestamp>, IEquatable<BsonTimestamp> {
        #region private fields
        private long value;
        #endregion

        #region constructors
        public BsonTimestamp(
            long value
        )
            : base(BsonType.Timestamp) {
            this.value = value;
        }

        public BsonTimestamp(
            int timestamp,
            int increment
        )
            : base(BsonType.Timestamp) {
            this.value = ((long) timestamp << 32) + increment;
        }
        #endregion

        #region public properties
        public override object RawValue {
            get { return value; }
        }

        public long Value {
            get { return value; }
        }

        public int Increment {
            get { return (int) value; }
        }

        public int Timestamp {
            get { return (int) (value >> 32); }
        }
        #endregion

        #region public static methods
        public static BsonTimestamp Create(
            long value
        ) {
            return new BsonTimestamp(value);
        }

        public static BsonTimestamp Create(
            int timestamp,
            int increment
        ) {
            return new BsonTimestamp(timestamp, increment);
        }

        public new static BsonTimestamp Create(
            object value
        ) {
            if (value != null) {
                return (BsonTimestamp) BsonTypeMapper.MapToBsonValue(value, BsonType.Timestamp);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        public int CompareTo(
            BsonTimestamp other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.value);
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherTimestamp = other as BsonTimestamp;
            if (otherTimestamp != null) {
                return value.CompareTo(otherTimestamp.value);
            }
            var otherDateTime = other as BsonDateTime;
            if (otherDateTime != null) {
                var seconds = (int) (otherDateTime.Value - BsonConstants.UnixEpoch).TotalSeconds;
                var otherTimestampValue = ((long) seconds) << 32;
                return value.CompareTo(otherTimestampValue);
            }
            return CompareTypeTo(other);
        }

        public bool Equals(
            BsonTimestamp rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonTimestamp); // works even if obj is null
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + value.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return XmlConvert.ToString(value);
        }
        #endregion
    }
}

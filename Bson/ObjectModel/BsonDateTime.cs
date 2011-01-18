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
    public class BsonDateTime : BsonValue, IComparable<BsonDateTime>, IEquatable<BsonDateTime> {
        #region private fields
        private DateTime value;
        #endregion

        #region constructors
        public BsonDateTime(
            DateTime value
        )
            : base(BsonType.DateTime) {
            this.value = value.ToUniversalTime();
        }
        #endregion

        #region public properties
        public override object RawValue {
            get { return value; }
        }

        public DateTime Value {
            get { return value; }
        }
        #endregion

        #region public operators
        public static implicit operator BsonDateTime(
            DateTime value
        ) {
            return new BsonDateTime(value);
        }
        #endregion

        #region public static methods
        public static BsonDateTime Create(
            DateTime value
        ) {
            return new BsonDateTime(value);
        }

        public new static BsonDateTime Create(
            object value
        ) {
            if (value != null) {
                return (BsonDateTime) BsonTypeMapper.MapToBsonValue(value, BsonType.DateTime);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        public int CompareTo(
            BsonDateTime other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.value);
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherDateTime = other as BsonDateTime;
            if (otherDateTime != null) {
                return value.CompareTo(otherDateTime.value);
            }
            var otherTimestamp = other as BsonTimestamp;
            if (otherTimestamp != null) {
                return value.CompareTo(BsonConstants.UnixEpoch.AddSeconds(otherTimestamp.Timestamp));
            }
            return CompareTypeTo(other);
        }

        public bool Equals(
            BsonDateTime rhs
        ) {
            if (rhs == null) { return false; }
            return this.value == rhs.value;
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonDateTime); // works even if obj is null
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + value.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind);
        }
        #endregion
    }
}

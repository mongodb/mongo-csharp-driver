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

namespace MongoDB.Bson {
    [Serializable]
    public class BsonBinaryData : BsonValue, IComparable<BsonBinaryData>, IEquatable<BsonBinaryData> {
        #region private fields
        private byte[] bytes;
        private BsonBinarySubType subType;
        #endregion

        #region constructors
        public BsonBinaryData(
            byte[] bytes
        )
            : base(BsonType.Binary) {
            this.bytes = bytes;
            this.subType = BsonBinarySubType.Binary;
        }

        public BsonBinaryData(
            byte[] bytes,
            BsonBinarySubType subType
        )
            : base(BsonType.Binary) {
            this.bytes = bytes;
            this.subType = subType;
        }

        public BsonBinaryData(
            Guid guid
        )
            : base(BsonType.Binary) {
            this.bytes = guid.ToByteArray();
            this.subType = BsonBinarySubType.Uuid;
        }
        #endregion

        #region public properties
        public byte[] Bytes {
            get { return bytes; }
        }

        public override object RawValue {
            get {
                if (bytes.Length == 16 && subType == BsonBinarySubType.Uuid) {
                    return new Guid(bytes);
                } else {
                    return null;
                }
            }
        }

        public BsonBinarySubType SubType {
            get { return subType; }
        }
        #endregion

        #region public operators
        public static implicit operator BsonBinaryData(
            byte[] value
        ) {
            return BsonBinaryData.Create(value);
        }

        public static implicit operator BsonBinaryData(
            Guid value
        ) {
            return BsonBinaryData.Create(value);
        }
        #endregion

        #region public static methods
        public static BsonBinaryData Create(
            byte[] bytes
        ) {
            return Create(bytes, BsonBinarySubType.Binary);
        }

        public static BsonBinaryData Create(
            byte[] bytes,
            BsonBinarySubType subType
        ) {
            if (bytes != null) {
                return new BsonBinaryData(bytes, subType);
            } else {
                return null;
            }
        }

        public static BsonBinaryData Create(
            Guid guid
        ) {
            return new BsonBinaryData(guid);
        }

        public new static BsonBinaryData Create(
            object value
        ) {
            if (value != null) {
                return (BsonBinaryData) BsonTypeMapper.MapToBsonValue(value, BsonType.Binary);
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        public int CompareTo(
            BsonBinaryData other
        ) {
            if (other == null) { return 1; }
            int r = subType.CompareTo(other.subType);
            if (r != 0) { return r; }
            for (int i = 0; i < bytes.Length && i < other.bytes.Length; i++) {
                r = bytes[i].CompareTo(other.bytes[i]);
                if (r != 0) { return r; }
            }
            return bytes.Length.CompareTo(other.bytes.Length);
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherBinaryData = other as BsonBinaryData;
            if (otherBinaryData != null) {
                return CompareTo(otherBinaryData);
            }
            return CompareTypeTo(other);
        }

        public bool Equals(
            BsonBinaryData rhs
        ) {
            if (rhs == null) { return false; }
            return object.ReferenceEquals(this, rhs) || this.subType == rhs.subType && this.bytes.SequenceEqual(rhs.bytes);
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonBinaryData); // works even if obj is null
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            foreach (byte b in bytes) {
                hash = 37 * hash + b;
            }
            hash = 37 * hash + subType.GetHashCode();
            return hash;
        }

        public Guid ToGuid() {
            if (subType == BsonBinarySubType.Uuid) {
                return new Guid(bytes);
            } else {
                throw new InvalidOperationException("BinaryData subtype is not UUID");
            }
        }

        public override string ToString() {
            return string.Format("{0}:0x{1}", subType, BsonUtils.ToHexString(bytes));
        }
        #endregion
    }
}

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
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MongoDB.Bson {
    [Serializable]
    public class BsonObjectId : BsonValue, IComparable<BsonObjectId>, IEquatable<BsonObjectId> {
        #region private static fields
        private static BsonObjectId emptyInstance = new BsonObjectId(ObjectId.Empty);
        #endregion

        #region private fields
        private ObjectId value;
        #endregion

        #region constructors
        public BsonObjectId(
            ObjectId value
        )
            : base(BsonType.ObjectId) {
            this.value = value;
        }

        public BsonObjectId(
            byte[] value
        )
            : base(BsonType.ObjectId) {
            this.value = new ObjectId(value);
        }

        public BsonObjectId(
            int timestamp,
            int machine,
            short pid,
            int increment
        )
            : base(BsonType.ObjectId) {
            this.value = new ObjectId(timestamp, machine, pid, increment);
        }

        public BsonObjectId(
            string value
        )
            : base(BsonType.ObjectId) {
            this.value = new ObjectId(value);
        }
        #endregion

        #region public static properties
        public static BsonObjectId Empty {
            get { return emptyInstance; }
        }
        #endregion

        #region public properties
        public int Timestamp {
            get { return value.Timestamp; }
        }

        public int Machine {
            get { return value.Machine; }
        }

        public short Pid {
            get { return value.Pid; }
        }

        public int Increment {
            get { return value.Increment; }
        }

        // a more or less accurate creation time derived from Timestamp
        public DateTime CreationTime {
            get { return value.CreationTime; }
        }

        public override object RawValue {
            get { return value; }
        }

        public ObjectId Value {
            get { return value; }
        }
        #endregion

        #region public operators
        public static implicit operator BsonObjectId(
            ObjectId value
        ) {
            return new BsonObjectId(value);
        }
        #endregion

        #region public static methods
        public static BsonObjectId Create(
            ObjectId value
        ) {
            return new BsonObjectId(value);
        }

        public static BsonObjectId Create(
            byte[] value
        ) {
            if (value != null) {
                return new BsonObjectId(value);
            } else {
                return null;
            }
        }

        public static BsonObjectId Create(
            int timestamp,
            int machine,
            short pid,
            int increment
        ) {
            return new BsonObjectId(timestamp, machine, pid, increment);
        }

        public new static BsonObjectId Create(
            object value
        ) {
            if (value != null) {
                return (BsonObjectId) BsonTypeMapper.MapToBsonValue(value, BsonType.ObjectId);
            } else {
                return null;
            }
        }

        public static BsonObjectId Create(
            string value
        ) {
            if (value != null) {
                return new BsonObjectId(value);
            } else {
                return null;
            }
        }

        public static BsonObjectId GenerateNewId() {
            return new BsonObjectId(ObjectId.GenerateNewId());
        }

        public static BsonObjectId Parse(
            string s
        ) {
            return new BsonObjectId(ObjectId.Parse(s));
        }

        public static bool TryParse(
            string s,
            out BsonObjectId value
        ) {
            ObjectId objectId;
            if (ObjectId.TryParse(s, out objectId)) {
                value = new BsonObjectId(objectId);
                return true;
            } else {
                value = null;
                return false;
            }
        }
        #endregion

        #region public methods
        public int CompareTo(
            BsonObjectId other
        ) {
            if (other == null) { return 1; }
            return value.CompareTo(other.Value);
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherObjectId = other as BsonObjectId;
            if (otherObjectId != null) {
                return value.CompareTo(otherObjectId.Value);
            }
            return CompareTypeTo(other);
        }

        public bool Equals(
            BsonObjectId rhs
        ) {
            if (rhs == null) { return false; }
            return this.Value == rhs.Value;
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonObjectId); // works even if obj is null
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            hash = 37 * hash + value.GetHashCode();
            return hash;
        }

        public byte[] ToByteArray() {
            return value.ToByteArray();
        }

        public override string ToString() {
            return value.ToString();
        }
        #endregion
    }
}

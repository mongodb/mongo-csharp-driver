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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;

namespace MongoDB.Bson {
    [Serializable]
    public class BsonArray : BsonValue, IComparable<BsonArray>, IEquatable<BsonArray>, IList<BsonValue> {
        #region private fields
        private List<BsonValue> values = new List<BsonValue>();
        #endregion

        #region constructors
        public BsonArray()
            : base(BsonType.Array) {
        }

        public BsonArray(
            IEnumerable<bool> values
        )
            : base(BsonType.Array) {
            AddRange(values);
        }

        public BsonArray(
            IEnumerable<BsonValue> values
        )
            : base(BsonType.Array) {
            AddRange(values);
        }

        public BsonArray(
            IEnumerable<DateTime> values
        )
            : base(BsonType.Array) {
            AddRange(values);
        }

        public BsonArray(
            IEnumerable<double> values
        )
            : base(BsonType.Array) {
            AddRange(values);
        }

        public BsonArray(
            IEnumerable<int> values
        )
            : base(BsonType.Array) {
            AddRange(values);
        }

        public BsonArray(
            IEnumerable<long> values
        )
            : base(BsonType.Array) {
            AddRange(values);
        }

        public BsonArray(
            IEnumerable<object> values
        )
            : base(BsonType.Array) {
            AddRange(values);
        }

        public BsonArray(
            IEnumerable<string> values
        )
            : base(BsonType.Array) {
            AddRange(values);
        }
        #endregion

        #region public properties
        public int Count {
            get { return values.Count; }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public IEnumerable<object> RawValues {
            get { return values.Select(v => v.RawValue); }
        }

        public IEnumerable<BsonValue> Values {
            get { return values; }
        }
        #endregion

        #region public indexers
        public BsonValue this[
            int index
        ] {
            get { return values[index]; }
            set { values[index] = value; }
        }
        #endregion

        #region public static methods
        public static BsonArray Create(
            IEnumerable<bool> values
        ) {
            if (values != null) {
                return new BsonArray(values);
            } else {
                return null;
            }
        }

        public static BsonArray Create(
            IEnumerable<BsonValue> values
        ) {
            if (values != null) {
                return new BsonArray(values);
            } else {
                return null;
            }
        }

        public static BsonArray Create(
            IEnumerable<DateTime> values
        ) {
            if (values != null) {
                return new BsonArray(values);
            } else {
                return null;
            }
        }

        public static BsonArray Create(
            IEnumerable<double> values
        ) {
            if (values != null) {
                return new BsonArray(values);
            } else {
                return null;
            }
        }

        public static BsonArray Create(
            IEnumerable<int> values
        ) {
            if (values != null) {
                return new BsonArray(values);
            } else {
                return null;
            }
        }

        public static BsonArray Create(
            IEnumerable<long> values
        ) {
            if (values != null) {
                return new BsonArray(values);
            } else {
                return null;
            }
        }

        public static BsonArray Create(
           IEnumerable<object> values
       ) {
            if (values != null) {
                return new BsonArray(values);
            } else {
                return null;
            }
        }

        public static BsonArray Create(
            IEnumerable<string> values
        ) {
            if (values != null) {
                return new BsonArray(values);
            } else {
                return null;
            }
        }

        public new static BsonArray Create(
            object value
        ) {
            if (value != null) {
                return (BsonArray) BsonTypeMapper.MapToBsonValue(value, BsonType.Array);
            } else {
                return null;
            }
        }

        public static new BsonArray ReadFrom(
            BsonReader bsonReader
        ) {
            var array = new BsonArray();
            bsonReader.ReadStartArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                var value = BsonValue.ReadFrom(bsonReader);
                array.Add(value);
            }
            bsonReader.ReadEndArray();
            return array;
        }
        #endregion

        #region public methods
        public BsonArray Add(
            BsonValue value
        ) {
            if (value != null) {
                values.Add(value);
            }
            return this;
        }

        public BsonArray AddRange(
            IEnumerable<bool> values
        ) {
            if (values != null) {
                foreach (var value in values) {
                    this.values.Add(BsonBoolean.Create(value));
                }
            }
            return this;
        }

        public BsonArray AddRange(
            IEnumerable<BsonValue> values
        ) {
            if (values != null) {
                this.values.AddRange(values);
            }
            return this;
        }

        public BsonArray AddRange(
            IEnumerable<DateTime> values
        ) {
            if (values != null) {
                foreach (var value in values) {
                    this.values.Add(BsonDateTime.Create(value));
                }
            }
            return this;
        }

        public BsonArray AddRange(
            IEnumerable<double> values
        ) {
            if (values != null) {
                foreach (var value in values) {
                    this.values.Add(BsonDouble.Create(value));
                }
            }
            return this;
        }

        public BsonArray AddRange(
            IEnumerable<int> values
        ) {
            if (values != null) {
                foreach (var value in values) {
                    this.values.Add(BsonInt32.Create(value));
                }
            }
            return this;
        }

        public BsonArray AddRange(
            IEnumerable<long> values
        ) {
            if (values != null) {
                foreach (var value in values) {
                    this.values.Add(BsonInt64.Create(value));
                }
            }
            return this;
        }

        public BsonArray AddRange(
            IEnumerable<object> values
        ) {
            if (values != null) {
                foreach (var value in values) {
                    this.values.Add(BsonValue.Create(value));
                }
            }
            return this;
        }

        public BsonArray AddRange(
            IEnumerable<string> values
        ) {
            if (values != null) {
                foreach (var value in values) {
                    this.values.Add(BsonString.Create(value));
                }
            }
            return this;
        }

        public override BsonValue Clone() {
            BsonArray clone = new BsonArray();
            foreach (var value in values) {
                clone.Add(value.Clone());
            }
            return clone;
        }

        public void Clear() {
            values.Clear();
        }

        public int CompareTo(
            BsonArray other
        ) {
            if (other == null) { return 1; }
            for (int i = 0; i < values.Count && i < other.values.Count; i++) {
                int r = values[i].CompareTo(other.values[i]);
                if (r != 0) { return r; }
            }
            return values.Count.CompareTo(other.values.Count);
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            var otherArray = other as BsonArray;
            if (otherArray != null) {
                return CompareTo(otherArray);
            }
            return CompareTypeTo(other);
        }

        public bool Contains(
            BsonValue value
        ) {
            return values.Contains(value);
        }

        public void CopyTo(
            BsonValue[] array,
            int arrayIndex
        ) {
            for (int i = 0, j = arrayIndex; i < values.Count; i++, j++) {
                array[j] = values[i];
            }
        }

        public void CopyTo(
            object[] array,
            int arrayIndex
        ) {
            for (int i = 0, j = arrayIndex; i < values.Count; i++, j++) {
                array[j] = values[i].RawValue;
            }
        }

        public override BsonValue DeepClone() {
            BsonArray clone = new BsonArray();
            foreach (var value in values) {
                clone.Add(value.DeepClone());
            }
            return clone;
        }

        public bool Equals(
            BsonArray rhs
        ) {
            if (rhs == null) { return false; }
            return object.ReferenceEquals(this, rhs) || this.values.SequenceEqual(rhs.values);
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonArray); // works even if obj is null
        }

        public IEnumerator<BsonValue> GetEnumerator() {
            return values.GetEnumerator();
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + bsonType.GetHashCode();
            foreach (var value in values) {
                hash = 37 * hash + value.GetHashCode();
            }
            return hash;
        }

        public int IndexOf(
            BsonValue value
        ) {
            return values.IndexOf(value);
        }

        public int IndexOf(
            BsonValue value,
            int index
        ) {
            return values.IndexOf(value, index);
        }

        public int IndexOf(
            BsonValue value,
            int index,
            int count
        ) {
            return values.IndexOf(value, index, count);
        }

        public void Insert(
            int index,
            BsonValue value
        ) {
            values.Insert(index, value);
        }

        public bool Remove(
            BsonValue value
        ) {
            return values.Remove(value);
        }

        public void RemoveAt(
            int index
        ) {
            values.RemoveAt(index);
        }

        public BsonValue[] ToArray() {
            return values.ToArray();
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < values.Count; i++) {
                if (i > 0) { sb.Append(", "); }
                sb.Append(values[i].ToString());
            }
            sb.Append("]");
            return sb.ToString();
        }

        public new void WriteTo(
            BsonWriter bsonWriter
        ) {
            bsonWriter.WriteStartArray();
            for (int i = 0; i < values.Count; i++) {
                values[i].WriteTo(bsonWriter);
            }
            bsonWriter.WriteEndArray();
        }
        #endregion

        #region private methods
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        #endregion

        #region explicit interface implementations
        // our version of Add returns BsonArray
        void ICollection<BsonValue>.Add(
            BsonValue value
        ) {
            Add(value);
        }
        #endregion
    }
}

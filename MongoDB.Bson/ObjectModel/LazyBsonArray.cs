/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents a BSON array that is deserialized lazily.
    /// </summary>
    [Serializable]
    [BsonSerializer(typeof(LazyBsonArraySerializer))]
    public class LazyBsonArray : BsonArray, IDisposable
    {
        // private fields
        private bool _disposed;
        private IByteBuffer _slice;
        private List<IDisposable> _disposableItems = new List<IDisposable>();
        private BsonBinaryReaderSettings _readerSettings = BsonBinaryReaderSettings.Defaults;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LazyBsonArray"/> class.
        /// </summary>
        /// <param name="slice">The slice.</param>
        /// <exception cref="System.ArgumentNullException">slice</exception>
        /// <exception cref="System.ArgumentException">LazyBsonArray cannot be used with an IByteBuffer that needs disposing.</exception>
        public LazyBsonArray(IByteBuffer slice)
        {
            if (slice == null)
            {
                throw new ArgumentNullException("slice");
            }

            _slice = slice;
        }

        // public properties
        /// <summary>
        /// Gets or sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        public override int Capacity
        {
            get
            {
                EnsureDataIsAccessible();
                return base.Capacity;
            }
            set
            {
                EnsureDataIsAccessible();
                base.Capacity = value;
            }
        }

        /// <summary>
        /// Gets the count of array elements.
        /// </summary>
        public override int Count
        {
            get
            {
                EnsureDataIsAccessible();
                return base.Count;
            }
        }

        /// <summary>
        /// Gets the array elements as raw values (see BsonValue.RawValue).
        /// </summary>
        [Obsolete("Use ToArray to ToList instead.")]
        public override IEnumerable<object> RawValues
        {
            get
            {
                EnsureDataIsAccessible();
                return base.RawValues;
            }
        }

        /// <summary>
        /// Gets the slice.
        /// </summary>
        /// <value>
        /// The slice.
        /// </value>
        public IByteBuffer Slice
        {
            get { return _slice; }
        }

        /// <summary>
        /// Gets the array elements.
        /// </summary>
        public override IEnumerable<BsonValue> Values
        {
            get
            {
                EnsureDataIsAccessible();
                return base.Values;
            }
        }

        // public indexers
        /// <summary>
        /// Gets or sets a value by position.
        /// </summary>
        /// <param name="index">The position.</param>
        /// <returns>The value.</returns>
        public override BsonValue this[int index]
        {
            get
            {
                EnsureDataIsAccessible();
                return base[index];
            }
            set
            {
                EnsureDataIsAccessible();
                base[index] = value;
            }
        }

        // public methods
        /// <summary>
        /// Adds an element to the array.
        /// </summary>
        /// <param name="value">The value to add to the array.</param>
        /// <returns>The array (so method calls can be chained).</returns>
        public override BsonArray Add(BsonValue value)
        {
            EnsureDataIsAccessible();
            return base.Add(value);
        }

        /// <summary>
        /// Adds multiple elements to the array.
        /// </summary>
        /// <param name="values">A list of values to add to the array.</param>
        /// <returns>The array (so method calls can be chained).</returns>
        public override BsonArray AddRange(IEnumerable<bool> values)
        {
            EnsureDataIsAccessible();
            return base.AddRange(values);
        }

        /// <summary>
        /// Adds multiple elements to the array.
        /// </summary>
        /// <param name="values">A list of values to add to the array.</param>
        /// <returns>The array (so method calls can be chained).</returns>
        public override BsonArray AddRange(IEnumerable<BsonValue> values)
        {
            EnsureDataIsAccessible();
            return base.AddRange(values);
        }

        /// <summary>
        /// Adds multiple elements to the array.
        /// </summary>
        /// <param name="values">A list of values to add to the array.</param>
        /// <returns>The array (so method calls can be chained).</returns>
        public override BsonArray AddRange(IEnumerable<DateTime> values)
        {
            EnsureDataIsAccessible();
            return base.AddRange(values);
        }

        /// <summary>
        /// Adds multiple elements to the array.
        /// </summary>
        /// <param name="values">A list of values to add to the array.</param>
        /// <returns>The array (so method calls can be chained).</returns>
        public override BsonArray AddRange(IEnumerable<double> values)
        {
            EnsureDataIsAccessible();
            return base.AddRange(values);
        }

        /// <summary>
        /// Adds multiple elements to the array.
        /// </summary>
        /// <param name="values">A list of values to add to the array.</param>
        /// <returns>The array (so method calls can be chained).</returns>
        public override BsonArray AddRange(IEnumerable<int> values)
        {
            EnsureDataIsAccessible();
            return base.AddRange(values);
        }

        /// <summary>
        /// Adds multiple elements to the array.
        /// </summary>
        /// <param name="values">A list of values to add to the array.</param>
        /// <returns>The array (so method calls can be chained).</returns>
        public override BsonArray AddRange(IEnumerable<long> values)
        {
            EnsureDataIsAccessible();
            return base.AddRange(values);
        }

        /// <summary>
        /// Adds multiple elements to the array.
        /// </summary>
        /// <param name="values">A list of values to add to the array.</param>
        /// <returns>The array (so method calls can be chained).</returns>
        public override BsonArray AddRange(IEnumerable<ObjectId> values)
        {
            EnsureDataIsAccessible();
            return base.AddRange(values);
        }

        /// <summary>
        /// Adds multiple elements to the array.
        /// </summary>
        /// <param name="values">A list of values to add to the array.</param>
        /// <returns>The array (so method calls can be chained).</returns>
        public override BsonArray AddRange(IEnumerable<string> values)
        {
            EnsureDataIsAccessible();
            return base.AddRange(values);
        }

        /// <summary>
        /// Adds multiple elements to the array.
        /// </summary>
        /// <param name="values">A list of values to add to the array.</param>
        /// <returns>The array (so method calls can be chained).</returns>
        public override BsonArray AddRange(IEnumerable values)
        {
            EnsureDataIsAccessible();
            return base.AddRange(values);
        }

        /// <summary>
        /// Creates a shallow clone of the array (see also DeepClone).
        /// </summary>
        /// <returns>A shallow clone of the array.</returns>
        public override BsonValue Clone()
        {
            if (_slice != null)
            {
                return new LazyBsonArray(CloneSlice());
            }
            else
            {
                return base.Clone();
            }
        }

        /// <summary>
        /// Clears the array.
        /// </summary>
        public override void Clear()
        {
            EnsureDataIsAccessible();
            base.Clear();
        }

        /// <summary>
        /// Compares the array to another array.
        /// </summary>
        /// <param name="other">The other array.</param>
        /// <returns>A 32-bit signed integer that indicates whether this array is less than, equal to, or greather than the other.</returns>
        public override int CompareTo(BsonArray other)
        {
            EnsureDataIsAccessible();
            return base.CompareTo(other);
        }

        /// <summary>
        /// Compares the array to another BsonValue.
        /// </summary>
        /// <param name="other">The other BsonValue.</param>
        /// <returns>A 32-bit signed integer that indicates whether this array is less than, equal to, or greather than the other BsonValue.</returns>
        public override int CompareTo(BsonValue other)
        {
            EnsureDataIsAccessible();
            return base.CompareTo(other);
        }

        /// <summary>
        /// Tests whether the array contains a value.
        /// </summary>
        /// <param name="value">The value to test for.</param>
        /// <returns>True if the array contains the value.</returns>
        public override bool Contains(BsonValue value)
        {
            EnsureDataIsAccessible();
            return base.Contains(value);
        }

        /// <summary>
        /// Copies elements from this array to another array.
        /// </summary>
        /// <param name="array">The other array.</param>
        /// <param name="arrayIndex">The zero based index of the other array at which to start copying.</param>
        public override void CopyTo(BsonValue[] array, int arrayIndex)
        {
            EnsureDataIsAccessible();
            base.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies elements from this array to another array as raw values (see BsonValue.RawValue).
        /// </summary>
        /// <param name="array">The other array.</param>
        /// <param name="arrayIndex">The zero based index of the other array at which to start copying.</param>
        [Obsolete("Use ToArray or ToList instead.")]
        public override void CopyTo(object[] array, int arrayIndex)
        {
            EnsureDataIsAccessible();
            base.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Creates a deep clone of the array (see also Clone).
        /// </summary>
        /// <returns>A deep clone of the array.</returns>
        public override BsonValue DeepClone()
        {
            if (_slice != null)
            {
                return new LazyBsonArray(CloneSlice());
            }
            else
            {
                return base.Clone();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets an enumerator that can enumerate the elements of the array.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public override IEnumerator<BsonValue> GetEnumerator()
        {
            EnsureDataIsAccessible();
            return base.GetEnumerator();
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            EnsureDataIsAccessible();
            return base.GetHashCode();
        }

        /// <summary>
        /// Gets the index of a value in the array.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <returns>The zero based index of the value (or -1 if not found).</returns>
        public override int IndexOf(BsonValue value)
        {
            EnsureDataIsAccessible();
            return base.IndexOf(value);
        }

        /// <summary>
        /// Gets the index of a value in the array.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <param name="index">The zero based index at which to start the search.</param>
        /// <returns>The zero based index of the value (or -1 if not found).</returns>
        public override int IndexOf(BsonValue value, int index)
        {
            EnsureDataIsAccessible();
            return base.IndexOf(value, index);
        }

        /// <summary>
        /// Gets the index of a value in the array.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <param name="index">The zero based index at which to start the search.</param>
        /// <param name="count">The number of elements to search.</param>
        /// <returns>The zero based index of the value (or -1 if not found).</returns>
        public override int IndexOf(BsonValue value, int index, int count)
        {
            EnsureDataIsAccessible();
            return base.IndexOf(value, index, count);
        }

        /// <summary>
        /// Inserts a new value into the array.
        /// </summary>
        /// <param name="index">The zero based index at which to insert the new value.</param>
        /// <param name="value">The new value.</param>
        public override void Insert(int index, BsonValue value)
        {
            EnsureDataIsAccessible();
            base.Insert(index, value);
        }

        /// <summary>
        /// Removes the first occurrence of a value from the array.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        /// <returns>True if the value was removed.</returns>
        public override bool Remove(BsonValue value)
        {
            EnsureDataIsAccessible();
            return base.Remove(value);
        }

        /// <summary>
        /// Removes an element from the array.
        /// </summary>
        /// <param name="index">The zero based index of the element to remove.</param>
        public override void RemoveAt(int index)
        {
            EnsureDataIsAccessible();
            base.RemoveAt(index);
        }

        /// <summary>
        /// Converts the BsonArray to an array of BsonValues.
        /// </summary>
        /// <returns>An array of BsonValues.</returns>
        public override BsonValue[] ToArray()
        {
            EnsureDataIsAccessible();
            return base.ToArray();
        }

        /// <summary>
        /// Converts the BsonArray to a list of BsonValues.
        /// </summary>
        /// <returns>A list of BsonValues.</returns>
        public override List<BsonValue> ToList()
        {
            EnsureDataIsAccessible();
            return base.ToList();
        }

        /// <summary>
        /// Returns a string representation of the array.
        /// </summary>
        /// <returns>A string representation of the array.</returns>
        public override string ToString()
        {
            EnsureDataIsAccessible();
            return base.ToString();
        }

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_slice != null)
                    {
                        _slice.Dispose();
                        _slice = null;
                    }
                    if (_disposableItems != null)
                    {
                        _disposableItems.ForEach(x => x.Dispose());
                        _disposableItems = null;
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Throws if disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        // private methods
        private IByteBuffer CloneSlice()
        {
            return _slice.GetSlice(0, _slice.Length);
        }

        private LazyBsonArray DeserializeLazyBsonArray(BsonBinaryReader bsonReader)
        {
            var slice = bsonReader.ReadRawBsonArray();
            var nestedArray = new LazyBsonArray(slice);
            _disposableItems.Add(nestedArray);
            return nestedArray;
        }

        private LazyBsonDocument DeserializeLazyBsonDocument(BsonBinaryReader bsonReader)
        {
            var slice = bsonReader.ReadRawBsonDocument();
            var nestedDocument = new LazyBsonDocument(slice);
            _disposableItems.Add(nestedDocument);
            return nestedDocument;
        }

        private void DeserializeThisLevel()
        {
            var values = new List<BsonValue>();

            using (var bsonReader = new BsonBinaryReader(new BsonBuffer(CloneSlice(), true), true, _readerSettings))
            {
                bsonReader.ReadStartDocument();
                BsonType bsonType;
                while ((bsonType = bsonReader.ReadBsonType()) != BsonType.EndOfDocument)
                {
                    bsonReader.SkipName();
                    BsonValue value;
                    switch (bsonType)
                    {
                        case BsonType.Array: value = DeserializeLazyBsonArray(bsonReader); break;
                        case BsonType.Document: value = DeserializeLazyBsonDocument(bsonReader); break;
                        default: value = (BsonValue)BsonValueSerializer.Instance.Deserialize(bsonReader, typeof(BsonValue), null); break;
                    }
                    values.Add(value);
                }
                bsonReader.ReadEndDocument();
            }

            var slice = _slice;
            try
            {
                _slice = null;
                Clear();
                AddRange(values);
                slice.Dispose();
            }
            catch
            {
                try { Clear(); } catch { }
                _slice = slice;
                throw;
            }
        }

        private void EnsureDataIsAccessible()
        {
            ThrowIfDisposed();
            EnsureThisLevelHasBeenDeserialized();
        }

        private void EnsureThisLevelHasBeenDeserialized()
        {
            if (_slice != null)
            {
                DeserializeThisLevel();
            }
        }
    }
}

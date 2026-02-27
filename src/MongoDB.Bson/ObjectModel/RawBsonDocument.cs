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
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents an immutable BSON document that is represented using only the raw bytes.
    /// </summary>
    [BsonSerializer(typeof(RawBsonDocumentSerializer))]
    public class RawBsonDocument : BsonDocument, IDisposable
    {
        // private fields
        private bool _disposed;
        private IByteBuffer _slice;
        private List<IDisposable> _disposableItems = new List<IDisposable>();
        private BsonBinaryReaderSettings _readerSettings = BsonBinaryReaderSettings.Defaults;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RawBsonDocument"/> class.
        /// </summary>
        /// <param name="slice">The slice.</param>
        /// <exception cref="System.ArgumentNullException">slice</exception>
        /// <exception cref="System.ArgumentException">RawBsonDocument cannot be used with an IByteBuffer that needs disposing.</exception>
        public RawBsonDocument(IByteBuffer slice)
        {
            if (slice == null)
            {
                throw new ArgumentNullException("slice");
            }

            _slice = slice;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RawBsonDocument"/> class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public RawBsonDocument(byte[] bytes)
            : this(new ByteArrayBuffer(bytes, isReadOnly: true))
        {
        }

        // public properties
        /// <inheritdoc/>
        public override int ElementCount
        {
            get
            {
                ThrowIfDisposed();
                using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);

                var elementCount = 0;

                bsonReader.ReadStartDocument();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    bsonReader.SkipName();
                    bsonReader.SkipValue();
                    elementCount++;
                }
                bsonReader.ReadEndDocument();

                return elementCount;
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<BsonElement> Elements
        {
            get
            {
                ThrowIfDisposed();
                using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);
                var context = BsonDeserializationContext.CreateRoot(bsonReader);

                bsonReader.ReadStartDocument();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var name = bsonReader.ReadName();
                    var value = DeserializeBsonValue(context);
                    yield return new BsonElement(name, value);
                }
                bsonReader.ReadEndDocument();
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<string> Names
        {
            get
            {
                ThrowIfDisposed();
                using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);

                bsonReader.ReadStartDocument();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    yield return bsonReader.ReadName();
                    bsonReader.SkipValue();
                }
                bsonReader.ReadEndDocument();
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
            get
            {
                ThrowIfDisposed();
                return _slice;
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<BsonValue> Values
        {
            get
            {
                ThrowIfDisposed();
                using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);
                var context = BsonDeserializationContext.CreateRoot(bsonReader);

                bsonReader.ReadStartDocument();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    bsonReader.SkipName();
                    yield return DeserializeBsonValue(context);
                }
                bsonReader.ReadEndDocument();
            }
        }

        // public indexers
        /// <inheritdoc/>
        public override BsonValue this[int index]
        {
            get { return GetValue(index); }
            set { Set(index, value); }
        }

        /// <inheritdoc/>
        public override BsonValue this[string name]
        {
            get { return GetValue(name); }
            set { Set(name, value); }
        }

        // public methods
        /// <inheritdoc/>
        public override BsonDocument Add(BsonElement element)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument Add(string name, BsonValue value)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument Add(string name, BsonValue value, bool condition)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument AddRange(Dictionary<string, object> dictionary)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument AddRange(IDictionary dictionary)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument AddRange(IEnumerable<BsonElement> elements)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument AddRange(IEnumerable<KeyValuePair<string, object>> dictionary)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonValue Clone()
        {
            ThrowIfDisposed();
            return new RawBsonDocument(CloneSlice());
        }

        /// <inheritdoc/>
        public override bool Contains(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            ThrowIfDisposed();

            using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);

            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                if (bsonReader.ReadName() == name)
                {
                    return true;
                }
                bsonReader.SkipValue();
            }
            bsonReader.ReadEndDocument();

            return false;
        }

        /// <inheritdoc/>
        public override bool ContainsValue(BsonValue value)
        {
            ThrowIfDisposed();
            using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);
            var context = BsonDeserializationContext.CreateRoot(bsonReader);

            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                bsonReader.SkipName();
                if (DeserializeBsonValue(context).Equals(value))
                {
                    return true;
                }
            }
            bsonReader.ReadEndDocument();

            return false;
        }

        /// <inheritdoc/>
        public override BsonValue DeepClone()
        {
            ThrowIfDisposed();
            return new RawBsonDocument(CloneSlice());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public override BsonElement GetElement(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            ThrowIfDisposed();

            using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);
            var context = BsonDeserializationContext.CreateRoot(bsonReader);

            bsonReader.ReadStartDocument();
            var i = 0;
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                if (i == index)
                {
                    var name = bsonReader.ReadName();
                    var value = DeserializeBsonValue(context);
                    return new BsonElement(name, value);
                }

                bsonReader.SkipName();
                bsonReader.SkipValue();
                i++;
            }
            bsonReader.ReadEndDocument();

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <inheritdoc/>
        public override BsonElement GetElement(string name)
        {
            ThrowIfDisposed();
            BsonElement element;
            if (TryGetElement(name, out element))
            {
                return element;
            }

            throw new KeyNotFoundException($"Element '{name}' not found.");
        }

        /// <inheritdoc/>
        public override IEnumerator<BsonElement> GetEnumerator()
        {
            ThrowIfDisposed();
            using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);
            var context = BsonDeserializationContext.CreateRoot(bsonReader);

            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var name = bsonReader.ReadName();
                var value = DeserializeBsonValue(context);
                yield return new BsonElement(name, value);
            }
        }

        /// <inheritdoc/>
        public override BsonValue GetValue(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            ThrowIfDisposed();

            using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);
            var context = BsonDeserializationContext.CreateRoot(bsonReader);

            bsonReader.ReadStartDocument();
            var i = 0;
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                bsonReader.SkipName();
                if (i == index)
                {
                    return DeserializeBsonValue(context);
                }

                bsonReader.SkipValue();
                i++;
            }
            bsonReader.ReadEndDocument();

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <inheritdoc/>
        public override BsonValue GetValue(string name)
        {
            ThrowIfDisposed();
            BsonValue value;
            if (TryGetValue(name, out value))
            {
                return value;
            }

            string message = string.Format("Element '{0}' not found.", name);
            throw new KeyNotFoundException(message);
        }

        /// <inheritdoc/>
        public override BsonValue GetValue(string name, BsonValue defaultValue)
        {
            ThrowIfDisposed();
            BsonValue value;
            if (TryGetValue(name, out value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <inheritdoc/>
        public override void InsertAt(int index, BsonElement element)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <summary>
        /// Materializes the RawBsonDocument into a regular BsonDocument.
        /// </summary>
        /// <param name="binaryReaderSettings">The binary reader settings.</param>
        /// <returns>A BsonDocument.</returns>
        public BsonDocument Materialize(BsonBinaryReaderSettings binaryReaderSettings)
        {
            ThrowIfDisposed();
            using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, binaryReaderSettings);
            var context = BsonDeserializationContext.CreateRoot(bsonReader);

            return BsonDocumentSerializer.Instance.Deserialize(context);
        }

        /// <inheritdoc/>
        public override BsonDocument Merge(BsonDocument document)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument Merge(BsonDocument document, bool overwriteExistingElements)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override void Remove(string name)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override void RemoveAt(int index)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override void RemoveElement(BsonElement element)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument Set(int index, BsonValue value)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument Set(string name, BsonValue value)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument SetElement(BsonElement element)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override BsonDocument SetElement(int index, BsonElement element)
        {
            throw new NotSupportedException("RawBsonDocument instances are immutable.");
        }

        /// <inheritdoc/>
        public override bool TryGetElement(string name, out BsonElement element)
        {
            ThrowIfDisposed();

            using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);
            var context = BsonDeserializationContext.CreateRoot(bsonReader);

            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                if (bsonReader.ReadName() == name)
                {
                    var value = DeserializeBsonValue(context);
                    element = new BsonElement(name, value);
                    return true;
                }

                bsonReader.SkipValue();
            }
            bsonReader.ReadEndDocument();

            element = default;
            return false;
        }

        /// <inheritdoc/>
        public override bool TryGetValue(string name, out BsonValue value)
        {
            ThrowIfDisposed();

            using var bsonReader = BsonBinaryReaderUtils.CreateBinaryReader(_slice, _readerSettings);
            var context = BsonDeserializationContext.CreateRoot(bsonReader);

            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                if (bsonReader.ReadName() == name)
                {
                    value = DeserializeBsonValue(context);
                    return true;
                }

                bsonReader.SkipValue();
            }
            bsonReader.ReadEndDocument();

            value = null;
            return false;
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
        /// <exception cref="System.ObjectDisposedException">RawBsonDocument</exception>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("RawBsonDocument");
            }
        }

        // private methods
        private IByteBuffer CloneSlice()
        {
            return _slice.GetSlice(0, _slice.Length);
        }

        private RawBsonArray DeserializeRawBsonArray(IBsonReader bsonReader)
        {
            var slice = bsonReader.ReadRawBsonArray();
            var nestedArray = new RawBsonArray(slice);
            _disposableItems.Add(nestedArray);
            return nestedArray;
        }

        private RawBsonDocument DeserializeRawBsonDocument(IBsonReader bsonReader)
        {
            var slice = bsonReader.ReadRawBsonDocument();
            var nestedDocument = new RawBsonDocument(slice);
            _disposableItems.Add(nestedDocument);
            return nestedDocument;
        }

        private BsonValue DeserializeBsonValue(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;
            switch (bsonReader.GetCurrentBsonType())
            {
                case BsonType.Array: return DeserializeRawBsonArray(bsonReader);
                case BsonType.Document: return DeserializeRawBsonDocument(bsonReader);
                default: return BsonValueSerializer.Instance.Deserialize(context);
            }
        }
    }
}

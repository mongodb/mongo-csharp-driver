/* Copyright 2010-2012 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a BSON reader for a binary BSON byte array.
    /// </summary>
    public class BsonBinaryReader : BsonReader
    {
        // private fields
        private BsonBuffer _buffer; // if reading from a stream Create will have loaded the buffer
        private bool _disposeBuffer;
        private BsonBinaryReaderSettings _binaryReaderSettings; // same value as in base class just declared as derived class
        private BsonBinaryReaderContext _context;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonBinaryReader class.
        /// </summary>
        /// <param name="buffer">A BsonBuffer.</param>
        /// <param name="settings">A BsonBinaryReaderSettings.</param>
        public BsonBinaryReader(BsonBuffer buffer, BsonBinaryReaderSettings settings)
            : base(settings)
        {
            if (buffer == null)
            {
                _buffer = new BsonBuffer();
                _disposeBuffer = true; // only call Dispose if we allocated the buffer
            }
            else
            {
                _buffer = buffer;
                _disposeBuffer = false;
            }
            _binaryReaderSettings = settings; // already frozen by base class
            _context = new BsonBinaryReaderContext(null, ContextType.TopLevel, 0, 0);
        }

        // public properties
        /// <summary>
        /// Gets the reader's buffer.
        /// </summary>
        public BsonBuffer Buffer
        {
            get { return _buffer; }
        }

        // public methods
        /// <summary>
        /// Closes the reader.
        /// </summary>
        public override void Close()
        {
            // Close can be called on Disposed objects
            if (State != BsonReaderState.Closed)
            {
                State = BsonReaderState.Closed;
            }
        }

        /// <summary>
        /// Gets a bookmark to the reader's current position and state.
        /// </summary>
        /// <returns>A bookmark.</returns>
        public override BsonReaderBookmark GetBookmark()
        {
            return new BsonBinaryReaderBookmark(State, CurrentBsonType, CurrentName, _context, _buffer.Position);
        }

        /// <summary>
        /// Reads BSON binary data from the reader.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
#pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        public override void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType,
            out GuidRepresentation guidRepresentation)
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBinaryData", BsonType.Binary);

            int size = ReadSize();
            subType = (BsonBinarySubType)_buffer.ReadByte();
            if (subType == BsonBinarySubType.OldBinary)
            {
                // sub type OldBinary has two sizes (for historical reasons)
                int size2 = ReadSize();
                if (size2 != size - 4)
                {
                    throw new FileFormatException("Binary sub type OldBinary has inconsistent sizes");
                }
                size = size2;

                if (_binaryReaderSettings.FixOldBinarySubTypeOnInput)
                {
                    subType = BsonBinarySubType.Binary; // replace obsolete OldBinary with new Binary sub type
                }
            }
            switch (subType)
            {
                case BsonBinarySubType.UuidLegacy:
                case BsonBinarySubType.UuidStandard:
                    if (_binaryReaderSettings.GuidRepresentation != GuidRepresentation.Unspecified)
                    {
                        var expectedSubType = (_binaryReaderSettings.GuidRepresentation == GuidRepresentation.Standard) ? BsonBinarySubType.UuidStandard : BsonBinarySubType.UuidLegacy;
                        if (subType != expectedSubType)
                        {
                            var message = string.Format(
                                "The GuidRepresentation for the reader is {0}, which requires the binary sub type to be {1}, not {2}.",
                                _binaryReaderSettings.GuidRepresentation, expectedSubType, subType);
                            throw new FileFormatException(message);
                        }
                    }
                    guidRepresentation = (subType == BsonBinarySubType.UuidStandard) ? GuidRepresentation.Standard : _binaryReaderSettings.GuidRepresentation;
                    break;
                default:
                    guidRepresentation = GuidRepresentation.Unspecified;
                    break;
            }
            bytes = _buffer.ReadBytes(size);

            State = GetNextState();
        }
#pragma warning restore 618

        /// <summary>
        /// Reads a BSON boolean from the reader.
        /// </summary>
        /// <returns>A Boolean.</returns>
        public override bool ReadBoolean()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBoolean", BsonType.Boolean);
            State = GetNextState();
            return _buffer.ReadBoolean();
        }

        /// <summary>
        /// Reads a BsonType from the reader.
        /// </summary>
        /// <param name="bsonTrie">An optional trie to search for a value that matches the next element name.</param>
        /// <param name="found">Set to true if a matching value was found in the trie.</param>
        /// <param name="value">Set to the matching value found in the trie or null if no matching value was found.</param>
        /// <returns>A BsonType.</returns>
        public override BsonType ReadBsonType<TValue>(BsonTrie<TValue> bsonTrie, out bool found, out TValue value)
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            found = false;
            value = default(TValue);
            if (State == BsonReaderState.Initial || State == BsonReaderState.Done || State == BsonReaderState.ScopeDocument)
            {
                // there is an implied type of Document for the top level and for scope documents
                CurrentBsonType = BsonType.Document;
                State = BsonReaderState.Value;
                return CurrentBsonType;
            }
            if (State != BsonReaderState.Type)
            {
                ThrowInvalidState("ReadBsonType", BsonReaderState.Type);
            }

            CurrentBsonType = _buffer.ReadBsonType();

            if (CurrentBsonType == BsonType.EndOfDocument)
            {
                switch (_context.ContextType)
                {
                    case ContextType.Array:
                        State = BsonReaderState.EndOfArray;
                        return BsonType.EndOfDocument;
                    case ContextType.Document:
                    case ContextType.ScopeDocument:
                        State = BsonReaderState.EndOfDocument;
                        return BsonType.EndOfDocument;
                    default:
                        var message = string.Format("BsonType EndOfDocument is not valid when ContextType is {0}.", _context.ContextType);
                        throw new FileFormatException(message);
                }
            }
            else
            {
                switch (_context.ContextType)
                {
                    case ContextType.Array:
                        _buffer.SkipCString(); // ignore array element names
                        State = BsonReaderState.Value;
                        break;
                    case ContextType.Document:
                    case ContextType.ScopeDocument:
                        CurrentName = _buffer.ReadCString(bsonTrie, out found, out value);
                        State = BsonReaderState.Name;
                        break;
                    default:
                        throw new BsonInternalException("Unexpected ContextType.");
                }

                return CurrentBsonType;
            }
        }

        /// <summary>
        /// Reads a BSON DateTime from the reader.
        /// </summary>
        /// <returns>The number of milliseconds since the Unix epoch.</returns>
        public override long ReadDateTime()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            State = GetNextState();
            var value = _buffer.ReadInt64();
            if (value == BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch + 1)
            {
                if (_binaryReaderSettings.FixOldDateTimeMaxValueOnInput)
                {
                    value = BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch;
                }
            }
            return value;
        }

        /// <summary>
        /// Reads a BSON Double from the reader.
        /// </summary>
        /// <returns>A Double.</returns>
        public override double ReadDouble()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDouble", BsonType.Double);
            State = GetNextState();
            return _buffer.ReadDouble();
        }

        /// <summary>
        /// Reads the end of a BSON array from the reader.
        /// </summary>
        public override void ReadEndArray()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            if (_context.ContextType != ContextType.Array)
            {
                ThrowInvalidContextType("ReadEndArray", _context.ContextType, ContextType.Array);
            }
            if (State == BsonReaderState.Type)
            {
                ReadBsonType(); // will set state to EndOfArray if at end of array
            }
            if (State != BsonReaderState.EndOfArray)
            {
                ThrowInvalidState("ReadEndArray", BsonReaderState.EndOfArray);
            }

            _context = _context.PopContext(_buffer.Position);
            switch (_context.ContextType)
            {
                case ContextType.Array: State = BsonReaderState.Type; break;
                case ContextType.Document: State = BsonReaderState.Type; break;
                case ContextType.TopLevel: State = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType.");
            }
        }

        /// <summary>
        /// Reads the end of a BSON document from the reader.
        /// </summary>
        public override void ReadEndDocument()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            if (_context.ContextType != ContextType.Document && _context.ContextType != ContextType.ScopeDocument)
            {
                ThrowInvalidContextType("ReadEndDocument", _context.ContextType, ContextType.Document, ContextType.ScopeDocument);
            }
            if (State == BsonReaderState.Type)
            {
                ReadBsonType(); // will set state to EndOfDocument if at end of document
            }
            if (State != BsonReaderState.EndOfDocument)
            {
                ThrowInvalidState("ReadEndDocument", BsonReaderState.EndOfDocument);
            }

            _context = _context.PopContext(_buffer.Position);
            if (_context != null && _context.ContextType == ContextType.JavaScriptWithScope)
            {
                _context = _context.PopContext(_buffer.Position); // JavaScriptWithScope
            }
            switch (_context.ContextType)
            {
                case ContextType.Array: State = BsonReaderState.Type; break;
                case ContextType.Document: State = BsonReaderState.Type; break;
                case ContextType.TopLevel: State = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType.");
            }
        }

        /// <summary>
        /// Reads a BSON Int32 from the reader.
        /// </summary>
        /// <returns>An Int32.</returns>
        public override int ReadInt32()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            State = GetNextState();
            return _buffer.ReadInt32();
        }

        /// <summary>
        /// Reads a BSON Int64 from the reader.
        /// </summary>
        /// <returns>An Int64.</returns>
        public override long ReadInt64()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            State = GetNextState();
            return _buffer.ReadInt64();
        }

        /// <summary>
        /// Reads a BSON JavaScript from the reader.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadJavaScript()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScript", BsonType.JavaScript);
            State = GetNextState();
            return _buffer.ReadString();
        }

        /// <summary>
        /// Reads a BSON JavaScript with scope from the reader (call ReadStartDocument next to read the scope).
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadJavaScriptWithScope()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScriptWithScope", BsonType.JavaScriptWithScope);

            var startPosition = _buffer.Position; // position of size field
            var size = ReadSize();
            _context = new BsonBinaryReaderContext(_context, ContextType.JavaScriptWithScope, startPosition, size);
            var code = _buffer.ReadString();

            State = BsonReaderState.ScopeDocument;
            return code;
        }

        /// <summary>
        /// Reads a BSON MaxKey from the reader.
        /// </summary>
        public override void ReadMaxKey()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMaxKey", BsonType.MaxKey);
            State = GetNextState();
        }

        /// <summary>
        /// Reads a BSON MinKey from the reader.
        /// </summary>
        public override void ReadMinKey()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMinKey", BsonType.MinKey);
            State = GetNextState();
        }

        /// <summary>
        /// Reads a BSON null from the reader.
        /// </summary>
        public override void ReadNull()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadNull", BsonType.Null);
            State = GetNextState();
        }

        /// <summary>
        /// Reads a BSON ObjectId from the reader.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public override void ReadObjectId(out int timestamp, out int machine, out short pid, out int increment)
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadObjectId", BsonType.ObjectId);
            _buffer.ReadObjectId(out timestamp, out machine, out pid, out increment);
            State = GetNextState();
        }

        /// <summary>
        /// Reads a BSON regular expression from the reader.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public override void ReadRegularExpression(out string pattern, out string options)
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadRegularExpression", BsonType.RegularExpression);
            pattern = _buffer.ReadCString();
            options = _buffer.ReadCString();
            State = GetNextState();
        }

        /// <summary>
        /// Reads the start of a BSON array.
        /// </summary>
        public override void ReadStartArray()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartArray", BsonType.Array);

            var startPosition = _buffer.Position; // position of size field
            var size = ReadSize();
            _context = new BsonBinaryReaderContext(_context, ContextType.Array, startPosition, size);
            State = BsonReaderState.Type;
        }

        /// <summary>
        /// Reads the start of a BSON document.
        /// </summary>
        public override void ReadStartDocument()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartDocument", BsonType.Document);

            var contextType = (State == BsonReaderState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
            var startPosition = _buffer.Position; // position of size field
            var size = ReadSize();
            _context = new BsonBinaryReaderContext(_context, contextType, startPosition, size);
            State = BsonReaderState.Type;
        }

        /// <summary>
        /// Reads a BSON string from the reader.
        /// </summary>
        /// <returns>A String.</returns>
        public override string ReadString()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadString", BsonType.String);
            State = GetNextState();
            return _buffer.ReadString();
        }

        /// <summary>
        /// Reads a BSON symbol from the reader.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadSymbol()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadSymbol", BsonType.Symbol);
            State = GetNextState();
            return _buffer.ReadString();
        }

        /// <summary>
        /// Reads a BSON timestamp from the reader.
        /// </summary>
        /// <returns>The combined timestamp/increment.</returns>
        public override long ReadTimestamp()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadTimestamp", BsonType.Timestamp);
            State = GetNextState();
            return _buffer.ReadInt64();
        }

        /// <summary>
        /// Reads a BSON undefined from the reader.
        /// </summary>
        public override void ReadUndefined()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadUndefined", BsonType.Undefined);
            State = GetNextState();
        }

        /// <summary>
        /// Returns the reader to previously bookmarked position and state.
        /// </summary>
        /// <param name="bookmark">The bookmark.</param>
        public override void ReturnToBookmark(BsonReaderBookmark bookmark)
        {
            var binaryReaderBookmark = (BsonBinaryReaderBookmark)bookmark;
            State = binaryReaderBookmark.State;
            CurrentBsonType = binaryReaderBookmark.CurrentBsonType;
            CurrentName = binaryReaderBookmark.CurrentName;
            _context = binaryReaderBookmark.CloneContext();
            _buffer.Position = binaryReaderBookmark.Position;
        }

        /// <summary>
        /// Skips the name (reader must be positioned on a name).
        /// </summary>
        public override void SkipName()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            if (State != BsonReaderState.Name)
            {
                ThrowInvalidState("SkipName", BsonReaderState.Name);
            }

            State = BsonReaderState.Value;
        }

        /// <summary>
        /// Skips the value (reader must be positioned on a value).
        /// </summary>
        public override void SkipValue()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            if (State != BsonReaderState.Value)
            {
                ThrowInvalidState("SkipValue", BsonReaderState.Value);
            }

            int skip;
            switch (CurrentBsonType)
            {
                case BsonType.Array: skip = ReadSize() - 4; break;
                case BsonType.Binary: skip = ReadSize() + 1; break;
                case BsonType.Boolean: skip = 1; break;
                case BsonType.DateTime: skip = 8; break;
                case BsonType.Document: skip = ReadSize() - 4; break;
                case BsonType.Double: skip = 8; break;
                case BsonType.Int32: skip = 4; break;
                case BsonType.Int64: skip = 8; break;
                case BsonType.JavaScript: skip = ReadSize(); break;
                case BsonType.JavaScriptWithScope: skip = ReadSize() - 4; break;
                case BsonType.MaxKey: skip = 0; break;
                case BsonType.MinKey: skip = 0; break;
                case BsonType.Null: skip = 0; break;
                case BsonType.ObjectId: skip = 12; break;
                case BsonType.RegularExpression: _buffer.SkipCString(); _buffer.SkipCString(); skip = 0; break;
                case BsonType.String: skip = ReadSize(); break;
                case BsonType.Symbol: skip = ReadSize(); break;
                case BsonType.Timestamp: skip = 8; break;
                case BsonType.Undefined: skip = 0; break;
                default: throw new BsonInternalException("Unexpected BsonType.");
            }
            _buffer.Skip(skip);

            State = BsonReaderState.Type;
        }

        // protected methods
        /// <summary>
        /// Disposes of any resources used by the reader.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Close();
                    if (_disposeBuffer)
                    {
                        _buffer.Dispose();
                        _buffer = null;
                    }
                }
                catch { } // ignore exceptions
            }
            base.Dispose(disposing);
        }

        // private methods
        private BsonReaderState GetNextState()
        {
            switch (_context.ContextType)
            {
                case ContextType.Array:
                case ContextType.Document:
                case ContextType.ScopeDocument:
                    return BsonReaderState.Type;
                case ContextType.TopLevel:
                    return BsonReaderState.Done;
                default:
                    throw new BsonInternalException("Unexpected ContextType.");
            }
        }

        private int ReadSize()
        {
            int size = _buffer.ReadInt32();
            if (size < 0)
            {
                var message = string.Format("Size {0} is not valid because it is negative.", size);
                throw new FileFormatException(message);
            }
            if (size > _binaryReaderSettings.MaxDocumentSize)
            {
                var message = string.Format("Size {0} is not valid because it is larger than MaxDocumentSize {1}.", size, _binaryReaderSettings.MaxDocumentSize);
                throw new FileFormatException(message);
            }
            return size;
        }
    }
}

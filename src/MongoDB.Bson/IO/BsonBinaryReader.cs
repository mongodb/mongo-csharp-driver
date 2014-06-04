/* Copyright 2010-2014 MongoDB Inc.
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
using System.IO;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a BSON reader for a binary BSON byte array.
    /// </summary>
    public class BsonBinaryReader : BsonReader
    {
        // private fields
        private readonly BsonStreamReader _streamReader;
        private readonly BsonBinaryReaderSettings _settings; // same value as in base class just declared as derived class
        private BsonBinaryReaderContext _context;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonBinaryReader class.
        /// </summary>
        /// <param name="stream">A stream (BsonBinary does not own the stream and will not Dispose it).</param>
        public BsonBinaryReader(Stream stream)
            : this(stream, BsonBinaryReaderSettings.Defaults)
        {
        }

        /// <summary>
        /// Initializes a new instance of the BsonBinaryReader class.
        /// </summary>
        /// <param name="stream">A stream (BsonBinary does not own the stream and will not Dispose it).</param>
        /// <param name="settings">A BsonBinaryReaderSettings.</param>
        public BsonBinaryReader(Stream stream, BsonBinaryReaderSettings settings)
            : base(settings)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (!stream.CanSeek)
            {
                throw new ArgumentException("The stream must be capable of seeking.", "stream");
            }

            _streamReader = new BsonStreamReader(stream, settings.Encoding);
            _settings = settings; // already frozen by base class

            _context = new BsonBinaryReaderContext(null, ContextType.TopLevel, 0, 0);
        }

        // public methods
        /// <summary>
        /// Closes the reader.
        /// </summary>
        public override void Close()
        {
            // Close can be called on Disposed objects
            State = BsonReaderState.Closed;
        }

        /// <summary>
        /// Gets a bookmark to the reader's current position and state.
        /// </summary>
        /// <returns>A bookmark.</returns>
        public override BsonReaderBookmark GetBookmark()
        {
            return new BsonBinaryReaderBookmark(State, CurrentBsonType, CurrentName, _context, (int)_streamReader.Position);
        }

        /// <summary>
        /// Determines whether this reader is at end of file.
        /// </summary>
        /// <returns>
        /// Whether this reader is at end of file.
        /// </returns>
        public override bool IsAtEndOfFile()
        {
            var stream = _streamReader.BaseStream;
            var c = stream.ReadByte();
            if (c == -1)
            {
                return true;
            }
            else
            {
                stream.Seek(-1, SeekOrigin.Current);
                return false;
            }
        }

        /// <summary>
        /// Reads BSON binary data from the reader.
        /// </summary>
        /// <returns>A BsonBinaryData.</returns>
#pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        public override BsonBinaryData ReadBinaryData()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBinaryData", BsonType.Binary);

            int size = ReadSize();

            var subType = (BsonBinarySubType)_streamReader.ReadByte();
            if (subType == BsonBinarySubType.OldBinary)
            {
                // sub type OldBinary has two sizes (for historical reasons)
                int size2 = ReadSize();
                if (size2 != size - 4)
                {
                    throw new FileFormatException("Binary sub type OldBinary has inconsistent sizes");
                }
                size = size2;

                if (_settings.FixOldBinarySubTypeOnInput)
                {
                    subType = BsonBinarySubType.Binary; // replace obsolete OldBinary with new Binary sub type
                }
            }

            var bytes = _streamReader.ReadBytes(size);

            var guidRepresentation = GuidRepresentation.Unspecified;
            if (subType == BsonBinarySubType.UuidLegacy || subType == BsonBinarySubType.UuidStandard)
            {
                if (_settings.GuidRepresentation != GuidRepresentation.Unspecified)
                {
                    var expectedSubType = (_settings.GuidRepresentation == GuidRepresentation.Standard) ? BsonBinarySubType.UuidStandard : BsonBinarySubType.UuidLegacy;
                    if (subType != expectedSubType)
                    {
                        var message = string.Format(
                            "The GuidRepresentation for the reader is {0}, which requires the binary sub type to be {1}, not {2}.",
                            _settings.GuidRepresentation, expectedSubType, subType);
                        throw new FileFormatException(message);
                    }
                }
                guidRepresentation = (subType == BsonBinarySubType.UuidStandard) ? GuidRepresentation.Standard : _settings.GuidRepresentation;
            }

            State = GetNextState();
            return new BsonBinaryData(bytes, subType, guidRepresentation);
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
            return _streamReader.ReadBoolean();
        }

        /// <summary>
        /// Reads a BsonType from the reader.
        /// </summary>
        /// <returns>A BsonType.</returns>
        public override BsonType ReadBsonType()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
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

            CurrentBsonType = _streamReader.ReadBsonType();

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
                        _streamReader.SkipCString(); // ignore array element names
                        State = BsonReaderState.Value;
                        break;
                    case ContextType.Document:
                    case ContextType.ScopeDocument:
                        State = BsonReaderState.Name;
                        break;
                    default:
                        throw new BsonInternalException("Unexpected ContextType.");
                }

                return CurrentBsonType;
            }
        }

        /// <summary>
        /// Reads BSON binary data from the reader.
        /// </summary>
        /// <returns>A byte array.</returns>
#pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        public override byte[] ReadBytes()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBytes", BsonType.Binary);

            int size = ReadSize();

            var subType = (BsonBinarySubType)_streamReader.ReadByte();
            if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary)
            {
                var message = string.Format("ReadBytes requires the binary sub type to be Binary, not {2}.", subType);
                throw new FileFormatException(message);
            }

            State = GetNextState();
            return _streamReader.ReadBytes(size);
        }
#pragma warning restore 618

        /// <summary>
        /// Reads a BSON DateTime from the reader.
        /// </summary>
        /// <returns>The number of milliseconds since the Unix epoch.</returns>
        public override long ReadDateTime()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            State = GetNextState();
            var value = _streamReader.ReadInt64();
            if (value == BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch + 1)
            {
                if (_settings.FixOldDateTimeMaxValueOnInput)
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
            return _streamReader.ReadDouble();
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

            _context = _context.PopContext(_streamReader.Position);
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

            _context = _context.PopContext(_streamReader.Position);
            if (_context.ContextType == ContextType.JavaScriptWithScope)
            {
                _context = _context.PopContext(_streamReader.Position); // JavaScriptWithScope
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
            return _streamReader.ReadInt32();
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
            return _streamReader.ReadInt64();
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
            return _streamReader.ReadString();
        }

        /// <summary>
        /// Reads a BSON JavaScript with scope from the reader (call ReadStartDocument next to read the scope).
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadJavaScriptWithScope()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScriptWithScope", BsonType.JavaScriptWithScope);

            var startPosition = _streamReader.Position; // position of size field
            var size = ReadSize();
            _context = new BsonBinaryReaderContext(_context, ContextType.JavaScriptWithScope, startPosition, size);
            var code = _streamReader.ReadString();

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
        /// Reads the name of an element from the reader.
        /// </summary>
        /// <param name="nameDecoder">The name decoder.</param>
        /// <returns>The name of the element.</returns>
        public override string ReadName(INameDecoder nameDecoder)
        {
            if (nameDecoder == null)
            {
                throw new ArgumentNullException("nameDecoder");
            }

            if (Disposed) { ThrowObjectDisposedException(); }
            if (State == BsonReaderState.Type)
            {
                ReadBsonType();
            }
            if (State != BsonReaderState.Name)
            {
                ThrowInvalidState("ReadName", BsonReaderState.Name);
            }

            CurrentName = nameDecoder.Decode(_streamReader);
            State = BsonReaderState.Value;

            return CurrentName;
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
        /// <returns>An ObjectId.</returns>
        public override ObjectId ReadObjectId()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadObjectId", BsonType.ObjectId);
            State = GetNextState();
            return _streamReader.ReadObjectId();
        }

        /// <summary>
        /// Reads a raw BSON array.
        /// </summary>
        /// <returns>
        /// The raw BSON array.
        /// </returns>
        public override IByteBuffer ReadRawBsonArray()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadRawBsonArray", BsonType.Array);

            var slice = ReadSlice();

            switch (_context.ContextType)
            {
                case ContextType.Array: State = BsonReaderState.Type; break;
                case ContextType.Document: State = BsonReaderState.Type; break;
                case ContextType.TopLevel: State = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType.");
            }

            return slice;
        }

        /// <summary>
        /// Reads a raw BSON document.
        /// </summary>
        /// <returns>
        /// The raw BSON document.
        /// </returns>
        public override IByteBuffer ReadRawBsonDocument()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadRawBsonDocument", BsonType.Document);

            var slice = ReadSlice();

            if (_context.ContextType == ContextType.JavaScriptWithScope)
            {
                _context = _context.PopContext(_streamReader.Position); // JavaScriptWithScope
            }
            switch (_context.ContextType)
            {
                case ContextType.Array: State = BsonReaderState.Type; break;
                case ContextType.Document: State = BsonReaderState.Type; break;
                case ContextType.TopLevel: State = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType.");
            }

            return slice;
        }

        /// <summary>
        /// Reads a BSON regular expression from the reader.
        /// </summary>
        /// <returns>A BsonRegularExpression.</returns>
        public override BsonRegularExpression ReadRegularExpression()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadRegularExpression", BsonType.RegularExpression);
            State = GetNextState();
            var pattern = _streamReader.ReadCString();
            var options = _streamReader.ReadCString();
            return new BsonRegularExpression(pattern, options);
        }

        /// <summary>
        /// Reads the start of a BSON array.
        /// </summary>
        public override void ReadStartArray()
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartArray", BsonType.Array);

            var startPosition = _streamReader.Position; // position of size field
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
            var startPosition = _streamReader.Position; // position of size field
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
            return _streamReader.ReadString();
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
            return _streamReader.ReadString();
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
            return _streamReader.ReadInt64();
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
            _streamReader.Position = binaryReaderBookmark.Position;
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

            _streamReader.SkipCString();
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
                case BsonType.RegularExpression: _streamReader.SkipCString(); _streamReader.SkipCString(); skip = 0; break;
                case BsonType.String: skip = ReadSize(); break;
                case BsonType.Symbol: skip = ReadSize(); break;
                case BsonType.Timestamp: skip = 8; break;
                case BsonType.Undefined: skip = 0; break;
                default: throw new BsonInternalException("Unexpected BsonType.");
            }
            _streamReader.BaseStream.Seek(skip, SeekOrigin.Current);

            State = BsonReaderState.Type;
        }

        // protected methods
        /// <summary>
        /// Disposes of any resources used by the reader.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected override void Dispose(bool disposing)
        {
            // don't Dispose the _stream because we don't own it
            if (disposing)
            {
                try
                {
                    Close();
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
            int size = _streamReader.ReadInt32();
            if (size < 0)
            {
                var message = string.Format("Size {0} is not valid because it is negative.", size);
                throw new FileFormatException(message);
            }
            if (size > _settings.MaxDocumentSize)
            {
                var message = string.Format("Size {0} is not valid because it is larger than MaxDocumentSize {1}.", size, _settings.MaxDocumentSize);
                throw new FileFormatException(message);
            }
            return size;
        }

        private IByteBuffer ReadSlice()
        {
            var position = (int)_streamReader.Position;
            var length = ReadSize();

            var sliceableStream = _streamReader.BaseStream as ISliceableStream;
            if (sliceableStream != null && !_streamReader.BaseStream.CanWrite)
            {
                _streamReader.Position = position + length;
                return sliceableStream.GetSlice(position, length);
            }
            else
            {
                var bytes = new byte[length];
                _streamReader.Position = position;
                _streamReader.ReadBytes(bytes, 0, length);
                return new ByteArrayBuffer(bytes, 0, length, isReadOnly: true);
            }
        }
    }
}

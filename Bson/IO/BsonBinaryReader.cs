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
using System.IO;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO {
    /// <summary>
    /// Represents a BSON reader for a binary BSON byte array.
    /// </summary>
    public class BsonBinaryReader : BsonReader {
        #region private fields
        private BsonBuffer buffer; // if reading from a stream Create will have loaded the buffer
        private bool disposeBuffer;
        private new BsonBinaryReaderSettings settings; // same value as in base class just declared as derived class
        private BsonBinaryReaderContext context;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBinaryReader class.
        /// <param name="buffer">A BsonBuffer.</param>
        /// <param name="settings">A BsonBinaryReaderSettings.</param>
        /// </summary>
        public BsonBinaryReader(
            BsonBuffer buffer,
            BsonBinaryReaderSettings settings
        )
            : base(settings) {
            if (buffer == null) {
                this.buffer = new BsonBuffer();
                this.disposeBuffer = true; // only call Dispose if we allocated the buffer
            } else {
                this.buffer = buffer;
                this.disposeBuffer = false;
            }
            this.settings = settings; // already frozen by base class
            context = new BsonBinaryReaderContext(null, ContextType.TopLevel, 0, 0);
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the reader's buffer.
        /// </summary>
        public BsonBuffer Buffer {
            get { return buffer; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Closes the reader.
        /// </summary>
        public override void Close() {
            // Close can be called on Disposed objects
            if (state != BsonReaderState.Closed) {
                state = BsonReaderState.Closed;
            }
        }

        /// <summary>
        /// Gets a bookmark to the reader's current position and state.
        /// </summary>
        /// <returns>A bookmark.</returns>
        public override BsonReaderBookmark GetBookmark() {
            return new BsonBinaryReaderBookmark(state, currentBsonType, currentName, context, buffer.Position);
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
            out GuidRepresentation guidRepresentation
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBinaryData", BsonType.Binary);

            int size = ReadSize();
            subType = (BsonBinarySubType) buffer.ReadByte();
            if (subType == BsonBinarySubType.OldBinary) {
                // sub type OldBinary has two sizes (for historical reasons)
                int size2 = ReadSize();
                if (size2 != size - 4) {
                    throw new FileFormatException("Binary sub type OldBinary has inconsistent sizes");
                }
                size = size2;

                if (settings.FixOldBinarySubTypeOnInput) {
                    subType = BsonBinarySubType.Binary; // replace obsolete OldBinary with new Binary sub type
                }
            }
            switch (subType) {
                case BsonBinarySubType.UuidLegacy:
                case BsonBinarySubType.UuidStandard:
                    if (settings.GuidRepresentation != GuidRepresentation.Unspecified) {
                        var expectedSubType = (settings.GuidRepresentation == GuidRepresentation.Standard) ? BsonBinarySubType.UuidStandard : BsonBinarySubType.UuidLegacy;
                        if (subType != expectedSubType) {
                            var message = string.Format("The GuidRepresentation for the reader is {0}, which requires the binary sub type to be {1}, not {2}.", settings.GuidRepresentation, expectedSubType, subType);
                            throw new FileFormatException(message);
                        }
                    }
                    guidRepresentation = (subType == BsonBinarySubType.UuidStandard) ? GuidRepresentation.Standard : settings.GuidRepresentation;
                    break;
                default:
                    guidRepresentation = GuidRepresentation.Unspecified;
                    break;
            }
            bytes = buffer.ReadBytes(size);

            state = GetNextState();
        }
        #pragma warning restore 618

        /// <summary>
        /// Reads a BSON boolean from the reader.
        /// </summary>
        /// <returns>A Boolean.</returns>
        public override bool ReadBoolean() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBoolean", BsonType.Boolean);
            state = GetNextState();
            return buffer.ReadBoolean();
        }

        /// <summary>
        /// Reads a BsonType from the reader.
        /// </summary>
        /// <returns>A BsonType.</returns>
        public override BsonType ReadBsonType() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state == BsonReaderState.Initial || state == BsonReaderState.Done || state == BsonReaderState.ScopeDocument) {
                // there is an implied type of Document for the top level and for scope documents
                currentBsonType = BsonType.Document;
                state = BsonReaderState.Value;
                return currentBsonType;
            }
            if (state != BsonReaderState.Type) {
                ThrowInvalidState("ReadBsonType", BsonReaderState.Type);
            }

            currentBsonType = buffer.ReadBsonType();

            if (currentBsonType == BsonType.EndOfDocument) {
                switch (context.ContextType) {
                    case ContextType.Array:
                        state = BsonReaderState.EndOfArray;
                        return BsonType.EndOfDocument;
                    case ContextType.Document:
                    case ContextType.ScopeDocument:
                        state = BsonReaderState.EndOfDocument;
                        return BsonType.EndOfDocument;
                    default:
                        var message = string.Format("BsonType EndOfDocument is not valid when ContextType is {0}.", context.ContextType);
                        throw new FileFormatException(message);
                }
            } else {
                switch (context.ContextType) {
                    case ContextType.Array:
                        buffer.SkipCString(); // ignore array element names
                        state = BsonReaderState.Value;
                        break;
                    case ContextType.Document:
                    case ContextType.ScopeDocument:
                        currentName = buffer.ReadCString();
                        state = BsonReaderState.Name;
                        break;
                    default:
                        throw new BsonInternalException("Unexpected ContextType.");
                }

                return currentBsonType;
            }
        }

        /// <summary>
        /// Reads a BSON DateTime from the reader.
        /// </summary>
        /// <returns>The number of milliseconds since the Unix epoch.</returns>
        public override long ReadDateTime() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            state = GetNextState();
            var value = buffer.ReadInt64();
            if (value == BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch + 1) {
                if (settings.FixOldDateTimeMaxValueOnInput) {
                    value = BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch;
                }
            }
            return value;
        }

        /// <summary>
        /// Reads a BSON Double from the reader.
        /// </summary>
        /// <returns>A Double.</returns>
        public override double ReadDouble() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDouble", BsonType.Double);
            state = GetNextState();
            return buffer.ReadDouble();
        }

        /// <summary>
        /// Reads the end of a BSON array from the reader.
        /// </summary>
        public override void ReadEndArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (context.ContextType != ContextType.Array) {
                ThrowInvalidContextType("ReadEndArray", context.ContextType, ContextType.Array);
            }
            if (state == BsonReaderState.Type) {
                ReadBsonType(); // will set state to EndOfArray if at end of array
            }
            if (state != BsonReaderState.EndOfArray) {
                ThrowInvalidState("ReadEndArray", BsonReaderState.EndOfArray);
            }

            context = context.PopContext(buffer.Position);
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReaderState.Type; break;
                case ContextType.Document: state = BsonReaderState.Type; break;
                case ContextType.TopLevel: state = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType.");
            }
        }

        /// <summary>
        /// Reads the end of a BSON document from the reader.
        /// </summary>
        public override void ReadEndDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (
                context.ContextType != ContextType.Document &&
                context.ContextType != ContextType.ScopeDocument
            ) {
                ThrowInvalidContextType("ReadEndDocument", context.ContextType, ContextType.Document, ContextType.ScopeDocument);
            }
            if (state == BsonReaderState.Type) {
                ReadBsonType(); // will set state to EndOfDocument if at end of document
            }
            if (state != BsonReaderState.EndOfDocument) {
                ThrowInvalidState("ReadEndDocument", BsonReaderState.EndOfDocument);
            }

            context = context.PopContext(buffer.Position);
            if (context != null && context.ContextType == ContextType.JavaScriptWithScope) {
                context = context.PopContext(buffer.Position); // JavaScriptWithScope
            }
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReaderState.Type; break;
                case ContextType.Document: state = BsonReaderState.Type; break;
                case ContextType.TopLevel: state = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType.");
            }
        }

        /// <summary>
        /// Reads a BSON Int32 from the reader.
        /// </summary>
        /// <returns>An Int32.</returns>
        public override int ReadInt32() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            state = GetNextState();
            return buffer.ReadInt32();
        }

        /// <summary>
        /// Reads a BSON Int64 from the reader.
        /// </summary>
        /// <returns>An Int64.</returns>
        public override long ReadInt64() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            state = GetNextState();
            return buffer.ReadInt64();
        }

        /// <summary>
        /// Reads a BSON JavaScript from the reader.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadJavaScript() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScript", BsonType.JavaScript);
            state = GetNextState();
            return buffer.ReadString();
        }

        /// <summary>
        /// Reads a BSON JavaScript with scope from the reader (call ReadStartDocument next to read the scope).
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadJavaScriptWithScope() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScriptWithScope", BsonType.JavaScriptWithScope);

            var startPosition = buffer.Position; // position of size field
            var size = ReadSize();
            context = new BsonBinaryReaderContext(context, ContextType.JavaScriptWithScope, startPosition, size);
            var code = buffer.ReadString();

            state = BsonReaderState.ScopeDocument;
            return code;
        }

        /// <summary>
        /// Reads a BSON MaxKey from the reader.
        /// </summary>
        public override void ReadMaxKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMaxKey", BsonType.MaxKey);
            state = GetNextState();
        }

        /// <summary>
        /// Reads a BSON MinKey from the reader.
        /// </summary>
        public override void ReadMinKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMinKey", BsonType.MinKey);
            state = GetNextState();
        }

        /// <summary>
        /// Reads a BSON null from the reader.
        /// </summary>
        public override void ReadNull() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadNull", BsonType.Null);
            state = GetNextState();
        }

        /// <summary>
        /// Reads a BSON ObjectId from the reader.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public override void ReadObjectId(
            out int timestamp,
            out int machine,
            out short pid,
            out int increment
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadObjectId", BsonType.ObjectId);
            buffer.ReadObjectId(out timestamp, out machine, out pid, out increment);
            state = GetNextState();
        }

        /// <summary>
        /// Reads a BSON regular expression from the reader.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public override void ReadRegularExpression(
            out string pattern,
            out string options
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadRegularExpression", BsonType.RegularExpression);
            pattern = buffer.ReadCString();
            options = buffer.ReadCString();
            state = GetNextState();
        }

        /// <summary>
        /// Reads the start of a BSON array.
        /// </summary>
        public override void ReadStartArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartArray", BsonType.Array);

            var startPosition = buffer.Position; // position of size field
            var size = ReadSize();
            context = new BsonBinaryReaderContext(context, ContextType.Array, startPosition, size);
            state = BsonReaderState.Type;
        }

        /// <summary>
        /// Reads the start of a BSON document.
        /// </summary>
        public override void ReadStartDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartDocument", BsonType.Document);

            var contextType = (state == BsonReaderState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
            var startPosition = buffer.Position; // position of size field
            var size = ReadSize();
            context = new BsonBinaryReaderContext(context, contextType, startPosition, size);
            state = BsonReaderState.Type;
        }

        /// <summary>
        /// Reads a BSON string from the reader.
        /// </summary>
        /// <returns>A String.</returns>
        public override string ReadString() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadString", BsonType.String);
            state = GetNextState();
            return buffer.ReadString();
        }

        /// <summary>
        /// Reads a BSON symbol from the reader.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadSymbol() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadSymbol", BsonType.Symbol);
            state = GetNextState();
            return buffer.ReadString();
        }

        /// <summary>
        /// Reads a BSON timestamp from the reader.
        /// </summary>
        /// <returns>The combined timestamp/increment.</returns>
        public override long ReadTimestamp() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadTimestamp", BsonType.Timestamp);
            state = GetNextState();
            return buffer.ReadInt64();
        }

        /// <summary>
        /// Reads a BSON undefined from the reader.
        /// </summary>
        public override void ReadUndefined() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadUndefined", BsonType.Undefined);
            state = GetNextState();
        }

        /// <summary>
        /// Returns the reader to previously bookmarked position and state.
        /// </summary>
        /// <param name="bookmark">The bookmark.</param>
        public override void ReturnToBookmark(
            BsonReaderBookmark bookmark
        ) {
            var binaryReaderBookmark = (BsonBinaryReaderBookmark) bookmark;
            state = binaryReaderBookmark.State;
            currentBsonType = binaryReaderBookmark.CurrentBsonType;
            currentName = binaryReaderBookmark.CurrentName;
            context = binaryReaderBookmark.CloneContext();
            buffer.Position = binaryReaderBookmark.Position;
        }

        /// <summary>
        /// Skips the name (reader must be positioned on a name).
        /// </summary>
        public override void SkipName() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Name) {
                ThrowInvalidState("SkipName", BsonReaderState.Name);
            }

            state = BsonReaderState.Value;
        }

        /// <summary>
        /// Skips the value (reader must be positioned on a value).
        /// </summary>
        public override void SkipValue() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Value) {
                ThrowInvalidState("SkipValue", BsonReaderState.Value);
            }

            int skip;
            switch (currentBsonType) {
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
                case BsonType.RegularExpression: buffer.SkipCString(); buffer.SkipCString(); skip = 0; break;
                case BsonType.String: skip = ReadSize(); break;
                case BsonType.Symbol: skip = ReadSize(); break;
                case BsonType.Timestamp: skip = 8; break;
                case BsonType.Undefined: skip = 0; break;
                default: throw new BsonInternalException("Unexpected BsonType.");
            }
            buffer.Skip(skip);

            state = BsonReaderState.Type;
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Disposes of any resources used by the reader.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected override void Dispose(
            bool disposing
        ) {
            if (disposing) {
                try {
                    Close();
                    if (disposeBuffer) {
                        buffer.Dispose();
                        buffer = null;
                    }
                } catch { } // ignore exceptions
            }
            base.Dispose(disposing);
        }
        #endregion

        #region private methods
        private BsonReaderState GetNextState() {
            switch (context.ContextType) {
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

        private int ReadSize() {
            int size = buffer.ReadInt32();
            if (size < 0) {
                var message = string.Format("Size {0} is not valid because it is negative.", size);
                throw new FileFormatException(message);
            }
            if (size > settings.MaxDocumentSize) {
                var message = string.Format("Size {0} is not valid because it is larger than MaxDocumentSize {0}.", size, settings.MaxDocumentSize);
                throw new FileFormatException(message);
            }
            return size;
        }
        #endregion
    }
}

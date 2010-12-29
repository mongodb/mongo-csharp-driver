/* Copyright 2010 10gen Inc.
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
    public class BsonBinaryReader : BsonBaseReader {
        #region private fields
        private BsonBuffer buffer; // if reading from a stream Create will have loaded the buffer
        private bool disposeBuffer;
        private BsonBinaryReaderSettings settings;
        private BsonBinaryReaderContext context;
        #endregion

        #region constructors
        public BsonBinaryReader(
            BsonBuffer buffer,
            BsonBinaryReaderSettings settings
        ) {
            this.buffer = buffer ?? new BsonBuffer();
            this.disposeBuffer = buffer == null; // only call Dispose if we allocated the buffer
            this.settings = settings;
            context = new BsonBinaryReaderContext(null, ContextType.TopLevel, 0, 0);
        }
        #endregion

        #region public properties
        public BsonBuffer Buffer {
            get { return buffer; }
        }
        #endregion

        #region public methods
        public override void Close() {
            // Close can be called on Disposed objects
            if (state != BsonReaderState.Closed) {
                state = BsonReaderState.Closed;
            }
        }

        public override BsonReaderBookmark GetBookmark() {
            return new BsonBinaryReaderBookmark(state, currentBsonType, currentName, context, buffer.Position);
        }

        #pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        public override void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType
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
            bytes = buffer.ReadBytes(size);

            state = GetNextState();
        }
        #pragma warning restore 618
        
        public override bool ReadBoolean() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBoolean", BsonType.Boolean);
            state = GetNextState();
            return buffer.ReadBoolean();
        }

        public override BsonType ReadBsonType() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state == BsonReaderState.Initial || state == BsonReaderState.Done || state == BsonReaderState.ScopeDocument) {
                // there is an implied type of Document for the top level and for scope documents
                currentBsonType = BsonType.Document;
                state = BsonReaderState.Value;
                return currentBsonType;
            }
            if (state != BsonReaderState.Type) {
                var message = string.Format("ReadBsonType cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
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
                        var message = string.Format("EndOfDocument BsonType not valid when ContextType is: '{0}'", context.ContextType);
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
                        throw new BsonInternalException("Unexpected ContextType");
                }

                return currentBsonType;
            }
        }

        public override DateTime ReadDateTime() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            state = GetNextState();
            long milliseconds = buffer.ReadInt64();
            if (milliseconds == 253402300800000) {
                // special case to avoid ArgumentOutOfRangeException in AddMilliseconds
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            } else {
                return BsonConstants.UnixEpoch.AddMilliseconds(milliseconds); // Kind = DateTimeKind.Utc
            }
        }

        public override double ReadDouble() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDouble", BsonType.Double);
            state = GetNextState();
            return buffer.ReadDouble();
        }

        public override void ReadEndArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (context.ContextType != ContextType.Array) {
                var message = string.Format("ReadEndArray cannot be called when ContextType is: {0}", context.ContextType);
                throw new InvalidOperationException(message);
            }
            if (state == BsonReaderState.Type) {
                ReadBsonType(); // will set state to EndOfArray if at end of array
            }
            if (state != BsonReaderState.EndOfArray) {
                var message = string.Format("ReadEndArray cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context = context.PopContext(buffer.Position);
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReaderState.Type; break;
                case ContextType.Document: state = BsonReaderState.Type; break;
                case ContextType.TopLevel: state = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType");
            }
        }

        public override void ReadEndDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (
                context.ContextType != ContextType.Document &&
                context.ContextType != ContextType.ScopeDocument
            ) {
                var message = string.Format("ReadEndDocument cannot be called when ContextType is: {0}", context.ContextType);
                throw new InvalidOperationException(message);
            }
            if (state == BsonReaderState.Type) {
                ReadBsonType(); // will set state to EndOfDocument if at end of document
            }
            if (state != BsonReaderState.EndOfDocument) {
                var message = string.Format("ReadEndDocument cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context = context.PopContext(buffer.Position);
            if (context != null && context.ContextType == ContextType.JavaScriptWithScope) {
                context = context.PopContext(buffer.Position); // JavaScriptWithScope
            }
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReaderState.Type; break;
                case ContextType.Document: state = BsonReaderState.Type; break;
                case ContextType.TopLevel: state = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType");
            }
        }

        public override int ReadInt32() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            state = GetNextState();
            return buffer.ReadInt32();
        }

        public override long ReadInt64() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            state = GetNextState();
            return buffer.ReadInt64();
        }

        public override string ReadJavaScript() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScript", BsonType.JavaScript);
            state = GetNextState();
            return buffer.ReadString();
        }

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

        public override void ReadMaxKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMaxKey", BsonType.MaxKey);
            state = GetNextState();
        }

        public override void ReadMinKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMinKey", BsonType.MinKey);
            state = GetNextState();
        }

        public override void ReadNull() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadNull", BsonType.Null);
            state = GetNextState();
        }

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

        public override void ReadStartArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartArray", BsonType.Array);

            var startPosition = buffer.Position; // position of size field
            var size = ReadSize();
            context = new BsonBinaryReaderContext(context, ContextType.Array, startPosition, size);
            state = BsonReaderState.Type;
        }

        public override void ReadStartDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartDocument", BsonType.Document);

            var contextType = (state == BsonReaderState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
            var startPosition = buffer.Position; // position of size field
            var size = ReadSize();
            context = new BsonBinaryReaderContext(context, contextType, startPosition, size);
            state = BsonReaderState.Type;
        }

        public override string ReadString() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadString", BsonType.String);
            state = GetNextState();
            return buffer.ReadString();
        }

        public override string ReadSymbol() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadSymbol", BsonType.Symbol);
            state = GetNextState();
            return buffer.ReadString();
        }

        public override long ReadTimestamp() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadTimestamp", BsonType.Timestamp);
            state = GetNextState();
            return buffer.ReadInt64();
        }

        public override void ReturnToBookmark(
            BsonReaderBookmark bookmark
        ) {
            var binaryReaderBookmark = (BsonBinaryReaderBookmark) bookmark;
            state = binaryReaderBookmark.State;
            currentBsonType = binaryReaderBookmark.CurrentBsonType;
            currentName = binaryReaderBookmark.CurrentName;
            context = binaryReaderBookmark.Context;
            buffer.Position = binaryReaderBookmark.Position;
        }

        public override void SkipName() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Name) {
                var message = string.Format("SkipName cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            state = BsonReaderState.Value;
        }

        public override void SkipValue() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Value) {
                var message = string.Format("SkipValue cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
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
                default: throw new BsonInternalException("Unexpected BsonType");
            }
            buffer.Skip(skip);

            state = BsonReaderState.Type;
        }
        #endregion

        #region protected methods
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
                    throw new BsonInternalException("Unexpected ContextType");
            }
        }

        private int ReadSize() {
            int size = buffer.ReadInt32();
            if (size < 0) {
                throw new FileFormatException("Size is negative");
            }
            if (size > settings.MaxDocumentSize) {
                throw new FileFormatException("Size is larger than MaxDocumentSize");
            }
            return size;
        }
        #endregion
    }
}

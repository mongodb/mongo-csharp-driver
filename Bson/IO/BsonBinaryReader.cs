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
    public class BsonBinaryReader : BsonReader {
        #region private fields
        private bool disposed = false;
        private BsonBuffer buffer; // if reading from a stream Create will have loaded the buffer
        private bool disposeBuffer;
        private BsonBinaryReaderSettings settings;
        private BsonBinaryReaderContext context;
        private BsonReadState state;
        private BsonType currentBsonType;
        #endregion

        #region constructors
        public BsonBinaryReader(
            BsonBuffer buffer,
            BsonBinaryReaderSettings settings
        ) {
            this.buffer = buffer ?? new BsonBuffer();
            this.disposeBuffer = buffer == null; // only call Dispose if we allocated the buffer
            this.settings = settings;
            context = null;
            state = BsonReadState.Initial;
            currentBsonType = BsonType.Document;
        }
        #endregion

        #region public properties
        public BsonBuffer Buffer {
            get { return buffer; }
        }

        public override BsonType CurrentBsonType {
            get { return currentBsonType; }
        }

        public override BsonReadState ReadState {
            get { return state; }
        }
        #endregion

        #region public methods
        public override void Close() {
            // Close can be called on Disposed objects
            if (state != BsonReadState.Closed) {
                state = BsonReadState.Closed;
            }
        }

        public override void Dispose() {
            if (!disposed) {
                Close();
                if (disposeBuffer) {
                    buffer.Dispose();
                    buffer = null;
                }
                disposed = true;
            }
        }

        // looks for an element of the given name and leaves the reader positioned at the value
        // or at EndOfDocument if the element is not found
        public override bool FindElement(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (state != BsonReadState.Type) {
                var message = string.Format("FindElement cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            BsonType bsonType;
            while ((bsonType = ReadBsonType()) != BsonType.EndOfDocument) {
                var elementName = ReadName();
                if (elementName == name) {
                    return true;
                }
                SkipValue();
            }

            return false;
        }

        // this is like ReadString but scans ahead to find a string element with the desired name
        // it leaves the reader positioned just after the value (i.e. at the BsonType of the next element)
        // or at EndOfDocument if the element is not found
        public override string FindString(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (state != BsonReadState.Type) {
                var message = string.Format("FindString cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            BsonType bsonType;
            while ((bsonType = ReadBsonType()) != BsonType.EndOfDocument) {
                var elementName = ReadName();
                if (bsonType == BsonType.String && elementName == name) {
                    return ReadString();
                } else {
                    SkipValue();
                }
            }

            return null;
        }

        public override BsonBinaryReaderBookmark GetBookmark() {
            return new BsonBinaryReaderBookmark(context, state, currentBsonType, buffer.Position);
        }

        #pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        public override void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
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

            state = BsonReadState.Type;
        }
        #pragma warning restore 618
        
        public override void ReadBinaryData(
            string name,
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
            VerifyName(name);
            ReadBinaryData(out bytes, out subType);
        }

        public override bool ReadBoolean() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadBoolean", BsonType.Boolean);
            state = BsonReadState.Type;
            return buffer.ReadBoolean();
        }

        public override bool ReadBoolean(
            string name
        ) {
            VerifyName(name);
            return ReadBoolean();
        }

        public override BsonType ReadBsonType() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (state != BsonReadState.Type) {
                var message = string.Format("ReadBsonType cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            currentBsonType = buffer.ReadBsonType(); // set currentBsonType before state
            state = (currentBsonType == BsonType.EndOfDocument) ? BsonReadState.EndOfDocument : BsonReadState.Name;
            return currentBsonType;
        }

        public override DateTime ReadDateTime() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            state = BsonReadState.Type;
            long milliseconds = buffer.ReadInt64();
            if (milliseconds == 253402300800000) {
                // special case to avoid ArgumentOutOfRangeException in AddMilliseconds
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            } else {
                return BsonConstants.UnixEpoch.AddMilliseconds(milliseconds); // Kind = DateTimeKind.Utc
            }
        }

        public override DateTime ReadDateTime(
            string name
        ) {
            VerifyName(name);
            return ReadDateTime();
        }

        public override double ReadDouble() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadDouble", BsonType.Double);
            state = BsonReadState.Type;
            return buffer.ReadDouble();
        }

        public override double ReadDouble(
            string name
        ) {
            VerifyName(name);
            return ReadDouble();
        }

        public override void ReadEndArray() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (state == BsonReadState.Type && buffer.PeekByte() == 0) {
                buffer.Skip(1); // automatically advance to EndOfDocument state
                state = BsonReadState.EndOfDocument;
            }
            if (state != BsonReadState.EndOfDocument) {
                var message = string.Format("ReadEndArray cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }
            if (context.ContextType != ContextType.Array) {
                var message = string.Format("ReadEndArray cannot be called when ContextType is: {0}", context.ContextType);
                throw new InvalidOperationException(message);
            }

            context = context.PopContext(buffer.Position);
            state = BsonReadState.Type;
        }

        public override void ReadEndDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (state == BsonReadState.Type && buffer.PeekByte() == 0) {
                buffer.Skip(1); // automatically advance to EndOfDocument state
                state = BsonReadState.EndOfDocument;
            }
            if (state != BsonReadState.EndOfDocument) {
                var message = string.Format("ReadEndDocument cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            if (context.ContextType == ContextType.Document) {
                context = context.PopContext(buffer.Position); // Document
            } else if (context.ContextType == ContextType.ScopeDocument) {
                context = context.PopContext(buffer.Position); // ScopeDocument
                context = context.PopContext(buffer.Position); // JavaScriptWithScope
            } else {
                var message = string.Format("ReadEndDocument cannot be called when ContextType is: {0}", context.ContextType);
                throw new InvalidOperationException(message);
            }
            state = (context == null) ? BsonReadState.Done : BsonReadState.Type;
        }

        public override int ReadInt32() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            state = BsonReadState.Type;
            return buffer.ReadInt32();
        }

        public override int ReadInt32(
            string name
        ) {
            VerifyName(name);
            return ReadInt32();
        }

        public override long ReadInt64() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            state = BsonReadState.Type;
            return buffer.ReadInt64();
        }

        public override long ReadInt64(
            string name
         ) {
            VerifyName(name);
            return ReadInt64();
        }

        public override string ReadJavaScript() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadJavaScript", BsonType.JavaScript);
            state = BsonReadState.Type;
            return buffer.ReadString();
        }

        public override string ReadJavaScript(
            string name
        ) {
            VerifyName(name);
            return ReadJavaScript();
        }

        public override string ReadJavaScriptWithScope() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadJavaScriptWithScope", BsonType.JavaScriptWithScope);

            var startPosition = buffer.Position; // position of size field
            var size = ReadSize();
            context = new BsonBinaryReaderContext(context, ContextType.JavaScriptWithScope, startPosition, size);
            var code = buffer.ReadString();

            state = BsonReadState.ScopeDocument;
            return code;
        }

        public override string ReadJavaScriptWithScope(
            string name
        ) {
            VerifyName(name);
            return ReadJavaScriptWithScope();
        }

        public override void ReadMaxKey() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadMaxKey", BsonType.MaxKey);
            state = BsonReadState.Type;
        }

        public override void ReadMaxKey(
            string name
        ) {
            VerifyName(name);
            ReadMaxKey();
        }

        public override void ReadMinKey() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadMinKey", BsonType.MinKey);
            state = BsonReadState.Type;
        }

        public override void ReadMinKey(
            string name
        ) {
            VerifyName(name);
            ReadMinKey();
        }

        public override string ReadName() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (state != BsonReadState.Name) {
                var message = string.Format("ReadName cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            state = BsonReadState.Value;
            return buffer.ReadCString();
        }

        public override void ReadNull() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadNull", BsonType.Null);
            state = BsonReadState.Type;
        }

        public override void ReadNull(
            string name
        ) {
            VerifyName(name);
            ReadNull();
        }

        public override void ReadObjectId(
            out int timestamp,
            out long machinePidIncrement
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadObjectId", BsonType.ObjectId);
            buffer.ReadObjectId(out timestamp, out machinePidIncrement);
            state = BsonReadState.Type;
        }

        public override void ReadObjectId(
            string name,
            out int timestamp,
            out long machinePidIncrement
        ) {
            VerifyName(name);
            ReadObjectId(out timestamp, out machinePidIncrement);
        }

        public override void ReadRegularExpression(
            out string pattern,
            out string options
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadRegularExpression", BsonType.RegularExpression);
            pattern = buffer.ReadCString();
            options = buffer.ReadCString();
            state = BsonReadState.Type;
        }

        public override void ReadRegularExpression(
            string name,
            out string pattern,
            out string options
        ) {
            VerifyName(name);
            ReadRegularExpression(out pattern, out options);
        }

        public override void ReadStartArray() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (state != BsonReadState.Value || currentBsonType != BsonType.Array) {
                string message = string.Format("ReadStartArray cannot be called when ReadState is: {0} and BsonType is: {1}", state, currentBsonType);
                throw new InvalidOperationException(message);
            }

            var startPosition = buffer.Position; // position of size field
            var size = ReadSize();
            context = new BsonBinaryReaderContext(context, ContextType.Array, startPosition, size);
            state = BsonReadState.Type;
        }

        public override void ReadStartDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (
                state != BsonReadState.Initial &&
                state != BsonReadState.Done &&
                state != BsonReadState.ScopeDocument &&
                (state != BsonReadState.Value || currentBsonType != BsonType.Document)
            ) {
                string message = string.Format("ReadStartDocument cannot be called when ReadState is: {0} and BsonType is: {1}", state, currentBsonType);
                throw new InvalidOperationException(message);
            }

            var contextType = (state == BsonReadState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
            var startPosition = buffer.Position; // position of size field
            var size = ReadSize();
            context = new BsonBinaryReaderContext(context, contextType, startPosition, size);
            state = BsonReadState.Type;
        }

        public override string ReadString() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadString", BsonType.String);
            state = BsonReadState.Type;
            return buffer.ReadString();
        }

        public override string ReadString(
            string name
         ) {
            VerifyName(name);
            return ReadString();
        }

        public override string ReadSymbol() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadSymbol", BsonType.Symbol);
            state = BsonReadState.Type;
            return buffer.ReadString();
        }

        public override string ReadSymbol(
            string name
         ) {
            VerifyName(name);
            return ReadSymbol();
        }

        public override long ReadTimestamp() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadTimestamp", BsonType.Timestamp);
            state = BsonReadState.Type;
            return buffer.ReadInt64();
        }

        public override long ReadTimestamp(
            string name
         ) {
            VerifyName(name);
            return ReadTimestamp();
        }

        public override void ReturnToBookmark(
            BsonBinaryReaderBookmark bookmark
        ) {
            context = bookmark.Context;
            state = bookmark.State;
            currentBsonType = bookmark.CurrentBsonType;
            buffer.Position = bookmark.Position;
        }

        public override void SkipName() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (state != BsonReadState.Name) {
                var message = string.Format("SkipName cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.SkipCString();
            state = BsonReadState.Value;
        }

        public override void SkipValue() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (state != BsonReadState.Value) {
                var message = string.Format("SkipValue cannot be called when ReadState is: {0}", state);
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

            state = BsonReadState.Type;
        }
        #endregion

        #region private methods
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

        private void VerifyBsonType(
            string methodName,
            BsonType requiredBsonType
        ) {
            if (state != BsonReadState.Value) {
                var message = string.Format("{0} cannot be called when ReadState is: {1}", methodName, state);
                throw new InvalidOperationException(message);
            }
            if (currentBsonType != requiredBsonType) {
                var message = string.Format("{0} cannot be called when BsonType is: {1}", methodName, currentBsonType);
                throw new InvalidOperationException(message);
            }
        }

        private void VerifyName(
            string expectedName
        ) {
            ReadBsonType();
            var actualName = ReadName();
            if (actualName != expectedName) {
                var message = string.Format("Element name is not: {0}", expectedName);
                throw new FileFormatException(message);
            }
        }
        #endregion
    }
}

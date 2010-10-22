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
        #endregion

        #region constructors
        public BsonBinaryReader(
            BsonBuffer buffer,
            BsonBinaryReaderSettings settings
        ) {
            this.buffer = buffer ?? new BsonBuffer();
            this.disposeBuffer = buffer == null; // only call Dispose if we allocated the buffer
            this.settings = settings;
            context = new BsonBinaryReaderContext(null, BsonReadState.Initial);
        }
        #endregion

        #region public properties
        public BsonBuffer Buffer {
            get { return buffer; }
        }

        public override BsonReadState ReadState {
            get { return context.ReadState; }
        }
        #endregion

        #region public methods
        public override void Close() {
            // Close can be called on Disposed objects
            if (context.ReadState != BsonReadState.Closed) {
                context = new BsonBinaryReaderContext(null, BsonReadState.Closed);
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

        // this is like ReadString but scans ahead to find a string element with the desired name
        public override string FindString(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if ((context.ReadState & BsonReadState.Document) == 0) {
                string message = string.Format("FindString cannot be called when ReadState is: {0}", context.ReadState);
                throw new InvalidOperationException(message);
            }

            BsonType bsonType;
            while ((bsonType = buffer.ReadBsonType()) != BsonType.EndOfDocument) {
                var elementName = buffer.ReadCString();
                if (bsonType == BsonType.String && elementName == name) {
                    return buffer.ReadString();
                } else {
                    buffer.Position += GetValueSize(bsonType); // skip over value
                }
            }
            return null;
        }

        public override bool HasElement() {
            BsonType bsonType;
            return HasElement(out bsonType);
        }

        public override bool HasElement(
            out BsonType bsonType
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if ((context.ReadState & BsonReadState.Document) == 0) {
                string message = string.Format("HasElement cannot be called when ReadState is: {0}", context.ReadState);
                throw new InvalidOperationException(message);
            }
            bsonType = buffer.PeekBsonType();
            return bsonType != BsonType.EndOfDocument;
        }

        public override bool HasElement(
            out BsonType bsonType,
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if ((context.ReadState & BsonReadState.Document) == 0) {
                string message = string.Format("HasElement cannot be called when ReadState is: {0}", context.ReadState);
                throw new InvalidOperationException(message);
            }

            int currentPosition = buffer.Position;
            bsonType = buffer.ReadBsonType();
            if (bsonType == BsonType.EndOfDocument) {
                name = null;
            } else {
                name = buffer.ReadCString();
            }
            buffer.Position = currentPosition;

            return bsonType != BsonType.EndOfDocument;
        }

        public override BsonType PeekBsonType() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if ((context.ReadState & BsonReadState.Document) == 0) {
                string message = string.Format("PeekBsonType cannot be called when ReadState is: {0}", context.ReadState);
                throw new InvalidOperationException(message);
            }
            return buffer.PeekBsonType();
        }

        public override void PopBookmark() {
            var bookmark = context.GetBookmark();
            buffer.Position = bookmark.BookmarkPosition;
            context = bookmark.BookmarkParentContext;
        }

        public override void PushBookmark() {
            context = context.CreateBookmark();
            context.BookmarkPosition = buffer.Position;
        }

        public override void ReadArrayName(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadArrayName", BsonType.Array);
            name = buffer.ReadCString();
            context = new BsonBinaryReaderContext(context, BsonReadState.Array);
            context = new BsonBinaryReaderContext(context, BsonReadState.StartDocument);
        }

        public override void ReadArrayName(
            string expectedName
        ) {
            string actualName;
            ReadArrayName(out actualName);
            VerifyName(actualName, expectedName);
        }

        public override void ReadBinaryData(
            out string name,
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadBinaryData", BsonType.Binary);
            name = buffer.ReadCString();
            ReadBinaryDataHelper(out bytes, out subType);
        }

        public override void ReadBinaryData(
            string expectedName,
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
            string actualName;
            ReadBinaryData(out actualName, out bytes, out subType);
            VerifyName(actualName, expectedName);
        }

        public override bool ReadBoolean(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadBoolean", BsonType.Boolean);
            name = buffer.ReadCString();
            return buffer.ReadBoolean();
        }

        public override bool ReadBoolean(
            string expectedName
        ) {
            string actualName;
            var value = ReadBoolean(out actualName);
            VerifyName(actualName, expectedName);
            return value;
        }

        public override DateTime ReadDateTime(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            name = buffer.ReadCString();
            long milliseconds = buffer.ReadInt64();
            if (milliseconds == 253402300800000) {
                // special case to avoid ArgumentOutOfRangeException in AddMilliseconds
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            } else {
                return BsonConstants.UnixEpoch.AddMilliseconds(milliseconds); // Kind = DateTimeKind.Utc
            }
        }

        public override DateTime ReadDateTime(
            string expectedName
        ) {
            string actualName;
            var value = ReadDateTime(out actualName);
            VerifyName(actualName, expectedName);
            return value;
        }

        public override void ReadDocumentName(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadDocumentName", BsonType.Document);
            name = buffer.ReadCString();
            context = new BsonBinaryReaderContext(context, BsonReadState.EmbeddedDocument);
            context = new BsonBinaryReaderContext(context, BsonReadState.StartDocument);
        }

        public override void ReadDocumentName(
            string expectedName
        ) {
            string actualName;
            ReadDocumentName(out actualName);
            VerifyName(actualName, expectedName);
        }

        public override double ReadDouble(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadDouble", BsonType.Double);
            name = buffer.ReadCString();
            return buffer.ReadDouble();
        }

        public override double ReadDouble(
            string expectedName
        ) {
            string actualName;
            var value = ReadDouble(out actualName);
            VerifyName(actualName, expectedName);
            return value;
        }

        public override void ReadEndDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadEndDocument", BsonType.EndOfDocument);
            if (context.Size != buffer.Position - context.StartPosition) {
                throw new FileFormatException("Document size was incorrect");
            }
            context = context.ParentContext;

            if (context.ReadState == BsonReadState.JavaScriptWithScope) {
                if (context.Size != buffer.Position - context.StartPosition) {
                    throw new FileFormatException("JavaScriptWithScope size was incorrect");
                }
                context = context.ParentContext;
            }

            if (context.ReadState == BsonReadState.Initial) {
                context = new BsonBinaryReaderContext(null, BsonReadState.Done);
            }
        }

        public override int ReadInt32(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            name = buffer.ReadCString();
            return buffer.ReadInt32();
        }

        public override int ReadInt32(
            string expectedName
        ) {
            string actualName;
            var value = ReadInt32(out actualName);
            VerifyName(actualName, expectedName);
            return value;
        }

        public override long ReadInt64(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            name = buffer.ReadCString();
            return buffer.ReadInt64();
        }

        public override long ReadInt64(
            string expectedName
        ) {
            string actualName;
            var value = ReadInt64(out actualName);
            VerifyName(actualName, expectedName);
            return value;
        }

        public override string ReadJavaScript(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadJavaScript", BsonType.JavaScript);
            name = buffer.ReadCString();
            return buffer.ReadString();
        }

        public override string ReadJavaScript(
            string expectedName
        ) {
            string actualName;
            var code = ReadJavaScript(out actualName);
            VerifyName(actualName, expectedName);
            return code;
        }

        public override string ReadJavaScriptWithScope(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadJavaScriptWithScope", BsonType.JavaScriptWithScope);
            name = buffer.ReadCString();
            context = new BsonBinaryReaderContext(context, BsonReadState.JavaScriptWithScope);
            context.StartPosition = buffer.Position;
            context.Size = ReadSize();
            var code = buffer.ReadString();
            context = new BsonBinaryReaderContext(context, BsonReadState.ScopeDocument);
            context = new BsonBinaryReaderContext(context, BsonReadState.StartDocument);
            return code;
        }

        public override string ReadJavaScriptWithScope(
            string expectedName
        ) {
            string actualName;
            var code = ReadJavaScriptWithScope(out actualName);
            VerifyName(actualName, expectedName);
            return code;
        }

        public override void ReadMaxKey(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadMaxKey", BsonType.MaxKey);
            name = buffer.ReadCString();
        }

        public override void ReadMaxKey(
            string expectedName
        ) {
            string actualName;
            ReadMaxKey(out actualName);
            VerifyName(actualName, expectedName);
        }

        public override void ReadMinKey(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadMinKey", BsonType.MinKey);
            name = buffer.ReadCString();
        }

        public override void ReadMinKey(
            string expectedName
        ) {
            string actualName;
            ReadMinKey(out actualName);
            VerifyName(actualName, expectedName);
        }

        public override void ReadNull(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadNull", BsonType.Null);
            name = buffer.ReadCString();
        }

        public override void ReadNull(
            string expectedName
        ) {
            string actualName;
            ReadNull(out actualName);
            VerifyName(actualName, expectedName);
        }

        public override void ReadObjectId(
            out string name,
            out int timestamp,
            out long machinePidIncrement
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadObjectId", BsonType.ObjectId);
            name = buffer.ReadCString();
            buffer.ReadObjectId(out timestamp, out machinePidIncrement);
        }

        public override void ReadObjectId(
            string expectedName,
            out int timestamp,
            out long machinePidIncrement
        ) {
            string actualName;
            ReadObjectId(out actualName, out timestamp, out machinePidIncrement);
            VerifyName(actualName, expectedName);
        }

        public override void ReadRegularExpression(
            out string name,
            out string pattern,
            out string options
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadRegularExpression", BsonType.RegularExpression);
            name = buffer.ReadCString();
            pattern = buffer.ReadCString();
            options = buffer.ReadCString();
        }

        public override void ReadRegularExpression(
            string expectedName,
            out string pattern,
            out string options
        ) {
            string actualName;
            ReadRegularExpression(out actualName, out pattern, out options);
            VerifyName(actualName, expectedName);
        }

        public override void ReadStartDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState == BsonReadState.StartDocument) {
                context = context.ParentContext;
            } else  if (context.ReadState == BsonReadState.Initial || context.ReadState == BsonReadState.Done) {
                context = new BsonBinaryReaderContext(context, BsonReadState.Document);
            } else {
                string message = string.Format("ReadStartDocument cannot be called when ReadState is: {0}", context.ReadState);
                throw new InvalidOperationException(message);
            }
            context.StartPosition = buffer.Position;
            context.Size = ReadSize();
        }

        public override string ReadString(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadString", BsonType.String);
            name = buffer.ReadCString();
            return buffer.ReadString();
        }

        public override string ReadString(
            string expectedName
        ) {
            string actualName;
            var value = ReadString(out actualName);
            VerifyName(actualName, expectedName);
            return value;
        }

        public override string ReadSymbol(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadSymbol", BsonType.Symbol);
            name = buffer.ReadCString();
            return buffer.ReadString();
        }

        public override string ReadSymbol(
            string expectedName
        ) {
            string actualName;
            var value = ReadSymbol(out actualName);
            VerifyName(actualName, expectedName);
            return value;
        }

        public override long ReadTimestamp(
            out string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            VerifyBsonType("ReadTimestamp", BsonType.Timestamp);
            name = buffer.ReadCString();
            return buffer.ReadInt64();
        }

        public override long ReadTimestamp(
            string expectedName
        ) {
            string actualName;
            var value = ReadTimestamp(out actualName);
            VerifyName(actualName, expectedName);
            return value;
        }

        public override void VerifyString(
            string expectedName,
            string expectedValue
        ) {
            string actualName;
            var actualValue = ReadString(out actualName);
            VerifyName(actualName, expectedName);
            if (actualValue != expectedValue) {
                string message = string.Format("Value for element {0} is not: \"{1}\"", expectedName, expectedValue);
                throw new FileFormatException(message);
            }
        }

        public override void SkipElement() {
            SkipElement(null);
        }

        public override void SkipElement(
            string expectedName
        ) {
            if ((context.ReadState & BsonReadState.Document) == 0) {
                string message = string.Format("SkipElement cannot be called when ReadState is: {1}", context.ReadState);
                throw new InvalidOperationException(message);
            }
            var bsonType = buffer.ReadBsonType();
            var name = buffer.ReadCString();
            if (expectedName != null && name != expectedName) {
                var message = string.Format("Element name was not: {0}", expectedName);
                throw new FileFormatException(message);
            }
            buffer.Position += GetValueSize(bsonType); // skip over value
        }
        #endregion

        #region private methods
        private int GetValueSize(
            BsonType bsonType
        ) {
            int size;
            switch (bsonType) {
                case BsonType.Array: size = PeekSize(); break;
                case BsonType.Binary: size = PeekSize() + 5; break;
                case BsonType.Boolean: size = 1; break;
                case BsonType.DateTime: size = 8; break;
                case BsonType.Document: size = PeekSize(); break;
                case BsonType.Double: size = 8; break;
                case BsonType.Int32: size = 4; break;
                case BsonType.Int64: size = 8; break;
                case BsonType.JavaScript: size = PeekSize() + 4; break;
                case BsonType.JavaScriptWithScope: size = PeekSize(); break;
                case BsonType.MaxKey: size = 0; break;
                case BsonType.MinKey: size = 0; break;
                case BsonType.Null: size = 0; break;
                case BsonType.ObjectId: size = 12; break;
                case BsonType.RegularExpression: var p = buffer.Position; buffer.ReadCString(); buffer.ReadCString(); size = buffer.Position - p; buffer.Position = p; break;
                case BsonType.String: size = PeekSize() + 4; break;
                case BsonType.Symbol: size = PeekSize() + 4; break;
                case BsonType.Timestamp: size = 8; break;
                default: throw new BsonInternalException("Unexpected BsonType");
            }
            return size;
        }

        private int PeekSize() {
            var size = ReadSize();
            buffer.Position -= 4;
            return size;
        }

        #pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        private void ReadBinaryDataHelper(
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
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
        }
        #pragma warning restore 618

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
            if ((context.ReadState & BsonReadState.Document) == 0) {
                string message = string.Format("{0} cannot be called when ReadState is: {1}", methodName, context.ReadState);
                throw new InvalidOperationException(message);
            }
            var bsonType = buffer.ReadBsonType();
            if (bsonType != requiredBsonType) {
                string message = string.Format("BSON type is not {0}", requiredBsonType);
                throw new FileFormatException(message);
            }
        }

        private void VerifyName(
            string actualName,
            string expectedName
        ) {
            if (actualName != expectedName) {
                string message = string.Format("Element name is not {0}", expectedName);
                throw new FileFormatException(message);
            }
        }
        #endregion
    }
}

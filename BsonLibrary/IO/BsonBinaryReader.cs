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

namespace MongoDB.BsonLibrary.IO {
    public class BsonBinaryReader : BsonReader {
        #region private fields
        private bool disposed = false;
        private BsonBuffer buffer;
        private bool disposeBuffer;
        private BsonBinaryReaderSettings settings;
        private BsonReaderContext context;
        private BsonType bsonType;
        #endregion

        #region constructors
        public BsonBinaryReader(
            BsonBuffer buffer,
            BsonBinaryReaderSettings settings
        ) {
            this.buffer = buffer ?? new BsonBuffer();
            this.disposeBuffer = buffer != null;
            this.settings = settings;
            context = new BsonReaderContext(null, 0, 0, 0, BsonReadState.Initial);
        }
        #endregion

        #region public properties
        public override BsonReaderDocumentType DocumentType {
            get { return context.DocumentType; }
        }

        public override BsonReadState ReadState {
            get { return context.ReadState; }
        }
        #endregion

        #region public methods
        public override void Close() {
            if (context.ReadState != BsonReadState.Closed) {
                if (settings.CloseInput) {
                    // TODO: close stream?
                }
                context = new BsonReaderContext(null, 0, 0, 0, BsonReadState.Closed);
            }
        }

        public override void Dispose() {
            if (!disposed) {
                Close();
                if (disposeBuffer) {
                    buffer.Dispose();
                }
                buffer = null;
                disposed = true;
            }
        }

        // verifies that subType is Binary (possibly automatically converted from OldBinary)
        public override byte[] ReadBinaryData() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Binary) {
                throw new InvalidOperationException("ReadBinaryData can only be called when ReadState is Value and BsonType is Binary");
            }
            BsonBinarySubType subType;
            byte[] bytes = ReadBinaryDataHelper(out subType);
            if (subType != BsonBinarySubType.Binary) {
                throw new InvalidOperationException("Binary sub type is not Binary");
            }
            context.ReadState = BsonReadState.Type;
            return bytes;
        }

        public override byte[] ReadBinaryData(
            out BsonBinarySubType subType
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Binary) {
                throw new InvalidOperationException("ReadBinaryData can only be called when ReadState is Value and BsonType is Binary");
            }
            context.ReadState = BsonReadState.Type;
            return ReadBinaryDataHelper(out subType);
        }

        public override bool ReadBoolean() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Boolean) {
                throw new InvalidOperationException("ReadBoolean can only be called when ReadState is Value and BsonType is Boolean");
            }
            context.ReadState = BsonReadState.Type;
            return buffer.ReadBoolean();
        }

        public override BsonType ReadBsonType() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Type) {
                throw new InvalidOperationException("ReadBsonType can only be called when ReadState is Type");
            }
            bsonType = (BsonType) buffer.ReadByte();
            if (!Enum.IsDefined(typeof(BsonType), bsonType)) {
                string message = string.Format("Invalid BsonType: {0}", (int) bsonType);
                throw new FileFormatException(message);
            }
            if (bsonType == BsonType.EndOfDocument) {
                switch (context.DocumentType) {
                    case BsonReaderDocumentType.Document: context.ReadState = BsonReadState.EndOfDocument; break;
                    case BsonReaderDocumentType.EmbeddedDocument: context.ReadState = BsonReadState.EndOfEmbeddedDocument; break;
                    case BsonReaderDocumentType.ArrayDocument: context.ReadState = BsonReadState.EndOfArray; break;
                    case BsonReaderDocumentType.ScopeDocument: context.ReadState = BsonReadState.EndOfScopeDocument; break;
                    default: throw new BsonInternalException("Unexpected DocumentType");
                }
            } else {
                context.ReadState = BsonReadState.Name;
            }
            return bsonType;
        }

        public override DateTime ReadDateTime() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.DateTime) {
                throw new InvalidOperationException("ReadDateTime can only be called when ReadState is Value and BsonType is DateTime");
            }
            context.ReadState = BsonReadState.Type;
            long milliseconds = buffer.ReadInt64();
            return DateTime.SpecifyKind(Bson.UnixEpoch.AddMilliseconds(milliseconds), DateTimeKind.Utc);
        }

        public override double ReadDouble() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Double) {
                throw new InvalidOperationException("ReadDouble can only be called when ReadState is Value and BsonType is Double");
            }
            context.ReadState = BsonReadState.Type;
            return buffer.ReadDouble();
        }

        public override void ReadEndArray() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.EndOfArray) {
                throw new InvalidOperationException("ReadEndArray can only be called when ReadState is EndOfArray");
            }
            if (context.Size != buffer.Position - context.StartPosition) {
                throw new FileFormatException("Document size was incorrect");
            }
            context = context.ParentContext;
        }

        public override void ReadEndDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.EndOfDocument) {
                throw new InvalidOperationException("ReadEndDocument can only be called when ReadState is EndOfDocument");
            }
            if (context.Size != buffer.Position - context.StartPosition) {
                throw new FileFormatException("Document size was incorrect");
            }
            context = context.ParentContext;
        }

        public override void ReadEndEmbeddedDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.EndOfEmbeddedDocument) {
                throw new InvalidOperationException("ReadEndEmbeddedDocument can only be called when ReadState is EndOfEmbeddedDocument");
            }
            if (context.Size != buffer.Position - context.StartPosition) {
                throw new FileFormatException("Document size was incorrect");
            }
            context = context.ParentContext;
        }

        public override void ReadEndJavaScriptWithScope() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.EndOfScopeDocument) {
                throw new InvalidOperationException("ReadEndJavaScriptWithScope can only be called when ReadState is EndOfScopeDocument");
            }
            if (context.Size != buffer.Position - context.StartPosition) {
                throw new FileFormatException("Document size was incorrect");
            }
            context = context.ParentContext;
            if (context.Size != buffer.Position - context.StartPosition) {
                throw new FileFormatException("Document size was incorrect");
            }
            context = context.ParentContext;
        }

        public override Guid ReadGuid() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Binary) {
                throw new InvalidOperationException("ReadGuid can only be called when ReadState is Value and BsonType is Binary");
            }
            BsonBinarySubType subType;
            byte[] bytes = ReadBinaryData(out subType);
            if (subType != BsonBinarySubType.Uuid) {
                throw new FileFormatException("Binary sub type is not Uuid");
            }
            if (bytes.Length != 16) {
                throw new FileFormatException("Size of Uuid value is not 16");
            }
            context.ReadState = BsonReadState.Type;
            return new Guid(bytes);
        }

        public override int ReadInt32() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Int32) {
                throw new InvalidOperationException("ReadInt32 can only be called when ReadState is Value and BsonType is Int32");
            }
            context.ReadState = BsonReadState.Type;
            return buffer.ReadInt32();
        }

        public override long ReadInt64() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Int64) {
                throw new InvalidOperationException("ReadInt64 can only be called when ReadState is Value and BsonType is Int64");
            }
            context.ReadState = BsonReadState.Type;
            return buffer.ReadInt64();
        }

        public override string ReadJavaScript() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.JavaScript) {
                throw new InvalidOperationException("ReadJavaScript can only be called when ReadState is Value and BsonType is JavaScript");
            }
            context.ReadState = BsonReadState.Type;
            return buffer.ReadString();
        }

        public override void ReadMaxKey() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.MaxKey) {
                throw new InvalidOperationException("ReadMaxKey can only be called when ReadState is Value and BsonType is MaxKey");
            }
            context.ReadState = BsonReadState.Type;
        }

        public override void ReadMinKey() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.MinKey) {
                throw new InvalidOperationException("ReadMinKey can only be called when ReadState is Value and BsonType is MinKey");
            }
            context.ReadState = BsonReadState.Type;
        }

        public override string ReadName() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Name) {
                throw new InvalidOperationException("ReadName can only be called when ReadState is Name");
            }
            context.ReadState = BsonReadState.Value;
            return buffer.ReadCString(); 
        }

        public override void ReadNull() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Null) {
                throw new InvalidOperationException("ReadNull can only be called when ReadState is Value and BsonType is Null");
            }
            context.ReadState = BsonReadState.Type;
        }

        public override void ReadObjectId(
            out int timestamp,
            out long machinePidIncrement
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.ObjectId) {
                throw new InvalidOperationException("ReadObjectId can only be called when ReadState is Value and BsonType is ObjectId");
            }
            context.ReadState = BsonReadState.Type;
            buffer.ReadObjectId(out timestamp, out machinePidIncrement);
        }

        public override void ReadRegularExpression(
            out string pattern,
            out string options
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.RegularExpression) {
                throw new InvalidOperationException("ReadRegularExpression can only be called when ReadState is Value and BsonType is RegularExpression");
            }
            context.ReadState = BsonReadState.Type;
            pattern = buffer.ReadCString();
            options = buffer.ReadCString();
        }

        public override void ReadStartArray() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Array) {
                throw new InvalidOperationException("ReadStartArray can only be called when ReadState is Value and BsonType is Array");
            }
            int startPosition = buffer.Position;
            int size = ReadSize();
            context.ReadState = BsonReadState.Type;
            context = new BsonReaderContext(context, startPosition, size, BsonReaderDocumentType.ArrayDocument, BsonReadState.Type);
        }

        public override void ReadStartDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Initial && context.ReadState != BsonReadState.Done) {
                throw new InvalidOperationException("ReadStartDocument can only be called when ReadState is Initial or Done");
            }
            int startPosition = buffer.Position;
            int size = ReadSize();
            context.ReadState = BsonReadState.Done;
            context = new BsonReaderContext(context, startPosition, size, BsonReaderDocumentType.Document, BsonReadState.Type);
        }

        public override void ReadStartEmbeddedDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Document) {
                throw new InvalidOperationException("ReadStartEmbeddedDocument can only be called when ReadState is Value and BsonType is Document");
            }
            int startPosition = buffer.Position;
            int size = ReadSize();
            context.ReadState = BsonReadState.Type;
            context = new BsonReaderContext(context, startPosition, size, BsonReaderDocumentType.EmbeddedDocument, BsonReadState.Type);
        }

        public override void ReadStartJavaScriptWithScope(
            out string code
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.JavaScriptWithScope) {
                throw new InvalidOperationException("ReadStartJavaScriptWithScope can only be called when ReadState is Value and BsonType is JavaScriptWithScope");
            }
            int startPosition = buffer.Position;
            int size = ReadSize();
            context.ReadState = BsonReadState.Type;
            context = new BsonReaderContext(context, startPosition, size, 0, 0);
            code = buffer.ReadString();
            startPosition = buffer.Position;
            size = ReadSize();
            context = new BsonReaderContext(context, startPosition, size, BsonReaderDocumentType.ScopeDocument, BsonReadState.Type);
        }

        public override string ReadString() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.String) {
                throw new InvalidOperationException("ReadString can only be called when ReadState is Value and BsonType is String");
            }
            context.ReadState = BsonReadState.Type;
            return buffer.ReadString();
        }

        public override string ReadSymbol() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Symbol) {
                throw new InvalidOperationException("ReadSymbol can only be called when ReadState is Value and BsonType is Symbol");
            }
            context.ReadState = BsonReadState.Type;
            return buffer.ReadString();
        }

        public override long ReadTimestamp() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryReader"); }
            if (context.ReadState != BsonReadState.Value || bsonType != BsonType.Timestamp) {
                throw new InvalidOperationException("ReadTimestamp can only be called when ReadState is Value and BsonType is Timestamp");
            }
            context.ReadState = BsonReadState.Type;
            return buffer.ReadInt64();
        }
        #endregion

        #region private methods
        #pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        private byte[] ReadBinaryDataHelper(
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
            return buffer.ReadBytes(size);
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
        #endregion
    }
}

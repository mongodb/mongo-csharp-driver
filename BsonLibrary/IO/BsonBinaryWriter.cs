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
    public class BsonBinaryWriter : BsonWriter {
        #region private fields
        private bool disposed = false;
        private Stream stream; // can be null if we're only writing to the buffer
        private BsonBuffer buffer;
        private bool disposeBuffer;
        private BsonBinaryWriterSettings settings;
        private BsonBinaryWriterContext context;
        #endregion

        #region constructors
        public BsonBinaryWriter(
            Stream stream,
            BsonBuffer buffer,
            BsonBinaryWriterSettings settings
        ) {
            this.stream = stream;
            this.buffer = buffer ?? new BsonBuffer();
            this.disposeBuffer = buffer != null; // only call Dispose if we allocated the buffer
            this.settings = settings;
            context = new BsonBinaryWriterContext(null, BsonWriteState.Initial);
        }
        #endregion

        #region public properties
        public BsonBuffer Buffer {
            get { return buffer; }
        }

        public override BsonWriteState WriteState {
            get { return context.WriteState; }
        }
        #endregion

        #region public methods
        public override void Close() {
            // Close can be called on Disposed objects
            if (context.WriteState != BsonWriteState.Closed) {
                if (context.WriteState == BsonWriteState.Done) {
                    Flush();
                }
                if (stream != null && settings.CloseOutput) {
                    stream.Close();
                }
                context = new BsonBinaryWriterContext(null, BsonWriteState.Closed);
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

        public override void Flush() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (context.WriteState == BsonWriteState.Closed) {
                throw new InvalidOperationException("Flush called on closed BsonWriter");
            }
            if (context.WriteState != BsonWriteState.Done) {
                throw new InvalidOperationException("Flush called before BsonBinaryWriter was finished writing to buffer");
            }
            if (stream != null) {
                buffer.WriteTo(stream);
                stream.Flush();
                buffer.Clear(); // only clear the buffer if we have written it to a stream
            }
        }

        public override void WriteArrayName(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteStartArray can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.Array);
            buffer.WriteCString(name);
            context = new BsonBinaryWriterContext(context, BsonWriteState.Array);
            context = new BsonBinaryWriterContext(context, BsonWriteState.StartDocument);
        }

        #pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        public override void WriteBinaryData(
            string name,
            byte[] bytes,
            BsonBinarySubType subType
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteBinaryData can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.Binary);
            buffer.WriteCString(name);

            if (subType == BsonBinarySubType.OldBinary && settings.FixOldBinarySubTypeOnOutput) {
                subType = BsonBinarySubType.Binary; // replace obsolete OldBinary with new Binary sub type
            }

            if (subType == BsonBinarySubType.OldBinary) {
                // sub type OldBinary has two sizes (for historical reasons)
                buffer.WriteInt32(bytes.Length + 4);
                buffer.WriteByte((byte) subType);
                buffer.WriteInt32(bytes.Length);
            } else {
                buffer.WriteInt32(bytes.Length);
                buffer.WriteByte((byte) subType);
            }
            buffer.WriteBytes(bytes);
        }
        #pragma warning restore 618

        public override void WriteBoolean(
            string name,
            bool value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteBoolean can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.Boolean);
            buffer.WriteCString(name);
            buffer.WriteBoolean(value);
        }

        public override void WriteDateTime(
            string name,
            DateTime value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteDateTime can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.DateTime);
            buffer.WriteCString(name);
            long milliseconds = (long) Math.Floor((value.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds);
            buffer.WriteInt64(milliseconds);
        }

        public override void WriteDocumentName(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteStartEmbeddedDocument can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.Document);
            buffer.WriteCString(name);
            context = new BsonBinaryWriterContext(context, BsonWriteState.EmbeddedDocument);
            context = new BsonBinaryWriterContext(context, BsonWriteState.StartDocument);
        }

        public override void WriteDouble(
            string name,
            double value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteDouble can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.Double);
            buffer.WriteCString(name);
            buffer.WriteDouble(value);
        }

        public override void WriteEndDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteEndDocument can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte(0);
            BackpatchSize(); // size of document
            context = context.ParentContext;

            if (context.WriteState == BsonWriteState.JavaScriptWithScope) {
                BackpatchSize(); // size of the JavaScript with scope value
                context = context.ParentContext;
            }

            if (context.WriteState == BsonWriteState.Initial) {
                context = new BsonBinaryWriterContext(null, BsonWriteState.Done);
            }
        }

        public override void WriteInt32(
            string name,
            int value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteInt32 can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.Int32);
            buffer.WriteCString(name);
            buffer.WriteInt32(value);
        }

        public override void WriteInt64(
            string name,
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteInt64 can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.Int64);
            buffer.WriteCString(name);
            buffer.WriteInt64(value);
        }

        public override void WriteJavaScript(
            string name,
            string code
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteJavaScript can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.JavaScript);
            buffer.WriteCString(name);
            buffer.WriteString(code);
        }

        public override void WriteJavaScriptWithScope(
            string name,
            string code
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteStartJavaScriptWithScope can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.JavaScriptWithScope);
            buffer.WriteCString(name);
            context = new BsonBinaryWriterContext(context, BsonWriteState.JavaScriptWithScope);
            context.StartPosition = buffer.Position;
            buffer.WriteInt32(0); // reserve space for size of JavaScript with scope value
            buffer.WriteString(code);
            context = new BsonBinaryWriterContext(context, BsonWriteState.ScopeDocument);
            context = new BsonBinaryWriterContext(context, BsonWriteState.StartDocument);
        }

        public override void WriteMaxKey(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteMaxKey can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.MaxKey);
            buffer.WriteCString(name);
        }

        public override void WriteMinKey(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteMinKey can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.MinKey);
            buffer.WriteCString(name);
        }

        public override void WriteNull(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteNull can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.Null);
            buffer.WriteCString(name);
        }

        public override void WriteObjectId(
            string name,
            int timestamp,
            long machinePidIncrement
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteObjectId can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.ObjectId);
            buffer.WriteCString(name);
            buffer.WriteObjectId(timestamp, machinePidIncrement);
        }

        public override void WriteRegularExpression(
            string name,
            string pattern,
            string options
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteRegularExpression can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.RegularExpression);
            buffer.WriteCString(name);
            buffer.WriteCString(pattern);
            buffer.WriteCString(options);
        }

        public override void WriteStartDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (context.WriteState == BsonWriteState.StartDocument) {
                context = context.ParentContext;
            } else if (context.WriteState == BsonWriteState.Initial || context.WriteState == BsonWriteState.Done) {
                context = new BsonBinaryWriterContext(context, BsonWriteState.Document);
            } else {
                throw new InvalidOperationException("WriteStartDocument can only be called when WriteState is Initial, StartDocument, or Done");
            }
            context.StartPosition = buffer.Position;
            buffer.WriteInt32(0); // reserve space for size
        }

        public override void WriteString(
            string name,
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteString can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.String);
            buffer.WriteCString(name);
            buffer.WriteString(value);
        }

        public override void WriteSymbol(
            string name,
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteSymbol can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.Symbol);
            buffer.WriteCString(name);
            buffer.WriteString(value);
        }

        public override void WriteTimestamp(
            string name,
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteTimestamp can only be called when WriteState is one of the document states");
            }
            buffer.WriteByte((byte) BsonType.Timestamp);
            buffer.WriteCString(name);
            buffer.WriteInt64(value);
        }
        #endregion

        #region private methods
        private void BackpatchSize() {
            int size = buffer.Position - context.StartPosition;
            if (size > settings.MaxDocumentSize) {
                throw new FileFormatException("Size is larger than MaxDocumentSize");
            }
            buffer.Backpatch(context.StartPosition, size);
        }
        #endregion
    }
}

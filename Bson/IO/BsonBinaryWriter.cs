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
    public class BsonBinaryWriter : BsonBaseWriter {
        #region private fields
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
            if (buffer == null) {
                this.buffer = new BsonBuffer();
                this.disposeBuffer = true; // only call Dispose if we allocated the buffer
            } else {
                this.buffer = buffer;
                this.disposeBuffer = false;
            }
            this.settings = settings;

            context = null;
            state = BsonWriterState.Initial;
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
            if (state != BsonWriterState.Closed) {
                if (state == BsonWriterState.Done) {
                    Flush();
                }
                if (stream != null && settings.CloseOutput) {
                    stream.Close();
                }
                context = null;
                state = BsonWriterState.Closed;
            }
        }

        public override void Flush() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state == BsonWriterState.Closed) {
                throw new InvalidOperationException("Flush called on closed BsonWriter");
            }
            if (state != BsonWriterState.Done) {
                throw new InvalidOperationException("Flush called before BsonBinaryWriter was finished writing to buffer");
            }
            if (stream != null) {
                buffer.WriteTo(stream);
                stream.Flush();
                buffer.Clear(); // only clear the buffer if we have written it to a stream
            }
        }

        #pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
        public override void WriteBinaryData(
            byte[] bytes,
            BsonBinarySubType subType
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteBinaryData cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.Binary);
            WriteNameHelper();
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

            state = GetNextState();
        }
        #pragma warning restore 618

        public override void WriteBoolean(
            bool value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteBoolean cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.Boolean);
            WriteNameHelper();
            buffer.WriteBoolean(value);

            state = GetNextState();
        }

        public override void WriteDateTime(
            DateTime value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteDateTime cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }
            if (value.Kind != DateTimeKind.Utc) {
                throw new ArgumentException("DateTime value must be in UTC");
            }

            buffer.WriteByte((byte) BsonType.DateTime);
            WriteNameHelper();
            long milliseconds = (long) Math.Floor((value.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds);
            buffer.WriteInt64(milliseconds);

            state = GetNextState();
        }

        public override void WriteDouble(
            double value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteDouble cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.Double);
            WriteNameHelper();
            buffer.WriteDouble(value);

            state = GetNextState();
        }

        public override void WriteEndArray() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value || context.ContextType != ContextType.Array) {
                var message = string.Format("WriteEndArray cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte(0);
            BackpatchSize(); // size of document

            context = context.ParentContext;
            state = GetNextState();
        }

        public override void WriteEndDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Name || (context.ContextType != ContextType.Document && context.ContextType != ContextType.ScopeDocument)) {
                var message = string.Format("WriteEndDocument cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte(0);
            BackpatchSize(); // size of document

            context = context.ParentContext;
            if (context == null) {
                state = BsonWriterState.Done;
            } else {
                if (context.ContextType == ContextType.JavaScriptWithScope) {
                    BackpatchSize(); // size of the JavaScript with scope value
                    context = context.ParentContext;
                }
                state = GetNextState();
            }
        }

        public override void WriteInt32(
            int value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteInt32 cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.Int32);
            WriteNameHelper();
            buffer.WriteInt32(value);

            state = GetNextState();
        }

        public override void WriteInt64(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteInt64 cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.Int64);
            WriteNameHelper();
            buffer.WriteInt64(value);

            state = GetNextState();
        }

        public override void WriteJavaScript(
            string code
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteJavaScript cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.JavaScript);
            WriteNameHelper();
            buffer.WriteString(code);

            state = GetNextState();
        }

        public override void WriteJavaScriptWithScope(
            string code
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteJavaScriptWithScope cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.JavaScriptWithScope);
            WriteNameHelper();
            context = new BsonBinaryWriterContext(context, ContextType.JavaScriptWithScope, buffer.Position);
            buffer.WriteInt32(0); // reserve space for size of JavaScript with scope value
            buffer.WriteString(code);

            state = BsonWriterState.ScopeDocument;
        }

        public override void WriteMaxKey() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteMaxKey cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.MaxKey);
            WriteNameHelper();

            state = GetNextState();
        }

        public override void WriteMinKey() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteMinKey cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.MinKey);
            WriteNameHelper();

            state = GetNextState();
        }

        public override void WriteNull() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteNull cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.Null);
            WriteNameHelper();

            state = GetNextState();
        }

        public override void WriteObjectId(
            int timestamp,
            int machine,
            short pid,
            int increment
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteObjectId cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.ObjectId);
            WriteNameHelper();
            buffer.WriteObjectId(timestamp, machine, pid, increment);

            state = GetNextState();
        }

        public override void WriteRegularExpression(
            string pattern,
            string options
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteRegularExpression cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.RegularExpression);
            WriteNameHelper();
            buffer.WriteCString(pattern);
            buffer.WriteCString(options);

            state = GetNextState();
        }

        public override void WriteStartArray() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteStartArray cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.Array);
            WriteNameHelper();
            context = new BsonBinaryWriterContext(context, ContextType.Array, buffer.Position);
            buffer.WriteInt32(0); // reserve space for size

            state = BsonWriterState.Value;
        }

        public override void WriteStartDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Initial && state != BsonWriterState.Value && state != BsonWriterState.ScopeDocument && state != BsonWriterState.Done) {
                var message = string.Format("WriteStartDocument cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            if (state == BsonWriterState.Value) {
                buffer.WriteByte((byte) BsonType.Document);
                WriteNameHelper();
            }
            var contextType = (state == BsonWriterState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
            context = new BsonBinaryWriterContext(context, ContextType.Document, buffer.Position);
            buffer.WriteInt32(0); // reserve space for size

            state = BsonWriterState.Name;
        }

        public override void WriteString(
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteString cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.String);
            WriteNameHelper();
            buffer.WriteString(value);

            state = GetNextState();
        }

        public override void WriteSymbol(
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteSymbol cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.Symbol);
            WriteNameHelper();
            buffer.WriteString(value);

            state = GetNextState();
        }

        public override void WriteTimestamp(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteTimestamp cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.Timestamp);
            WriteNameHelper();
            buffer.WriteInt64(value);

            state = GetNextState();
        }
        #endregion

        #region protected methods
        protected override void Dispose(
            bool disposing
        ) {
            if (disposing) {
                Close();
                if (disposeBuffer) {
                    buffer.Dispose();
                    buffer = null;
                }
            }
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

        private BsonWriterState GetNextState() {
            if (context.ContextType == ContextType.Array) {
                return BsonWriterState.Value;
            } else {
                return BsonWriterState.Name;
            }
        }

        private void WriteNameHelper() {
            if (context.ContextType == ContextType.Array) {
                buffer.WriteCString((context.Index++).ToString());
            } else {
                buffer.WriteCString(name);
            }
        }
        #endregion
    }
}

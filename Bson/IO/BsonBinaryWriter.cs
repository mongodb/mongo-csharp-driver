﻿/* Copyright 2010-2011 10gen Inc.
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
    /// Represents a BSON writer to a BSON Stream.
    /// </summary>
    public class BsonBinaryWriter : BsonBaseWriter {
        #region private fields
        private Stream stream; // can be null if we're only writing to the buffer
        private BsonBuffer buffer;
        private bool disposeBuffer;
        private BsonBinaryWriterSettings settings;
        private BsonBinaryWriterContext context;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBinaryWriter class.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <param name="buffer">A BsonBuffer.</param>
        /// <param name="settings">Optional BsonBinaryWriter settings.</param>
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
            this.settings = settings.Freeze();

            context = null;
            state = BsonWriterState.Initial;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the writer's BsonBuffer.
        /// </summary>
        public BsonBuffer Buffer {
            get { return buffer; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Closes the writer.
        /// </summary>
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

        /// <summary>
        /// Flushes any pending data to the output destination.
        /// </summary>
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
        /// <summary>
        /// Writes BSON binary data to the writer.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
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

        /// <summary>
        /// Writes a BSON Boolean to the writer.
        /// </summary>
        /// <param name="value">The Boolean value.</param>
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

        /// <summary>
        /// Writes a BSON DateTime to the writer.
        /// </summary>
        /// <param name="value">The number of milliseconds since the Unix epoch.</param>
        public override void WriteDateTime(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteDateTime cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.DateTime);
            WriteNameHelper();
            buffer.WriteInt64(value);

            state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Double to the writer.
        /// </summary>
        /// <param name="value">The Double value.</param>
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

        /// <summary>
        /// Writes the end of a BSON array to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes the end of a BSON document to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes a BSON Int32 to the writer.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
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

        /// <summary>
        /// Writes a BSON Int64 to the writer.
        /// </summary>
        /// <param name="value">The Int64 value.</param>
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

        /// <summary>
        /// Writes a BSON JavaScript to the writer.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
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

        /// <summary>
        /// Writes a BSON JavaScript to the writer (call WriteStartDocument to start writing the scope).
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
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

        /// <summary>
        /// Writes a BSON MaxKey to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes a BSON MinKey to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes a BSON null to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes a BSON ObjectId to the writer.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
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

        /// <summary>
        /// Writes a BSON regular expression to the writer.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
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

        /// <summary>
        /// Writes the start of a BSON array to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes the start of a BSON document to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes a BSON String to the writer.
        /// </summary>
        /// <param name="value">The String value.</param>
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

        /// <summary>
        /// Writes a BSON Symbol to the writer.
        /// </summary>
        /// <param name="value">The symbol.</param>
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

        /// <summary>
        /// Writes a BSON timestamp to the writer.
        /// </summary>
        /// <param name="value">The combined timestamp/increment value.</param>
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

        /// <summary>
        /// Writes a BSON undefined to the writer.
        /// </summary>
        public override void WriteUndefined() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteUndefined cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            buffer.WriteByte((byte) BsonType.Undefined);
            WriteNameHelper();

            state = GetNextState();
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Disposes of any resources used by the writer.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
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

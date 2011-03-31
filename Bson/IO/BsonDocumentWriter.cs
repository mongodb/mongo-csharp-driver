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
    /// Represents a BSON writer to a BsonDocument.
    /// </summary>
    public class BsonDocumentWriter : BsonBaseWriter {
        #region private fields
        private BsonDocument topLevelDocument;
        private BsonDocumentWriterContext context;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDocumentWriter class.
        /// </summary>
        public BsonDocumentWriter()
            : this(new BsonDocument()) {
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocumentWriter class.
        /// </summary>
        /// <param name="topLevelDocument">The document to write to (normally starts out as an empty document).</param>
        public BsonDocumentWriter(
            BsonDocument topLevelDocument
        ) {
            this.topLevelDocument = topLevelDocument;
            context = null;
            state = BsonWriterState.Initial;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the top level BsonDocument.
        /// </summary>
        public BsonDocument TopLevelDocument {
            get { return topLevelDocument; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Closes the writer.
        /// </summary>
        public override void Close() {
            // Close can be called on Disposed objects
            if (state != BsonWriterState.Closed) {
                context = null;
                state = BsonWriterState.Closed;
            }
        }

        /// <summary>
        /// Flushes any pending data to the output destination.
        /// </summary>
        public override void Flush() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
        }

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

            WriteValue(new BsonBinaryData(bytes, subType));
            state = GetNextState();
        }

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

            WriteValue(value);
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

            WriteValue(new BsonDateTime(value));
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

            WriteValue(value);
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

            var array = context.Array;
            context = context.ParentContext;
            WriteValue(array);
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

            if (context.ContextType == ContextType.ScopeDocument) {
                var scope = context.Document;
                context = context.ParentContext;
                var code = context.Code;
                context = context.ParentContext;
                WriteValue(new BsonJavaScriptWithScope(code, scope));
            } else {
                var document = context.Document;
                context = context.ParentContext;
                if (context != null) {
                    WriteValue(document);
                }
            }

            if (context == null) {
                state = BsonWriterState.Done;
            } else {
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

            WriteValue(value);
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

            WriteValue(value);
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

            WriteValue(new BsonJavaScript(code));
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

            context = new BsonDocumentWriterContext(context, ContextType.JavaScriptWithScope, code);
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

            WriteValue(BsonMaxKey.Value);
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

            WriteValue(BsonMinKey.Value);
            state = GetNextState();
        }

        /// <summary>
        /// Writes the name of an element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void WriteName(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException(this.GetType().Name); }
            if (state != BsonWriterState.Name) {
                var message = string.Format("WriteName cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context.Name = name;
            state = BsonWriterState.Value;
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

            WriteValue(BsonNull.Value);
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

            WriteValue(new ObjectId(timestamp, machine, pid, increment));
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

            WriteValue(new BsonRegularExpression(pattern, options));
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

            context = new BsonDocumentWriterContext(context, ContextType.Array, new BsonArray());
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

            switch (state) {
                case BsonWriterState.Initial:
                case BsonWriterState.Done:
                    context = new BsonDocumentWriterContext(null, ContextType.Document, topLevelDocument);
                    break;
                case BsonWriterState.Value:
                    context = new BsonDocumentWriterContext(context, ContextType.Document, new BsonDocument());
                    break;
                case BsonWriterState.ScopeDocument:
                    context = new BsonDocumentWriterContext(context, ContextType.ScopeDocument, new BsonDocument());
                    break;
                default:
                    throw new BsonInternalException("Unexpected state");
            }

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

            WriteValue(value);
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

            WriteValue(BsonSymbol.Create(value));
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

            WriteValue(new BsonTimestamp(value));
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

            WriteValue(BsonUndefined.Value);
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
            }
        }
        #endregion

        #region private methods
        private BsonWriterState GetNextState() {
            if (context.ContextType == ContextType.Array) {
                return BsonWriterState.Value;
            } else {
                return BsonWriterState.Name;
            }
        }

        private void WriteValue(
            BsonValue value
        ) {
            if (context.ContextType == ContextType.Array) {
                context.Array.Add(value);
            } else {
                context.Document.Add(context.Name, value);
            }
        }
        #endregion
    }
}

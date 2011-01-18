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
    public class BsonDocumentWriter : BsonBaseWriter {
        #region private fields
        private BsonDocument topLevelDocument;
        private BsonDocumentWriterContext context;
        #endregion

        #region constructors
        public BsonDocumentWriter()
            : this(new BsonDocument()) {
        }

        public BsonDocumentWriter(
            BsonDocument topLevelDocument
        ) {
            this.topLevelDocument = topLevelDocument;
            context = null;
            state = BsonWriterState.Initial;
        }
        #endregion

        #region public properties
        public BsonDocument TopLevelDocument {
            get { return topLevelDocument; }
        }
        #endregion

        #region public methods
        public override void Close() {
            // Close can be called on Disposed objects
            if (state != BsonWriterState.Closed) {
                context = null;
                state = BsonWriterState.Closed;
            }
        }

        public override void Flush() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
        }

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

            WriteValue(value);
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

            WriteValue(value);
            state = GetNextState();
        }

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

        public override void WriteMaxKey() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteMaxKey cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteValue(BsonMaxKey.Value);
            state = GetNextState();
        }

        public override void WriteMinKey() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteMinKey cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteValue(BsonMinKey.Value);
            state = GetNextState();
        }

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

        public override void WriteNull() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteNull cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteValue(BsonNull.Value);
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

            WriteValue(new ObjectId(timestamp, machine, pid, increment));
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

            WriteValue(new BsonRegularExpression(pattern, options));
            state = GetNextState();
        }

        public override void WriteStartArray() {
            if (disposed) { throw new ObjectDisposedException("BsonBinaryWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteStartArray cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context = new BsonDocumentWriterContext(context, ContextType.Array, new BsonArray());
            state = BsonWriterState.Value;
        }

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
        #endregion

        #region protected methods
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

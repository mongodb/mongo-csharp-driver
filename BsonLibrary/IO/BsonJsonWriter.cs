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
    public class BsonJsonWriter : BsonWriter {
        #region private fields
        private bool disposed = false;
        private TextWriter textWriter;
        private BsonJsonWriterSettings settings;
        private BsonJsonWriterContext context;
        #endregion

        #region constructors
        public BsonJsonWriter(
            TextWriter writer,
            BsonJsonWriterSettings settings
        ) {
            this.textWriter = writer;
            this.settings = settings;
            context = new BsonJsonWriterContext(null, BsonWriteState.Initial, "");
        }
        #endregion

        #region public properties
        public override BsonWriteState WriteState {
            get { return context.WriteState; }
        }
        #endregion

        #region public methods
        public override void Close() {
            // Close can be called on Disposed objects
            if (context.WriteState != BsonWriteState.Closed) {
                Flush();
                if (settings.CloseOutput) {
                    textWriter.Close();
                }
                context = new BsonJsonWriterContext(null, BsonWriteState.Closed, "");
            }
        }

        public override void Dispose() {
            if (!disposed) {
                Close();
                textWriter.Dispose();
                disposed = true;
            }
        }

        public override void Flush() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            textWriter.Flush();
        }

        public override void WriteArrayName(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteStartArray can only be called when WriteState is one of the document states");
            }
            WriteDocumentName(name, BsonWriteState.Array);
        }

        public override void WriteBinaryData(
            string name,
            byte[] bytes,
            BsonBinarySubType subType
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteBinaryData can only be called when WriteState is one of the document states");
            }
            WriteDocumentName(name);
            WriteStartDocument();
            WriteString("$binary", Convert.ToBase64String(bytes));
            WriteString("$type", ((int) subType).ToString("x2"));
            WriteEndDocument();
        }

        public override void WriteBoolean(
            string name,
            bool value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteBoolean can only be called when WriteState is one of the document states");
            }
            WriteName(name);
            textWriter.Write(value ? "true" : "false");
        }

        public override void WriteDateTime(
            string name,
            DateTime value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteDateTime can only be called when WriteState is one of the document states");
            }
            long milliseconds = (long) Math.Floor((value.ToUniversalTime() - Bson.UnixEpoch).TotalMilliseconds);
            switch (settings.OutputMode) {
                case BsonJsonOutputMode.Strict:
                    WriteDocumentName(name);
                    WriteStartDocument();
                    WriteInt64("$date", milliseconds);
                    WriteEndDocument();
                    break;
                case BsonJsonOutputMode.JavaScript:
                case BsonJsonOutputMode.TenGen:
                    WriteName(name);
                    textWriter.Write("Date({0})", milliseconds);
                    break;
                default:
                    throw new BsonInternalException("Unexpected BsonJsonOutputMode");
            }
        }

        public override void WriteDocumentName(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteStartEmbeddedDocument can only be called when WriteState is one of the document states");
            }
            WriteDocumentName(name, BsonWriteState.EmbeddedDocument);
        }

        public override void WriteDouble(
            string name,
            double value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteDouble can only be called when WriteState is one of the document states");
            }
            WriteName(name);
            textWriter.Write(value);
        }

        public override void WriteEndDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteEndDocument can only be called when WriteState is one of the document states");
            }
            if (context.WriteState == BsonWriteState.Array) {
                textWriter.Write("]");
            } else {
                if (settings.Indent && context.HasElements) {
                    textWriter.Write(settings.NewLineChars);
                    textWriter.Write(context.ParentContext.Indentation);
                    textWriter.Write("}");
                } else {
                    textWriter.Write(" }");
                }
            }
            context = context.ParentContext;

            if (context.WriteState == BsonWriteState.JavaScriptWithScope) {
                WriteEndDocument();
            }

            if (context.WriteState == BsonWriteState.Initial) {
                context = new BsonJsonWriterContext(null, BsonWriteState.Done, "");
            }
        }

        public override void WriteInt32(
            string name,
            int value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteInt32 can only be called when WriteState is one of the document states");
            }
            WriteName(name);
            textWriter.Write(value);
        }

        public override void WriteInt64(
            string name,
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteInt64 can only be called when WriteState is one of the document states");
            }
            WriteName(name);
            textWriter.Write(value);
        }

        public override void WriteJavaScript(
            string name,
            string code
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteJavaScript can only be called when WriteState is one of the document states");
            }
            WriteDocumentName(name);
            WriteStartDocument();
            WriteString("$code", code);
            WriteEndDocument();
        }

        public override void WriteJavaScriptWithScope(
            string name,
            string code
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteStartJavaScriptWithScope can only be called when WriteState is one of the document states");
            }
            WriteDocumentName(name, BsonWriteState.JavaScriptWithScope);
            WriteStartDocument();
            WriteString("$code", code);
            WriteDocumentName("$scope", BsonWriteState.ScopeDocument);
        }

        public override void WriteMaxKey(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteMaxKey can only be called when WriteState is one of the document states");
            }
            WriteDocumentName(name);
            WriteStartDocument();
            WriteInt32("$maxkey", 1);
            WriteEndDocument();
        }

        public override void WriteMinKey(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteMinKey can only be called when WriteState is one of the document states");
            }
            WriteDocumentName(name);
            WriteStartDocument();
            WriteInt32("$minkey", 1);
            WriteEndDocument();
        }

        public override void WriteNull(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteMinKey can only be called when WriteState is one of the document states");
            }
            WriteName(name);
            textWriter.Write("null");
        }

        public override void WriteObjectId(
            string name,
            int timestamp,
            long machinePidIncrement
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteObjectId can only be called when WriteState is one of the document states");
            }
            var bytes = ObjectId.Pack(timestamp, machinePidIncrement);
            switch (settings.OutputMode) {
                case BsonJsonOutputMode.Strict:
                case BsonJsonOutputMode.JavaScript:
                    WriteDocumentName(name);
                    WriteStartDocument();
                    WriteString("$oid", BsonUtils.ToHexString(bytes));
                    WriteEndDocument();
                    break;
                case BsonJsonOutputMode.TenGen:
                    WriteName(name);
                    textWriter.Write(string.Format("ObjectId(\"{0}\")", BsonUtils.ToHexString(bytes)));
                    break;
            }
        }

        public override void WriteRegularExpression(
            string name,
            string pattern,
            string options
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteRegularExpression can only be called when WriteState is one of the document states");
            }
            switch (settings.OutputMode) {
                case BsonJsonOutputMode.Strict:
                    WriteDocumentName(name);
                    WriteStartDocument();
                    WriteString("$regex", pattern);
                    WriteString("$options", options);
                    WriteEndDocument();
                    break;
                case BsonJsonOutputMode.JavaScript:
                case BsonJsonOutputMode.TenGen:
                    WriteName(name);
                    textWriter.Write("/");
                    textWriter.Write(pattern.Replace(@"\", @"\\"));
                    textWriter.Write("/");
                    foreach (char c in options.ToLower()) {
                        switch (c) {
                            case 'g':
                            case 'i':
                            case 'm':
                                textWriter.Write(c);
                                break;
                        }
                    }
                    break;
            }
        }

        public override void WriteStartDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (context.WriteState == BsonWriteState.StartDocument) {
                context = context.ParentContext;
            } else if (context.WriteState == BsonWriteState.Initial || context.WriteState == BsonWriteState.Done) {
                context = new BsonJsonWriterContext(context, BsonWriteState.Document, context.Indentation + settings.IndentChars);
            } else {
                throw new InvalidOperationException("WriteStartDocument can only be called when WriteState is Initial, StartDocument or Done");
            }
            if (context.WriteState == BsonWriteState.Array) {
                textWriter.Write("[");
            } else {
                textWriter.Write("{");
            }
        }

        public override void WriteString(
            string name,
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteString can only be called when WriteState is one of the document states");
            }
            WriteName(name);
            WriteString(value);
        }

        public override void WriteSymbol(
            string name,
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteSymbol can only be called when WriteState is one of the document states");
            }
            WriteDocumentName(name);
            WriteStartDocument();
            WriteString("$symbol", value);
            WriteEndDocument();
        }

        public override void WriteTimestamp(
            string name,
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if ((context.WriteState & BsonWriteState.Document) == 0) {
                throw new InvalidOperationException("WriteTimestamp can only be called when WriteState is one of the document states");
            }
            WriteDocumentName(name);
            WriteStartDocument();
            WriteInt64("$timestamp", value);
            WriteEndDocument();
        }
        #endregion

        #region private methods
        private void WriteDocumentName(
            string name,
            BsonWriteState documentType
        ) {
            WriteName(name);
            context = new BsonJsonWriterContext(context, documentType, context.Indentation + settings.IndentChars);
            context = new BsonJsonWriterContext(context, BsonWriteState.StartDocument, null);
        }

        private void WriteName(
            string name
        ) {
            // don't write Array element names in Json
            if (context.WriteState == BsonWriteState.Array) {
                if (context.HasElements) {
                    textWriter.Write(", ");
                }
            } else {
                if (context.HasElements) {
                    textWriter.Write(",");
                }
                if (settings.Indent) {
                    textWriter.Write(settings.NewLineChars);
                    textWriter.Write(context.Indentation);
                } else {
                    textWriter.Write(" ");
                }
                WriteString(name);
                textWriter.Write(" : ");
            }

            context.HasElements = true;
        }

        private void WriteString(
            string value
        ) {
            textWriter.Write("\"");
            foreach (char c in value) {
                switch (c) {
                    case '"': textWriter.Write("\\\""); break;
                    case '\\': textWriter.Write("\\\\"); break;
                    case '/': textWriter.Write("\\/"); break;
                    case '\b': textWriter.Write("\\b"); break;
                    case '\f': textWriter.Write("\\f"); break;
                    case '\n': textWriter.Write("\\n"); break;
                    case '\r': textWriter.Write("\\r"); break;
                    case '\t': textWriter.Write("\\t"); break;
                    default:
                        if (c <= '\x001f' || c == '\x007f' || (c >= '\x0080' && c <= '\x009f')) {
                            textWriter.Write("\\u{0:x4}", c);
                        } else {
                            textWriter.Write(c);
                        }
                        break;
                }
            }
            textWriter.Write("\"");
        }
        #endregion
    }
}

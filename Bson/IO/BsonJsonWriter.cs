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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MongoDB.Bson.IO {
    public class BsonJsonWriter : BsonBaseWriter {
        #region private fields
        private TextWriter textWriter;
        private BsonJsonWriterSettings settings;
        private BsonJsonWriterContext context;
        private BsonWriteState state;
        #endregion

        #region constructors
        public BsonJsonWriter(
            TextWriter writer,
            BsonJsonWriterSettings settings
        ) {
            this.textWriter = writer;
            this.settings = settings;
            context = null;
            state = BsonWriteState.Initial;
        }
        #endregion

        #region public properties
        public override BsonWriteState WriteState {
            get { return state; }
        }
        #endregion

        #region public methods
        public override void Close() {
            // Close can be called on Disposed objects
            if (state != BsonWriteState.Closed) {
                Flush();
                if (settings.CloseOutput) {
                    textWriter.Close();
                }
                context = null;
                state = BsonWriteState.Closed;
            }
        }

        public override void Flush() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            textWriter.Flush();
        }

        public override void WriteBinaryData(
            byte[] bytes,
            BsonBinarySubType subType
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteBinaryData cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteString("$binary", Convert.ToBase64String(bytes));
            WriteString("$type", ((int) subType).ToString("x2"));
            WriteEndDocument();

            state = BsonWriteState.Name;
        }

        public override void WriteBoolean(
            bool value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteBoolean cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write(value ? "true" : "false");

            state = BsonWriteState.Name;
        }

        public override void WriteDateTime(
            DateTime value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteDateTime cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            if (value.Kind != DateTimeKind.Utc) {
                throw new ArgumentException("DateTime value must be in UTC");
            }

            long milliseconds = (long) Math.Floor((value.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds);
            switch (settings.OutputMode) {
                case BsonJsonOutputMode.Strict:
                    WriteStartDocument();
                    WriteInt64("$date", milliseconds);
                    WriteEndDocument();
                    break;
                case BsonJsonOutputMode.JavaScript:
                case BsonJsonOutputMode.TenGen:
                    WriteNameHelper(name);
                    textWriter.Write("Date({0})", milliseconds);
                    break;
                case BsonJsonOutputMode.ISO:
                    WriteNameHelper(name);
                    textWriter.Write("\"{0}\"", value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    break;
                default:
                    throw new BsonInternalException("Unexpected BsonJsonOutputMode");
            }

            state = BsonWriteState.Name;
        }

        public override void WriteDouble(
            double value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteDateTime cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write(XmlConvert.ToString(value));

            state = BsonWriteState.Name;
        }

        public override void WriteEndArray() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Name) {
                var message = string.Format("WriteEndArray cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            textWriter.Write("]");

            context = context.ParentContext;
            state = BsonWriteState.Name;
        }

        public override void WriteEndDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Name) {
                var message = string.Format("WriteDateTime cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            if (settings.Indent && context.HasElements) {
                textWriter.Write(settings.NewLineChars);
                if (context.ParentContext != null) {
                    textWriter.Write(context.ParentContext.Indentation);
                }
                textWriter.Write("}");
            } else {
                textWriter.Write(" }");
            }

            if (context.ContextType == ContextType.ScopeDocument) {
                context = context.ParentContext;
                WriteEndDocument();
            } else {
                context = context.ParentContext;
            }

            if (context == null) {
                state = BsonWriteState.Done;
            } else {
                state = BsonWriteState.Name;
            }
        }

        public override void WriteInt32(
            int value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteInt32 cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write(value);

            state = BsonWriteState.Name;
        }

        public override void WriteInt64(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteInt64 cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write(value);

            state = BsonWriteState.Name;
        }

        public override void WriteJavaScript(
            string code
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteJavaScript cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteString("$code", code);
            WriteEndDocument();

            state = BsonWriteState.Name;
        }

        public override void WriteJavaScriptWithScope(
            string code
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteJavaScriptWithScope cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteString("$code", code);
            WriteName("$scope");

            state = BsonWriteState.ScopeDocument;
        }

        public override void WriteMaxKey() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteMaxKey cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteInt32("$maxkey", 1);
            WriteEndDocument();

            state = BsonWriteState.Name;
        }

        public override void WriteMinKey() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteMinKey cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteInt32("$minkey", 1);
            WriteEndDocument();

            state = BsonWriteState.Name;
        }

        public override void WriteName(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Name) {
                var message = string.Format("WriteMinKey cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            this.name = name;
            state = BsonWriteState.Value;
        }

        public override void WriteNull() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteNull cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write("null");

            state = BsonWriteState.Name;
        }

        public override void WriteObjectId(
            int timestamp,
            int machine,
            short pid,
            int increment
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteObjectId cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            var bytes = ObjectId.Pack(timestamp, machine, pid, increment);
            switch (settings.OutputMode) {
                case BsonJsonOutputMode.Strict:
                case BsonJsonOutputMode.JavaScript:
                    WriteStartDocument();
                    WriteString("$oid", BsonUtils.ToHexString(bytes));
                    WriteEndDocument();
                    break;
                case BsonJsonOutputMode.TenGen:
                    WriteNameHelper(name);
                    textWriter.Write(string.Format("ObjectId(\"{0}\")", BsonUtils.ToHexString(bytes)));
                    break;
            }

            state = BsonWriteState.Name;
        }

        public override void WriteRegularExpression(
            string pattern,
            string options
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteRegularExpression cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            switch (settings.OutputMode) {
                case BsonJsonOutputMode.Strict:
                    WriteStartDocument();
                    WriteString("$regex", pattern);
                    WriteString("$options", options);
                    WriteEndDocument();
                    break;
                case BsonJsonOutputMode.JavaScript:
                case BsonJsonOutputMode.TenGen:
                    WriteNameHelper(name);
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

            state = BsonWriteState.Name;
        }

        public override void WriteStartArray() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Value) {
                var message = string.Format("WriteStartArray cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write("[");

            context = new BsonJsonWriterContext(context, ContextType.Array, settings.IndentChars);
            state = BsonWriteState.Name;
        }

        public override void WriteStartDocument() {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Initial && state != BsonWriteState.Value && state != BsonWriteState.ScopeDocument) {
                var message = string.Format("WriteStartDocument cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            if (state == BsonWriteState.Value || state == BsonWriteState.ScopeDocument) {
                WriteNameHelper(name);
            }
            textWriter.Write("{");

            var contextType = (state == BsonWriteState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
            context = new BsonJsonWriterContext(context, contextType, settings.IndentChars);
            state = BsonWriteState.Name;
        }

        public override void WriteString(
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Initial && state != BsonWriteState.Value && state != BsonWriteState.ScopeDocument) {
                var message = string.Format("WriteString cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            WriteStringHelper(value);

            state = BsonWriteState.Name;
        }

        public override void WriteSymbol(
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Initial && state != BsonWriteState.Value && state != BsonWriteState.ScopeDocument) {
                var message = string.Format("WriteSymbol cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteString("$symbol", value);
            WriteEndDocument();

            state = BsonWriteState.Name;
        }

        public override void WriteTimestamp(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("BsonJsonWriter"); }
            if (state != BsonWriteState.Initial && state != BsonWriteState.Value && state != BsonWriteState.ScopeDocument) {
                var message = string.Format("WriteTimestamp cannot be called when WriterState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteInt64("$timestamp", value);
            WriteEndDocument();

            state = BsonWriteState.Name;
        }
        #endregion

        #region protected methods
        protected override void Dispose(
            bool disposing
        ) {
            if (disposing) {
                Close();
                textWriter.Dispose();
            }
        }
        #endregion

        #region private methods
        private void WriteNameHelper(
            string name
        ) {
            // don't write Array element names in Json
            if (context.ContextType == ContextType.Array) {
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
                WriteStringHelper(name);
                textWriter.Write(" : ");
            }

            context.HasElements = true;
        }

        private void WriteStringHelper(
            string value
        ) {
            textWriter.Write("\"");
            foreach (char c in value) {
                switch (c) {
                    case '"': textWriter.Write("\\\""); break;
                    case '\\': textWriter.Write("\\\\"); break;
                    case '\b': textWriter.Write("\\b"); break;
                    case '\f': textWriter.Write("\\f"); break;
                    case '\n': textWriter.Write("\\n"); break;
                    case '\r': textWriter.Write("\\r"); break;
                    case '\t': textWriter.Write("\\t"); break;
                    default:
                        switch (char.GetUnicodeCategory(c)) {
                            case UnicodeCategory.UppercaseLetter:
                            case UnicodeCategory.LowercaseLetter:
                            case UnicodeCategory.TitlecaseLetter:
                            case UnicodeCategory.OtherLetter:
                            case UnicodeCategory.DecimalDigitNumber:
                            case UnicodeCategory.LetterNumber:
                            case UnicodeCategory.OtherNumber:
                            case UnicodeCategory.SpaceSeparator:
                            case UnicodeCategory.ConnectorPunctuation:
                            case UnicodeCategory.DashPunctuation:
                            case UnicodeCategory.OpenPunctuation:
                            case UnicodeCategory.ClosePunctuation:
                            case UnicodeCategory.InitialQuotePunctuation:
                            case UnicodeCategory.FinalQuotePunctuation:
                            case UnicodeCategory.OtherPunctuation:
                            case UnicodeCategory.MathSymbol:
                            case UnicodeCategory.CurrencySymbol:
                            case UnicodeCategory.ModifierSymbol:
                            case UnicodeCategory.OtherSymbol:
                                textWriter.Write(c);
                                break;
                            default:
                                textWriter.Write("\\u{0:x4}", (int) c);
                                break;
                        }
                        break;
                }
            }
            textWriter.Write("\"");
        }
        #endregion
    }
}

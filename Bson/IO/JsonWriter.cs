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
    public class JsonWriter : BsonBaseWriter {
        #region private fields
        private TextWriter textWriter;
        private JsonWriterSettings settings;
        private JsonWriterContext context;
        #endregion

        #region constructors
        public JsonWriter(
            TextWriter writer,
            JsonWriterSettings settings
        ) {
            this.textWriter = writer;
            this.settings = settings;
            context = new JsonWriterContext(null, ContextType.TopLevel, "");
            state = BsonWriterState.Initial;
        }
        #endregion

        #region public methods
        public override void Close() {
            // Close can be called on Disposed objects
            if (state != BsonWriterState.Closed) {
                Flush();
                if (settings.CloseOutput) {
                    textWriter.Close();
                }
                context = null;
                state = BsonWriterState.Closed;
            }
        }

        public override void Flush() {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            textWriter.Flush();
        }

        public override void WriteBinaryData(
            byte[] bytes,
            BsonBinarySubType subType
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteBinaryData cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteString("$binary", Convert.ToBase64String(bytes));
            WriteString("$type", ((int) subType).ToString("x2"));
            WriteEndDocument();

            state = GetNextState();
        }

        public override void WriteBoolean(
            bool value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteBoolean cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write(value ? "true" : "false");

            state = GetNextState();
        }

        public override void WriteDateTime(
            DateTime value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteDateTime cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            if (value.Kind != DateTimeKind.Utc) {
                throw new ArgumentException("DateTime value must be in UTC");
            }

            long milliseconds = (long) Math.Floor((value.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds);
            switch (settings.OutputMode) {
                case JsonOutputMode.Strict:
                    WriteStartDocument();
                    WriteInt64("$date", milliseconds);
                    WriteEndDocument();
                    break;
                case JsonOutputMode.JavaScript:
                case JsonOutputMode.TenGen:
                    WriteNameHelper(name);
                    textWriter.Write("Date({0})", milliseconds);
                    break;
                default:
                    throw new BsonInternalException("Unexpected JsonOutputMode");
            }

            state = GetNextState();
        }

        public override void WriteDouble(
            double value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteDouble cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write(XmlConvert.ToString(value));

            state = GetNextState();
        }

        public override void WriteEndArray() {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value) {
                var message = string.Format("WriteEndArray cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            textWriter.Write("]");

            context = context.ParentContext;
            state = GetNextState();
        }

        public override void WriteEndDocument() {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Name) {
                var message = string.Format("WriteEndDocument cannot be called when State is: {0}", state);
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
                state = BsonWriterState.Done;
            } else {
                state = GetNextState();
            }
        }

        public override void WriteInt32(
            int value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteInt32 cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write(value);

            state = GetNextState();
        }

        public override void WriteInt64(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteInt64 cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write(value);

            state = GetNextState();
        }

        public override void WriteJavaScript(
            string code
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteJavaScript cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteString("$code", code);
            WriteEndDocument();

            state = GetNextState();
        }

        public override void WriteJavaScriptWithScope(
            string code
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteJavaScriptWithScope cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteString("$code", code);
            WriteName("$scope");

            state = BsonWriterState.ScopeDocument;
        }

        public override void WriteMaxKey() {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteMaxKey cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteInt32("$maxkey", 1);
            WriteEndDocument();

            state = GetNextState();
        }

        public override void WriteMinKey() {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteMinKey cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteInt32("$minkey", 1);
            WriteEndDocument();

            state = GetNextState();
        }

        public override void WriteNull() {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteNull cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write("null");

            state = GetNextState();
        }

        public override void WriteObjectId(
            int timestamp,
            int machine,
            short pid,
            int increment
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteObjectId cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            var bytes = ObjectId.Pack(timestamp, machine, pid, increment);
            switch (settings.OutputMode) {
                case JsonOutputMode.Strict:
                case JsonOutputMode.JavaScript:
                    WriteStartDocument();
                    WriteString("$oid", BsonUtils.ToHexString(bytes));
                    WriteEndDocument();
                    break;
                case JsonOutputMode.TenGen:
                    WriteNameHelper(name);
                    textWriter.Write(string.Format("ObjectId(\"{0}\")", BsonUtils.ToHexString(bytes)));
                    break;
            }

            state = GetNextState();
        }

        public override void WriteRegularExpression(
            string pattern,
            string options
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteRegularExpression cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            switch (settings.OutputMode) {
                case JsonOutputMode.Strict:
                    WriteStartDocument();
                    WriteString("$regex", pattern);
                    WriteString("$options", options);
                    WriteEndDocument();
                    break;
                case JsonOutputMode.JavaScript:
                case JsonOutputMode.TenGen:
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

            state = GetNextState();
        }

        public override void WriteStartArray() {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteStartArray cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write("[");

            context = new JsonWriterContext(context, ContextType.Array, settings.IndentChars);
            state = BsonWriterState.Value;
        }

        public override void WriteStartDocument() {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial && state != BsonWriterState.ScopeDocument) {
                var message = string.Format("WriteStartDocument cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            if (state == BsonWriterState.Value || state == BsonWriterState.ScopeDocument) {
                WriteNameHelper(name);
            }
            textWriter.Write("{");

            var contextType = (state == BsonWriterState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
            context = new JsonWriterContext(context, contextType, settings.IndentChars);
            state = BsonWriterState.Name;
        }

        public override void WriteString(
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteString cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            WriteStringHelper(value);

            state = GetNextState();
        }

        public override void WriteSymbol(
            string value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteSymbol cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteString("$symbol", value);
            WriteEndDocument();

            state = GetNextState();
        }

        public override void WriteTimestamp(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteTimestamp cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteStartDocument();
            WriteInt64("$timestamp", value);
            WriteEndDocument();

            state = GetNextState();
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
        private BsonWriterState GetNextState() {
            if (context.ContextType == ContextType.Array) {
                return BsonWriterState.Value;
            } else {
                return BsonWriterState.Name;
            }
        }

        private void WriteNameHelper(
            string name
        ) {
            switch (context.ContextType) {
                case ContextType.Array:
                    // don't write Array element names in Json
                    if (context.HasElements) {
                        textWriter.Write(", ");
                    }
                    break;
                case ContextType.Document:
                case ContextType.ScopeDocument:
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
                    break;
                case ContextType.TopLevel:
                    break;
                default:
                    throw new BsonInternalException("Invalid ContextType");
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

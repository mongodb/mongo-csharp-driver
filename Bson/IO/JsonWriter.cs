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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MongoDB.Bson.IO {
    /// <summary>
    /// Represents a BSON writer to a TextWriter (in JSON format).
    /// </summary>
    public class JsonWriter : BsonBaseWriter {
        #region private fields
        private TextWriter textWriter;
        private JsonWriterSettings settings;
        private JsonWriterContext context;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the JsonWriter class.
        /// </summary>
        /// <param name="writer">A TextWriter.</param>
        /// <param name="settings">Optional JsonWriter settings.</param>
        public JsonWriter(
            TextWriter writer,
            JsonWriterSettings settings
        ) {
            this.textWriter = writer;
            this.settings = settings.Freeze();
            context = new JsonWriterContext(null, ContextType.TopLevel, "");
            state = BsonWriterState.Initial;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Closes the writer.
        /// </summary>
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

        /// <summary>
        /// Flushes any pending data to the output destination.
        /// </summary>
        public override void Flush() {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            textWriter.Flush();
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
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteBinaryData cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            if (settings.OutputMode == JsonOutputMode.Shell) {
                WriteNameHelper(name);
                textWriter.Write("new BinData({0}, \"{1}\")", (int) subType, Convert.ToBase64String(bytes));
            } else {
                WriteStartDocument();
                WriteString("$binary", Convert.ToBase64String(bytes));
                WriteString("$type", ((int) subType).ToString("x2"));
                WriteEndDocument();
            }

            state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Boolean to the writer.
        /// </summary>
        /// <param name="value">The Boolean value.</param>
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

        /// <summary>
        /// Writes a BSON DateTime to the writer.
        /// </summary>
        /// <param name="value">The number of milliseconds since the Unix epoch.</param>
        public override void WriteDateTime(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteDateTime cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            switch (settings.OutputMode) {
                case JsonOutputMode.Strict:
                    WriteStartDocument();
                    WriteInt64("$date", value);
                    WriteEndDocument();
                    break;
                case JsonOutputMode.JavaScript:
                case JsonOutputMode.TenGen:
                    WriteNameHelper(name);
                    textWriter.Write("Date({0})", value);
                    break;
                case JsonOutputMode.Shell:
                    WriteNameHelper(name);
                    // use ISODate for values that fall within .NET's DateTime range, and "new Date" for all others
                    if (
                        value >= BsonConstants.DateTimeMinValueMillisecondsSinceEpoch &&
                        value <= BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch
                    ) {
                        var utcDateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(value);
                        textWriter.Write("ISODate(\"{0}\")", utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
                    } else {
                        textWriter.Write("new Date({0})", value);
                    }
                    break;
                default:
                    throw new BsonInternalException("Unexpected JsonOutputMode");
            }

            state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Double to the writer.
        /// </summary>
        /// <param name="value">The Double value.</param>
        public override void WriteDouble(
            double value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteDouble cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            // if string representation looks like an integer add ".0" so that it looks like a double
            var stringRepresentation = value.ToString("R", NumberFormatInfo.InvariantInfo);
            if (Regex.IsMatch(stringRepresentation, @"^[+-]?\d+$")) {
                stringRepresentation += ".0";
            }
            textWriter.Write(stringRepresentation);

            state = GetNextState();
        }

        /// <summary>
        /// Writes the end of a BSON array to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes the end of a BSON document to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes a BSON Int32 to the writer.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
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

        /// <summary>
        /// Writes a BSON Int64 to the writer.
        /// </summary>
        /// <param name="value">The Int64 value.</param>
        public override void WriteInt64(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteInt64 cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            switch (settings.OutputMode) {
                case JsonOutputMode.Strict:
                case JsonOutputMode.JavaScript:
                    WriteNameHelper(name);
                    textWriter.Write(value);
                    break;
                case JsonOutputMode.TenGen:
                case JsonOutputMode.Shell:
                    WriteNameHelper(name);
                    if (value >= int.MinValue && value <= int.MaxValue) {
                        textWriter.Write("NumberLong({0})", value);
                    } else {
                        textWriter.Write("NumberLong(\"{0}\")", value);
                    }
                    break;
                default:
                    WriteNameHelper(name);
                    textWriter.Write(value);
                    break;
            }

            state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON JavaScript to the writer.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
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

        /// <summary>
        /// Writes a BSON JavaScript to the writer (call WriteStartDocument to start writing the scope).
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
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

        /// <summary>
        /// Writes a BSON MaxKey to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes a BSON MinKey to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes a BSON null to the writer.
        /// </summary>
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
                case JsonOutputMode.Shell:
                    WriteNameHelper(name);
                    textWriter.Write(string.Format("ObjectId(\"{0}\")", BsonUtils.ToHexString(bytes)));
                    break;
            }

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
                case JsonOutputMode.Shell:
                    WriteNameHelper(name);
                    textWriter.Write("/");
                    var escaped = (pattern == "") ? "(?:)" : pattern.Replace(@"\", @"\\").Replace("/", @"\/");
                    textWriter.Write(escaped);
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

        /// <summary>
        /// Writes the start of a BSON array to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes the start of a BSON document to the writer.
        /// </summary>
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

        /// <summary>
        /// Writes a BSON String to the writer.
        /// </summary>
        /// <param name="value">The String value.</param>
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

        /// <summary>
        /// Writes a BSON Symbol to the writer.
        /// </summary>
        /// <param name="value">The symbol.</param>
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

        /// <summary>
        /// Writes a BSON timestamp to the writer.
        /// </summary>
        /// <param name="value">The combined timestamp/increment value.</param>
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

        /// <summary>
        /// Writes a BSON undefined to the writer.
        /// </summary>
        public override void WriteUndefined() {
            if (disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (state != BsonWriterState.Value && state != BsonWriterState.Initial) {
                var message = string.Format("WriteUndefined cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            WriteNameHelper(name);
            textWriter.Write("undefined");

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

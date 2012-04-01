/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a BSON writer to a TextWriter (in JSON format).
    /// </summary>
    public class JsonWriter : BsonWriter
    {
        // private fields
        private TextWriter _textWriter;
        private JsonWriterSettings _jsonWriterSettings; // same value as in base class just declared as derived class
        private JsonWriterContext _context;

        // constructors
        /// <summary>
        /// Initializes a new instance of the JsonWriter class.
        /// </summary>
        /// <param name="writer">A TextWriter.</param>
        /// <param name="settings">Optional JsonWriter settings.</param>
        public JsonWriter(TextWriter writer, JsonWriterSettings settings)
            : base(settings)
        {
            _textWriter = writer;
            _jsonWriterSettings = settings; // already frozen by base class
            _context = new JsonWriterContext(null, ContextType.TopLevel, "");
            State = BsonWriterState.Initial;
        }

        // public methods
        /// <summary>
        /// Closes the writer.
        /// </summary>
        public override void Close()
        {
            // Close can be called on Disposed objects
            if (State != BsonWriterState.Closed)
            {
                Flush();
                if (_jsonWriterSettings.CloseOutput)
                {
                    _textWriter.Close();
                }
                _context = null;
                State = BsonWriterState.Closed;
            }
        }

        /// <summary>
        /// Flushes any pending data to the output destination.
        /// </summary>
        public override void Flush()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            _textWriter.Flush();
        }

        /// <summary>
        /// Writes BSON binary data to the writer.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        public override void WriteBinaryData(
            byte[] bytes,
            BsonBinarySubType subType,
            GuidRepresentation guidRepresentation)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteBinaryData", BsonWriterState.Value, BsonWriterState.Initial);
            }

            if (_jsonWriterSettings.OutputMode == JsonOutputMode.Shell)
            {
                WriteNameHelper(Name);
                switch (subType)
                {
                    case BsonBinarySubType.UuidLegacy:
                    case BsonBinarySubType.UuidStandard:
                        if (bytes.Length != 16)
                        {
                            var message = string.Format("Length of binary subtype {0} must be 16, not {1}.", subType, bytes.Length);
                            throw new ArgumentException(message);
                        }
                        if (subType == BsonBinarySubType.UuidLegacy && guidRepresentation == GuidRepresentation.Standard)
                        {
                            throw new ArgumentException("GuidRepresentation for binary subtype UuidLegacy must not be Standard.");
                        }
                        if (subType == BsonBinarySubType.UuidStandard && guidRepresentation != GuidRepresentation.Standard)
                        {
                            var message = string.Format("GuidRepresentation for binary subtype UuidStandard must be Standard, not {0}.", guidRepresentation);
                            throw new ArgumentException(message);
                        }
                        if (_jsonWriterSettings.ShellVersion >= new Version(2, 0, 0))
                        {
                            if (guidRepresentation == GuidRepresentation.Unspecified)
                            {
                                var s = BsonUtils.ToHexString(bytes);
                                var parts = new string[]
                                {
                                    s.Substring(0, 8),
                                    s.Substring(8, 4),
                                    s.Substring(12, 4),
                                    s.Substring(16, 4),
                                    s.Substring(20, 12)
                                };
                                _textWriter.Write("HexData({0}, \"{1}\")", (int)subType, string.Join("-", parts));
                            }
                            else
                            {
                                string uuidConstructorName;
                                switch (guidRepresentation)
                                {
                                    case GuidRepresentation.CSharpLegacy: uuidConstructorName = "CSUUID"; break;
                                    case GuidRepresentation.JavaLegacy: uuidConstructorName = "JUUID"; break;
                                    case GuidRepresentation.PythonLegacy: uuidConstructorName = "PYUUID"; break;
                                    case GuidRepresentation.Standard: uuidConstructorName = "UUID"; break;
                                    default: throw new BsonInternalException("Unexpected GuidRepresentation");
                                }
                                var guid = GuidConverter.FromBytes(bytes, guidRepresentation);
                                _textWriter.Write("{0}(\"{1}\")", uuidConstructorName, guid.ToString());
                            }
                        }
                        else
                        {
                            _textWriter.Write("new BinData({0}, \"{1}\")", (int)subType, Convert.ToBase64String(bytes));
                        }
                        break;
                    default:
                        _textWriter.Write("new BinData({0}, \"{1}\")", (int)subType, Convert.ToBase64String(bytes));
                        break;
                }
            }
            else
            {
                WriteStartDocument();
                WriteString("$binary", Convert.ToBase64String(bytes));
                WriteString("$type", ((int)subType).ToString("x2"));
                WriteEndDocument();
            }

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Boolean to the writer.
        /// </summary>
        /// <param name="value">The Boolean value.</param>
        public override void WriteBoolean(bool value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteBoolean", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteNameHelper(Name);
            _textWriter.Write(value ? "true" : "false");

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON DateTime to the writer.
        /// </summary>
        /// <param name="value">The number of milliseconds since the Unix epoch.</param>
        public override void WriteDateTime(long value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteDateTime", BsonWriterState.Value, BsonWriterState.Initial);
            }

            switch (_jsonWriterSettings.OutputMode)
            {
                case JsonOutputMode.Strict:
                    WriteStartDocument();
                    WriteInt64("$date", value);
                    WriteEndDocument();
                    break;
                case JsonOutputMode.JavaScript:
                case JsonOutputMode.TenGen:
                    WriteNameHelper(Name);
                    _textWriter.Write("new Date({0})", value);
                    break;
                case JsonOutputMode.Shell:
                    WriteNameHelper(Name);
                    if (_jsonWriterSettings.ShellVersion >= new Version(1, 8, 0))
                    {
                        // use ISODate for values that fall within .NET's DateTime range, and "new Date" for all others
                        if (value >= BsonConstants.DateTimeMinValueMillisecondsSinceEpoch &&
                            value <= BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch)
                        {
                            var utcDateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(value);
                            _textWriter.Write("ISODate(\"{0}\")", utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
                        }
                        else
                        {
                            _textWriter.Write("new Date({0})", value);
                        }
                    }
                    else
                    {
                        _textWriter.Write("new Date({0})", value);
                    }
                    break;
                default:
                    throw new BsonInternalException("Unexpected JsonOutputMode.");
            }

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Double to the writer.
        /// </summary>
        /// <param name="value">The Double value.</param>
        public override void WriteDouble(double value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteDouble", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteNameHelper(Name);
            // if string representation looks like an integer add ".0" so that it looks like a double
            var stringRepresentation = value.ToString("R", NumberFormatInfo.InvariantInfo);
            if (Regex.IsMatch(stringRepresentation, @"^[+-]?\d+$"))
            {
                stringRepresentation += ".0";
            }
            _textWriter.Write(stringRepresentation);

            State = GetNextState();
        }

        /// <summary>
        /// Writes the end of a BSON array to the writer.
        /// </summary>
        public override void WriteEndArray()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteEndArray", BsonWriterState.Value);
            }

            base.WriteEndArray();
            _textWriter.Write("]");

            _context = _context.ParentContext;
            State = GetNextState();
        }

        /// <summary>
        /// Writes the end of a BSON document to the writer.
        /// </summary>
        public override void WriteEndDocument()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Name)
            {
                ThrowInvalidState("WriteEndDocument", BsonWriterState.Name);
            }

            base.WriteEndDocument();
            if (_jsonWriterSettings.Indent && _context.HasElements)
            {
                _textWriter.Write(_jsonWriterSettings.NewLineChars);
                if (_context.ParentContext != null)
                {
                    _textWriter.Write(_context.ParentContext.Indentation);
                }
                _textWriter.Write("}");
            }
            else
            {
                _textWriter.Write(" }");
            }

            if (_context.ContextType == ContextType.ScopeDocument)
            {
                _context = _context.ParentContext;
                WriteEndDocument();
            }
            else
            {
                _context = _context.ParentContext;
            }

            if (_context == null)
            {
                State = BsonWriterState.Done;
            }
            else
            {
                State = GetNextState();
            }
        }

        /// <summary>
        /// Writes a BSON Int32 to the writer.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
        public override void WriteInt32(int value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteInt32", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteNameHelper(Name);
            _textWriter.Write(value);

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Int64 to the writer.
        /// </summary>
        /// <param name="value">The Int64 value.</param>
        public override void WriteInt64(long value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteInt64", BsonWriterState.Value, BsonWriterState.Initial);
            }

            switch (_jsonWriterSettings.OutputMode)
            {
                case JsonOutputMode.Strict:
                case JsonOutputMode.JavaScript:
                    WriteNameHelper(Name);
                    _textWriter.Write(value);
                    break;
                case JsonOutputMode.TenGen:
                case JsonOutputMode.Shell:
                    WriteNameHelper(Name);
                    if (_jsonWriterSettings.OutputMode == JsonOutputMode.TenGen || _jsonWriterSettings.ShellVersion >= new Version(1, 6, 0))
                    {
                        if (value >= int.MinValue && value <= int.MaxValue)
                        {
                            _textWriter.Write("NumberLong({0})", value);
                        }
                        else
                        {
                            _textWriter.Write("NumberLong(\"{0}\")", value);
                        }
                    }
                    else
                    {
                        _textWriter.Write(value);
                    }
                    break;
                default:
                    WriteNameHelper(Name);
                    _textWriter.Write(value);
                    break;
            }

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON JavaScript to the writer.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public override void WriteJavaScript(string code)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteJavaScript", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteStartDocument();
            WriteString("$code", code);
            WriteEndDocument();

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON JavaScript to the writer (call WriteStartDocument to start writing the scope).
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public override void WriteJavaScriptWithScope(string code)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteJavaScriptWithScope", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteStartDocument();
            WriteString("$code", code);
            WriteName("$scope");

            State = BsonWriterState.ScopeDocument;
        }

        /// <summary>
        /// Writes a BSON MaxKey to the writer.
        /// </summary>
        public override void WriteMaxKey()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteMaxKey", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteStartDocument();
            WriteInt32("$maxkey", 1);
            WriteEndDocument();

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON MinKey to the writer.
        /// </summary>
        public override void WriteMinKey()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteMinKey", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteStartDocument();
            WriteInt32("$minkey", 1);
            WriteEndDocument();

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON null to the writer.
        /// </summary>
        public override void WriteNull()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteNull", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteNameHelper(Name);
            _textWriter.Write("null");

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON ObjectId to the writer.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public override void WriteObjectId(int timestamp, int machine, short pid, int increment)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteObjectId", BsonWriterState.Value, BsonWriterState.Initial);
            }

            var bytes = ObjectId.Pack(timestamp, machine, pid, increment);
            switch (_jsonWriterSettings.OutputMode)
            {
                case JsonOutputMode.Strict:
                case JsonOutputMode.JavaScript:
                    WriteStartDocument();
                    WriteString("$oid", BsonUtils.ToHexString(bytes));
                    WriteEndDocument();
                    break;
                case JsonOutputMode.TenGen:
                case JsonOutputMode.Shell:
                    WriteNameHelper(Name);
                    _textWriter.Write(string.Format("ObjectId(\"{0}\")", BsonUtils.ToHexString(bytes)));
                    break;
            }

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON regular expression to the writer.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public override void WriteRegularExpression(string pattern, string options)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteRegularExpression", BsonWriterState.Value, BsonWriterState.Initial);
            }

            switch (_jsonWriterSettings.OutputMode)
            {
                case JsonOutputMode.Strict:
                    WriteStartDocument();
                    WriteString("$regex", pattern);
                    WriteString("$options", options);
                    WriteEndDocument();
                    break;
                case JsonOutputMode.JavaScript:
                case JsonOutputMode.TenGen:
                case JsonOutputMode.Shell:
                    WriteNameHelper(Name);
                    _textWriter.Write("/");
                    var escaped = (pattern == "") ? "(?:)" : pattern.Replace(@"\", @"\\").Replace("/", @"\/");
                    _textWriter.Write(escaped);
                    _textWriter.Write("/");
                    _textWriter.Write(options);
                    break;
            }

            State = GetNextState();
        }

        /// <summary>
        /// Writes the start of a BSON array to the writer.
        /// </summary>
        public override void WriteStartArray()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteStartArray", BsonWriterState.Value, BsonWriterState.Initial);
            }

            base.WriteStartArray();
            WriteNameHelper(Name);
            _textWriter.Write("[");

            _context = new JsonWriterContext(_context, ContextType.Array, _jsonWriterSettings.IndentChars);
            State = BsonWriterState.Value;
        }

        /// <summary>
        /// Writes the start of a BSON document to the writer.
        /// </summary>
        public override void WriteStartDocument()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial && State != BsonWriterState.ScopeDocument)
            {
                ThrowInvalidState("WriteStartDocument", BsonWriterState.Value, BsonWriterState.Initial, BsonWriterState.ScopeDocument);
            }

            base.WriteStartDocument();
            if (State == BsonWriterState.Value || State == BsonWriterState.ScopeDocument)
            {
                WriteNameHelper(Name);
            }
            _textWriter.Write("{");

            var contextType = (State == BsonWriterState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
            _context = new JsonWriterContext(_context, contextType, _jsonWriterSettings.IndentChars);
            State = BsonWriterState.Name;
        }

        /// <summary>
        /// Writes a BSON String to the writer.
        /// </summary>
        /// <param name="value">The String value.</param>
        public override void WriteString(string value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteString", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteNameHelper(Name);
            WriteStringHelper(value);

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Symbol to the writer.
        /// </summary>
        /// <param name="value">The symbol.</param>
        public override void WriteSymbol(string value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteSymbol", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteStartDocument();
            WriteString("$symbol", value);
            WriteEndDocument();

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON timestamp to the writer.
        /// </summary>
        /// <param name="value">The combined timestamp/increment value.</param>
        public override void WriteTimestamp(long value)
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteTimestamp", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteStartDocument();
            WriteInt64("$timestamp", value);
            WriteEndDocument();

            State = GetNextState();
        }

        /// <summary>
        /// Writes a BSON undefined to the writer.
        /// </summary>
        public override void WriteUndefined()
        {
            if (Disposed) { throw new ObjectDisposedException("JsonWriter"); }
            if (State != BsonWriterState.Value && State != BsonWriterState.Initial)
            {
                ThrowInvalidState("WriteUndefined", BsonWriterState.Value, BsonWriterState.Initial);
            }

            WriteNameHelper(Name);
            _textWriter.Write("undefined");

            State = GetNextState();
        }

        // protected methods
        /// <summary>
        /// Disposes of any resources used by the writer.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
                _textWriter.Dispose();
            }
        }

        // private methods
        private BsonWriterState GetNextState()
        {
            if (_context.ContextType == ContextType.Array)
            {
                return BsonWriterState.Value;
            }
            else
            {
                return BsonWriterState.Name;
            }
        }

        private void WriteNameHelper(string name)
        {
            switch (_context.ContextType)
            {
                case ContextType.Array:
                    // don't write Array element names in Json
                    if (_context.HasElements)
                    {
                        _textWriter.Write(", ");
                    }
                    break;
                case ContextType.Document:
                case ContextType.ScopeDocument:
                    if (_context.HasElements)
                    {
                        _textWriter.Write(",");
                    }
                    if (_jsonWriterSettings.Indent)
                    {
                        _textWriter.Write(_jsonWriterSettings.NewLineChars);
                        _textWriter.Write(_context.Indentation);
                    }
                    else
                    {
                        _textWriter.Write(" ");
                    }
                    WriteStringHelper(name);
                    _textWriter.Write(" : ");
                    break;
                case ContextType.TopLevel:
                    break;
                default:
                    throw new BsonInternalException("Invalid ContextType.");
            }

            _context.HasElements = true;
        }

        private void WriteStringHelper(string value)
        {
            _textWriter.Write("\"");
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': _textWriter.Write("\\\""); break;
                    case '\\': _textWriter.Write("\\\\"); break;
                    case '\b': _textWriter.Write("\\b"); break;
                    case '\f': _textWriter.Write("\\f"); break;
                    case '\n': _textWriter.Write("\\n"); break;
                    case '\r': _textWriter.Write("\\r"); break;
                    case '\t': _textWriter.Write("\\t"); break;
                    default:
                        switch (char.GetUnicodeCategory(c))
                        {
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
                                _textWriter.Write(c);
                                break;
                            default:
                                _textWriter.Write("\\u{0:x4}", (int)c);
                                break;
                        }
                        break;
                }
            }
            _textWriter.Write("\"");
        }
    }
}

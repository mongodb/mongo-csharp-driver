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
    /// Represents a BSON reader for a JSON string.
    /// </summary>
    public class JsonReader : BsonReader {
        #region private fields
        private JsonBuffer buffer;
        private new JsonReaderSettings settings; // same value as in base class just declared as derived class
        private JsonReaderContext context;
        private JsonToken currentToken;
        private BsonValue currentValue;
        private JsonToken pushedToken;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the JsonReader class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="settings">The reader settings.</param>
        public JsonReader(
            JsonBuffer buffer,
            JsonReaderSettings settings
        )
            : base(settings) {
            this.buffer = buffer;
            this.settings = settings; // already frozen by base class
            this.context = new JsonReaderContext(null, ContextType.TopLevel);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Closes the reader.
        /// </summary>
        public override void Close() {
            // Close can be called on Disposed objects
            if (state != BsonReaderState.Closed) {
                state = BsonReaderState.Closed;
            }
        }

        /// <summary>
        /// Gets a bookmark to the reader's current position and state.
        /// </summary>
        /// <returns>A bookmark.</returns>
        public override BsonReaderBookmark GetBookmark() {
            return new JsonReaderBookmark(state, currentBsonType, currentName, context, currentToken, currentValue, pushedToken, buffer.Position);
        }

        /// <summary>
        /// Reads BSON binary data from the reader.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        public override void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType,
            out GuidRepresentation guidRepresentation
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBinaryData", BsonType.Binary);
            state = GetNextState();
            var binaryData = currentValue.AsBsonBinaryData;
            bytes = binaryData.Bytes;
            subType = binaryData.SubType;
            guidRepresentation = binaryData.GuidRepresentation;
        }

        /// <summary>
        /// Reads a BSON boolean from the reader.
        /// </summary>
        /// <returns>A Boolean.</returns>
        public override bool ReadBoolean() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBoolean", BsonType.Boolean);
            state = GetNextState();
            return currentValue.AsBoolean;
        }

        /// <summary>
        /// Reads a BsonType from the reader.
        /// </summary>
        /// <returns>A BsonType.</returns>
        public override BsonType ReadBsonType() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state == BsonReaderState.Initial || state == BsonReaderState.Done || state == BsonReaderState.ScopeDocument) {
                // in JSON the top level value can be of any type so fall through
                state = BsonReaderState.Type;
            }
            if (state != BsonReaderState.Type) {
                ThrowInvalidState("ReadBsonType", BsonReaderState.Type);
            }

            if (context.ContextType == ContextType.Document) {
                var nameToken = PopToken();
                switch (nameToken.Type) {
                    case JsonTokenType.String:
                    case JsonTokenType.UnquotedString:
                        currentName = nameToken.StringValue;
                        break;
                    case JsonTokenType.EndObject:
                        state = BsonReaderState.EndOfDocument;
                        return BsonType.EndOfDocument;
                    default:
                        var message = string.Format("JSON reader was expecting a name but found '{0}'.", nameToken.Lexeme);
                        throw new FileFormatException(message);
                }

                var colonToken = PopToken();
                if (colonToken.Type != JsonTokenType.Colon) {
                    var message = string.Format("JSON reader was expecting ':' but found '{0}'.", colonToken.Lexeme);
                    throw new FileFormatException(message);
                }
            }

            var valueToken = PopToken();
            if (context.ContextType == ContextType.Array && valueToken.Type == JsonTokenType.EndArray) {
                state = BsonReaderState.EndOfArray;
                return BsonType.EndOfDocument;
            }

            switch (valueToken.Type) {
                case JsonTokenType.BeginArray:
                    currentBsonType = BsonType.Array;
                    break;
                case JsonTokenType.BeginObject:
                    currentBsonType = ParseExtendedJson();
                    break;
                case JsonTokenType.DateTime:
                    currentBsonType = BsonType.DateTime;
                    currentValue = valueToken.DateTimeValue;
                    break;
                case JsonTokenType.Double:
                    currentBsonType = BsonType.Double;
                    currentValue = valueToken.DoubleValue;
                    break;
                case JsonTokenType.EndOfFile:
                    currentBsonType = BsonType.EndOfDocument;
                    break;
                case JsonTokenType.Int32:
                    currentBsonType = BsonType.Int32;
                    currentValue = valueToken.Int32Value;
                    break;
                case JsonTokenType.Int64:
                    currentBsonType = BsonType.Int64;
                    currentValue = valueToken.Int64Value;
                    break;
                case JsonTokenType.ObjectId:
                    currentBsonType = BsonType.ObjectId;
                    currentValue = valueToken.ObjectIdValue;
                    break;
                case JsonTokenType.RegularExpression:
                    currentBsonType = BsonType.RegularExpression;
                    currentValue = valueToken.RegularExpressionValue;
                    break;
                case JsonTokenType.String:
                    currentBsonType = BsonType.String;
                    currentValue = valueToken.StringValue;
                    break;
                case JsonTokenType.UnquotedString:
                    var isConstant = true;
                    switch (valueToken.Lexeme) {
                        case "false":
                        case "true":
                            currentBsonType = BsonType.Boolean;
                            currentValue = XmlConvert.ToBoolean(valueToken.Lexeme);
                            break;
                        case "Infinity":
                            currentBsonType = BsonType.Double;
                            currentValue = double.PositiveInfinity;
                            break;
                        case "NaN":
                            currentBsonType = BsonType.Double;
                            currentValue = double.NaN;
                            break;
                        case "null":
                            currentBsonType = BsonType.Null;
                            break;
                        case "undefined":
                            currentBsonType = BsonType.Undefined;
                            break;
                        case "BinData":
                            currentBsonType = BsonType.Binary;
                            currentValue = ParseBinDataConstructor();
                            break;
                        case "Date":
                            currentBsonType = BsonType.String;
                            currentValue = ParseDateTimeConstructor(false); // withNew = false
                            break;
                        case "HexData":
                            currentBsonType = BsonType.Binary;
                            currentValue = ParseHexDataConstructor();
                            break;
                        case "ISODate":
                            currentBsonType = BsonType.DateTime;
                            currentValue = ParseISODateTimeConstructor();
                            break;
                        case "NumberLong":
                            currentBsonType = BsonType.Int64;
                            currentValue = ParseNumberLongConstructor();
                            break;
                        case "ObjectId":
                            currentBsonType = BsonType.ObjectId;
                            currentValue = ParseObjectIdConstructor();
                            break;
                        case "RegExp":
                            currentBsonType = BsonType.RegularExpression;
                            currentValue = ParseRegularExpressionConstructor();
                            break;
                        case "UUID":
                        case "GUID":
                        case "CSUUID":
                        case "CSGUID":
                        case "JUUID":
                        case "JGUID":
                        case "PYUUID":
                        case "PYGUID":
                            currentBsonType = BsonType.Binary;
                            currentValue = ParseUUIDConstructor(valueToken.Lexeme);
                            break;
                        case "new":
                            currentBsonType = ParseNew(out currentValue);
                            break;
                        default:
                            isConstant = false;
                            break;
                    }
                    if (isConstant) {
                        break;
                    } else {
                        goto default;
                    }
                default:
                    var message = string.Format("JSON reader was expecting a value but found '{0}'.", valueToken.Lexeme);
                    throw new FileFormatException(message);
            }
            currentToken = valueToken;

            if (context.ContextType == ContextType.Array || context.ContextType == ContextType.Document) {
                var commaToken = PopToken();
                if (commaToken.Type != JsonTokenType.Comma) {
                    PushToken(commaToken);
                }
            }

            state = (context.ContextType == ContextType.Document) ? BsonReaderState.Name : BsonReaderState.Value;
            return currentBsonType;
        }

        /// <summary>
        /// Reads a BSON DateTime from the reader.
        /// </summary>
        /// <returns>The number of milliseconds since the Unix epoch.</returns>
        public override long ReadDateTime() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            state = GetNextState();
            return currentValue.AsBsonDateTime.MillisecondsSinceEpoch;
        }

        /// <summary>
        /// Reads a BSON Double from the reader.
        /// </summary>
        /// <returns>A Double.</returns>
        public override double ReadDouble() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDouble", BsonType.Double);
            state = GetNextState();
            return currentValue.AsDouble;
        }

        /// <summary>
        /// Reads the end of a BSON array from the reader.
        /// </summary>
        public override void ReadEndArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (context.ContextType != ContextType.Array) {
                ThrowInvalidContextType("ReadEndArray", context.ContextType, ContextType.Array);
            }
            if (state == BsonReaderState.Type) {
                ReadBsonType(); // will set state to EndOfArray if at end of array
            }
            if (state != BsonReaderState.EndOfArray) {
                ThrowInvalidState("ReadEndArray", BsonReaderState.EndOfArray);
            }

            context = context.PopContext();
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReaderState.Type; break;
                case ContextType.Document: state = BsonReaderState.Type; break;
                case ContextType.TopLevel: state = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType.");
            }

            if (context.ContextType == ContextType.Array || context.ContextType == ContextType.Document) {
                var commaToken = PopToken();
                if (commaToken.Type != JsonTokenType.Comma) {
                    PushToken(commaToken);
                }
            }
        }

        /// <summary>
        /// Reads the end of a BSON document from the reader.
        /// </summary>
        public override void ReadEndDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (
                context.ContextType != ContextType.Document &&
                context.ContextType != ContextType.ScopeDocument
            ) {
                ThrowInvalidContextType("ReadEndDocument", context.ContextType, ContextType.Document, ContextType.ScopeDocument);
            }
            if (state == BsonReaderState.Type) {
                ReadBsonType(); // will set state to EndOfDocument if at end of document
            }
            if (state != BsonReaderState.EndOfDocument) {
                ThrowInvalidState("ReadEndDocument", BsonReaderState.EndOfDocument);
            }

            context = context.PopContext();
            if (context != null && context.ContextType == ContextType.JavaScriptWithScope) {
                context = context.PopContext(); // JavaScriptWithScope
                VerifyToken("}"); // outermost closing bracket for JavaScriptWithScope
            }
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReaderState.Type; break;
                case ContextType.Document: state = BsonReaderState.Type; break;
                case ContextType.TopLevel: state = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType");
            }

            if (context.ContextType == ContextType.Array || context.ContextType == ContextType.Document) {
                var commaToken = PopToken();
                if (commaToken.Type != JsonTokenType.Comma) {
                    PushToken(commaToken);
                }
            }
        }

        /// <summary>
        /// Reads a BSON Int32 from the reader.
        /// </summary>
        /// <returns>An Int32.</returns>
        public override int ReadInt32() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            state = GetNextState();
            return currentValue.AsInt32;
        }

        /// <summary>
        /// Reads a BSON Int64 from the reader.
        /// </summary>
        /// <returns>An Int64.</returns>
        public override long ReadInt64() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            state = GetNextState();
            return currentValue.AsInt64;
        }

        /// <summary>
        /// Reads a BSON JavaScript from the reader.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadJavaScript() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScript", BsonType.JavaScript);
            state = GetNextState();
            return currentValue.AsString;
        }

        /// <summary>
        /// Reads a BSON JavaScript with scope from the reader (call ReadStartDocument next to read the scope).
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadJavaScriptWithScope() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScriptWithScope", BsonType.JavaScriptWithScope);
            context = new JsonReaderContext(context, ContextType.JavaScriptWithScope);
            state = BsonReaderState.ScopeDocument;
            return currentValue.AsString;
        }

        /// <summary>
        /// Reads a BSON MaxKey from the reader.
        /// </summary>
        public override void ReadMaxKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMaxKey", BsonType.MaxKey);
            state = GetNextState();
        }

        /// <summary>
        /// Reads a BSON MinKey from the reader.
        /// </summary>
        public override void ReadMinKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMinKey", BsonType.MinKey);
            state = GetNextState();
        }

        /// <summary>
        /// Reads a BSON null from the reader.
        /// </summary>
        public override void ReadNull() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadNull", BsonType.Null);
            state = GetNextState();
        }

        /// <summary>
        /// Reads a BSON ObjectId from the reader.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public override void ReadObjectId(
            out int timestamp,
            out int machine,
            out short pid,
            out int increment
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadObjectId", BsonType.ObjectId);
            var objectId = currentValue.AsObjectId;
            timestamp = objectId.Timestamp;
            machine = objectId.Machine;
            pid = objectId.Pid;
            increment = objectId.Increment;
            state = GetNextState();
        }

        /// <summary>
        /// Reads a BSON regular expression from the reader.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public override void ReadRegularExpression(
            out string pattern,
            out string options
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadRegularExpression", BsonType.RegularExpression);
            state = GetNextState();
            var regex = currentValue.AsBsonRegularExpression;
            pattern = regex.Pattern;
            options = regex.Options;
        }

        /// <summary>
        /// Reads the start of a BSON array.
        /// </summary>
        public override void ReadStartArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartArray", BsonType.Array);

            context = new JsonReaderContext(context, ContextType.Array);
            state = BsonReaderState.Type;
        }

        /// <summary>
        /// Reads the start of a BSON document.
        /// </summary>
        public override void ReadStartDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartDocument", BsonType.Document);

            context = new JsonReaderContext(context, ContextType.Document);
            state = BsonReaderState.Type;
        }

        /// <summary>
        /// Reads a BSON string from the reader.
        /// </summary>
        /// <returns>A String.</returns>
        public override string ReadString() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadString", BsonType.String);
            state = GetNextState();
            return currentValue.AsString;
        }

        /// <summary>
        /// Reads a BSON symbol from the reader.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadSymbol() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadSymbol", BsonType.Symbol);
            state = GetNextState();
            return currentValue.AsString;
        }

        /// <summary>
        /// Reads a BSON timestamp from the reader.
        /// </summary>
        /// <returns>The combined timestamp/increment.</returns>
        public override long ReadTimestamp() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadTimestamp", BsonType.Timestamp);
            state = GetNextState();
            var timestamp = currentValue.AsBsonTimestamp;
            return timestamp.Value;
        }

        /// <summary>
        /// Reads a BSON undefined from the reader.
        /// </summary>
        public override void ReadUndefined() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadUndefined", BsonType.Undefined);
            state = GetNextState();
        }

        /// <summary>
        /// Returns the reader to previously bookmarked position and state.
        /// </summary>
        /// <param name="bookmark">The bookmark.</param>
        public override void ReturnToBookmark(
            BsonReaderBookmark bookmark
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            var jsonReaderBookmark = (JsonReaderBookmark) bookmark;
            state = jsonReaderBookmark.State;
            currentBsonType = jsonReaderBookmark.CurrentBsonType;
            currentName = jsonReaderBookmark.CurrentName;
            context = jsonReaderBookmark.CloneContext();
            currentToken = jsonReaderBookmark.CurrentToken;
            currentValue = jsonReaderBookmark.CurrentValue;
            pushedToken = jsonReaderBookmark.PushedToken;
            buffer.Position = jsonReaderBookmark.Position;
        }

        /// <summary>
        /// Skips the name (reader must be positioned on a name).
        /// </summary>
        public override void SkipName() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Name) {
                ThrowInvalidState("SkipName", BsonReaderState.Name);
            }

            state = BsonReaderState.Value;
        }

        /// <summary>
        /// Skips the value (reader must be positioned on a value).
        /// </summary>
        public override void SkipValue() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Value) {
                ThrowInvalidState("SkipValue", BsonReaderState.Value);
            }

            switch (currentBsonType) {
                case BsonType.Array:
                    ReadStartArray();
                    while (ReadBsonType() != BsonType.EndOfDocument) {
                        SkipValue();
                    }
                    ReadEndArray();
                    break;
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    ReadBinaryData(out bytes, out subType);
                    break;
                case BsonType.Boolean:
                    ReadBoolean();
                    break;
                case BsonType.DateTime:
                    ReadDateTime();
                    break;
                case BsonType.Document:
                    ReadStartDocument();
                    while (ReadBsonType() != BsonType.EndOfDocument) {
                        SkipName();
                        SkipValue();
                    }
                    ReadEndDocument();
                    break;
                case BsonType.Double:
                    ReadDouble();
                    break;
                case BsonType.Int32:
                    ReadInt32();
                    break;
                case BsonType.Int64:
                    ReadInt64();
                    break;
                case BsonType.JavaScript:
                    ReadJavaScript();
                    break;
                case BsonType.JavaScriptWithScope:
                    ReadJavaScriptWithScope();
                    ReadStartDocument();
                    while (ReadBsonType() != BsonType.EndOfDocument) {
                        SkipName();
                        SkipValue();
                    }
                    ReadEndDocument();
                    break;
                case BsonType.MaxKey:
                    ReadMaxKey();
                    break;
                case BsonType.MinKey:
                    ReadMinKey();
                    break;
                case BsonType.Null:
                    ReadNull();
                    break;
                case BsonType.ObjectId:
                    int timestamp, machine, increment;
                    short pid;
                    ReadObjectId(out timestamp, out machine, out pid, out increment);
                    break;
                case BsonType.RegularExpression:
                    string pattern, options;
                    ReadRegularExpression(out pattern, out options);
                    break;
                case BsonType.String:
                    ReadString();
                    break;
                case BsonType.Symbol:
                    ReadSymbol();
                    break;
                case BsonType.Timestamp:
                    ReadTimestamp();
                    break;
                case BsonType.Undefined:
                    ReadUndefined();
                    break;
                default:
                    throw new BsonInternalException("Invalid BsonType.");
            }
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Disposes of any resources used by the reader.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected override void Dispose(
            bool disposing
        ) {
            if (disposing) {
                try {
                    Close();
                } catch { } // ignore exceptions
            }
            base.Dispose(disposing);
        }
        #endregion

        #region private methods
        private string FormatInvalidTokenMessage(
            JsonToken token
        ) {
            return string.Format("Invalid JSON token: '{0}'", token.Lexeme);
        }

        private string FormatJavaScriptDateTimeString(
            DateTime dateTime
        ) {
            var utc = dateTime.ToUniversalTime();
            var local = utc.ToLocalTime();
            var offsetSign = "+";
            var offset = local - utc;
            if (offset < TimeSpan.Zero) {
                offsetSign = "-";
                offset = -offset;
            }
            var timeZone = TimeZoneInfo.Local;
            var timeZoneName = local.IsDaylightSavingTime() ? timeZone.DaylightName : timeZone.StandardName;
            var dateTimeString = string.Format(
                "{0} GMT{1}{2:D2}{3:D2} ({4})",
                local.ToString("ddd MMM dd yyyy HH:mm:ss"),
                offsetSign,
                offset.Hours,
                offset.Minutes,
                timeZoneName
            );
            return dateTimeString;
        }

        private BsonReaderState GetNextState() {
            switch (context.ContextType) {
                case ContextType.Array:
                case ContextType.Document:
                    return BsonReaderState.Type;
                case ContextType.TopLevel:
                    return BsonReaderState.Done;
                default:
                    throw new BsonInternalException("Unexpected ContextType.");
            }
        }

        private BsonValue ParseBinDataConstructor() {
            VerifyToken("(");
            var subTypeToken = PopToken();
            if (subTypeToken.Type != JsonTokenType.Int32) {
                var message = string.Format("JSON reader expected a binary subtype but found '{0}'.", subTypeToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken(",");
            var bytesToken = PopToken();
            if (bytesToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", bytesToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken(")");
            var bytes = Convert.FromBase64String(bytesToken.StringValue);
            var subType = (BsonBinarySubType) subTypeToken.Int32Value;
            GuidRepresentation guidRepresentation;
            switch (subType) {
                case BsonBinarySubType.UuidLegacy: guidRepresentation = settings.GuidRepresentation; break;
                case BsonBinarySubType.UuidStandard: guidRepresentation = GuidRepresentation.Standard; break;
                default: guidRepresentation = GuidRepresentation.Unspecified; break;
            }
            return new BsonBinaryData(bytes, subType, guidRepresentation);
        }

        private BsonValue ParseBinDataExtendedJson() {
            VerifyToken(":");
            var bytesToken = PopToken();
            if (bytesToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", bytesToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken(",");
            VerifyString("$type");
            VerifyToken(":");
            var subTypeToken = PopToken();
            if (subTypeToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", subTypeToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            var bytes = Convert.FromBase64String(bytesToken.StringValue);
            var subType = (BsonBinarySubType) Convert.ToInt32(subTypeToken.StringValue, 16);
            GuidRepresentation guidRepresentation;
            switch (subType) {
                case BsonBinarySubType.UuidLegacy: guidRepresentation = settings.GuidRepresentation; break;
                case BsonBinarySubType.UuidStandard: guidRepresentation = GuidRepresentation.Standard; break;
                default: guidRepresentation = GuidRepresentation.Unspecified; break;
            }
            return new BsonBinaryData(bytes, subType, guidRepresentation);
        }

        private BsonValue ParseHexDataConstructor() {
            VerifyToken("(");
            var subTypeToken = PopToken();
            if (subTypeToken.Type != JsonTokenType.Int32) {
                var message = string.Format("JSON reader expected a binary subtype but found '{0}'.", subTypeToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken(",");
            var bytesToken = PopToken();
            if (bytesToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", bytesToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken(")");
            var bytes = BsonUtils.ParseHexString(bytesToken.StringValue);
            var subType = (BsonBinarySubType) subTypeToken.Int32Value;
            GuidRepresentation guidRepresentation;
            switch (subType) {
                case BsonBinarySubType.UuidLegacy: guidRepresentation = settings.GuidRepresentation; break;
                case BsonBinarySubType.UuidStandard: guidRepresentation = GuidRepresentation.Standard; break;
                default: guidRepresentation = GuidRepresentation.Unspecified; break;
            }
            return new BsonBinaryData(bytes, subType, guidRepresentation);
        }

        private BsonType ParseJavaScriptExtendedJson(
            out BsonValue value
        ) {
            VerifyToken(":");
            var codeToken = PopToken();
            if (codeToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", codeToken.Lexeme);
                throw new FileFormatException(message);
            }
            var nextToken = PopToken();
            switch (nextToken.Type) {
                case JsonTokenType.Comma:
                    VerifyString("$scope");
                    VerifyToken(":");
                    state = BsonReaderState.Value;
                    value = codeToken.StringValue;
                    return BsonType.JavaScriptWithScope;
                case JsonTokenType.EndObject:
                    value = codeToken.StringValue;
                    return BsonType.JavaScript;
                default:
                    var message = string.Format("JSON reader expected ',' or '}' but found '{0}'.", codeToken.Lexeme);
                    throw new FileFormatException(message);
            }
        }

        private BsonValue ParseISODateTimeConstructor() {
            VerifyToken("(");
            var valueToken = PopToken();
            if (valueToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", valueToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken(")");
            var formats = new string[] {
                            "yyyy-MM-ddK",
                            "yyyy-MM-ddTHH:mm:ssK",
                            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
                        };
            var utcDateTime = DateTime.ParseExact(valueToken.StringValue, formats, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            return new BsonDateTime(utcDateTime);
        }

        private BsonValue ParseDateTimeExtendedJson() {
            VerifyToken(":");
            var valueToken = PopToken();
            if (valueToken.Type != JsonTokenType.Int32 && valueToken.Type != JsonTokenType.Int64) {
                var message = string.Format("JSON reader expected an integer but found '{0}'.", valueToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            return new BsonDateTime(valueToken.Int64Value);
        }

        private BsonValue ParseDateTimeConstructor(
            bool withNew
        ) {
            VerifyToken("(");

            // Date when used without "new" behaves differently (JavaScript has some weird parts)
            if (!withNew) {
                VerifyToken(")");
                var dateTimeString = FormatJavaScriptDateTimeString(DateTime.UtcNow);
                return new BsonString(dateTimeString);
            }

            var token = PopToken();
            if (token.Lexeme == ")") {
                return new BsonDateTime(DateTime.UtcNow);
            } else if (token.Type == JsonTokenType.String) {
                VerifyToken(")");
                var dateTimeString = token.StringValue;
                var dateTime = ParseJavaScriptDateTimeString(dateTimeString);
                return new BsonDateTime(dateTime);
            } else if (token.Type == JsonTokenType.Int32 || token.Type == JsonTokenType.Int64) {
                var args = new List<long>();
                while (true) {
                    args.Add(token.Int64Value);
                    token = PopToken();
                    if (token.Lexeme == ")") {
                        break;
                    }
                    if (token.Lexeme != ",") {
                        var message = string.Format("JSON reader expected a ',' or a ')' but found '{0}'.", token.Lexeme);
                        throw new FileFormatException(message);
                    }
                    token = PopToken();
                    if (token.Type != JsonTokenType.Int32 && token.Type != JsonTokenType.Int64) {
                        var message = string.Format("JSON reader expected an integer but found '{0}'.", token.Lexeme);
                        throw new FileFormatException(message);
                    }
                }
                switch (args.Count) {
                    case 1:
                        return new BsonDateTime(args[0]);
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        var year = (int) args[0];
                        var month = (int) args[1] + 1; // JavaScript starts at 0 but .NET starts at 1
                        var day = (int) args[2];
                        var hours = (args.Count >= 4) ? (int) args[3] : 0;
                        var minutes = (args.Count >= 5) ? (int) args[4] : 0;
                        var seconds = (args.Count >= 6) ? (int) args[5] : 0;
                        var milliseconds = (args.Count == 7) ? (int) args[6] : 0;
                        var dateTime = new DateTime(year, month, day, hours, minutes, seconds, milliseconds, DateTimeKind.Utc);
                        return new BsonDateTime(dateTime);
                    default:
                        var message = string.Format("JSON reader expected 1 or 3-7 integers but found {0}.", args.Count);
                        throw new FileFormatException(message);
                }
            } else {
                var message = string.Format("JSON reader expected an integer or a string but found '{0}'.", token.Lexeme);
                throw new FileFormatException(message);
            }
        }

        private BsonType ParseExtendedJson() {
            var nameToken = PopToken();
            if (nameToken.Type == JsonTokenType.String || nameToken.Type == JsonTokenType.UnquotedString) {
                switch (nameToken.StringValue) {
                    case "$binary": currentValue = ParseBinDataExtendedJson(); return BsonType.Binary;
                    case "$code": return ParseJavaScriptExtendedJson(out currentValue);
                    case "$date": currentValue = ParseDateTimeExtendedJson(); return BsonType.DateTime;
                    case "$maxkey": currentValue = ParseMaxKeyExtendedJson(); return BsonType.MaxKey;
                    case "$minkey": currentValue = ParseMinKeyExtendedJson(); return BsonType.MinKey;
                    case "$oid": currentValue = ParseObjectIdExtendedJson(); return BsonType.ObjectId;
                    case "$regex": currentValue = ParseRegularExpressionExtendedJson(); return BsonType.RegularExpression;
                    case "$symbol": currentValue = ParseSymbolExtendedJson(); return BsonType.Symbol;
                    case "$timestamp": currentValue = ParseTimestampExtendedJson(); return BsonType.Timestamp;
                }
            }
            PushToken(nameToken);
            return BsonType.Document;
        }

        private DateTime ParseJavaScriptDateTimeString(
            string dateTimeString
        ) {
            // with some minor tweaks we can make a JavaScript dateTimeString acceptable to DateTime.Parse
            dateTimeString = Regex.Replace(dateTimeString, @" +\(.+\)$", ""); // remove timeZone name
            dateTimeString = Regex.Replace(dateTimeString, @"GMT([+-])(\d\d)(\d\d)", @"$1$2:$3"); // replace GMT+hhmm with +hh:mm
            return DateTime.Parse(dateTimeString);
        }

        private BsonValue ParseMaxKeyExtendedJson() {
            VerifyToken(":");
            VerifyToken("1");
            VerifyToken("}");
            return BsonMaxKey.Value;
        }

        private BsonValue ParseMinKeyExtendedJson() {
            VerifyToken(":");
            VerifyToken("1");
            VerifyToken("}");
            return BsonMinKey.Value;
        }

        private BsonType ParseNew(
            out BsonValue value
        ) {
            var typeToken = PopToken();
            if (typeToken.Type != JsonTokenType.UnquotedString) {
                var message = string.Format("JSON reader expected a type name but found '{0}'.", typeToken.Lexeme);
                throw new FileFormatException(message);
            }
            switch (typeToken.Lexeme) {
                case "BinData":
                    value = ParseBinDataConstructor();
                    return BsonType.Binary;
                case "Date":
                    value = ParseDateTimeConstructor(true); // withNew = true
                    return BsonType.DateTime;
                case "HexData":
                    value = ParseHexDataConstructor();
                    return BsonType.Binary;
                case "ISODate":
                    value = ParseISODateTimeConstructor();
                    return BsonType.DateTime;
                case "NumberLong":
                    value = ParseNumberLongConstructor();
                    return BsonType.Int64;
                case "ObjectId":
                    value = ParseObjectIdConstructor();
                    return BsonType.ObjectId;
                case "RegExp":
                    value = ParseRegularExpressionConstructor();
                    return BsonType.RegularExpression;
                case "UUID":
                case "GUID":
                case "CSUUID":
                case "CSGUID":
                case "JUUID":
                case "JGUID":
                case "PYUUID":
                case "PYGUID":
                    value = ParseUUIDConstructor(typeToken.Lexeme);
                    return BsonType.Binary;
                default:
                    var message = string.Format("JSON reader expected a type name but found '{0}'.", typeToken.Lexeme);
                    throw new FileFormatException(message);
            }
        }

        private BsonValue ParseNumberLongConstructor() {
            VerifyToken("(");
            var valueToken = PopToken();
            long value;
            if (valueToken.Type == JsonTokenType.Int32 || valueToken.Type == JsonTokenType.Int64) {
                value = valueToken.Int64Value;
            } else if (valueToken.Type == JsonTokenType.String) {
                value = long.Parse(valueToken.StringValue);
            } else {
                var message = string.Format("JSON reader expected an integer or a string but found '{0}'.", valueToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken(")");
            return BsonInt64.Create(value);
        }

        private BsonValue ParseObjectIdConstructor() {
            VerifyToken("(");
            var valueToken = PopToken();
            if (valueToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", valueToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken(")");
            return BsonObjectId.Create(valueToken.StringValue);
        }

        private BsonValue ParseObjectIdExtendedJson() {
            VerifyToken(":");
            var valueToken = PopToken();
            if (valueToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", valueToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            return BsonObjectId.Create(valueToken.StringValue);
        }

        private BsonValue ParseRegularExpressionConstructor() {
            VerifyToken("(");
            var patternToken = PopToken();
            if (patternToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", patternToken.Lexeme);
                throw new FileFormatException(message);
            }
            var options = "";
            var commaToken = PopToken();
            if (commaToken.Lexeme == ",") {
                var optionsToken = PopToken();
                if (optionsToken.Type != JsonTokenType.String) {
                    var message = string.Format("JSON reader expected a string but found '{0}'.", optionsToken.Lexeme);
                    throw new FileFormatException(message);
                }
                options = optionsToken.StringValue;
            } else {
                PushToken(commaToken);
            }
            VerifyToken(")");
            return BsonRegularExpression.Create(patternToken.StringValue, options);
        }

        private BsonValue ParseRegularExpressionExtendedJson() {
            VerifyToken(":");
            var patternToken = PopToken();
            if (patternToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", patternToken.Lexeme);
                throw new FileFormatException(message);
            }
            var options = "";
            var commaToken = PopToken();
            if (commaToken.Lexeme == ",") {
                VerifyString("$options");
                VerifyToken(":");
                var optionsToken = PopToken();
                if (optionsToken.Type != JsonTokenType.String) {
                    var message = string.Format("JSON reader expected a string but found '{0}'.", optionsToken.Lexeme);
                    throw new FileFormatException(message);
                }
                options = optionsToken.StringValue;
            } else {
                PushToken(commaToken);
            }
            VerifyToken("}");
            return BsonRegularExpression.Create(patternToken.StringValue, options);
        }

        private BsonValue ParseSymbolExtendedJson() {
            VerifyToken(":");
            var nameToken = PopToken();
            if (nameToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", nameToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            return BsonString.Create(nameToken.StringValue); // will be converted to a BsonSymbol at a higher level
        }

        private BsonValue ParseTimestampExtendedJson() {
            VerifyToken(":");
            var valueToken = PopToken();
            long value;
            if (valueToken.Type == JsonTokenType.Int32 || valueToken.Type == JsonTokenType.Int64) {
                value = valueToken.Int64Value;
            } else if (valueToken.Type == JsonTokenType.UnquotedString && valueToken.Lexeme == "NumberLong") {
                value = ParseNumberLongConstructor().AsInt64;
            } else {
                var message = string.Format("JSON reader expected an integer but found '{0}'.", valueToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            return BsonTimestamp.Create(value);
        }

        private BsonValue ParseUUIDConstructor(
            string uuidConstructorName
        ) {
            VerifyToken("(");
            var bytesToken = PopToken();
            if (bytesToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found '{0}'.", bytesToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken(")");
            var hexString = bytesToken.StringValue.Replace("{", "").Replace("}", "").Replace("-", "");
            var bytes = BsonUtils.ParseHexString(hexString);
            var guid = GuidConverter.FromBytes(bytes, GuidRepresentation.Standard);
            GuidRepresentation guidRepresentation;
            switch (uuidConstructorName) {
                case "CSUUID":
                case "CSGUID":
                    guidRepresentation = GuidRepresentation.CSharpLegacy;
                    break;
                case "JUUID":
                case "JGUID":
                    guidRepresentation = GuidRepresentation.JavaLegacy;
                    break;
                case "PYUUID":
                case "PYGUID":
                    guidRepresentation = GuidRepresentation.PythonLegacy;
                    break;
                case "UUID":
                case "GUID":
                    guidRepresentation = GuidRepresentation.Standard;
                    break;
                default:
                    throw new BsonInternalException("Unexpected uuidConstructorName");
            }
            bytes = GuidConverter.ToBytes(guid, guidRepresentation);
            var subType = (guidRepresentation == GuidRepresentation.Standard) ? BsonBinarySubType.UuidStandard : BsonBinarySubType.UuidLegacy;
            return new BsonBinaryData(bytes, subType, guidRepresentation);
        }

        private JsonToken PopToken() {
            if (pushedToken != null) {
                var token = pushedToken;
                pushedToken = null;
                return token;
            } else {
                return JsonScanner.GetNextToken(buffer);
            }
        }

        private void PushToken(
            JsonToken token
        ) {
            if (pushedToken == null) {
                pushedToken = token;
            } else {
                throw new BsonInternalException("There is already a pending token.");
            }
        }

        private void VerifyString(
            string expectedString
        ) {
            var token = PopToken();
            if ((token.Type != JsonTokenType.String && token.Type != JsonTokenType.UnquotedString) || token.StringValue != expectedString) {
                var message = string.Format("JSON reader expected '{0}' but found '{1}'.", expectedString, token.StringValue);
                throw new FileFormatException(message);
            }
        }

        private void VerifyToken(
            string expectedLexeme
        ) {
            var token = PopToken();
            if (token.Lexeme != expectedLexeme) {
                var message = string.Format("JSON reader expected '{0}' but found '{1}'.", expectedLexeme, token.Lexeme);
                throw new FileFormatException(message);
            }
        }
        #endregion
    }
}

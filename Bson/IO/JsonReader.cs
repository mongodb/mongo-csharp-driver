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
using System.Xml;

namespace MongoDB.Bson.IO {
    /// <summary>
    /// Represents a BSON reader for a JSON string.
    /// </summary>
    public class JsonReader : BsonBaseReader {
        #region private fields
        private JsonBuffer buffer;
        private JsonReaderContext context;
        private JsonToken currentToken;
        private BsonValue currentValue;
        private JsonToken pushedToken;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the JsonReader class.
        /// </summary>
        /// <param name="buffer"></param>
        public JsonReader(
            JsonBuffer buffer
        ) {
            this.buffer = buffer;
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
        public override void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBinaryData", BsonType.Binary);
            state = GetNextState();
            var binaryData = currentValue.AsBsonBinaryData;
            bytes = binaryData.Bytes;
            subType = binaryData.SubType;
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
                var message = string.Format("ReadBsonType cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
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
                        var message = string.Format("JSON reader was expecting a name but found: '{0}'", nameToken.Lexeme);
                        throw new FileFormatException(message);
                }

                var colonToken = PopToken();
                if (colonToken.Type != JsonTokenType.Colon) {
                    var message = string.Format("JSON reader was expecting ':' but found: '{0}'", colonToken.Lexeme);
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
                    var validConstant = true;
                    switch (valueToken.Lexeme) {
                        case "true":
                        case "false":
                            currentBsonType = BsonType.Boolean;
                            currentValue = XmlConvert.ToBoolean(valueToken.Lexeme);
                            break;
                        case "null":
                            currentBsonType = BsonType.Null;
                            break;
                        case "undefined":
                            currentBsonType = BsonType.Undefined;
                            break;
                        default:
                            validConstant = false;
                            break;
                    }
                    if (validConstant) {
                        break;
                    } else {
                        goto default;
                    }
                default:
                    var message = string.Format("JSON reader was expecting a value but found: '{0}'", valueToken.Lexeme);
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
        /// <returns>A DateTime.</returns>
        public override DateTime ReadDateTime() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            state = GetNextState();
            return currentValue.AsDateTime;
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
                var message = string.Format("ReadEndArray cannot be called when ContextType is: {0}", context.ContextType);
                throw new InvalidOperationException(message);
            }
            if (state == BsonReaderState.Type) {
                ReadBsonType(); // will set state to EndOfArray if at end of array
            }
            if (state != BsonReaderState.EndOfArray) {
                var message = string.Format("ReadEndArray cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context = context.PopContext();
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
        /// Reads the end of a BSON document from the reader.
        /// </summary>
        public override void ReadEndDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (
                context.ContextType != ContextType.Document &&
                context.ContextType != ContextType.ScopeDocument
            ) {
                var message = string.Format("ReadEndDocument cannot be called when ContextType is: {0}", context.ContextType);
                throw new InvalidOperationException(message);
            }
            if (state == BsonReaderState.Type) {
                ReadBsonType(); // will set state to EndOfDocument if at end of document
            }
            if (state != BsonReaderState.EndOfDocument) {
                var message = string.Format("ReadEndDocument cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
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
        /// Reads a BSON regular expression element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
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
                var message = string.Format("SkipName cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            state = BsonReaderState.Value;
        }

        /// <summary>
        /// Skips the value (reader must be positioned on a value).
        /// </summary>
        public override void SkipValue() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Value) {
                var message = string.Format("SkipValue cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
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
                    throw new BsonInternalException("Invalid BsonType");
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

        private BsonReaderState GetNextState() {
            switch (context.ContextType) {
                case ContextType.Array:
                case ContextType.Document:
                    return BsonReaderState.Type;
                case ContextType.TopLevel:
                    return BsonReaderState.Done;
                default:
                    throw new BsonInternalException("Unexpected ContextType");
            }
        }

        private BsonType ParseBinary() {
            VerifyToken(":");
            var bytesToken = PopToken();
            if (bytesToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found: '{0}'", bytesToken.Lexeme);
                throw new FileFormatException(message);
            }
            var bytes = Convert.FromBase64String(bytesToken.StringValue);
            VerifyToken(",");
            VerifyString("$type");
            VerifyToken(":");
            var subTypeToken = PopToken();
            if (subTypeToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found: '{0}'", subTypeToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            var subType = (BsonBinarySubType) Convert.ToInt32(subTypeToken.StringValue, 16);
            currentValue = new BsonBinaryData(bytes, subType);
            return BsonType.Binary;
        }

        private BsonType ParseJavaScript() {
            VerifyToken(":");
            var codeToken = PopToken();
            if (codeToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found: '{0}'", codeToken.Lexeme);
                throw new FileFormatException(message);
            }
            var nextToken = PopToken();
            switch (nextToken.Type) {
                case JsonTokenType.Comma:
                    VerifyString("$scope");
                    VerifyToken(":");
                    state = BsonReaderState.Value;
                    currentBsonType = BsonType.JavaScriptWithScope;
                    currentValue = codeToken.StringValue;
                    return BsonType.JavaScriptWithScope;
                case JsonTokenType.EndObject:
                    currentValue = codeToken.StringValue;
                    return BsonType.JavaScript;
                default:
                    var message = string.Format("JSON reader expected ',' or '}' but found: '{0}'", codeToken.Lexeme);
                    throw new FileFormatException(message);
            }
        }

        private BsonType ParseDateTime() {
            VerifyToken(":");
            var valueToken = PopToken();
            if (valueToken.Type != JsonTokenType.Int32 && valueToken.Type != JsonTokenType.Int64) {
                var message = string.Format("JSON reader expected an integer but found: '{0}'", valueToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            currentValue = BsonConstants.UnixEpoch.AddMilliseconds(valueToken.Int64Value);
            return BsonType.DateTime;
        }

        private BsonType ParseExtendedJson() {
            var nameToken = PopToken();
            if (nameToken.Type == JsonTokenType.String || nameToken.Type == JsonTokenType.UnquotedString) {
                switch (nameToken.StringValue) {
                    case "$binary": return ParseBinary();
                    case "$code": return ParseJavaScript();
                    case "$date": return ParseDateTime();
                    case "$maxkey": return ParseMaxKey();
                    case "$minkey": return ParseMinKey();
                    case "$oid": return ParseObjectId();
                    case "$regex": return ParseRegularExpression();
                    case "$symbol": return ParseSymbol();
                    case "$timestamp": return ParseTimestamp();
                }
            }
            PushToken(nameToken);
            return BsonType.Document;
        }

        private BsonType ParseMaxKey() {
            VerifyToken(":");
            VerifyToken("1");
            VerifyToken("}");
            return BsonType.MaxKey;
        }

        private BsonType ParseMinKey() {
            VerifyToken(":");
            VerifyToken("1");
            VerifyToken("}");
            return BsonType.MinKey;
        }

        private BsonType ParseObjectId() {
            VerifyToken(":");
            var valueToken = PopToken();
            if (valueToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found: '{0}'", valueToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            currentValue = BsonObjectId.Create(valueToken.StringValue);
            return BsonType.ObjectId;
        }

        private BsonType ParseRegularExpression() {
            VerifyToken(":");
            var patternToken = PopToken();
            if (patternToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found: '{0}'", patternToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken(",");
            VerifyString("$options");
            VerifyToken(":");
            var optionsToken = PopToken();
            if (optionsToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found: '{0}'", optionsToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            currentValue = BsonRegularExpression.Create(patternToken.StringValue, optionsToken.StringValue);
            return BsonType.RegularExpression;
        }

        private BsonType ParseSymbol() {
            VerifyToken(":");
            var nameToken = PopToken();
            if (nameToken.Type != JsonTokenType.String) {
                var message = string.Format("JSON reader expected a string but found: '{0}'", nameToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            currentValue = BsonString.Create(nameToken.StringValue); // will be converted to a BsonSymbol at a higher level
            return BsonType.Symbol;
        }

        private BsonType ParseTimestamp() {
            VerifyToken(":");
            var valueToken = PopToken();
            if (valueToken.Type != JsonTokenType.Int32 && valueToken.Type != JsonTokenType.Int64) {
                var message = string.Format("JSON reader expected an integer but found: '{0}'", valueToken.Lexeme);
                throw new FileFormatException(message);
            }
            VerifyToken("}");
            currentValue = BsonTimestamp.Create(valueToken.Int64Value);
            return BsonType.Timestamp;
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
                throw new BsonInternalException("There is already a pending token");
            }
        }

        private void VerifyString(
            string expectedString
        ) {
            var token = PopToken();
            if (token.Type != JsonTokenType.String || token.StringValue != expectedString) {
                var message = string.Format("JSON reader expected '{0}' but found: '{1}'", expectedString, token.StringValue);
                throw new FileFormatException(message);
            }
        }

        private void VerifyToken(
            string expectedLexeme
        ) {
            var token = PopToken();
            if (token.Lexeme != expectedLexeme) {
                var message = string.Format("JSON reader expected '{0}' but found: '{1}'", expectedLexeme, token.Lexeme);
                throw new FileFormatException(message);
            }
        }
        #endregion
    }
}

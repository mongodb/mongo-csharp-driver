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
using System.Xml;

namespace MongoDB.Bson.IO {
    public class BsonJsonReader : BsonBaseReader {
        #region private fields
        private TextReader textReader;
        private BsonJsonReaderContext context;
        private JsonToken currentToken;
        private JsonToken pushedToken;
        #endregion

        #region constructors
        public BsonJsonReader(
            TextReader textReader
        ) {
            this.textReader = textReader;
            this.context = new BsonJsonReaderContext(null, ContextType.TopLevel);
        }
        #endregion

        #region public methods
        public override void Close() {
            // Close can be called on Disposed objects
            if (state != BsonReadState.Closed) {
                state = BsonReadState.Closed;
            }
        }

        public override BsonReaderBookmark GetBookmark() {
            throw new NotImplementedException();
        }

        public override void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBinaryData", BsonType.Binary);

            throw new NotImplementedException();
        }

        public override bool ReadBoolean() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBoolean", BsonType.Boolean);
            state = GetNextState();
            return XmlConvert.ToBoolean(currentToken.Lexeme);
        }

        public override BsonType ReadBsonType() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state == BsonReadState.Initial) {
                // in JSON the top level value can be of any type so fall through
                state = BsonReadState.Type;
            }
            if (state != BsonReadState.Type) {
                var message = string.Format("ReadBsonType cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            if (context.ContextType == ContextType.Document) {
                var nameToken = PopToken();
                switch (nameToken.Type) {
                    case JsonTokenType.String:
                    case JsonTokenType.UnquotedString:
                        currentName = nameToken.Lexeme;
                        break;
                    case JsonTokenType.EndObject:
                        state = BsonReadState.EndOfDocument;
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
                state = BsonReadState.EndOfArray;
                return BsonType.EndOfDocument;
            }

            switch (valueToken.Type) {
                case JsonTokenType.BeginArray: currentBsonType = BsonType.Array; break;
                case JsonTokenType.BeginObject: currentBsonType = BsonType.Document; break;
                case JsonTokenType.FloatingPoint: currentBsonType = BsonType.Double; break;
                case JsonTokenType.Integer: currentBsonType = valueToken.IntegerBsonType; break;
                case JsonTokenType.String: currentBsonType = BsonType.String; break;
                case JsonTokenType.UnquotedString:
                    var validConstant = true;
                    switch (valueToken.Lexeme) {
                        case "true":
                        case "false":
                            currentBsonType = BsonType.Boolean;
                            break;
                        case "null":
                            currentBsonType = BsonType.Null;
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

            state = (context.ContextType == ContextType.Document) ? BsonReadState.Name : BsonReadState.Value;
            return currentBsonType;
        }

        public override DateTime ReadDateTime() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            throw new NotImplementedException();
        }

        public override double ReadDouble() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDouble", BsonType.Double);
            state = GetNextState();
            return XmlConvert.ToDouble(currentToken.Lexeme);
        }

        public override void ReadEndArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (context.ContextType != ContextType.Array) {
                var message = string.Format("ReadEndArray cannot be called when ContextType is: {0}", context.ContextType);
                throw new InvalidOperationException(message);
            }
            if (state == BsonReadState.Type) {
                ReadBsonType(); // will set state to EndOfArray if at end of array
            }
            if (state != BsonReadState.EndOfArray) {
                var message = string.Format("ReadEndArray cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context = context.PopContext();
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReadState.Type; break;
                case ContextType.Document: state = BsonReadState.Type; break;
                case ContextType.TopLevel: state = BsonReadState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType");
            }
        }

        public override void ReadEndDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (context.ContextType != ContextType.Document) {
                var message = string.Format("ReadEndDocument cannot be called when ContextType is: {0}", context.ContextType);
                throw new InvalidOperationException(message);
            }
            if (state == BsonReadState.Type) {
                ReadBsonType(); // will set state to EndOfDocument if at end of document
            }
            if (state != BsonReadState.EndOfDocument) {
                var message = string.Format("ReadEndDocument cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context = context.PopContext();
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReadState.Type; break;
                case ContextType.Document: state = BsonReadState.Type; break;
                case ContextType.TopLevel: state = BsonReadState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType");
            }
        }

        public override int ReadInt32() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            state = GetNextState();
            return (int) currentToken.IntegerValue;
        }

        public override long ReadInt64() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            state = GetNextState();
            return currentToken.IntegerValue;
        }

        public override string ReadJavaScript() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScript", BsonType.JavaScript);
            throw new NotImplementedException();
        }

        public override string ReadJavaScriptWithScope() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScriptWithScope", BsonType.JavaScriptWithScope);
            throw new NotImplementedException();
        }

        public override void ReadMaxKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMaxKey", BsonType.MaxKey);
            throw new NotImplementedException();
        }

        public override void ReadMinKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMinKey", BsonType.MinKey);
            throw new NotImplementedException();
        }

        public override void ReadNull() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadNull", BsonType.Null);
            state = BsonReadState.Type;
        }

        public override void ReadObjectId(
            out int timestamp,
            out int machine,
            out short pid,
            out int increment
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadObjectId", BsonType.ObjectId);
            throw new NotImplementedException();
        }

        public override void ReadRegularExpression(
            out string pattern,
            out string options
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadRegularExpression", BsonType.RegularExpression);
            throw new NotImplementedException();
        }

        public override void ReadStartArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartArray", BsonType.Array);

            context = new BsonJsonReaderContext(context, ContextType.Array);
            state = BsonReadState.Type;
        }

        public override void ReadStartDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartDocument", BsonType.Document);

            context = new BsonJsonReaderContext(context, ContextType.Document);
            state = BsonReadState.Type;
        }

        public override string ReadString() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadString", BsonType.String);
            state = GetNextState();
            return currentToken.Lexeme;
        }

        public override string ReadSymbol() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadSymbol", BsonType.Symbol);
            throw new NotImplementedException();
        }

        public override long ReadTimestamp() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadTimestamp", BsonType.Timestamp);
            throw new NotImplementedException();
        }

        public override void ReturnToBookmark(
            BsonReaderBookmark bookmark
        ) {
            throw new NotImplementedException();
        }

        public override void SkipName() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReadState.Name) {
                var message = string.Format("SkipName cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            state = BsonReadState.Value;
        }

        public override void SkipValue() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReadState.Value) {
                var message = string.Format("SkipValue cannot be called when ReadState is: {0}", state);
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
                default:
                    throw new BsonInternalException("Invalid BsonType");
            }
        }
        #endregion

        #region protected methods
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

        private BsonReadState GetNextState() {
            switch (context.ContextType) {
                case ContextType.Array:
                case ContextType.Document:
                    return BsonReadState.Type;
                case ContextType.TopLevel:
                    return BsonReadState.Done;
                default:
                    throw new BsonInternalException("Unexpected ContextType");
            }
        }

        private JsonToken PopToken() {
            if (pushedToken != null) {
                var token = pushedToken;
                pushedToken = null;
                return token;
            } else {
                return BsonJsonScanner.GetNextToken(textReader);
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
        #endregion
    }
}

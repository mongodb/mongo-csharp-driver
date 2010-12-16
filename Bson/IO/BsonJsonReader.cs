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
        private string currentName;
        private JsonToken currentToken;
        private JsonToken pushedToken;
        private long currentInteger; // used when currentToken is an integer to avoid extra call to XmlConvert.ToInt64
        #endregion

        #region constructors
        public BsonJsonReader(
            TextReader textReader
        ) {
            this.textReader = textReader;
            this.context = new BsonJsonReaderContext(null, ContextType.TopLevel);
        }
        #endregion

        #region public properties
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
            if (
                state != BsonReadState.Initial &&
                state != BsonReadState.Type
            ) {
                var message = string.Format("ReadBsonType cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            var token = PopToken();
            if (context.ContextType == ContextType.Array) {
                if (token.Type == JsonTokenType.EndArray) {
                    PushToken(token);
                    state = BsonReadState.EndOfArray;
                    return BsonType.EndOfDocument;
                }
            } else if (context.ContextType == ContextType.Document) {
                switch (token.Type) {
                    case JsonTokenType.String:
                    case JsonTokenType.UnquotedString:
                        currentName = token.Lexeme;
                        break;
                    case JsonTokenType.EndObject:
                        PushToken(token);
                        state = BsonReadState.EndOfDocument;
                        return BsonType.EndOfDocument;
                    default:
                        throw new FileFormatException(FormatInvalidTokenMessage(token));
                }

                token = PopToken();
                if (token.Type != JsonTokenType.Colon) {
                    throw new FileFormatException(FormatInvalidTokenMessage(token));
                }

                token = PopToken(); // value token
            }

            currentBsonType = token.BsonType;
            switch (currentBsonType) {
                case BsonType.Array:
                case BsonType.Document:
                    PushToken(token);
                    return currentBsonType;
            }

            currentToken = token;
            currentInteger = token.Integer;

            if (context.ContextType == ContextType.Array || context.ContextType == ContextType.Document) {
                token = PopToken();
                if (token.Type != JsonTokenType.Comma) {
                    PushToken(token);
                }
            }

            switch (context.ContextType) {
                case ContextType.Array:
                case ContextType.TopLevel:
                    state = BsonReadState.Value;
                    break;
                case ContextType.Document:
                    state = BsonReadState.Name;
                    break;
                default:
                    throw new BsonInternalException("Unexpected ContextType");
            }

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
                var token = PopToken();
                if (token.Type != JsonTokenType.EndArray) {
                    throw new FileFormatException("Expecting '}'");
                }
                state = BsonReadState.EndOfArray;
            }
            if (state != BsonReadState.EndOfArray) {
                var message = string.Format("ReadEndArray cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context = context.PopContext();
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReadState.Type; break;
                case ContextType.Document: state = BsonReadState.Name; break;
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
                var token = PopToken();
                if (token.Type != JsonTokenType.EndObject) {
                    throw new FileFormatException("Expecting '}'");
                }
                state = BsonReadState.EndOfDocument;
            }
            if (state != BsonReadState.EndOfDocument) {
                var message = string.Format("ReadEndDocument cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context = context.PopContext();
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReadState.Type; break;
                case ContextType.Document: state = BsonReadState.Name; break;
                case ContextType.TopLevel: state = BsonReadState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType");
            }
        }

        public override int ReadInt32() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            state = GetNextState();
            return (int) currentInteger;
        }

        public override long ReadInt64() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            state = GetNextState();
            return currentInteger;
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

        public override string ReadName() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state == BsonReadState.EndOfDocument) {
                return null;
            }
            if (state != BsonReadState.Name) {
                var message = string.Format("ReadName cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            state = BsonReadState.Value;
            return currentName;
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
            if (
                state != BsonReadState.Initial &&
                (state != BsonReadState.Value || currentBsonType != BsonType.Array)
            ) {
                string message = string.Format("ReadStartArray cannot be called when ReadState is: {0} and BsonType is: {1}", state, currentBsonType);
                throw new InvalidOperationException(message);
            }

            var token = PopToken();
            if (token.Type != JsonTokenType.BeginArray) {
                throw new FileFormatException(FormatInvalidTokenMessage(token));
            }

            context = new BsonJsonReaderContext(context, ContextType.Array);
            state = BsonReadState.Type;
        }

        public override void ReadStartDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (
                state != BsonReadState.Initial &&
                state != BsonReadState.ScopeDocument &&
                (state != BsonReadState.Value || currentBsonType != BsonType.Document)
            ) {
                string message = string.Format("ReadStartDocument cannot be called when ReadState is: {0} and BsonType is: {1}", state, currentBsonType);
                throw new InvalidOperationException(message);
            }

            var token = PopToken();
            if (token.Type != JsonTokenType.BeginObject) {
                throw new FileFormatException(FormatInvalidTokenMessage(token));
            }

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

            throw new NotImplementedException();
        }

        public override void SkipValue() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReadState.Value) {
                var message = string.Format("SkipValue cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }
            throw new NotImplementedException();
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

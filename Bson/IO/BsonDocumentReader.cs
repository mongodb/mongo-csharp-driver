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
    public class BsonDocumentReader : BsonBaseReader {
        #region private fields
        private BsonDocumentReaderContext context;
        private BsonValue currentValue;
        #endregion

        #region constructors
        public BsonDocumentReader(
            BsonDocument document
        ) {
            context = new BsonDocumentReaderContext(null, ContextType.TopLevel, document);
            currentValue = document;
        }
        #endregion

        #region public properties
        #endregion

        #region public methods
        public override void Close() {
            // Close can be called on Disposed objects
            if (state != BsonReaderState.Closed) {
                state = BsonReaderState.Closed;
            }
        }

        public override BsonReaderBookmark GetBookmark() {
            return new BsonDocumentReaderBookmark(state, currentBsonType, currentName, context, currentValue);
        }

        public override void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBinaryData", BsonType.Binary);

            var binaryData = currentValue.AsBsonBinaryData;
            bytes = binaryData.Bytes;
            subType = binaryData.SubType;
            state = GetNextState();
        }

        public override bool ReadBoolean() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBoolean", BsonType.Boolean);
            state = GetNextState();
            return currentValue.AsBoolean;
        }

        public override BsonType ReadBsonType() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state == BsonReaderState.Initial || state == BsonReaderState.ScopeDocument) {
                // there is an implied type of Document for the top level and for scope documents
                currentBsonType = BsonType.Document;
                state = BsonReaderState.Value;
                return currentBsonType;
            }
            if (state != BsonReaderState.Type) {
                var message = string.Format("ReadBsonType cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            switch (context.ContextType) {
                case ContextType.Array:
                    currentValue = context.GetNextValue();
                    if (currentValue == null) {
                        state = BsonReaderState.EndOfArray;
                        return BsonType.EndOfDocument;
                    }
                    state = BsonReaderState.Value;
                    break;
                case ContextType.Document:
                    var currentElement = context.GetNextElement();
                    if (currentElement == null) {
                        state = BsonReaderState.EndOfDocument;
                        return BsonType.EndOfDocument;
                    }
                    currentName = currentElement.Name;
                    currentValue = currentElement.Value;
                    state = BsonReaderState.Name;
                    break;
                default:
                    throw new BsonInternalException("Invalid ContextType");
            }

            currentBsonType = currentValue.BsonType;
            return currentBsonType;
        }

        public override DateTime ReadDateTime() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            state = GetNextState();
            return currentValue.AsDateTime;
        }

        public override double ReadDouble() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDouble", BsonType.Double);
            state = GetNextState();
            return currentValue.AsDouble;
        }

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
        }

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
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReaderState.Type; break;
                case ContextType.Document: state = BsonReaderState.Type; break;
                case ContextType.TopLevel: state = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType");
            }
        }

        public override int ReadInt32() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            state = GetNextState();
            return currentValue.AsInt32;
        }

        public override long ReadInt64() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            state = GetNextState();
            return currentValue.AsInt64;
        }

        public override string ReadJavaScript() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScript", BsonType.JavaScript);
            state = GetNextState();
            return currentValue.AsBsonJavaScript.Code;
        }

        public override string ReadJavaScriptWithScope() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScriptWithScope", BsonType.JavaScriptWithScope);

            state = BsonReaderState.ScopeDocument;
            return currentValue.AsBsonJavaScriptWithScope.Code;
        }

        public override void ReadMaxKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMaxKey", BsonType.MaxKey);
            state = GetNextState();
        }

        public override void ReadMinKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMinKey", BsonType.MinKey);
            state = GetNextState();
        }

        public override void ReadNull() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadNull", BsonType.Null);
            state = GetNextState();
        }

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

        public override void ReadRegularExpression(
            out string pattern,
            out string options
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadRegularExpression", BsonType.RegularExpression);
            var regex = currentValue.AsBsonRegularExpression;
            pattern = regex.Pattern;
            options = regex.Options;
            state = GetNextState();
        }

        public override void ReadStartArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartArray", BsonType.Array);

            var array = currentValue.AsBsonArray;
            context = new BsonDocumentReaderContext(context, ContextType.Array, array);
            state = BsonReaderState.Type;
        }

        public override void ReadStartDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartDocument", BsonType.Document);

            BsonDocument document;
            var script = currentValue as BsonJavaScriptWithScope;
            if (script != null) {
                document = script.Scope;
            } else {
                document = currentValue.AsBsonDocument;
            }
            context = new BsonDocumentReaderContext(context, ContextType.Document, document);
            state = BsonReaderState.Type;
        }

        public override string ReadString() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadString", BsonType.String);
            state = GetNextState();
            return currentValue.AsString;
        }

        public override string ReadSymbol() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadSymbol", BsonType.Symbol);
            state = GetNextState();
            return currentValue.AsBsonSymbol.Name;
        }

        public override long ReadTimestamp() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadTimestamp", BsonType.Timestamp);
            state = GetNextState();
            return currentValue.AsBsonTimestamp.Value;
        }

        public override void ReturnToBookmark(
            BsonReaderBookmark bookmark
        ) {
            var documentReaderBookmark = (BsonDocumentReaderBookmark) bookmark;
            state = documentReaderBookmark.State;
            currentBsonType = documentReaderBookmark.CurrentBsonType;
            currentName = documentReaderBookmark.CurrentName;
            context = documentReaderBookmark.CloneContext();
            currentValue = documentReaderBookmark.CurrentValue;
        }

        public override void SkipName() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Name) {
                var message = string.Format("SkipName cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            state = BsonReaderState.Value;
        }

        public override void SkipValue() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Value) {
                var message = string.Format("SkipValue cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }
            state = BsonReaderState.Type;
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
        #endregion
    }
}

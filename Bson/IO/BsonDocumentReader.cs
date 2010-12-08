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

namespace MongoDB.Bson.IO {
    public class BsonDocumentReader : BsonBaseReader {
        #region private fields
        private BsonDocumentReaderContext context;
        private BsonElement currentElement;
        #endregion

        #region constructors
        public BsonDocumentReader(
            BsonDocument document
        ) {
            context = new BsonDocumentReaderContext(null, ContextType.Document, document);
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
            return new BsonDocumentReaderBookmark(context, state, currentBsonType);
        }

        public override void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBinaryData", BsonType.Binary);

            var binaryData = currentElement.Value.AsBsonBinaryData;
            bytes = binaryData.Bytes;
            subType = binaryData.SubType;
            state = BsonReadState.Type;
        }

        public override bool ReadBoolean() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadBoolean", BsonType.Boolean);
            state = BsonReadState.Type;
            return currentElement.Value.AsBoolean;
        }

        public override BsonType ReadBsonType() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReadState.Type) {
                var message = string.Format("ReadBsonType cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            currentElement = context.GetNextElement();
            currentBsonType = (currentElement != null) ? currentElement.Value.BsonType : BsonType.EndOfDocument; // set currentBsonType before state
            state = (currentBsonType == BsonType.EndOfDocument) ? BsonReadState.EndOfDocument : BsonReadState.Name;
            return currentBsonType;
        }

        public override DateTime ReadDateTime() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            state = BsonReadState.Type;
            return currentElement.Value.AsDateTime;
        }

        public override double ReadDouble() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDouble", BsonType.Double);
            state = BsonReadState.Type;
            return currentElement.Value.AsDouble;
        }

        public override void ReadEndArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (context.ContextType != ContextType.Array) {
                var message = string.Format("ReadEndArray cannot be called when ContextType is: {0}", context.ContextType);
                throw new InvalidOperationException(message);
            }
            if (state == BsonReadState.Type && context.GetNextElement() == null) {
                // automatically advance to EndOfDocument state
                state = BsonReadState.EndOfDocument;
            }
            if (state != BsonReadState.EndOfDocument) {
                var message = string.Format("ReadEndArray cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context = context.PopContext();
            state = BsonReadState.Type;
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
            if (state == BsonReadState.Type && context.GetNextElement() == null) {
                // automatically advance to EndOfDocument state
                state = BsonReadState.EndOfDocument;
            }
            if (state != BsonReadState.EndOfDocument) {
                var message = string.Format("ReadEndDocument cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            context = context.PopContext();
            state = (context == null) ? BsonReadState.Done : BsonReadState.Type;
        }

        public override int ReadInt32() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            state = BsonReadState.Type;
            return currentElement.Value.AsInt32;
        }

        public override long ReadInt64() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            state = BsonReadState.Type;
            return currentElement.Value.AsInt64;
        }

        public override string ReadJavaScript() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScript", BsonType.JavaScript);
            state = BsonReadState.Type;
            return currentElement.Value.AsBsonJavaScript.Code;
        }

        public override string ReadJavaScriptWithScope() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScriptWithScope", BsonType.JavaScriptWithScope);

            state = BsonReadState.ScopeDocument;
            return currentElement.Value.AsBsonJavaScriptWithScope.Code;
        }

        public override void ReadMaxKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMaxKey", BsonType.MaxKey);
            state = BsonReadState.Type;
        }

        public override void ReadMinKey() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadMinKey", BsonType.MinKey);
            state = BsonReadState.Type;
        }

        public override string ReadName() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReadState.Name) {
                var message = string.Format("ReadName cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            state = BsonReadState.Value;
            return currentElement.Name;
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
            var objectId = currentElement.Value.AsObjectId;
            timestamp = objectId.Timestamp;
            machine = objectId.Machine;
            pid = objectId.Pid;
            increment = objectId.Increment;
            state = BsonReadState.Type;
        }

        public override void ReadRegularExpression(
            out string pattern,
            out string options
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadRegularExpression", BsonType.RegularExpression);
            var regex = currentElement.Value.AsBsonRegularExpression;
            pattern = regex.Pattern;
            options = regex.Options;
            state = BsonReadState.Type;
        }

        public override void ReadStartArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReadState.Value || currentBsonType != BsonType.Array) {
                string message = string.Format("ReadStartArray cannot be called when ReadState is: {0} and BsonType is: {1}", state, currentBsonType);
                throw new InvalidOperationException(message);
            }

            var array = currentElement.Value.AsBsonArray;
            context = new BsonDocumentReaderContext(context, ContextType.Array, array);
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

            if (state == BsonReadState.ScopeDocument) {
                var scope = currentElement.Value.AsBsonJavaScriptWithScope.Scope;
                context = new BsonDocumentReaderContext(context, ContextType.ScopeDocument, scope);
            } else if (state == BsonReadState.Value) {
                var document = currentElement.Value.AsBsonDocument;
                context = new BsonDocumentReaderContext(context, ContextType.Document, document);
            }
            state = BsonReadState.Type;
        }

        public override string ReadString() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadString", BsonType.String);
            state = BsonReadState.Type;
            return currentElement.Value.AsString;
        }

        public override string ReadSymbol() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadSymbol", BsonType.Symbol);
            state = BsonReadState.Type;
            return currentElement.Value.AsBsonSymbol.Name;
        }

        public override long ReadTimestamp() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadTimestamp", BsonType.Timestamp);
            state = BsonReadState.Type;
            return currentElement.Value.AsBsonTimestamp.Value;
        }

        public override void ReturnToBookmark(
            BsonReaderBookmark bookmark
        ) {
            var documentReaderBookmark = (BsonDocumentReaderBookmark) bookmark;
            context = documentReaderBookmark.Context;
            state = documentReaderBookmark.State;
            currentBsonType = documentReaderBookmark.CurrentBsonType;
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
            state = BsonReadState.Type;
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
        #endregion
    }
}

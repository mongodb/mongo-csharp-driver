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
    public class BsonJsonReader : BsonBaseReader {
        #region private fields
        private TextReader textReader;
        #endregion

        #region constructors
        public BsonJsonReader(
            TextReader textReader
        ) {
            this.textReader = textReader;
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
            throw new NotImplementedException();
        }

        public override BsonType ReadBsonType() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReadState.Type) {
                var message = string.Format("ReadBsonType cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            throw new NotImplementedException();
        }

        public override DateTime ReadDateTime() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDateTime", BsonType.DateTime);
            throw new NotImplementedException();
        }

        public override double ReadDouble() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadDouble", BsonType.Double);
            throw new NotImplementedException();
        }

        public override void ReadEndArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            throw new NotImplementedException();
        }

        public override void ReadEndDocument() {
            if (disposed) { ThrowObjectDisposedException(); }
            throw new NotImplementedException();
        }

        public override int ReadInt32() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt32", BsonType.Int32);
            throw new NotImplementedException();
        }

        public override long ReadInt64() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadInt64", BsonType.Int64);
            throw new NotImplementedException();
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
            if (state != BsonReadState.Name) {
                var message = string.Format("ReadName cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

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
            if (state != BsonReadState.Value || currentBsonType != BsonType.Array) {
                string message = string.Format("ReadStartArray cannot be called when ReadState is: {0} and BsonType is: {1}", state, currentBsonType);
                throw new InvalidOperationException(message);
            }

            throw new NotImplementedException();
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

            throw new NotImplementedException();
        }

        public override string ReadString() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadString", BsonType.String);
            throw new NotImplementedException();
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
        #endregion
    }
}

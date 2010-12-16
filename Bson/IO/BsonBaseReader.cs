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
    public abstract class BsonBaseReader : BsonReader {
        #region protected fields
        protected bool disposed = false;
        protected BsonReadState state;
        protected BsonType currentBsonType;
        #endregion

        #region constructors
        protected BsonBaseReader() {
            state = BsonReadState.Initial;
            currentBsonType = BsonType.Document;
        }
        #endregion

        #region public properties
        public override BsonType CurrentBsonType {
            get {
                if (state == BsonReadState.Initial || state == BsonReadState.Done || state == BsonReadState.ScopeDocument) {
                    return BsonType.Document; // the root level is sort of like sitting at a value of type Document
                }
                if (state != BsonReadState.Value) {
                    var message = string.Format("CurrentBsonType cannot be called when ReadState is: {0}", state);
                    throw new InvalidOperationException(message);
                }
                return currentBsonType;
            }
        }

        public override BsonReadState ReadState {
            get { return state; }
        }
        #endregion

        #region public methods
        public override void Dispose() {
            if (!disposed) {
                Dispose(true);
                disposed = true;
            }
        }

        // looks for an element of the given name and leaves the reader positioned at the value
        // or at EndOfDocument if the element is not found
        public override bool FindElement(
            string name
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReadState.Type) {
                var message = string.Format("FindElement cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            BsonType bsonType;
            while ((bsonType = ReadBsonType()) != BsonType.EndOfDocument) {
                var elementName = ReadName();
                if (elementName == name) {
                    return true;
                }
                SkipValue();
            }

            return false;
        }

        // this is like ReadString but scans ahead to find a string element with the desired name
        // it leaves the reader positioned just after the value (i.e. at the BsonType of the next element)
        // or at EndOfDocument if the element is not found
        public override string FindString(
            string name
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReadState.Type) {
                var message = string.Format("FindString cannot be called when ReadState is: {0}", state);
                throw new InvalidOperationException(message);
            }

            BsonType bsonType;
            while ((bsonType = ReadBsonType()) != BsonType.EndOfDocument) {
                var elementName = ReadName();
                if (bsonType == BsonType.String && elementName == name) {
                    return ReadString();
                } else {
                    SkipValue();
                }
            }

            return null;
        }

        public override void ReadBinaryData(
            string name,
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
            VerifyName(name);
            ReadBinaryData(out bytes, out subType);
        }

        public override bool ReadBoolean(
            string name
        ) {
            VerifyName(name);
            return ReadBoolean();
        }

        public override DateTime ReadDateTime(
            string name
        ) {
            VerifyName(name);
            return ReadDateTime();
        }

        public override double ReadDouble(
            string name
        ) {
            VerifyName(name);
            return ReadDouble();
        }

        public override int ReadInt32(
            string name
        ) {
            VerifyName(name);
            return ReadInt32();
        }

        public override long ReadInt64(
            string name
         ) {
            VerifyName(name);
            return ReadInt64();
        }

        public override string ReadJavaScript(
            string name
        ) {
            VerifyName(name);
            return ReadJavaScript();
        }

        public override string ReadJavaScriptWithScope(
            string name
        ) {
            VerifyName(name);
            return ReadJavaScriptWithScope();
        }

        public override void ReadMaxKey(
            string name
        ) {
            VerifyName(name);
            ReadMaxKey();
        }

        public override void ReadMinKey(
            string name
        ) {
            VerifyName(name);
            ReadMinKey();
        }

        public override void ReadNull(
            string name
        ) {
            VerifyName(name);
            ReadNull();
        }

        public override void ReadObjectId(
            string name,
            out int timestamp,
            out int machine,
            out short pid,
            out int increment
        ) {
            VerifyName(name);
            ReadObjectId(out timestamp, out machine, out pid, out increment);
        }

        public override void ReadRegularExpression(
            string name,
            out string pattern,
            out string options
        ) {
            VerifyName(name);
            ReadRegularExpression(out pattern, out options);
        }

        public override string ReadString(
            string name
         ) {
            VerifyName(name);
            return ReadString();
        }

        public override string ReadSymbol(
            string name
         ) {
            VerifyName(name);
            return ReadSymbol();
        }

        public override long ReadTimestamp(
            string name
         ) {
            VerifyName(name);
            return ReadTimestamp();
        }
        #endregion

        #region protected methods
        protected virtual void Dispose(
            bool disposing
        ) {
        }

        protected void ThrowObjectDisposedException() {
            throw new ObjectDisposedException(this.GetType().Name);
        }

        protected void VerifyBsonType(
            string methodName,
            BsonType requiredBsonType
        ) {
            if (state != BsonReadState.Value) {
                var message = string.Format("{0} cannot be called when ReadState is: {1}", methodName, state);
                throw new InvalidOperationException(message);
            }
            if (currentBsonType != requiredBsonType) {
                var message = string.Format("{0} cannot be called when BsonType is: {1}", methodName, currentBsonType);
                throw new InvalidOperationException(message);
            }
        }

        protected void VerifyName(
            string expectedName
        ) {
            ReadBsonType();
            var actualName = ReadName();
            if (actualName != expectedName) {
                var message = string.Format("Element name is not: {0}", expectedName);
                throw new FileFormatException(message);
            }
        }
        #endregion
    }
}

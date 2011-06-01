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
    /// <summary>
    /// A base class for the various implementations of BsonReader.
    /// </summary>
    public abstract class BsonBaseReader : BsonReader {
        #region protected fields
        /// <summary>
        /// Whether the reader has been disposed.
        /// </summary>
        protected bool disposed = false;
        /// <summary>
        /// The current state of the reader.
        /// </summary>
        protected BsonReaderState state;
        /// <summary>
        /// The current BSON type.
        /// </summary>
        protected BsonType currentBsonType;
        /// <summary>
        /// The name of the current element.
        /// </summary>
        protected string currentName;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBaseReader class.
        /// </summary>
        protected BsonBaseReader() {
            state = BsonReaderState.Initial;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the current BsonType.
        /// </summary>
        public override BsonType CurrentBsonType {
            get {
                if (state == BsonReaderState.Initial || state == BsonReaderState.Done || state == BsonReaderState.ScopeDocument || state == BsonReaderState.Type) {
                    ReadBsonType();
                }
                if (state != BsonReaderState.Value) {
                    ThrowInvalidState("CurrentBsonType", BsonReaderState.Value);
                }
                return currentBsonType;
            }
        }

        /// <summary>
        /// Gets the current state of the reader.
        /// </summary>
        public override BsonReaderState State {
            get { return state; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Disposes of any resources used by the reader.
        /// </summary>
        public override void Dispose() {
            if (!disposed) {
                Dispose(true);
                disposed = true;
            }
        }

        /// <summary>
        /// Positions the reader to an element by name.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>True if the element was found.</returns>
        public override bool FindElement(
            string name
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Type) {
                ThrowInvalidState("FindElement", BsonReaderState.Type);
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

        /// <summary>
        /// Positions the reader to a string element by name.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>True if the element was found.</returns>
        public override string FindStringElement(
            string name
        ) {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state != BsonReaderState.Type) {
                ThrowInvalidState("FindStringElement", BsonReaderState.Type);
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

        /// <summary>
        /// Reads a BSON binary data element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        public override void ReadBinaryData(
            string name,
            out byte[] bytes,
            out BsonBinarySubType subType
        ) {
            VerifyName(name);
            ReadBinaryData(out bytes, out subType);
        }

        /// <summary>
        /// Reads a BSON boolean element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A Boolean.</returns>
        public override bool ReadBoolean(
            string name
        ) {
            VerifyName(name);
            return ReadBoolean();
        }

        /// <summary>
        /// Reads a BSON DateTime element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>The number of milliseconds since the Unix epoch.</returns>
        public override long ReadDateTime(
            string name
        ) {
            VerifyName(name);
            return ReadDateTime();
        }

        /// <summary>
        /// Reads a BSON Double element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A Double.</returns>
        public override double ReadDouble(
            string name
        ) {
            VerifyName(name);
            return ReadDouble();
        }

        /// <summary>
        /// Reads a BSON Int32 element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>An Int32.</returns>
        public override int ReadInt32(
            string name
        ) {
            VerifyName(name);
            return ReadInt32();
        }

        /// <summary>
        /// Reads a BSON Int64 element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>An Int64.</returns>
        public override long ReadInt64(
            string name
        ) {
            VerifyName(name);
            return ReadInt64();
        }

        /// <summary>
        /// Reads a BSON JavaScript element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A string.</returns>
        public override string ReadJavaScript(
            string name
        ) {
            VerifyName(name);
            return ReadJavaScript();
        }

        /// <summary>
        /// Reads a BSON JavaScript with scope element from the reader (call ReadStartDocument next to read the scope).
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A string.</returns>
        public override string ReadJavaScriptWithScope(
            string name
        ) {
            VerifyName(name);
            return ReadJavaScriptWithScope();
        }

        /// <summary>
        /// Reads a BSON MaxKey element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void ReadMaxKey(
            string name
        ) {
            VerifyName(name);
            ReadMaxKey();
        }

        /// <summary>
        /// Reads a BSON MinKey element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void ReadMinKey(
            string name
        ) {
            VerifyName(name);
            ReadMinKey();
        }

        /// <summary>
        /// Reads the name of an element from the reader.
        /// </summary>
        /// <returns>The name of the element.</returns>
        public override string ReadName() {
            if (disposed) { ThrowObjectDisposedException(); }
            if (state == BsonReaderState.Type) {
                ReadBsonType();
            }
            if (state != BsonReaderState.Name) {
                ThrowInvalidState("ReadName", BsonReaderState.Name);
            }

            state = BsonReaderState.Value;
            return currentName;
        }

        /// <summary>
        /// Reads the name of an element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void ReadName(
            string name
        ) {
            VerifyName(name);
        }

        /// <summary>
        /// Reads a BSON null element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void ReadNull(
            string name
        ) {
            VerifyName(name);
            ReadNull();
        }

        /// <summary>
        /// Reads a BSON ObjectId element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
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

        /// <summary>
        /// Reads a BSON regular expression element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public override void ReadRegularExpression(
            string name,
            out string pattern,
            out string options
        ) {
            VerifyName(name);
            ReadRegularExpression(out pattern, out options);
        }

        /// <summary>
        /// Reads a BSON string element from the reader.
        /// </summary>
        /// <returns>A String.</returns>
        /// <param name="name">The name of the element.</param>
        public override string ReadString(
            string name
         ) {
            VerifyName(name);
            return ReadString();
        }

        /// <summary>
        /// Reads a BSON symbol element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A string.</returns>
        public override string ReadSymbol(
            string name
         ) {
            VerifyName(name);
            return ReadSymbol();
        }

        /// <summary>
        /// Reads a BSON timestamp element from the reader.
        /// </summary>
        /// <returns>The combined timestamp/increment.</returns>
        /// <param name="name">The name of the element.</param>
        public override long ReadTimestamp(
            string name
         ) {
            VerifyName(name);
            return ReadTimestamp();
        }

        /// <summary>
        /// Reads a BSON undefined element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void ReadUndefined(
            string name
        ) {
            VerifyName(name);
            ReadUndefined();
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Disposes of any resources used by the reader.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected virtual void Dispose(
            bool disposing
        ) {
        }

        /// <summary>
        /// Throws an InvalidOperationException when the method called is not valid for the current ContextType.
        /// </summary>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="actualContextType">The actual ContextType.</param>
        /// <param name="validContextTypes">The valid ContextTypes.</param>
        protected void ThrowInvalidContextType(
            string methodName,
            ContextType actualContextType,
            params ContextType[] validContextTypes
        ) {
            var validContextTypesString = string.Join(" or ", validContextTypes.Select(c => c.ToString()).ToArray());
            var message = string.Format("{0} can only be called when ContextType is {1}, not when ContextType is {2}.", methodName, validContextTypesString, actualContextType);
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Throws an InvalidOperationException when the method called is not valid for the current state.
        /// </summary>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="validStates">The valid states.</param>
        protected void ThrowInvalidState(
            string methodName,
            params BsonReaderState[] validStates
        ) {
            var validStatesString = string.Join(" or ", validStates.Select(s => s.ToString()).ToArray());
            var message = string.Format("{0} can only be called when State is {1}, not when State is {2}.", methodName, validStatesString, state);
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Throws an ObjectDisposedException.
        /// </summary>
        protected void ThrowObjectDisposedException() {
            throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        /// Verifies the current state and BsonType of the reader.
        /// </summary>
        /// <param name="methodName">The name of the method calling this one.</param>
        /// <param name="requiredBsonType">The required BSON type.</param>
        protected void VerifyBsonType(
            string methodName,
            BsonType requiredBsonType
        ) {
            if (state == BsonReaderState.Initial || state == BsonReaderState.ScopeDocument || state == BsonReaderState.Type) {
                ReadBsonType();
            }
            if (state == BsonReaderState.Name) {
                // ignore name
                state = BsonReaderState.Value;
            }
            if (state != BsonReaderState.Value) {
                ThrowInvalidState(methodName, BsonReaderState.Value);
            }
            if (currentBsonType != requiredBsonType) {
                var message = string.Format("{0} can only be called when CurrentBsonType is {1}, not when CurrentBsonType is {2}.", methodName, requiredBsonType, currentBsonType);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Verifies the name of the current element.
        /// </summary>
        /// <param name="expectedName">The expected name.</param>
        protected void VerifyName(
            string expectedName
        ) {
            ReadBsonType();
            var actualName = ReadName();
            if (actualName != expectedName) {
                var message = string.Format("Expected element name to be '{0}', not '{1}'.", expectedName, actualName);
                throw new FileFormatException(message);
            }
        }
        #endregion
    }
}

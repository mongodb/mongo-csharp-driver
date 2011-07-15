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
    /// Represents a BSON reader for a BsonDocument.
    /// </summary>
    public class BsonDocumentReader : BsonReader {
        #region private fields
        private new BsonDocumentReaderSettings settings; // same value as in base class just declared as derived class
        private BsonDocumentReaderContext context;
        private BsonValue currentValue;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDocumentReader class.
        /// </summary>
        /// <param name="document">A BsonDocument.</param>
        /// <param name="settings">The reader settings.</param>
        public BsonDocumentReader(
            BsonDocument document,
            BsonDocumentReaderSettings settings
        )
            : base(settings) {
            context = new BsonDocumentReaderContext(null, ContextType.TopLevel, document);
            currentValue = document;
            this.settings = settings; // already frozen by base class
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
            return new BsonDocumentReaderBookmark(state, currentBsonType, currentName, context, currentValue);
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

            var binaryData = currentValue.AsBsonBinaryData;
            bytes = binaryData.Bytes;
            subType = binaryData.SubType;
            guidRepresentation = binaryData.GuidRepresentation;
            state = GetNextState();
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
            if (state == BsonReaderState.Initial || state == BsonReaderState.ScopeDocument) {
                // there is an implied type of Document for the top level and for scope documents
                currentBsonType = BsonType.Document;
                state = BsonReaderState.Value;
                return currentBsonType;
            }
            if (state != BsonReaderState.Type) {
                ThrowInvalidState("ReadBsonType", BsonReaderState.Type);
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
                    throw new BsonInternalException("Invalid ContextType.");
            }

            currentBsonType = currentValue.BsonType;
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
            switch (context.ContextType) {
                case ContextType.Array: state = BsonReaderState.Type; break;
                case ContextType.Document: state = BsonReaderState.Type; break;
                case ContextType.TopLevel: state = BsonReaderState.Done; break;
                default: throw new BsonInternalException("Unexpected ContextType.");
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
            return currentValue.AsBsonJavaScript.Code;
        }

        /// <summary>
        /// Reads a BSON JavaScript with scope from the reader (call ReadStartDocument next to read the scope).
        /// </summary>
        /// <returns>A string.</returns>
        public override string ReadJavaScriptWithScope() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadJavaScriptWithScope", BsonType.JavaScriptWithScope);

            state = BsonReaderState.ScopeDocument;
            return currentValue.AsBsonJavaScriptWithScope.Code;
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
            var regex = currentValue.AsBsonRegularExpression;
            pattern = regex.Pattern;
            options = regex.Options;
            state = GetNextState();
        }

        /// <summary>
        /// Reads the start of a BSON array.
        /// </summary>
        public override void ReadStartArray() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadStartArray", BsonType.Array);

            var array = currentValue.AsBsonArray;
            context = new BsonDocumentReaderContext(context, ContextType.Array, array);
            state = BsonReaderState.Type;
        }

        /// <summary>
        /// Reads the start of a BSON document.
        /// </summary>
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
            return currentValue.AsBsonSymbol.Name;
        }

        /// <summary>
        /// Reads a BSON timestamp from the reader.
        /// </summary>
        /// <returns>The combined timestamp/increment.</returns>
        public override long ReadTimestamp() {
            if (disposed) { ThrowObjectDisposedException(); }
            VerifyBsonType("ReadTimestamp", BsonType.Timestamp);
            state = GetNextState();
            return currentValue.AsBsonTimestamp.Value;
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
            var documentReaderBookmark = (BsonDocumentReaderBookmark) bookmark;
            state = documentReaderBookmark.State;
            currentBsonType = documentReaderBookmark.CurrentBsonType;
            currentName = documentReaderBookmark.CurrentName;
            context = documentReaderBookmark.CloneContext();
            currentValue = documentReaderBookmark.CurrentValue;
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
            state = BsonReaderState.Type;
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
        #endregion
    }
}

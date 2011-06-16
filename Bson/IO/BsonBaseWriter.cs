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
    /// A base class for the various implementations of BsonWriter.
    /// </summary>
    public abstract class BsonBaseWriter : BsonWriter {
        #region protected fields
        /// <summary>
        /// Whether the object has been disposed.
        /// </summary>
        protected bool disposed = false;
        /// <summary>
        /// The current state of the writer.
        /// </summary>
        protected BsonWriterState state;
        /// <summary>
        /// The name of the current element.
        /// </summary>
        protected string name;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBaseWriter class.
        /// </summary>
        protected BsonBaseWriter() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the current state of the writer.
        /// </summary>
        public override BsonWriterState State {
            get { return state; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Disposes of any resources used by the writer.
        /// </summary>
        public override void Dispose() {
            if (!disposed) {
                Dispose(true);
                disposed = true;
            }
        }

        /// <summary>
        /// Writes a BSON binary data element to the writer.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        public override void WriteBinaryData(
            byte[] bytes,
            BsonBinarySubType subType
        ) {
            var guidRepresentation = (subType == BsonBinarySubType.UuidStandard) ? GuidRepresentation.Standard : GuidRepresentation.Unspecified;
            WriteBinaryData(bytes, subType, guidRepresentation);
        }

        /// <summary>
        /// Writes a BSON binary data element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        public override void WriteBinaryData(
            string name,
            byte[] bytes,
            BsonBinarySubType subType
        ) {
            WriteName(name);
            WriteBinaryData(bytes, subType);
        }

        /// <summary>
        /// Writes a BSON binary data element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        public override void WriteBinaryData(
            string name,
            byte[] bytes,
            BsonBinarySubType subType,
            GuidRepresentation guidRepresentation
        ) {
            WriteName(name);
            WriteBinaryData(bytes, subType, guidRepresentation);
        }

        /// <summary>
        /// Writes a BSON Boolean element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Boolean value.</param>
        public override void WriteBoolean(
            string name,
            bool value
        ) {
            WriteName(name);
            WriteBoolean(value);
        }

        /// <summary>
        /// Writes a BSON DateTime element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The number of milliseconds since the Unix epoch.</param>
        public override void WriteDateTime(
            string name,
            long value
        ) {
            WriteName(name);
            WriteDateTime(value);
        }

        /// <summary>
        /// Writes a BSON Double element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Double value.</param>
        public override void WriteDouble(
            string name,
            double value
        ) {
            WriteName(name);
            WriteDouble(value);
        }

        /// <summary>
        /// Writes a BSON Int32 element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Int32 value.</param>
        public override void WriteInt32(
            string name,
            int value
        ) {
            WriteName(name);
            WriteInt32(value);
        }

        /// <summary>
        /// Writes a BSON Int64 element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Int64 value.</param>
        public override void WriteInt64(
            string name,
            long value
        ) {
            WriteName(name);
            WriteInt64(value);
        }

        /// <summary>
        /// Writes a BSON JavaScript element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="code">The JavaScript code.</param>
        public override void WriteJavaScript(
            string name,
            string code
        ) {
            WriteName(name);
            WriteJavaScript(code);
        }

        /// <summary>
        /// Writes a BSON JavaScript element to the writer (call WriteStartDocument to start writing the scope).
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="code">The JavaScript code.</param>
        public override void WriteJavaScriptWithScope(
            string name,
            string code
        ) {
            WriteName(name);
            WriteJavaScriptWithScope(code);
        }

        /// <summary>
        /// Writes a BSON MaxKey element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void WriteMaxKey(
            string name
        ) {
            WriteName(name);
            WriteMaxKey();
        }

        /// <summary>
        /// Writes a BSON MinKey element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void WriteMinKey(
            string name
        ) {
            WriteName(name);
            WriteMinKey();
        }

        /// <summary>
        /// Writes the name of an element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void WriteName(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException(this.GetType().Name); }
            if (state != BsonWriterState.Name) {
                ThrowInvalidState("WriteName", BsonWriterState.Name);
            }

            this.name = name;
            state = BsonWriterState.Value;
        }

        /// <summary>
        /// Writes a BSON null element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void WriteNull(
            string name
        ) {
            WriteName(name);
            WriteNull();
        }

        /// <summary>
        /// Writes a BSON ObjectId element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public override void WriteObjectId(
            string name,
            int timestamp,
            int machine,
            short pid,
            int increment
        ) {
            WriteName(name);
            WriteObjectId(timestamp, machine, pid, increment);
        }

        /// <summary>
        /// Writes a BSON regular expression element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public override void WriteRegularExpression(
            string name,
            string pattern,
            string options
        ) {
            WriteName(name);
            WriteRegularExpression(pattern, options);
        }

        /// <summary>
        /// Writes the start of a BSON array element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void WriteStartArray(
            string name
        ) {
            WriteName(name);
            WriteStartArray();
        }

        /// <summary>
        /// Writes the start of a BSON document element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void WriteStartDocument(
            string name
        ) {
            WriteName(name);
            WriteStartDocument();
        }

        /// <summary>
        /// Writes a BSON String element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The String value.</param>
        public override void WriteString(
            string name,
            string value
        ) {
            WriteName(name);
            WriteString(value);
        }

        /// <summary>
        /// Writes a BSON Symbol element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The symbol.</param>
        public override void WriteSymbol(
            string name,
            string value
        ) {
            WriteName(name);
            WriteSymbol(value);
        }

        /// <summary>
        /// Writes a BSON timestamp element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The combined timestamp/increment value.</param>
        public override void WriteTimestamp(
            string name,
            long value
        ) {
            WriteName(name);
            WriteTimestamp(value);
        }

        /// <summary>
        /// Writes a BSON undefined element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void WriteUndefined(
            string name
        ) {
            WriteName(name);
            WriteUndefined();
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Disposes of any resources used by the writer.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected abstract void Dispose(
            bool disposing
        );

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
            params BsonWriterState[] validStates
        ) {
            var validStatesString = string.Join(" or ", validStates.Select(s => s.ToString()).ToArray());
            var message = string.Format("{0} can only be called when State is {1}, not when State is {2}", methodName, validStatesString, state);
            throw new InvalidOperationException(message);
        }
        #endregion
    }
}

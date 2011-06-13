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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MongoDB.Bson.IO {
    /// <summary>
    /// Represents a BSON writer for some external format (see subclasses).
    /// </summary>
    public abstract class BsonWriter : IDisposable {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonWriter class.
        /// </summary>
        protected BsonWriter() {
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a BsonWriter to a BsonBuffer.
        /// </summary>
        /// <param name="settings">Optional BsonBinaryWriterSettings.</param>
        /// <returns>A BsonWriter.</returns>
        public static BsonWriter Create(
            BsonBinaryWriterSettings settings
        ) {
            return new BsonBinaryWriter(null, null, settings);
        }

        /// <summary>
        /// Creates a BsonWriter to a BsonBuffer.
        /// </summary>
        /// <param name="buffer">A BsonBuffer.</param>
        /// <returns>A BsonWriter.</returns>
        public static BsonWriter Create(
            BsonBuffer buffer
        ) {
            return new BsonBinaryWriter(null, buffer, BsonBinaryWriterSettings.Defaults);
        }

        /// <summary>
        /// Creates a BsonWriter to a BsonBuffer.
        /// </summary>
        /// <param name="buffer">A BsonBuffer.</param>
        /// <param name="settings">Optional BsonBinaryWriterSettings.</param>
        /// <returns>A BsonWriter.</returns>
        public static BsonWriter Create(
            BsonBuffer buffer,
            BsonBinaryWriterSettings settings
        ) {
            return new BsonBinaryWriter(null, buffer, settings);
        }

        /// <summary>
        /// Creates a BsonWriter to a BsonDocument.
        /// </summary>
        /// <param name="document">A BsonDocument.</param>
        /// <returns>A BsonWriter.</returns>
        public static BsonWriter Create(
            BsonDocument document
        ) {
            return Create(document, BsonDocumentWriterSettings.Defaults);
        }

        /// <summary>
        /// Creates a BsonWriter to a BsonDocument.
        /// </summary>
        /// <param name="document">A BsonDocument.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>A BsonWriter.</returns>
        public static BsonWriter Create(
            BsonDocument document,
            BsonDocumentWriterSettings settings
        ) {
            return new BsonDocumentWriter(document, settings);
        }

        /// <summary>
        /// Creates a BsonWriter to a BSON Stream.
        /// </summary>
        /// <param name="stream">A Stream.</param>
        /// <returns>A BsonWriter.</returns>
        public static BsonWriter Create(
            Stream stream
        ) {
            return Create(stream, BsonBinaryWriterSettings.Defaults);
        }

        /// <summary>
        /// Creates a BsonWriter to a BSON Stream.
        /// </summary>
        /// <param name="stream">A Stream.</param>
        /// <param name="settings">Optional BsonBinaryWriterSettings.</param>
        /// <returns>A BsonWriter.</returns>
        public static BsonWriter Create(
            Stream stream,
            BsonBinaryWriterSettings settings
        ) {
            return new BsonBinaryWriter(stream, null, BsonBinaryWriterSettings.Defaults);
        }

        /// <summary>
        /// Creates a BsonWriter to a JSON TextWriter.
        /// </summary>
        /// <param name="writer">A TextWriter.</param>
        /// <returns>A BsonWriter.</returns>
        public static BsonWriter Create(
            TextWriter writer
        ) {
            return new JsonWriter(writer, JsonWriterSettings.Defaults);
        }

        /// <summary>
        /// Creates a BsonWriter to a JSON TextWriter.
        /// </summary>
        /// <param name="writer">A TextWriter.</param>
        /// <param name="settings">Optional JsonWriterSettings.</param>
        /// <returns>A BsonWriter.</returns>
        public static BsonWriter Create(
            TextWriter writer,
            JsonWriterSettings settings
        ) {
            return new JsonWriter(writer, settings);
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the representation for Guids.
        /// </summary>
        public abstract GuidRepresentation GuidRepresentation { get; }

        /// <summary>
        /// Gets the current state of the writer.
        /// </summary>
        public abstract BsonWriterState State { get; }
        #endregion

        #region public methods
        /// <summary>
        /// Closes the writer.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Disposes of any resources used by the writer.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Flushes any pending data to the output destination.
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Writes BSON binary data to the writer.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        public abstract void WriteBinaryData(
            byte[] bytes,
            BsonBinarySubType subType
        );

        /// <summary>
        /// Writes BSON binary data to the writer.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The respresentation for Guids.</param>
        public abstract void WriteBinaryData(
            byte[] bytes,
            BsonBinarySubType subType,
            GuidRepresentation guidRepresentation
        );

        /// <summary>
        /// Writes a BSON binary data element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        public abstract void WriteBinaryData(
            string name,
            byte[] bytes,
            BsonBinarySubType subType
        );

        /// <summary>
        /// Writes a BSON binary data element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The respresentation for Guids.</param>
        public abstract void WriteBinaryData(
            string name,
            byte[] bytes,
            BsonBinarySubType subType,
            GuidRepresentation guidRepresentation
        );

        /// <summary>
        /// Writes a BSON Boolean to the writer.
        /// </summary>
        /// <param name="value">The Boolean value.</param>
        public abstract void WriteBoolean(
            bool value
        );

        /// <summary>
        /// Writes a BSON Boolean element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Boolean value.</param>
        public abstract void WriteBoolean(
            string name,
            bool value
        );

        /// <summary>
        /// Writes a BSON DateTime to the writer.
        /// </summary>
        /// <param name="value">The number of milliseconds since the Unix epoch.</param>
        public abstract void WriteDateTime(
            long value
        );

        /// <summary>
        /// Writes a BSON DateTime element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The number of milliseconds since the Unix epoch.</param>
        public abstract void WriteDateTime(
            string name,
            long value
        );

        /// <summary>
        /// Writes a BSON Double to the writer.
        /// </summary>
        /// <param name="value">The Double value.</param>
        public abstract void WriteDouble(
            double value
        );

        /// <summary>
        /// Writes a BSON Double element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Double value.</param>
        public abstract void WriteDouble(
            string name,
            double value
        );

        /// <summary>
        /// Writes the end of a BSON array to the writer.
        /// </summary>
        public abstract void WriteEndArray();

        /// <summary>
        /// Writes the end of a BSON document to the writer.
        /// </summary>
        public abstract void WriteEndDocument();

        /// <summary>
        /// Writes a BSON Int32 to the writer.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
        public abstract void WriteInt32(
            int value
        );

        /// <summary>
        /// Writes a BSON Int32 element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Int32 value.</param>
        public abstract void WriteInt32(
            string name,
            int value
        );

        /// <summary>
        /// Writes a BSON Int64 to the writer.
        /// </summary>
        /// <param name="value">The Int64 value.</param>
        public abstract void WriteInt64(
            long value
        );

        /// <summary>
        /// Writes a BSON Int64 element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Int64 value.</param>
        public abstract void WriteInt64(
            string name,
            long value
        );

        /// <summary>
        /// Writes a BSON JavaScript to the writer.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public abstract void WriteJavaScript(
            string code
        );

        /// <summary>
        /// Writes a BSON JavaScript element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="code">The JavaScript code.</param>
        public abstract void WriteJavaScript(
            string name,
            string code
        );

        /// <summary>
        /// Writes a BSON JavaScript to the writer (call WriteStartDocument to start writing the scope).
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public abstract void WriteJavaScriptWithScope(
            string code
        );

        /// <summary>
        /// Writes a BSON JavaScript element to the writer (call WriteStartDocument to start writing the scope).
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="code">The JavaScript code.</param>
        public abstract void WriteJavaScriptWithScope(
            string name,
            string code
        );

        /// <summary>
        /// Writes a BSON MaxKey to the writer.
        /// </summary>
        public abstract void WriteMaxKey();

        /// <summary>
        /// Writes a BSON MaxKey element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void WriteMaxKey(
            string name
        );

        /// <summary>
        /// Writes a BSON MinKey to the writer.
        /// </summary>
        public abstract void WriteMinKey();

        /// <summary>
        /// Writes a BSON MinKey element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void WriteMinKey(
            string name
        );

        /// <summary>
        /// Writes the name of an element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void WriteName(
            string name
        );

        /// <summary>
        /// Writes a BSON null to the writer.
        /// </summary>
        public abstract void WriteNull();

        /// <summary>
        /// Writes a BSON null element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void WriteNull(
            string name
        );

        /// <summary>
        /// Writes a BSON ObjectId to the writer.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public abstract void WriteObjectId(
            int timestamp,
            int machine,
            short pid,
            int increment
        );

        /// <summary>
        /// Writes a BSON ObjectId element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public abstract void WriteObjectId(
            string name,
            int timestamp,
            int machine,
            short pid,
            int increment
        );

        /// <summary>
        /// Writes a BSON regular expression to the writer.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public abstract void WriteRegularExpression(
            string pattern,
            string options
        );

        /// <summary>
        /// Writes a BSON regular expression element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public abstract void WriteRegularExpression(
            string name,
            string pattern,
            string options
        );

        /// <summary>
        /// Writes the start of a BSON array to the writer.
        /// </summary>
        public abstract void WriteStartArray();

        /// <summary>
        /// Writes the start of a BSON array element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void WriteStartArray(
            string name
        );

        /// <summary>
        /// Writes the start of a BSON document to the writer.
        /// </summary>
        public abstract void WriteStartDocument();

        /// <summary>
        /// Writes the start of a BSON document element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void WriteStartDocument(
            string name
        );

        /// <summary>
        /// Writes a BSON String to the writer.
        /// </summary>
        /// <param name="value">The String value.</param>
        public abstract void WriteString(
            string value
        );

        /// <summary>
        /// Writes a BSON String element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The String value.</param>
        public abstract void WriteString(
            string name,
            string value
        );

        /// <summary>
        /// Writes a BSON Symbol to the writer.
        /// </summary>
        /// <param name="value">The symbol.</param>
        public abstract void WriteSymbol(
            string value
        );

        /// <summary>
        /// Writes a BSON Symbol element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The symbol.</param>
        public abstract void WriteSymbol(
            string name,
            string value
        );

        /// <summary>
        /// Writes a BSON timestamp to the writer.
        /// </summary>
        /// <param name="value">The combined timestamp/increment value.</param>
        public abstract void WriteTimestamp(
            long value
        );

        /// <summary>
        /// Writes a BSON timestamp element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The combined timestamp/increment value.</param>
        public abstract void WriteTimestamp(
            string name,
            long value
        );

        /// <summary>
        /// Writes a BSON undefined to the writer.
        /// </summary>
        public abstract void WriteUndefined();

        /// <summary>
        /// Writes a BSON undefined element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void WriteUndefined(
            string name
        );
        #endregion
    }
}

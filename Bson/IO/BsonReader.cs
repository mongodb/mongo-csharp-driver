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
    /// Represents a BSON reader for some external format (see subclasses).
    /// </summary>
    public abstract class BsonReader : IDisposable {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonReader class.
        /// </summary>
        protected BsonReader() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the current BsonType.
        /// </summary>
        public abstract BsonType CurrentBsonType { get; }

        /// <summary>
        /// Gets the representation for Guids.
        /// </summary>
        public abstract GuidRepresentation GuidRepresentation { get; }

        /// <summary>
        /// Gets the current state of the reader.
        /// </summary>
        public abstract BsonReaderState State { get; }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a BsonReader for a BsonBuffer.
        /// </summary>
        /// <param name="buffer">The BsonBuffer.</param>
        /// <returns>A BsonReader.</returns>
        public static BsonReader Create(
            BsonBuffer buffer
        ) {
            return Create(buffer, BsonBinaryReaderSettings.Defaults);
        }

        /// <summary>
        /// Creates a BsonReader for a BsonBuffer.
        /// </summary>
        /// <param name="buffer">The BsonBuffer.</param>
        /// <param name="settings">Optional reader settings.</param>
        /// <returns>A BsonReader.</returns>
        public static BsonReader Create(
            BsonBuffer buffer,
            BsonBinaryReaderSettings settings
        ) {
            return new BsonBinaryReader(buffer, settings);
        }

        /// <summary>
        /// Creates a BsonReader for a BsonDocument.
        /// </summary>
        /// <param name="document">The BsonDocument.</param>
        /// <returns>A BsonReader.</returns>
        public static BsonReader Create(
            BsonDocument document
        ) {
            return Create(document, BsonDocumentReaderSettings.Defaults);
        }

        /// <summary>
        /// Creates a BsonReader for a BsonDocument.
        /// </summary>
        /// <param name="document">The BsonDocument.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>A BsonReader.</returns>
        public static BsonReader Create(
            BsonDocument document,
            BsonDocumentReaderSettings settings
        ) {
            return new BsonDocumentReader(document, settings);
        }

        /// <summary>
        /// Creates a BsonReader for a JsonBuffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>A BsonReader.</returns>
        public static BsonReader Create(
            JsonBuffer buffer
        ) {
            return Create(buffer, JsonReaderSettings.Defaults);
        }

        /// <summary>
        /// Creates a BsonReader for a JsonBuffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>A BsonReader.</returns>
        public static BsonReader Create(
            JsonBuffer buffer,
            JsonReaderSettings settings
        ) {
            return new JsonReader(buffer, settings);
        }

        /// <summary>
        /// Creates a BsonReader for a BSON Stream.
        /// </summary>
        /// <param name="stream">The BSON Stream.</param>
        /// <returns>A BsonReader.</returns>
        public static BsonReader Create(
            Stream stream
        ) {
            return Create(stream, BsonBinaryReaderSettings.Defaults);
        }

        /// <summary>
        /// Creates a BsonReader for a BSON Stream.
        /// </summary>
        /// <param name="stream">The BSON Stream.</param>
        /// <param name="settings">Optional reader settings.</param>
        /// <returns>A BsonReader.</returns>
        public static BsonReader Create(
            Stream stream,
            BsonBinaryReaderSettings settings
        ) {
            var reader = new BsonBinaryReader(null, settings);
            reader.Buffer.LoadFrom(stream);
            return reader;
        }

        /// <summary>
        /// Creates a BsonReader for a JSON string.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>A BsonReader.</returns>
        public static BsonReader Create(
            string json
        ) {
            var buffer = new JsonBuffer(json);
            return Create(buffer);
        }

        /// <summary>
        /// Creates a BsonReader for a JSON TextReader.
        /// </summary>
        /// <param name="textReader">The JSON TextReader.</param>
        /// <returns>A BsonReader.</returns>
        public static BsonReader Create(
            TextReader textReader
        ) {
            var json = textReader.ReadToEnd();
            return Create(json);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Closes the reader.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Disposes of any resources used by the reader.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Positions the reader to an element by name.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>True if the element was found.</returns>
        public abstract bool FindElement(
            string name
        );

        /// <summary>
        /// Positions the reader to a string element by name.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>True if the element was found.</returns>
        public abstract string FindStringElement(
            string name
        );

        /// <summary>
        /// Gets a bookmark to the reader's current position and state.
        /// </summary>
        /// <returns>A bookmark.</returns>
        public abstract BsonReaderBookmark GetBookmark();

        /// <summary>
        /// Reads BSON binary data from the reader.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        public abstract void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType
        );

        /// <summary>
        /// Reads a BSON binary data element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        public abstract void ReadBinaryData(
            string name,
            out byte[] bytes,
            out BsonBinarySubType subType
        );

        /// <summary>
        /// Reads a BSON boolean from the reader.
        /// </summary>
        /// <returns>A Boolean.</returns>
        public abstract bool ReadBoolean();

        /// <summary>
        /// Reads a BSON boolean element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A Boolean.</returns>
        public abstract bool ReadBoolean(
            string name
        );

        /// <summary>
        /// Reads a BsonType from the reader.
        /// </summary>
        /// <returns>A BsonType.</returns>
        public abstract BsonType ReadBsonType();

        /// <summary>
        /// Reads a BSON DateTime from the reader.
        /// </summary>
        /// <returns>The number of milliseconds since the Unix epoch.</returns>
        public abstract long ReadDateTime();

        /// <summary>
        /// Reads a BSON DateTime element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>The number of milliseconds since the Unix epoch.</returns>
        public abstract long ReadDateTime(
            string name
        );

        /// <summary>
        /// Reads a BSON Double from the reader.
        /// </summary>
        /// <returns>A Double.</returns>
        public abstract double ReadDouble();

        /// <summary>
        /// Reads a BSON Double element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A Double.</returns>
        public abstract double ReadDouble(
            string name
        );

        /// <summary>
        /// Reads the end of a BSON array from the reader.
        /// </summary>
        public abstract void ReadEndArray();

        /// <summary>
        /// Reads the end of a BSON document from the reader.
        /// </summary>
        public abstract void ReadEndDocument();

        /// <summary>
        /// Reads a BSON Int32 from the reader.
        /// </summary>
        /// <returns>An Int32.</returns>
        public abstract int ReadInt32();

        /// <summary>
        /// Reads a BSON Int32 element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>An Int32.</returns>
        public abstract int ReadInt32(
            string name
        );

        /// <summary>
        /// Reads a BSON Int64 from the reader.
        /// </summary>
        /// <returns>An Int64.</returns>
        public abstract long ReadInt64();

        /// <summary>
        /// Reads a BSON Int64 element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>An Int64.</returns>
        public abstract long ReadInt64(
            string name
        );

        /// <summary>
        /// Reads a BSON JavaScript from the reader.
        /// </summary>
        /// <returns>A string.</returns>
        public abstract string ReadJavaScript();

        /// <summary>
        /// Reads a BSON JavaScript element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A string.</returns>
        public abstract string ReadJavaScript(
            string name
        );

        /// <summary>
        /// Reads a BSON JavaScript with scope from the reader (call ReadStartDocument next to read the scope).
        /// </summary>
        /// <returns>A string.</returns>
        public abstract string ReadJavaScriptWithScope();

        /// <summary>
        /// Reads a BSON JavaScript with scope element from the reader (call ReadStartDocument next to read the scope).
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A string.</returns>
        public abstract string ReadJavaScriptWithScope(
            string name
        );

        /// <summary>
        /// Reads a BSON MaxKey from the reader.
        /// </summary>
        public abstract void ReadMaxKey();

        /// <summary>
        /// Reads a BSON MaxKey element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void ReadMaxKey(
            string name
        );

        /// <summary>
        /// Reads a BSON MinKey from the reader.
        /// </summary>
        public abstract void ReadMinKey();

        /// <summary>
        /// Reads a BSON MinKey element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void ReadMinKey(
            string name
        );

        /// <summary>
        /// Reads the name of an element from the reader.
        /// </summary>
        /// <returns>The name of the element.</returns>
        public abstract string ReadName();

        /// <summary>
        /// Reads the name of an element from the reader.
        /// </summary>
        /// <returns>The name of the element.</returns>
        public abstract void ReadName(
            string name
        );

        /// <summary>
        /// Reads a BSON null from the reader.
        /// </summary>
        public abstract void ReadNull();

        /// <summary>
        /// Reads a BSON null element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void ReadNull(
            string name
        );

        /// <summary>
        /// Reads a BSON ObjectId from the reader.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public abstract void ReadObjectId(
            out int timestamp,
            out int machine,
            out short pid,
            out int increment
        );

        /// <summary>
        /// Reads a BSON ObjectId element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public abstract void ReadObjectId(
            string name,
            out int timestamp,
            out int machine,
            out short pid,
            out int increment
        );

        /// <summary>
        /// Reads a BSON regular expression from the reader.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public abstract void ReadRegularExpression(
            out string pattern,
            out string options
        );

        /// <summary>
        /// Reads a BSON regular expression element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public abstract void ReadRegularExpression(
            string name,
            out string pattern,
            out string options
        );

        /// <summary>
        /// Reads the start of a BSON array.
        /// </summary>
        public abstract void ReadStartArray();

        /// <summary>
        /// Reads the start of a BSON document.
        /// </summary>
        public abstract void ReadStartDocument();

        /// <summary>
        /// Reads a BSON string from the reader.
        /// </summary>
        /// <returns>A String.</returns>
        public abstract string ReadString();

        /// <summary>
        /// Reads a BSON string element from the reader.
        /// </summary>
        /// <returns>A String.</returns>
        /// <param name="name">The name of the element.</param>
        public abstract string ReadString(
            string name
        );

        /// <summary>
        /// Reads a BSON symbol from the reader.
        /// </summary>
        /// <returns>A string.</returns>
        public abstract string ReadSymbol();

        /// <summary>
        /// Reads a BSON symbol element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A string.</returns>
        public abstract string ReadSymbol(
            string name
        );

        /// <summary>
        /// Reads a BSON timestamp from the reader.
        /// </summary>
        /// <returns>The combined timestamp/increment.</returns>
        public abstract long ReadTimestamp();

        /// <summary>
        /// Reads a BSON timestamp element from the reader.
        /// </summary>
        /// <returns>The combined timestamp/increment.</returns>
        /// <param name="name">The name of the element.</param>
        public abstract long ReadTimestamp(
            string name
        );

        /// <summary>
        /// Reads a BSON undefined from the reader.
        /// </summary>
        public abstract void ReadUndefined();

        /// <summary>
        /// Reads a BSON undefined element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public abstract void ReadUndefined(
            string name
        );

        /// <summary>
        /// Returns the reader to previously bookmarked position and state.
        /// </summary>
        /// <param name="bookmark">The bookmark.</param>
        public abstract void ReturnToBookmark(BsonReaderBookmark bookmark);

        /// <summary>
        /// Skips the name (reader must be positioned on a name).
        /// </summary>
        public abstract void SkipName();

        /// <summary>
        /// Skips the value (reader must be positioned on a value).
        /// </summary>
        public abstract void SkipValue();
        #endregion
    }
}

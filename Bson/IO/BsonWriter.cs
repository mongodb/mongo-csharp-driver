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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MongoDB.Bson.IO {
    public abstract class BsonWriter : IDisposable {
        #region constructors
        protected BsonWriter() {
        }
        #endregion

        #region public static methods
        public static BsonWriter Create(
            BsonBinaryWriterSettings settings
        ) {
            return new BsonBinaryWriter(null, null, settings);
        }

        public static BsonWriter Create(
            BsonBuffer buffer
        ) {
            return new BsonBinaryWriter(null, buffer, BsonBinaryWriterSettings.Defaults);
        }

        public static BsonWriter Create(
            BsonBuffer buffer,
            BsonBinaryWriterSettings settings
        ) {
            return new BsonBinaryWriter(null, buffer, settings);
        }

        public static BsonWriter Create(
            Stream stream
        ) {
            return Create(stream, BsonBinaryWriterSettings.Defaults);
        }

        public static BsonWriter Create(
            Stream stream,
            BsonBinaryWriterSettings settings
        ) {
            return new BsonBinaryWriter(stream, null, BsonBinaryWriterSettings.Defaults);
        }

        public static BsonWriter Create(
            TextWriter writer
        ) {
            return new JsonWriter(writer, JsonWriterSettings.Defaults);
        }

        public static BsonWriter Create(
            TextWriter writer,
            JsonWriterSettings settings
        ) {
            return new JsonWriter(writer, settings);
        }
        #endregion

        #region public properties
        public abstract BsonWriterState State { get; }
        #endregion

        #region public methods
        public abstract void Close();
        public abstract void Dispose();
        public abstract void Flush();
        public abstract void WriteBinaryData(
            byte[] bytes,
            BsonBinarySubType subType
        );
        public abstract void WriteBinaryData(
            string name,
            byte[] bytes,
            BsonBinarySubType subType
        );
        public abstract void WriteBoolean(
            bool value
        );
        public abstract void WriteBoolean(
            string name,
            bool value
        );
        public abstract void WriteDateTime(
            DateTime value
        );
        public abstract void WriteDateTime(
            string name,
            DateTime value
        );
        public abstract void WriteDouble(
            double value
        );
        public abstract void WriteDouble(
            string name,
            double value
        );
        public abstract void WriteEndArray();
        public abstract void WriteEndDocument();
        public abstract void WriteInt32(
            int value
        );
        public abstract void WriteInt32(
            string name,
            int value
        );
        public abstract void WriteInt64(
            long value
        );
        public abstract void WriteInt64(
            string name,
            long value
        );
        public abstract void WriteJavaScript(
            string code
        );
        public abstract void WriteJavaScript(
            string name,
            string code
        );
        public abstract void WriteJavaScriptWithScope(
            string code
        );
        public abstract void WriteJavaScriptWithScope(
            string name,
            string code
        );
        public abstract void WriteMaxKey();
        public abstract void WriteMaxKey(
            string name
        );
        public abstract void WriteMinKey();
        public abstract void WriteMinKey(
            string name
        );
        public abstract void WriteName(
            string name
        );
        public abstract void WriteNull();
        public abstract void WriteNull(
            string name
        );
        public abstract void WriteObjectId(
            int timestamp,
            int machine,
            short pid,
            int increment
        );
        public abstract void WriteObjectId(
            string name,
            int timestamp,
            int machine,
            short pid,
            int increment
        );
        public abstract void WriteRegularExpression(
            string pattern,
            string options
        );
        public abstract void WriteRegularExpression(
            string name,
            string pattern,
            string options
        );
        public abstract void WriteStartArray();
        public abstract void WriteStartArray(
            string name
        );
        public abstract void WriteStartDocument();
        public abstract void WriteStartDocument(
            string name
        );
        public abstract void WriteString(
            string value
        );
        public abstract void WriteString(
            string name,
            string value
        );
        public abstract void WriteSymbol(
            string value
        );
        public abstract void WriteSymbol(
            string name,
            string value
        );
        public abstract void WriteTimestamp(
            long value
        );
        public abstract void WriteTimestamp(
            string name,
            long value
        );
        #endregion
    }
}

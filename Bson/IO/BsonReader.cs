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
    public abstract class BsonReader : IDisposable {
        #region constructors
        protected BsonReader() {
        }
        #endregion

        #region public properties
        public abstract BsonType CurrentBsonType { get; }
        public abstract BsonReadState ReadState { get; }
        #endregion

        #region public static methods
        public static BsonReader Create(
            BsonBuffer buffer
        ) {
            return Create(buffer, BsonBinaryReaderSettings.Defaults);
        }

        public static BsonReader Create(
            BsonBuffer buffer,
            BsonBinaryReaderSettings settings
        ) {
            return new BsonBinaryReader(buffer, settings);
        }

        public static BsonReader Create(
            BsonDocument document
        ) {
            return new BsonDocumentReader(document);
        }

        public static BsonReader Create(
            Stream stream
        ) {
            return Create(stream, BsonBinaryReaderSettings.Defaults);
        }

        public static BsonReader Create(
            Stream stream,
            BsonBinaryReaderSettings settings
        ) {
            BsonBuffer buffer = new BsonBuffer();
            buffer.LoadFrom(stream);
            return new BsonBinaryReader(buffer, settings);
        }
        #endregion

        #region public methods
        public abstract void Close();
        public abstract void Dispose();
        public abstract bool FindElement(
            string name
        );
        public abstract string FindString(
            string name
        );
        public abstract BsonReaderBookmark GetBookmark();
        public abstract void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType
        );
        public abstract void ReadBinaryData(
            string name,
            out byte[] bytes,
            out BsonBinarySubType subType
        );
        public abstract bool ReadBoolean();
        public abstract bool ReadBoolean(
            string name
        );
        public abstract BsonType ReadBsonType();
        public abstract DateTime ReadDateTime();
        public abstract DateTime ReadDateTime(
            string name
        );
        public abstract double ReadDouble();
        public abstract double ReadDouble(
            string name
        );
        public abstract void ReadEndArray();
        public abstract void ReadEndDocument();
        public abstract int ReadInt32();
        public abstract int ReadInt32(
            string name
        );
        public abstract long ReadInt64();
        public abstract long ReadInt64(
            string name
        );
        public abstract string ReadJavaScript();
        public abstract string ReadJavaScript(
            string name
        );
        public abstract string ReadJavaScriptWithScope();
        public abstract string ReadJavaScriptWithScope(
            string name
        );
        public abstract void ReadMaxKey();
        public abstract void ReadMaxKey(
            string name
        );
        public abstract void ReadMinKey();
        public abstract void ReadMinKey(
            string name
        );
        public abstract string ReadName();
        public abstract void ReadNull();
        public abstract void ReadNull(
            string name
        );
        public abstract void ReadObjectId(
            out int timestamp,
            out int machine,
            out short pid,
            out int increment
        );
        public abstract void ReadObjectId(
            string name,
            out int timestamp,
            out int machine,
            out short pid,
            out int increment
        );
        public abstract void ReadRegularExpression(
            out string pattern,
            out string options
        );
        public abstract void ReadRegularExpression(
            string name,
            out string pattern,
            out string options
        );
        public abstract void ReadStartArray();
        public abstract void ReadStartDocument();
        public abstract string ReadString();
        public abstract string ReadString(
            string name
        );
        public abstract string ReadSymbol();
        public abstract string ReadSymbol(
            string name
        );
        public abstract long ReadTimestamp();
        public abstract long ReadTimestamp(
            string name
        );
        public abstract void ReturnToBookmark(BsonReaderBookmark bookmark);
        public abstract void SkipName();
        public abstract void SkipValue();
        #endregion
    }
}

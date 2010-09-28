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

namespace MongoDB.BsonLibrary.IO {
    public abstract class BsonReader : IDisposable {
        #region constructors
        protected BsonReader() {
        }
        #endregion

        #region public properties
        public abstract BsonReaderDocumentType DocumentType { get; }
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
        public abstract byte[] ReadBinaryData();
        public abstract byte[] ReadBinaryData(
            out BsonBinarySubType subType
        );
        public abstract bool ReadBoolean();
        public abstract BsonType ReadBsonType();
        public abstract DateTime ReadDateTime();
        public abstract double ReadDouble();
        public abstract void ReadEndArray();
        public abstract void ReadEndDocument();
        public abstract void ReadEndEmbeddedDocument();
        public abstract void ReadEndJavaScriptWithScope();
        public abstract Guid ReadGuid();
        public abstract int ReadInt32() ;
        public abstract long ReadInt64() ;
        public abstract string ReadJavaScript();
        public abstract void ReadMaxKey();
        public abstract void ReadMinKey();
        public abstract string ReadName();
        public abstract void ReadNull();
        public abstract void ReadObjectId(
            out int timestamp,
            out long machinePidIncrement
        );
        public abstract void ReadRegularExpression(
            out string pattern,
            out string options
        );
        public abstract void ReadStartArray();
        public abstract void ReadStartDocument();
        public abstract void ReadStartEmbeddedDocument();
        public abstract void ReadStartJavaScriptWithScope(
            out string code
        );
        public abstract string ReadString();
        public abstract string ReadSymbol();
        public abstract long ReadTimestamp();
        #endregion
    }
}

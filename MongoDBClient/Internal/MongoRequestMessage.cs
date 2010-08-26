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

using MongoDB.BsonLibrary;

namespace MongoDB.MongoDBClient.Internal {
    public abstract class MongoRequestMessage : MongoMessage {
        #region protected fields
        protected MongoCollection collection;
        protected MemoryStream stream;
        protected BinaryWriter writer;
        protected long start;
        #endregion

        #region constructors
        protected MongoRequestMessage(
            MessageOpcode opcode,
            MongoCollection collection
        )
            : this(opcode, collection, new MemoryStream()) {
        }

        protected MongoRequestMessage(
            MessageOpcode opcode,
            MongoCollection collection,
            MemoryStream stream
        )
            : base(opcode) {
            if (stream == null) { stream = new MemoryStream(); }
            this.collection = collection;
            this.stream = stream;
            this.writer = new BinaryWriter(stream);
            this.start = stream.Position;
        }
        #endregion

        #region public properties
        public MemoryStream MemoryStream {
            get { return stream; }
        }
        #endregion

        #region public methods
        // used by safemode to piggy back a GetLastError on the same network transmission
        public void AddGetLastError() {
            var cmd = collection.Database.GetCollection("$cmd");
            var query = new BsonDocument("getLastError", 1);
            var queryMessage = new MongoQueryMessage(cmd, QueryFlags.None, 0, 1, query, null, stream);
        }

        public void WriteMessageToMemoryStream() {
            WriteMessageTo(writer);
        }
        #endregion

        #region protected methods
        protected void BackpatchMessageLength(
            BinaryWriter writer,
            long start
        ) {
            long position = writer.BaseStream.Position;
            messageLength = (int) (position - start);
            writer.BaseStream.Seek(start, SeekOrigin.Begin);
            writer.Write(messageLength);
            writer.BaseStream.Seek(position, SeekOrigin.Begin);
        }

        protected abstract void WriteBodyTo(
            BinaryWriter writer
        );

        protected void WriteCString(
            BinaryWriter writer,
            string value
        ) {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(value);
            if (utf8Bytes.Contains((byte) 0)) {
                throw new MongoException("A cstring cannot contain 0x00");
            }
            writer.Write(utf8Bytes);
            writer.Write((byte) 0);
        }

        protected void WriteMessageTo(
            BinaryWriter writer
        ) {
            WriteMessageHeaderTo(writer);
            WriteBodyTo(writer);
            BackpatchMessageLength(writer, start);
        }
        #endregion
    }
}

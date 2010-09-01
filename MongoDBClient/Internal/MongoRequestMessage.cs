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
    internal abstract class MongoRequestMessage : MongoMessage {
        #region protected fields
        protected MongoCollection collection; // null if subclass is not a collection related message (e.g. KillCursors)
        protected MemoryStream memoryStream; // null unless WriteTo has been called
        protected BinaryWriter binaryWriter; // null unless WriteTo has been called
        protected long messageStartPosition; // start position in stream for backpatching messageLength
        #endregion

        #region constructors
        protected MongoRequestMessage(
            MessageOpcode opcode,
            MongoCollection collection
        )
            : base(opcode) {
            this.collection = collection;
        }
        #endregion

        #region internal methods
        internal MemoryStream GetMemoryStream() {
            if (memoryStream == null) {
                WriteTo(new MemoryStream());
            }
            return memoryStream;
        }

        internal void WriteTo(
            MemoryStream memoryStream
        ) {
            this.memoryStream = memoryStream;
            this.binaryWriter = new BinaryWriter(memoryStream);

            messageStartPosition = memoryStream.Position;
            WriteMessageHeaderTo(binaryWriter);
            WriteBodyTo(binaryWriter);
            BackpatchMessageLength(binaryWriter);
        }
        #endregion

        #region protected methods
        protected void BackpatchMessageLength(
            BinaryWriter binaryWriter
        ) {
            long currentPosition = binaryWriter.BaseStream.Position;
            messageLength = (int) (currentPosition - messageStartPosition);
            binaryWriter.BaseStream.Seek(messageStartPosition, SeekOrigin.Begin);
            binaryWriter.Write(messageLength);
            binaryWriter.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
        }

        protected abstract void WriteBodyTo(
            BinaryWriter binaryWriter
        );

        protected void WriteCStringTo(
            BinaryWriter binaryWriter,
            string value
        ) {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(value);
            if (utf8Bytes.Contains((byte) 0)) {
                throw new MongoException("A cstring cannot contain 0x00");
            }
            binaryWriter.Write(utf8Bytes);
            binaryWriter.Write((byte) 0);
        }
        #endregion
    }
}

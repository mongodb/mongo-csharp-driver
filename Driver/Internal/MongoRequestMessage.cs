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
using System.Threading;

using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Internal {
    internal abstract class MongoRequestMessage : MongoMessage, IDisposable {
        #region private static fields
        private static int lastRequestId = 0;
        #endregion

        #region protected fields
        protected bool disposed = false;
        protected BsonBuffer buffer;
        protected bool disposeBuffer;
        protected int messageStartPosition = -1; // start position in buffer for backpatching messageLength
        #endregion

        #region constructors
        protected MongoRequestMessage(
            MongoServer server,
            MessageOpcode opcode
        )
            : this(server, opcode, null) {
        }

        protected MongoRequestMessage(
            MongoServer server,
            MessageOpcode opcode,
            BsonBuffer buffer // not null if piggybacking this message onto an existing buffer
        )
            : base(server, opcode) {
            if (buffer == null) {
                this.buffer = new BsonBuffer();
                this.disposeBuffer = true; // only call Dispose if we allocated the buffer
            } else {
                this.buffer = buffer;
                this.disposeBuffer = false;
            }
            this.requestId = Interlocked.Increment(ref lastRequestId);
        }
        #endregion

        #region public propertieds
        public BsonBuffer Buffer {
            get { return buffer; }
        }
        #endregion

        #region public methods
        public void Dispose() {
            if (!disposed) {
                if (disposeBuffer) {
                    buffer.Dispose();
                }
                buffer = null;
                disposed = true;
            }
        }
        #endregion

        #region internal methods
        internal void WriteToBuffer() {
            // this method is sometimes called more than once (see MongoConnection and MongoCollection)
            if (messageStartPosition == -1) {
                messageStartPosition = buffer.Position;
                WriteMessageHeaderTo(buffer);
                WriteBody();
                BackpatchMessageLength();
            }
        }
        #endregion

        #region protected methods
        protected BsonWriter CreateBsonWriter() {
            var settings = new BsonBinaryWriterSettings { MaxDocumentSize = server.MaxDocumentSize };
            return BsonWriter.Create(buffer, settings);
        }

        protected void BackpatchMessageLength() {
            messageLength = buffer.Position - messageStartPosition;
            buffer.Backpatch(messageStartPosition, messageLength);
        }

        protected abstract void WriteBody();
        #endregion
    }
}

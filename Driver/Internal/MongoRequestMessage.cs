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

namespace MongoDB.Driver.Internal
{
    internal abstract class MongoRequestMessage : MongoMessage, IDisposable
    {
        // private static fields
        private static int lastRequestId = 0;

        // protected fields
        protected bool disposed = false;
        protected BsonBuffer buffer;
        protected BsonBinaryWriterSettings writerSettings;
        protected bool disposeBuffer;
        protected int messageStartPosition = -1; // start position in buffer for backpatching messageLength

        // constructors
        protected MongoRequestMessage(MessageOpcode opcode, BsonBuffer buffer, BsonBinaryWriterSettings writerSettings)
            : base(opcode)
        {
            // buffer is not null if piggybacking this message onto an existing buffer
            if (buffer == null)
            {
                this.buffer = new BsonBuffer();
                this.disposeBuffer = true; // only call Dispose if we allocated the buffer
            }
            else
            {
                this.buffer = buffer;
                this.disposeBuffer = false;
            }
            this.writerSettings = writerSettings;
            this.requestId = Interlocked.Increment(ref lastRequestId);
        }

        // public properties
        public BsonBuffer Buffer
        {
            get { return buffer; }
        }

        public BsonBinaryWriterSettings WriterSettings
        {
            get { return writerSettings; }
        }

        // public methods
        public void Dispose()
        {
            if (!disposed)
            {
                if (disposeBuffer)
                {
                    buffer.Dispose();
                }
                buffer = null;
                disposed = true;
            }
        }

        // internal methods
        internal void WriteToBuffer()
        {
            // normally this method is only called once (from MongoConnection.SendMessage)
            // but in the case of InsertBatch it is called before SendMessage is called to initialize the message so that AddDocument can be called
            // therefore we need the if statement to ignore subsequent calls from SendMessage
            if (messageStartPosition == -1)
            {
                messageStartPosition = buffer.Position;
                WriteMessageHeaderTo(buffer);
                WriteBody();
                BackpatchMessageLength();
            }
        }

        // protected methods
        protected void BackpatchMessageLength()
        {
            messageLength = buffer.Position - messageStartPosition;
            buffer.Backpatch(messageStartPosition, messageLength);
        }

        protected abstract void WriteBody();
    }
}

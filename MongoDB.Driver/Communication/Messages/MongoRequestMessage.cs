/* Copyright 2010-2013 10gen Inc.
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
using System.Threading;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Internal
{
    internal abstract class MongoRequestMessage : MongoMessage, IDisposable
    {
        // private static fields
        private static int __lastRequestId = 0;

        // private fields
        private bool _disposed = false;
        private BsonBuffer _buffer;
        private BsonBinaryWriterSettings _writerSettings;
        private bool _disposeBuffer;
        private int _messageStartPosition = -1; // start position in buffer for backpatching messageLength

        // constructors
        protected MongoRequestMessage(
            MessageOpcode opcode,
            BsonBuffer buffer,
            BsonBinaryWriterSettings writerSettings)
            : base(opcode)
        {
            // buffer is not null if piggybacking this message onto an existing buffer
            if (buffer == null)
            {
                _buffer = new BsonBuffer();
                _disposeBuffer = true; // only call Dispose if we allocated the buffer
            }
            else
            {
                _buffer = buffer;
                _disposeBuffer = false;
            }
            _writerSettings = writerSettings;
            RequestId = Interlocked.Increment(ref __lastRequestId);
        }

        // public properties
        public BsonBuffer Buffer
        {
            get { return _buffer; }
        }

        public BsonBinaryWriterSettings WriterSettings
        {
            get { return _writerSettings; }
        }

        // public methods
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_disposeBuffer)
                {
                    _buffer.Dispose();
                }
                _buffer = null;
                _disposed = true;
            }
        }

        // internal methods
        internal void WriteToBuffer()
        {
            // normally this method is only called once (from MongoConnection.SendMessage)
            // but in the case of InsertBatch it is called before SendMessage is called to initialize the message so that AddDocument can be called
            // therefore we need the if statement to ignore subsequent calls from SendMessage
            if (_messageStartPosition == -1)
            {
                _messageStartPosition = _buffer.Position;
                WriteMessageHeaderTo(_buffer);
                WriteBody();
                BackpatchMessageLength();
            }
        }

        // protected methods
        protected void BackpatchMessageLength()
        {
            MessageLength = _buffer.Position - _messageStartPosition;
            _buffer.Backpatch(_messageStartPosition, MessageLength);
        }

        protected abstract void WriteBody();
    }
}

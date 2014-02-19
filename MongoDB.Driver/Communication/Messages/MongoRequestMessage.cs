/* Copyright 2010-2014 MongoDB Inc.
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
    internal abstract class MongoRequestMessage : MongoMessage
    {
        // private static fields
        private static int __lastRequestId = 0;

        // private fields
        private BsonBinaryWriterSettings _writerSettings;
        private int _messageStartPosition = -1; // start position in buffer for backpatching messageLength

        // constructors
        protected MongoRequestMessage(
            MessageOpcode opcode,
            BsonBinaryWriterSettings writerSettings)
            : base(opcode)
        {
            _writerSettings = writerSettings;
            RequestId = Interlocked.Increment(ref __lastRequestId);
        }

        // public properties
        public BsonBinaryWriterSettings WriterSettings
        {
            get { return _writerSettings; }
        }

        // internal methods
        internal void BackpatchMessageLength(BsonBuffer buffer)
        {
            MessageLength = buffer.Position - _messageStartPosition;
            buffer.Backpatch(_messageStartPosition, MessageLength);
        }

        internal abstract void WriteBodyTo(BsonBuffer buffer);

        internal override void WriteHeaderTo(BsonBuffer buffer)
        {
            _messageStartPosition = buffer.Position;
            base.WriteHeaderTo(buffer);
        }

        internal void WriteTo(BsonBuffer buffer)
        {
            WriteHeaderTo(buffer);
            WriteBodyTo(buffer);
            BackpatchMessageLength(buffer);
        }
    }
}

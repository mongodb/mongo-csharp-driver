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

using System.IO;
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
        internal void BackpatchMessageLength(BsonStreamWriter streamWriter)
        {
            MessageLength = (int)streamWriter.Position - _messageStartPosition;
            Backpatch(streamWriter.BaseStream, _messageStartPosition, MessageLength);
        }

        internal abstract void WriteBodyTo(BsonStreamWriter streamWriter);

        internal override void WriteHeaderTo(BsonStreamWriter streamWriter)
        {
            _messageStartPosition = (int)streamWriter.Position;
            base.WriteHeaderTo(streamWriter);
        }

        internal void WriteTo(Stream stream)
        {
            var streamWriter = new BsonStreamWriter(stream, WriterSettings.Encoding);
            WriteHeaderTo(streamWriter);
            WriteBodyTo(streamWriter);
            BackpatchMessageLength(streamWriter);
        }

        // private methods
        private void Backpatch(Stream stream, int position, int value)
        {
            var streamWriter = new BsonStreamWriter(stream, Utf8Helper.StrictUtf8Encoding);
            var currentPosition = stream.Position;
            stream.Position = position;
            streamWriter.WriteInt32(value);
            stream.Position = currentPosition;
        }
    }
}

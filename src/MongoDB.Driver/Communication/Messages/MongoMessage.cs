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
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Internal
{
    internal abstract class MongoMessage
    {
        // private fields
        private int _messageLength;
        private int _requestId;
        private int _responseTo;
        private MessageOpcode _opcode;

        // constructors
        protected MongoMessage(MessageOpcode opcode)
        {
            _opcode = opcode;
        }

        // internal properties
        internal int MessageLength
        {
            get { return _messageLength; }
            set { _messageLength = value; }
        }

        internal int RequestId
        {
            get { return _requestId; }
            set { _requestId = value; }
        }

        internal int ResponseTo
        {
            get { return _responseTo; }
        }

        // internal methods
        internal virtual void ReadHeaderFrom(BsonStreamReader streamReader)
        {
            _messageLength = streamReader.ReadInt32();
            _requestId = streamReader.ReadInt32();
            _responseTo = streamReader.ReadInt32();
            if ((MessageOpcode)streamReader.ReadInt32() != _opcode)
            {
                throw new FileFormatException("Message header opcode is not the expected one.");
            }
        }

        internal virtual void WriteHeaderTo(BsonStreamWriter streamWriter)
        {
            streamWriter.WriteInt32(0); // messageLength will be backpatched later
            streamWriter.WriteInt32(_requestId);
            streamWriter.WriteInt32(0); // responseTo not used in requests sent by client
            streamWriter.WriteInt32((int)_opcode);
        }
    }
}

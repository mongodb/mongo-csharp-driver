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

        // protected methods
        protected void ReadMessageHeaderFrom(BsonBuffer buffer)
        {
            _messageLength = buffer.ReadInt32();
            _requestId = buffer.ReadInt32();
            _responseTo = buffer.ReadInt32();
            if ((MessageOpcode)buffer.ReadInt32() != _opcode)
            {
                throw new FileFormatException("Message header opcode is not the expected one.");
            }
        }

        protected void WriteMessageHeaderTo(BsonBuffer buffer)
        {
            buffer.WriteInt32(0); // messageLength will be backpatched later
            buffer.WriteInt32(_requestId);
            buffer.WriteInt32(0); // responseTo not used in requests sent by client
            buffer.WriteInt32((int)_opcode);
        }
    }
}

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

using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Internal
{
    internal abstract class MongoMessage
    {
        // protected fields
        protected int messageLength;
        protected int requestId;
        protected int responseTo;
        protected MessageOpcode opcode;

        // constructors
        protected MongoMessage(MessageOpcode opcode)
        {
            this.opcode = opcode;
        }

        // internal properties
        internal int MessageLength
        {
            get { return messageLength; }
        }

        internal int RequestId
        {
            get { return requestId; }
        }

        internal int ResponseTo
        {
            get { return responseTo; }
        }

        // protected methods
        protected void ReadMessageHeaderFrom(BsonBuffer buffer)
        {
            messageLength = buffer.ReadInt32();
            requestId = buffer.ReadInt32();
            responseTo = buffer.ReadInt32();
            if ((MessageOpcode)buffer.ReadInt32() != opcode)
            {
                throw new FileFormatException("Message header opcode is not the expected one.");
            }
        }

        protected void WriteMessageHeaderTo(BsonBuffer buffer)
        {
            buffer.WriteInt32(0); // messageLength will be backpatched later
            buffer.WriteInt32(requestId);
            buffer.WriteInt32(0); // responseTo not used in requests sent by client
            buffer.WriteInt32((int)opcode);
        }
    }
}

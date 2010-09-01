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

namespace MongoDB.MongoDBClient.Internal {
    internal abstract class MongoMessage {
        #region protected fields
        protected int messageLength;
        protected int requestId;
        protected int responseTo;
        protected MessageOpcode opcode;
        #endregion

        #region constructors
        protected MongoMessage(
            MessageOpcode opcode
        ) {
            this.opcode = opcode;
        }
        #endregion

        #region internal properties
        internal int MessageLength {
            get { return messageLength; }
        }

        internal int RequestId {
            get { return requestId; }
        }

        internal int ResponseTo {
            get { return responseTo; }
        }

        internal MessageOpcode Opcode {
            get { return opcode; }
        }
        #endregion

        #region protected methods
        protected void ReadMessageHeaderFrom(
            BinaryReader binaryReader
        ) {
            messageLength = binaryReader.ReadInt32();
            requestId = binaryReader.ReadInt32();
            responseTo = binaryReader.ReadInt32();
            if ((MessageOpcode) binaryReader.ReadInt32() != opcode) {
                throw new MongoException("Message header opcode was not the expected one");
            }
        }

        protected void WriteMessageHeaderTo(
            BinaryWriter binaryWriter
        ) {
            binaryWriter.Write(0); // messageLength will be backpatched later
            binaryWriter.Write(requestId);
            binaryWriter.Write(0); // responseTo not used in requests sent by client
            binaryWriter.Write((int) opcode);
        }
        #endregion
    }
}

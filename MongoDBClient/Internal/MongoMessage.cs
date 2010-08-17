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
    public abstract class MongoMessage {
        #region private fields
        private int messageLength;
        private int requestID;
        private int responseTo;
        private RequestOpCode opCode;
        #endregion

        #region constructors
        protected MongoMessage(
            RequestOpCode opCode
        ) {
            this.opCode = opCode;
        }
        #endregion

        #region public properties
        public int MessageLength {
            get { return messageLength; }
            set { messageLength = value; }
        }

        public int RequestID {
            get { return requestID; }
            set { requestID = value; }
        }

        public int ResponseTo {
            get { return responseTo; }
            set { responseTo = value; }
        }

        public RequestOpCode OpCode {
            get { return opCode; }
            set { opCode = value; }
        }
        #endregion

        #region public methods
        public void WriteTo(
            Stream stream
        ) {
            BinaryWriter writer = new BinaryWriter(stream);
            long start = stream.Position;

            writer.Write(0); // will be backpatched
            writer.Write(requestID);
            writer.Write(0); // responseTo
            writer.Write((int) opCode);
            WriteBodyTo(writer);

            writer.Flush();
            long end = stream.Position;
            long messageLength = end - start;
            stream.Seek(start, SeekOrigin.Begin);
            writer.Write((int) messageLength);
            writer.Flush();
            stream.Seek(end, SeekOrigin.Begin);
        }
        #endregion

        #region protected methods
        protected abstract void WriteBodyTo(
            BinaryWriter writer
        );

        protected void WriteCString(
            BinaryWriter writer,
            string value
        ) {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(value);
            if (utf8Bytes.Contains((byte) 0)) {
                throw new MongoException("A cstring cannot contain 0x00");
            }
            writer.Write(utf8Bytes);
            writer.Write((byte) 0);
        }
        #endregion
    }
}

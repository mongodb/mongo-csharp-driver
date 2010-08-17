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
using System.Net.Sockets;
using System.Text;

namespace MongoDB.MongoDBClient.Internal {
    internal class MongoConnection : IDisposable {
        #region private fields
        private string host;
        private int port;
        private TcpClient tcpClient;
        #endregion

        #region constructors
        internal MongoConnection(
            string host,
            int port
        ) {
            this.host = host;
            this.port = port;

            tcpClient = new TcpClient(host, port);
        }
        #endregion

        #region public methods
        public void Dispose() {
        }

        internal MongoReplyMessage ReceiveMessage() {
            NetworkStream networkStream = tcpClient.GetStream();
            return MongoReplyMessage.ReadFrom(networkStream);
        }

        internal void SendMessage(
            MongoMessage message
        ) {
            MemoryStream memoryStream = new MemoryStream();
            message.WriteTo(memoryStream);
            byte[] bytes = memoryStream.ToArray();
            tcpClient.GetStream().Write(bytes, 0, bytes.Length);
        }
        #endregion
    }
}

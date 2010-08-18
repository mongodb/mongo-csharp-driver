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
        private bool disposed = false;
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
            if (!disposed) {
                try {
                    // note: TcpClient.Close doesn't close the NetworkStream!?
                    NetworkStream networkStream = tcpClient.GetStream();
                    if (networkStream != null) {
                        networkStream.Close();
                    }
                    tcpClient.Close();
                    ((IDisposable) tcpClient).Dispose(); // Dispose is not public!?
                } catch { } // ignore exceptions
                tcpClient = null;
                disposed = true;
            }
        }

        internal MongoReplyMessage<T> ReceiveMessage<T>() where T : class, new() {
            if (disposed) { throw new ObjectDisposedException("MongoConnection"); }
            var bytes = ReadReply();
            var reply = new MongoReplyMessage<T>();
            reply.ReadFrom(bytes);
            return reply;
        }

        internal void SendMessage(
            MongoMessage message
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoConnection"); }
            MemoryStream memoryStream = new MemoryStream(); // write to MemoryStream first because NetworkStream can't Seek
            message.WriteTo(memoryStream);
            NetworkStream networkStream = tcpClient.GetStream();
            networkStream.Write(memoryStream.GetBuffer(), 0, (int) memoryStream.Length);
        }
        #endregion

        #region private methods
        private byte[] ReadReply() {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] messageLengthBytes = new byte[4];
            networkStream.Read(messageLengthBytes, 0, 4);
            BinaryReader reader = new BinaryReader(new MemoryStream(messageLengthBytes));
            int messageLength = reader.ReadInt32();

            // create reply buffer and copy message length bytes to it
            byte[] reply = new byte[messageLength];
            Array.Copy(messageLengthBytes, reply, 4);

            // keep reading until entire reply has been received
            int offset = 4;
            int remaining = messageLength - 4;
            while (remaining > 0) {
                int bytesRead = networkStream.Read(reply, offset, remaining);
                offset += bytesRead;
                remaining -= bytesRead;
            }

            return reply;
        }
        #endregion
    }
}

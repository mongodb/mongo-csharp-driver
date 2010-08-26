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

using MongoDB.BsonLibrary;

namespace MongoDB.MongoDBClient.Internal {
    internal class MongoConnection : IDisposable {
        #region private fields
        private bool disposed = false;
        private string host;
        private int port;
        private TcpClient tcpClient;
        private int messageCounter;
        private MongoDatabase database; // this is constantly changing as pooled connection is reused
        #endregion

        #region constructors
        internal MongoConnection(
            string host,
            int port
        ) {
            this.host = host;
            this.port = port;

            tcpClient = new TcpClient(host, port);
            tcpClient.NoDelay = true; // turn off Nagle
            tcpClient.ReceiveBufferSize = Mongo.TcpReceiveBufferSize; // default 4MB
            tcpClient.SendBufferSize = Mongo.TcpSendBufferSize; // default 4MB
        }
        #endregion

        #region public properties
        public MongoDatabase Database {
            get { return database; }
            internal set { database = value; }
        }

        public int MessageCounter {
            get { return messageCounter; }
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

        public BsonDocument GetLastError(
            MongoDatabase database
        ) {
            throw new NotImplementedException();
        }

        internal MongoReplyMessage<T> ReceiveMessage<T>() where T : new() {
            if (disposed) { throw new ObjectDisposedException("MongoConnection"); }
            var bytes = ReadMessageBytes();
            var reply = new MongoReplyMessage<T>();
            reply.ReadFrom(bytes);
            return reply;
        }

        internal void SendMessage(
            MongoRequestMessage message
        ) {
            SendMessage(message, false);
        }

        internal void SendMessage(
            MongoRequestMessage message,
            bool safeMode
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoConnection"); }
            MemoryStream memoryStream = message.AsMemoryStream();

            if (safeMode) {
                var commandCollection = database.GetCollection("$cmd");
                var command = new BsonDocument("getLastError", 1);
                var getLastErrorMessage = new MongoQueryMessage(commandCollection, QueryFlags.None, 0, 1, command, null);
                getLastErrorMessage.WriteTo(memoryStream);
            }

            NetworkStream networkStream = tcpClient.GetStream();
            networkStream.Write(memoryStream.GetBuffer(), 0, (int) memoryStream.Length);
            messageCounter++;

            if (safeMode) {
                var replyMessage = ReceiveMessage<BsonDocument>();
                var result = replyMessage.Documents[0];

                object ok = result["ok"];
                if (ok == null) {
                    throw new MongoException("ok element is missing");
                }
                if (
                    ok is bool && !((bool) ok) ||
                    ok is int && (int) ok != 1 ||
                    ok is double && (double) ok != 1.0
                ) {
                    string errmsg = (string) result["errmsg"];
                    string errorMessage = string.Format("Safemode detected an error ({0})", errmsg);
                    throw new MongoException(errorMessage);
                }

                string err = result["err"] as string;
                if (!string.IsNullOrEmpty(err)) {
                    string errorMessage = string.Format("Safemode detected an error ({0})", err);
                    throw new MongoException(errorMessage);
                }
            }
        }

        public BsonDocument TryGetLastError(
            MongoDatabase database,
            int originalMessageCounter
        ) {
            if (messageCounter != originalMessageCounter) {
                throw new MongoException("Too late to call GetLastError");
            }
            return GetLastError(database);
        }
        #endregion

        #region private methods
        private byte[] ReadMessageBytes() {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] messageLengthBytes = new byte[4];
            networkStream.Read(messageLengthBytes, 0, 4);
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(messageLengthBytes));
            int messageLength = binaryReader.ReadInt32();

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

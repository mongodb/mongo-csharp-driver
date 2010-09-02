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
        private object connectionLock = new object();
        private bool disposed = false;
        private MongoServerAddress address;
        private TcpClient tcpClient;
        private int messageCounter;
        private MongoConnectionPool connectionPool;
        private MongoDatabase database; // this is constantly changing as pooled connection is reused
        #endregion

        #region constructors
        internal MongoConnection(
            MongoServerAddress address
        ) {
            this.address = address;

            tcpClient = new TcpClient(address.Host, address.Port);
            tcpClient.NoDelay = true; // turn off Nagle
            tcpClient.ReceiveBufferSize = MongoDefaults.TcpReceiveBufferSize; // default 4MB
            tcpClient.SendBufferSize = MongoDefaults.TcpSendBufferSize; // default 4MB
        }
        #endregion

        #region internal properties
        internal MongoConnectionPool ConnectionPool {
            get { return connectionPool; }
            set { connectionPool = value; }
        }

        internal MongoDatabase Database {
            get { return database; }
            set { database = value; }
        }

        internal int MessageCounter {
            get { return messageCounter; }
        }
        #endregion

        #region public methods
        // Dispose has to be public in order to implement IDisposable
        public void Dispose() {
            if (!disposed) {
                try {
                    Close();
                } catch { } // ignore exceptions
                disposed = true;
            }
        }
        #endregion

        #region internal methods
        internal void Close() {
            lock (connectionLock) {
                if (tcpClient != null) {
                    // note: TcpClient.Close doesn't close the NetworkStream!?
                    NetworkStream networkStream = tcpClient.GetStream();
                    if (networkStream != null) {
                        networkStream.Close();
                    }
                    tcpClient.Close();
                    ((IDisposable) tcpClient).Dispose(); // Dispose is not public!?
                    tcpClient = null;
                }
            }
        }

        internal MongoReplyMessage<T> ReceiveMessage<T>() where T : new() {
            lock (connectionLock) {
                if (disposed) { throw new ObjectDisposedException("MongoConnection"); }
                byte[] bytes;
                try {
                    bytes = ReadMessageBytes();
                } catch (SocketException ex) {
                    HandleSocketException(ex);
                    throw;
                }
                var reply = new MongoReplyMessage<T>();
                reply.ReadFrom(bytes);
                return reply;
            }
        }

        internal BsonDocument SendMessage(
            MongoRequestMessage message,
            SafeMode safeMode
        ) {
            lock (connectionLock) {
                if (disposed) { throw new ObjectDisposedException("MongoConnection"); }
                MemoryStream memoryStream = message.GetMemoryStream();

                if (safeMode.Enabled) {
                    var command = new BsonDocument {
                        { "getLastError", 1 },
                        { safeMode.Replications > 1, "w", safeMode.Replications },
                        { safeMode.Replications > 1 && safeMode.Timeout != TimeSpan.Zero, "wtimeout", (int) safeMode.Timeout.TotalMilliseconds }
                    };
                    var getLastErrorMessage = new MongoQueryMessage(database.CommandCollection.FullName, QueryFlags.None, 0, 1, command, null);
                    getLastErrorMessage.WriteTo(memoryStream); // piggy back on network transmission for message
                }

                try {
                    NetworkStream networkStream = tcpClient.GetStream();
                    networkStream.Write(memoryStream.GetBuffer(), 0, (int) memoryStream.Length);
                    messageCounter++;
                } catch (SocketException ex) {
                    HandleSocketException(ex);
                    throw;
                }

                BsonDocument lastError = null;
                if (safeMode.Enabled) {
                    var replyMessage = ReceiveMessage<BsonDocument>();
                    lastError = replyMessage.Documents[0];

                    if (!lastError.ContainsElement("ok")) {
                        throw new MongoException("ok element is missing");
                    }
                    if (!lastError.GetAsBoolean("ok")) {
                        string errmsg = (string) lastError["errmsg"];
                        string errorMessage = string.Format("Safemode detected an error ({0})", errmsg);
                        throw new MongoException(errorMessage);
                    }

                    string err = lastError["err"] as string;
                    if (!string.IsNullOrEmpty(err)) {
                        string errorMessage = string.Format("Safemode detected an error ({0})", err);
                        throw new MongoException(errorMessage);
                    }
                }

                return lastError;
            }
        }
        #endregion

        #region private methods
        private void HandleSocketException(
            SocketException ex
        ) {
            if (connectionPool != null) {
                // TODO: analyze SocketException to determine if the server is really down?
                // for now assume it is and force MongoServer to find a new primary by calling Disconnect
                try {
                    connectionPool.Server.Disconnect();
                } catch { } // ignore any further exceptions
            }
        }

        private byte[] ReadMessageBytes() {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] messageLengthBytes = new byte[4];
            networkStream.Read(messageLengthBytes, 0, 4);
            BinaryReader binaryReader = new BinaryReader(new MemoryStream(messageLengthBytes));
            int messageLength = binaryReader.ReadInt32();

            // create reply buffer and copy message length bytes to it
            byte[] reply = new byte[messageLength];
            Buffer.BlockCopy(messageLengthBytes, 0, reply, 0, 4);

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

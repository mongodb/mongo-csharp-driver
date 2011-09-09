﻿/* Copyright 2010-2011 10gen Inc.
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
using System.Net;
using System.Net.Sockets;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Internal {
    /// <summary>
    /// Represents the state of a connection.
    /// </summary>
    public enum MongoConnectionState {
        /// <summary>
        /// The connection has not yet been initialized.
        /// </summary>
        Initial,
        /// <summary>
        /// The connection is open.
        /// </summary>
        Open,
        /// <summary>
        /// The connection is closed.
        /// </summary>
        Closed
    }

    /// <summary>
    /// Represents a connection to a MongoServerInstance.
    /// </summary>
    public class MongoConnection {
        #region private fields
        private object connectionLock = new object();
        private MongoServerInstance serverInstance;
        private MongoConnectionPool connectionPool;
        private int generationId; // the generationId of the connection pool at the time this connection was created
        private MongoConnectionState state;
        private TcpClient tcpClient;
        private DateTime createdAt;
        private DateTime lastUsedAt; // set every time the connection is Released
        private int messageCounter;
        private int requestId;
        private Dictionary<string, Authentication> authentications = new Dictionary<string, Authentication>();
        #endregion

        #region constructors
        internal MongoConnection(
            MongoConnectionPool connectionPool
        ) {
            this.serverInstance = connectionPool.ServerInstance;
            this.connectionPool = connectionPool;
            this.generationId = connectionPool.GenerationId;
            this.createdAt = DateTime.UtcNow;
            this.state = MongoConnectionState.Initial;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the connection pool that this connection belongs to.
        /// </summary>
        public MongoConnectionPool ConnectionPool {
            get { return connectionPool; }
        }

        /// <summary>
        /// Gets the DateTime that this connection was created at.
        /// </summary>
        public DateTime CreatedAt {
            get { return createdAt; }
        }

        /// <summary>
        /// Gets the generation of the connection pool that this connection belongs to.
        /// </summary>
        public int GenerationId {
            get { return generationId; }
        }

        /// <summary>
        /// Gets the DateTime that this connection was last used at.
        /// </summary>
        public DateTime LastUsedAt {
            get { return lastUsedAt; }
            internal set { lastUsedAt = value; }
        }

        /// <summary>
        /// Gets a count of the number of messages that have been sent using this connection.
        /// </summary>
        public int MessageCounter {
            get { return messageCounter; }
        }

        /// <summary>
        /// Gets the RequestId of the last message sent on this connection.
        /// </summary>
        public int RequestId {
            get { return requestId; }
        }

        /// <summary>
        /// Gets the server instance this connection is connected to.
        /// </summary>
        public MongoServerInstance ServerInstance {
            get { return serverInstance; }
        }

        /// <summary>
        /// Gets the state of this connection.
        /// </summary>
        public MongoConnectionState State {
            get { return state; }
        }
        #endregion

        #region internal methods
        internal void Authenticate(
            string databaseName,
            MongoCredentials credentials
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }
            lock (connectionLock) {
                var nonceCommand = new CommandDocument("getnonce", 1);
                var commandCollectionName = string.Format("{0}.$cmd", databaseName);
                string nonce;
                try {
                    var nonceResult = RunCommand(commandCollectionName, QueryFlags.None, nonceCommand);
                    nonce = nonceResult.Response["nonce"].AsString;
                } catch (MongoCommandException ex) {
                    throw new MongoAuthenticationException("Error getting nonce for authentication.", ex);
                }

                var passwordDigest = MongoUtils.Hash(credentials.Username + ":mongo:" + credentials.Password);
                var digest = MongoUtils.Hash(nonce + credentials.Username + passwordDigest);
                var authenticateCommand = new CommandDocument {
                    { "authenticate", 1 },
                    { "user", credentials.Username },
                    { "nonce", nonce },
                    { "key", digest }
                };
                try {
                    RunCommand(commandCollectionName, QueryFlags.None, authenticateCommand);
                } catch (MongoCommandException ex) {
                    var message = string.Format("Invalid credentials for database '{0}'.", databaseName);
                    throw new MongoAuthenticationException(message, ex);
                }

                var authentication = new Authentication(credentials);
                authentications.Add(databaseName, authentication);
            }
        }

        // check whether the connection can be used with the given database (and credentials)
        // the following are the only valid authentication states for a connection:
        // 1. the connection is not authenticated against any database
        // 2. the connection has a single authentication against the admin database (with a particular set of credentials)
        // 3. the connection has one or more authentications against any databases other than admin
        //    (with the restriction that a particular database can only be authenticated against once and therefore with only one set of credentials)

        // assume that IsAuthenticated was called first and returned false
        internal bool CanAuthenticate(
            MongoDatabase database
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }
            if (database == null) {
                return true;
            }

            if (authentications.Count == 0) {
                // a connection with no existing authentications can authenticate anything
                return true;
            } else {
                // a connection with existing authentications can't be used without credentials
                if (database.Credentials == null) {
                    return false;
                }

                // a connection with existing authentications can't be used with new admin credentials
                if (database.Credentials.Admin) {
                    return false;
                }

                // a connection with an existing authentication to the admin database can't be used with any other credentials
                if (authentications.ContainsKey("admin")) {
                    return false;
                }

                // a connection with an existing authentication to a database can't authenticate for the same database again
                if (authentications.ContainsKey(database.Name)) {
                    return false;
                }

                return true;
            }
        }

        internal void CheckAuthentication(
            MongoDatabase database
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }
            if (database.Credentials == null) {
                if (authentications.Count != 0) {
                    throw new InvalidOperationException("Connection requires credentials.");
                }
            } else {
                var credentials = database.Credentials;
                var authenticationDatabaseName = credentials.Admin ? "admin" : database.Name;
                Authentication authentication;
                if (authentications.TryGetValue(authenticationDatabaseName, out authentication)) {
                    if (authentication.Credentials != database.Credentials) {
                        // this shouldn't happen because a connection would have been chosen from the connection pool only if it was viable
                        if (authenticationDatabaseName == "admin") {
                            throw new MongoInternalException("Connection already authenticated to the admin database with different credentials.");
                        } else {
                            throw new MongoInternalException("Connection already authenticated to the database with different credentials.");
                        }
                    }
                    authentication.LastUsed = DateTime.UtcNow;
                } else {
                    if (authenticationDatabaseName == "admin" && authentications.Count != 0) {
                        // this shouldn't happen because a connection would have been chosen from the connection pool only if it was viable
                        throw new MongoInternalException("The connection cannot be authenticated against the admin database because it is already authenticated against other databases.");
                    }
                    Authenticate(authenticationDatabaseName, database.Credentials);
                }
            }
        }

        internal void Close() {
            lock (connectionLock) {
                if (state != MongoConnectionState.Closed) {
                    if (tcpClient != null) {
                        if (tcpClient.Connected) {
                            // even though MSDN says TcpClient.Close doesn't close the underlying socket
                            // it actually does (as proven by disassembling TcpClient and by experimentation)
                            tcpClient.Close();
                        }
                        tcpClient = null;
                    }
                    state = MongoConnectionState.Closed;
                }
            }
        }

        internal bool IsAuthenticated(
            MongoDatabase database
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }
            if (database == null) {
                return true;
            }

            lock (connectionLock) {
                if (database.Credentials == null) {
                    return authentications.Count == 0;
                } else {
                    var authenticationDatabaseName = database.Credentials.Admin ? "admin" : database.Name;
                    Authentication authentication;
                    if (authentications.TryGetValue(authenticationDatabaseName, out authentication)) {
                        return database.Credentials == authentication.Credentials;
                    } else {
                        return false;
                    }
                }
            }
        }

        internal void Logout(
            string databaseName
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }
            lock (connectionLock) {
                var logoutCommand = new CommandDocument("logout", 1);
                var commandCollectionName = string.Format("{0}.$cmd", databaseName);
                try {
                    RunCommand(commandCollectionName, QueryFlags.None, logoutCommand);
                } catch (MongoCommandException ex) {
                    throw new MongoAuthenticationException("Error logging off.", ex);
                }

                authentications.Remove(databaseName);
            }
        }

        internal void Open() {
            if (state != MongoConnectionState.Initial) {
                throw new InvalidOperationException("Open called more than once.");
            }

            var endPoint = serverInstance.EndPoint;
            var tcpClient = new TcpClient(endPoint.AddressFamily);
            tcpClient.NoDelay = true; // turn off Nagle
            tcpClient.ReceiveBufferSize = MongoDefaults.TcpReceiveBufferSize;
            tcpClient.SendBufferSize = MongoDefaults.TcpSendBufferSize;
            tcpClient.Connect(endPoint);

            this.tcpClient = tcpClient;
            this.state = MongoConnectionState.Open;
        }

        // this is a low level method that doesn't require a MongoServer
        // so it can be used while connecting to a MongoServer
        internal CommandResult RunCommand(
            string collectionName,
            QueryFlags queryFlags,
            CommandDocument command
        ) {
            var commandName = command.GetElement(0).Name;

            var writerSettings = new BsonBinaryWriterSettings {
                GuidRepresentation = GuidRepresentation.Unspecified,
                MaxDocumentSize = serverInstance.MaxDocumentSize
            };
            using (
                var message = new MongoQueryMessage(
                    writerSettings,
                    collectionName,
                    queryFlags,
                    0, // numberToSkip
                    1, // numberToReturn (must be 1 or -1 for commands)
                    command,
                    null // fields
                )
            ) {
                SendMessage(message, SafeMode.False);
            }

            var readerSettings = new BsonBinaryReaderSettings {
                    GuidRepresentation = GuidRepresentation.Unspecified,
                    MaxDocumentSize = serverInstance.MaxDocumentSize
            };
            var reply = ReceiveMessage<BsonDocument>(readerSettings, null);
            if (reply.NumberReturned == 0) {
                var message = string.Format("Command '{0}' failed. No response returned.", commandName);
                throw new MongoCommandException(message);
            }

            var commandResult = new CommandResult(command, reply.Documents[0]);
            if (!commandResult.Ok) {
                throw new MongoCommandException(commandResult);
            }

            return commandResult;
        }

        internal MongoReplyMessage<TDocument> ReceiveMessage<TDocument>(
            BsonBinaryReaderSettings readerSettings,
            IBsonSerializationOptions serializationOptions
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }
            lock (connectionLock) {
                try {
                    using (var buffer = new BsonBuffer()) {
                        var networkStream = GetNetworkStream();
                        networkStream.ReadTimeout = (int) serverInstance.Server.Settings.SocketTimeout.TotalMilliseconds;
                        buffer.LoadFrom(networkStream);
                        var reply = new MongoReplyMessage<TDocument>(readerSettings);
                        reply.ReadFrom(buffer, serializationOptions);
                        return reply;
                    }
                } catch (Exception ex) {
                    HandleException(ex);
                    throw;
                }
            }
        }

        internal SafeModeResult SendMessage(
            MongoRequestMessage message,
            SafeMode safeMode
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }
            lock (connectionLock) {
                requestId = message.RequestId;

                message.WriteToBuffer();
                CommandDocument safeModeCommand = null;
                if (safeMode.Enabled) {
                    safeModeCommand = new CommandDocument {
                        { "getlasterror", 1 }, // use all lowercase for backward compatibility
                        { "fsync", true, safeMode.FSync },
                        { "w", safeMode.W, safeMode.W > 1 },
                        { "wtimeout", (int) safeMode.WTimeout.TotalMilliseconds, safeMode.W > 1 && safeMode.WTimeout != TimeSpan.Zero }
                    };
                    using (
                        var getLastErrorMessage = new MongoQueryMessage(
                            message.Buffer, // piggy back on network transmission for message
                            message.WriterSettings,
                            "admin.$cmd", // collectionFullName
                            QueryFlags.None,
                            0, // numberToSkip
                            1, // numberToReturn
                            safeModeCommand,
                            null // fields
                        )
                    ) {
                        getLastErrorMessage.WriteToBuffer();
                    }
                }

                try {
                    var networkStream = GetNetworkStream();
                    networkStream.WriteTimeout = (int) serverInstance.Server.Settings.SocketTimeout.TotalMilliseconds;
                    message.Buffer.WriteTo(networkStream);
                    messageCounter++;
                } catch (Exception ex) {
                    HandleException(ex);
                    throw;
                }

                SafeModeResult safeModeResult = null;
                if (safeMode.Enabled) {
                    var readerSettings = new BsonBinaryReaderSettings {
                        GuidRepresentation = message.WriterSettings.GuidRepresentation,
                        MaxDocumentSize = serverInstance.MaxDocumentSize
                    };
                    var replyMessage = ReceiveMessage<BsonDocument>(readerSettings, null);
                    var safeModeResponse = replyMessage.Documents[0];
                    safeModeResult = new SafeModeResult();
                    safeModeResult.Initialize(safeModeCommand, safeModeResponse);

                    if (!safeModeResult.Ok) {
                        var errorMessage = string.Format("Safemode detected an error '{0}'. (response was {1}).", safeModeResult.ErrorMessage, safeModeResponse.ToJson());
                        throw new MongoSafeModeException(errorMessage, safeModeResult);
                    }
                    if (safeModeResult.HasLastErrorMessage) {
                        var errorMessage = string.Format("Safemode detected an error '{0}'. (Response was {1}).", safeModeResult.LastErrorMessage, safeModeResponse.ToJson());
                        throw new MongoSafeModeException(errorMessage, safeModeResult);
                    }
                }

                return safeModeResult;
            }
        }
        #endregion

        #region private methods
        private NetworkStream GetNetworkStream() {
            if (state == MongoConnectionState.Initial) {
                Open();
            }
            return tcpClient.GetStream();
        }

        private void HandleException(
            Exception ex
        ) {
            // there are three possible situations:
            // 1. we can keep using the connection
            // 2. just this one connection needs to be closed
            // 3. the whole connection pool needs to be cleared

            switch (DetermineAction(ex)) {
                case HandleExceptionAction.KeepConnection:
                    break;
                case HandleExceptionAction.CloseConnection:
                    Close();
                    break;
                case HandleExceptionAction.ClearConnectionPool:
                    Close();
                    connectionPool.Clear();
                    break;
                default:
                    throw new MongoInternalException("Invalid HandleExceptionAction");
            }

            // forces a call to VerifyState before the next message is sent to this server instance
            // this is a bit drastic but at least it's safe (and perhaps we can optimize a bit in the future)
            serverInstance.State = MongoServerState.Unknown;
        }

        private enum HandleExceptionAction {
            KeepConnection,
            CloseConnection,
            ClearConnectionPool
        }

        private HandleExceptionAction DetermineAction(
            Exception ex
        ) {
            // TODO: figure out when to return KeepConnection or ClearConnectionPool (if ever)

            // don't return ClearConnectionPool unless you are *sure* it is the right action
            // definitely don't make ClearConnectionPool the default action
            // returning ClearConnectionPool frequently can result in Connect/Disconnect storms

            return HandleExceptionAction.CloseConnection; // this should always be the default action
        }
        #endregion

        #region private nested classes
        // keeps track of what credentials were used with a given database
        // and when that database was last used on this connection
        private class Authentication {
            #region private fields
            private MongoCredentials credentials;
            private DateTime lastUsed;
            #endregion

            #region constructors
            public Authentication(
                MongoCredentials credentials
            ) {
                this.credentials = credentials;
                this.lastUsed = DateTime.UtcNow;
            }
            #endregion

            public MongoCredentials Credentials {
                get { return credentials; }
            }

            public DateTime LastUsed {
                get { return lastUsed; }
                set { lastUsed = value; }
            }
        }
        #endregion
    }
}

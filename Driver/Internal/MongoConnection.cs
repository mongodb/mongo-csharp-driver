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

namespace MongoDB.Driver.Internal {
    internal enum MongoConnectionState {
        Initial,
        Open,
        Damaged,
        Closed
    }

    internal class MongoConnection {
        #region private fields
        private object connectionLock = new object();
        private MongoConnectionPool connectionPool;
        private IPEndPoint endPoint;
        private MongoConnectionState state;
        private TcpClient tcpClient;
        private DateTime createdAt;
        private DateTime lastUsedAt; // set every time the connection is Released
        private int messageCounter;
        private Dictionary<string, Authentication> authentications = new Dictionary<string, Authentication>();
        #endregion

        #region constructors
        internal MongoConnection(
            MongoConnectionPool connectionPool,
            IPEndPoint endPoint
        ) {
            this.connectionPool = connectionPool;
            this.endPoint = endPoint;
            this.createdAt = DateTime.UtcNow;
            this.state = MongoConnectionState.Initial;
        }
        #endregion

        #region internal properties
        internal MongoConnectionPool ConnectionPool {
            get { return connectionPool; }
        }

        internal DateTime CreatedAt {
            get { return createdAt; }
        }

        internal IPEndPoint EndPoint {
            get { return endPoint; }
        }

        internal DateTime LastUsedAt {
            get { return lastUsedAt; }
            set { lastUsedAt = value; }
        }

        internal int MessageCounter {
            get { return messageCounter; }
        }

        internal MongoConnectionState State {
            get { return state; }
        }
        #endregion

        #region internal methods
        internal void Authenticate(
            MongoServer server,
            string databaseName,
            MongoCredentials credentials
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
                var nonceCommand = new CommandDocument("getnonce", 1);
                var commandCollectionName = string.Format("{0}.$cmd", databaseName);
                string nonce;
                try {
                    var nonceResult = RunCommand(server, commandCollectionName, QueryFlags.None, nonceCommand);
                    nonce = nonceResult.Response["nonce"].AsString;
                } catch (MongoCommandException ex) {
                    throw new MongoAuthenticationException("Error getting nonce for authentication", ex);
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
                    RunCommand(server, commandCollectionName, QueryFlags.None, authenticateCommand);
                } catch (MongoCommandException ex) {
                    var message = string.Format("Invalid credentials for database: {0}", databaseName);
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
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed"); }
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
            MongoServer server,
            MongoDatabase database
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed"); }
            if (database.Credentials == null) {
                if (authentications.Count != 0) {
                    throw new InvalidOperationException("Connection requires credentials");
                }
            } else {
                var credentials = database.Credentials;
                var authenticationDatabaseName = credentials.Admin ? "admin" : database.Name;
                Authentication authentication;
                if (authentications.TryGetValue(authenticationDatabaseName, out authentication)) {
                    if (authentication.Credentials != database.Credentials) {
                        // this shouldn't happen because a connection would have been chosen from the connection pool only if it was viable
                        if (authenticationDatabaseName == "admin") {
                            throw new MongoInternalException("Connection already authenticated to the admin database with different credentials");
                        } else {
                            throw new MongoInternalException("Connection already authenticated to the database with different credentials");
                        }
                    }
                    authentication.LastUsed = DateTime.UtcNow;
                } else {
                    if (authenticationDatabaseName == "admin" && authentications.Count != 0) {
                        // this shouldn't happen because a connection would have been chosen from the connection pool only if it was viable
                        throw new MongoInternalException("The connection cannot be authenticated against the admin database because it is already authenticated against other databases");
                    }
                    Authenticate(server, authenticationDatabaseName, database.Credentials);
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
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed"); }
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

        // normally a connection is linked to a connection pool at the time it is created
        // but the very first connection was made by FindServer before the connection pool existed
        // we don't want to waste that connection so it becomes the first connection of the new connection pool
        internal void JoinConnectionPool(
            MongoConnectionPool connectionPool
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed"); }
            if (this.connectionPool != null) {
                throw new ArgumentException("The connection is already in a connection pool", "this");
            }
            if (connectionPool.EndPoint != endPoint) {
                throw new ArgumentException("A connection can only join a connection pool with the same IP address", "connectionPool");
            }

            this.connectionPool = connectionPool;
            this.lastUsedAt = DateTime.UtcNow;
        }

        internal void Logout(
            MongoServer server,
            string databaseName
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
                var logoutCommand = new CommandDocument("logout", 1);
                var commandCollectionName = string.Format("{0}.$cmd", databaseName);
                try {
                    RunCommand(server, commandCollectionName, QueryFlags.None, logoutCommand);
                } catch (MongoCommandException ex) {
                    throw new MongoAuthenticationException("Error logging off", ex);
                }

                authentications.Remove(databaseName);
            }
        }

        internal void Open() {
            if (state != MongoConnectionState.Initial) {
                throw new InvalidOperationException("Open called more than once");
            }

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
            MongoServer server,
            string collectionName,
            QueryFlags queryFlags,
            CommandDocument command
        ) {
            var commandName = command.GetElement(0).Name;

            using (
                var message = new MongoQueryMessage(
                    server,
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

            var reply = ReceiveMessage<BsonDocument>(server);
            if (reply.NumberReturned == 0) {
                var message = string.Format("Command '{0}' failed: no response returned", commandName);
                throw new MongoCommandException(message);
            }

            var commandResult = new CommandResult(command, reply.Documents[0]);
            if (!commandResult.Ok) {
                throw new MongoCommandException(commandResult);
            }

            return commandResult;
        }

        internal MongoReplyMessage<TDocument> ReceiveMessage<TDocument>(
            MongoServer server
        ) {
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
                try {
                    using (var buffer = new BsonBuffer()) {
                        var networkStream = GetNetworkStream();
                        networkStream.ReadTimeout = (int) server.Settings.SocketTimeout.TotalMilliseconds;
                        buffer.LoadFrom(networkStream);
                        var reply = new MongoReplyMessage<TDocument>(server);
                        reply.ReadFrom(buffer);
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
            if (state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
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
                            message.Server,
                            "admin.$cmd", // collectionFullName
                            QueryFlags.None,
                            0, // numberToSkip
                            1, // numberToReturn
                            safeModeCommand,
                            null, // fields
                            message.Buffer // piggy back on network transmission for message
                        )
                    ) {
                        getLastErrorMessage.WriteToBuffer();
                    }
                }

                try {
                    var networkStream = GetNetworkStream();
                    networkStream.WriteTimeout = (int) message.Server.Settings.SocketTimeout.TotalMilliseconds;
                    message.Buffer.WriteTo(networkStream);
                    messageCounter++;
                } catch (Exception ex) {
                    HandleException(ex);
                    throw;
                }

                SafeModeResult safeModeResult = null;
                if (safeMode.Enabled) {
                    var replyMessage = ReceiveMessage<BsonDocument>(message.Server);
                    var safeModeResponse = replyMessage.Documents[0];
                    safeModeResult = new SafeModeResult();
                    safeModeResult.Initialize(safeModeCommand, safeModeResponse);

                    if (!safeModeResult.Ok) {
                        var errorMessage = string.Format("Safemode detected an error: {0} (response: {1})", safeModeResult.ErrorMessage, safeModeResponse.ToJson());
                        throw new MongoSafeModeException(errorMessage, safeModeResult);
                    }
                    if (safeModeResult.HasLastErrorMessage) {
                        var errorMessage = string.Format("Safemode detected an error: {0} (response: {1})", safeModeResult.LastErrorMessage, safeModeResponse.ToJson());
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
            // TODO: figure out which exceptions are more serious than others
            // there are three possible situations:
            // 1. we can keep using the connection
            // 2. just this one connection needs to be discarded
            // 3. the whole connection pool needs to be discarded
            // for now the only exception we know affects only one connection is FileFormatException
            // and there are no cases where the connection can continue to be used

            state = MongoConnectionState.Damaged;
            if (!(ex is FileFormatException)) {
                if (connectionPool != null) {
                    try {
                        connectionPool.Server.Disconnect();
                    } catch { } // ignore any further exceptions
                }
            }
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

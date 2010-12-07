﻿/* Copyright 2010 10gen Inc.
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
    internal class MongoConnection {
        #region private fields
        private object connectionLock = new object();
        private MongoConnectionPool connectionPool;
        private IPEndPoint endPoint;
        private bool closed;
        private TcpClient tcpClient;
        private DateTime lastUsed; // set every time the connection is Released
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

            tcpClient = new TcpClient();
            tcpClient.Connect(endPoint);

            tcpClient.NoDelay = true; // turn off Nagle
            tcpClient.ReceiveBufferSize = MongoDefaults.TcpReceiveBufferSize;
            tcpClient.SendBufferSize = MongoDefaults.TcpSendBufferSize;
        }
        #endregion

        #region internal properties
        internal IPEndPoint EndPoint {
            get { return endPoint; }
        }

        internal MongoConnectionPool ConnectionPool {
            get { return connectionPool; }
        }

        internal bool Closed {
            get { return closed; }
        }

        internal DateTime LastUsed {
            get { return lastUsed; }
            set { lastUsed = value; }
        }

        internal int MessageCounter {
            get { return messageCounter; }
        }
        #endregion

        #region internal methods
        internal void Authenticate(
            string databaseName,
            MongoCredentials credentials
        ) {
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
                var nonceCommand = new CommandDocument("getnonce", 1);
                var commandCollectionName = string.Format("{0}.$cmd", databaseName);
                string nonce;
                try {
                    var nonceResult = RunCommand(commandCollectionName, QueryFlags.None, nonceCommand);
                    nonce = nonceResult["nonce"].AsString;
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
                    RunCommand(commandCollectionName, QueryFlags.None, authenticateCommand);
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
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
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
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
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
                    Authenticate(authenticationDatabaseName, database.Credentials);
                }
            }
        }

        internal void Close() {
            lock (connectionLock) {
                if (!closed) {
                    Exception exception = null;
                    // note: TcpClient.Close doesn't close the NetworkStream!?
                    try {
                        var networkStream = tcpClient.GetStream();
                        if (networkStream != null) {
                            networkStream.Close();
                        }
                    } catch (Exception ex) {
                        if (exception == null) { exception = ex; }
                    }
                    try {
                        tcpClient.Close();
                    } catch (Exception ex) {
                        if (exception == null) { exception = ex; }
                    }
                    try {
                        ((IDisposable) tcpClient).Dispose(); // Dispose is not public!?
                    } catch (Exception ex) {
                        if (exception == null) { exception = ex; }
                    }
                    tcpClient = null;
                    closed = true;
                    if (exception != null) { throw exception; }
                }
            }
        }

        internal bool IsAuthenticated(
            MongoDatabase database
        ) {
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
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
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
            if (this.connectionPool != null) {
                throw new ArgumentException("The connection is already in a connection pool", "this");
            }
            if (connectionPool.EndPoint != endPoint) {
                throw new ArgumentException("A connection can only join a connection pool with the same IP address", "connectionPool");
            }

            this.connectionPool = connectionPool;
            this.lastUsed = DateTime.UtcNow;
        }

        internal void Logout(
            string databaseName
        ) {
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
                var logoutCommand = new CommandDocument("logout", 1);
                var commandCollectionName = string.Format("{0}.$cmd", databaseName);
                try {
                    RunCommand(commandCollectionName, QueryFlags.None, logoutCommand);
                } catch (MongoCommandException ex) {
                    throw new MongoAuthenticationException("Error logging off", ex);
                }

                authentications.Remove(databaseName);
            }
        }

        // this is a low level method that doesn't require a MongoServer
        // so it can be used while connecting to a MongoServer
        internal BsonDocument RunCommand(
            string collectionName,
            QueryFlags queryFlags,
            CommandDocument command
        ) {
            var commandName = command.GetElement(0).Name;

            using (
                var message = new MongoQueryMessage(
                    collectionName,
                    queryFlags,
                    0, // numberToSkip
                    1, // numberToReturn
                    command,
                    null // fields
                )
            ) {
                SendMessage(message, SafeMode.False);
            }

            var reply = ReceiveMessage<BsonDocument>();
            if ((reply.ResponseFlags & ResponseFlags.QueryFailure) != 0) {
                var message = string.Format("Command '{0}' failed (QueryFailure flag set)", commandName);
                throw new MongoCommandException(message);
            }
            if (reply.NumberReturned != 1) {
                var message = string.Format("Command '{0}' failed (wrong number of documents returned: {1})", commandName, reply.NumberReturned);
                throw new MongoCommandException(message);
            }

            var commandResult = reply.Documents[0];
            if (!commandResult.Contains("ok")) {
                var message = string.Format("Command '{0}' failed (ok element missing in result)", commandName);
                throw new MongoCommandException(message, commandResult);
            }
            if (!commandResult["ok"].ToBoolean()) {
                string message;
                var err = commandResult["err", null];
                if (err == null || err.IsBsonNull) {
                    message = string.Format("Command '{0}' failed (no error message found)", commandName);
                } else {
                    message = string.Format("Command '{0}' failed ({1})", commandName, err.ToString());
                }
                throw new MongoCommandException(message, commandResult);
            }

            return commandResult;
        }

        internal MongoReplyMessage<TDocument> ReceiveMessage<TDocument>() {
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
                try {
                    var buffer = new BsonBuffer();
                    buffer.LoadFrom(tcpClient.GetStream());
                    var reply = new MongoReplyMessage<TDocument>();
                    reply.ReadFrom(buffer);
                    return reply;
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
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
                message.WriteToBuffer();
                if (safeMode.Enabled) {
                    var command = new CommandDocument {
                        { "getlasterror", 1 }, // use all lowercase for backward compatibility
                        { "fsync", true, safeMode.FSync },
                        { "w", safeMode.W, safeMode.W > 1 },
                        { "wtimeout", (int) safeMode.WTimeout.TotalMilliseconds, safeMode.W > 1 && safeMode.WTimeout != TimeSpan.Zero }
                    };
                    using (
                        var getLastErrorMessage = new MongoQueryMessage(
                            "admin.$cmd", // collectionFullName
                            QueryFlags.None,
                            0, // numberToSkip
                            1, // numberToReturn
                            command,
                            null, // fields
                            message.Buffer // piggy back on network transmission for message
                        )
                    ) {
                        getLastErrorMessage.WriteToBuffer();
                    }
                }

                try {
                    message.Buffer.WriteTo(tcpClient.GetStream());
                    messageCounter++;
                } catch (Exception ex) {
                    HandleException(ex);
                    throw;
                }

                SafeModeResult result = null;
                if (safeMode.Enabled) {
                    var replyMessage = ReceiveMessage<BsonDocument>();
                    var response = replyMessage.Documents[0];
                    result = new SafeModeResult();
                    result.Initialize(response);

                    if (!result.Ok) {
                        var errorMessage = string.Format("Safemode detected an error: {0}", result.ErrorMessage);
                        throw new MongoSafeModeException(errorMessage);
                    }
                    if (result.HasLastErrorMessage) {
                        var errorMessage = string.Format("Safemode detected an error: {0}", result.LastErrorMessage);
                        throw new MongoSafeModeException(errorMessage);
                    }
                }

                return result;
            }
        }
        #endregion

        #region private methods
        private void HandleException(
            Exception ex
        ) {
            // TODO: figure out which exceptions are more serious than others
            // there are three possible situations:
            // 1. we can keep using the connection
            // 2. this one connection needs to be discarded
            // 3. the whole connection pool needs to be discarded
            // for now the only exception we know affects only one connection is FileFormatException

            var disconnect = true;
            if (ex is FileFormatException) {
                disconnect = false;
            }

            if (disconnect) {
                if (connectionPool != null) {
                    try {
                        connectionPool.Server.Disconnect();
                    } catch { } // ignore any further exceptions
                }
            } else {
                try {
                    Close();
                } catch { } // ignore any further exceptions
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

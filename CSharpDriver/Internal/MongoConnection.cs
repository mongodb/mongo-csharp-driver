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
using MongoDB.BsonLibrary.IO;

namespace MongoDB.CSharpDriver.Internal {
    internal class MongoConnection {
        #region private fields
        private object connectionLock = new object();
        private MongoConnectionPool connectionPool;
        private MongoServerAddress address;
        private bool closed;
        private TcpClient tcpClient;
        private DateTime lastUsed; // set every time the connection is Released
        private int messageCounter;
        private Dictionary<string, Authentication> authentications = new Dictionary<string, Authentication>();
        #endregion

        #region constructors
        internal MongoConnection(
            MongoConnectionPool connectionPool,
            MongoServerAddress address
        ) {
            this.connectionPool = connectionPool;
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
                var nonceCommand = new BsonDocument("getnonce", 1);
                using (
                    var nonceMessage = new MongoQueryMessage<BsonDocument>(
                        string.Format("{0}.$cmd", databaseName), // collectionFullName
                        QueryFlags.None,
                        0, // numberToSkip
                        1, // numberToReturn
                        nonceCommand, // query
                        null // fields
                    )
                ) {
                    SendMessage(nonceMessage, SafeMode.False);
                }
                var nonceReply = ReceiveMessage<BsonDocument>();
                var nonceCommandResult = nonceReply.Documents[0];
                if (!nonceCommandResult["ok", false].ToBoolean()) {
                    throw new MongoAuthenticationException("Error getting nonce for authentication");
                }
                var nonce = nonceCommandResult["nonce"].AsString;

                var passwordDigest = MongoUtils.Hash(credentials.Username + ":mongo:" + credentials.Password);
                var digest = MongoUtils.Hash(nonce + credentials.Username + passwordDigest);
                var authenticateCommand = new BsonDocument {
                    { "authenticate", 1 },
                    { "user", credentials.Username },
                    { "nonce", nonce },
                    { "key", digest }
                };
                using (
                    var authenticateMessage = new MongoQueryMessage<BsonDocument>(
                        string.Format("{0}.$cmd", databaseName), // collectionFullName
                        QueryFlags.None,
                        0, // numberToSkip
                        1, // numberToReturn
                        authenticateCommand, // query
                        null // fields
                    )
                ) {
                    SendMessage(authenticateMessage, SafeMode.False);
                }
                var authenticationReply = ReceiveMessage<BsonDocument>();
                var authenticationResult = authenticationReply.Documents[0];
                if (!authenticationResult["ok", false].ToBoolean()) {
                    throw new MongoAuthenticationException("Invalid credentials for database");
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
                    // note: TcpClient.Close doesn't close the NetworkStream!?
                    NetworkStream networkStream = tcpClient.GetStream();
                    if (networkStream != null) {
                        networkStream.Close();
                    }
                    tcpClient.Close();
                    ((IDisposable) tcpClient).Dispose(); // Dispose is not public!?
                    tcpClient = null;
                    closed = true;
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
            if (connectionPool.Address != address) {
                throw new ArgumentException("A connection can only join a connection pool with the same server address", "connectionPool");
            }

            this.connectionPool = connectionPool;
            this.lastUsed = DateTime.UtcNow;
        }

        internal void Logout(
            string databaseName
        ) {
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
                var logoutCommand = new BsonDocument("logout", 1);
                using (
                    var logoutMessage = new MongoQueryMessage<BsonDocument>(
                        string.Format("{0}.$cmd", databaseName), // collectionFullName
                        QueryFlags.None,
                        0, // numberToSkip
                        1, // numberToReturn
                        logoutCommand,
                        null // fields
                    )
                ) {
                    SendMessage(logoutMessage, SafeMode.False);
                }
                var logoutReply = ReceiveMessage<BsonDocument>();
                var logoutCommandResult = logoutReply.Documents[0];
                if (!logoutCommandResult["ok", false].ToBoolean()) {
                    throw new MongoAuthenticationException("Error in logout");
                }

                authentications.Remove(databaseName);
            }
        }

        internal MongoReplyMessage<TDocument> ReceiveMessage<TDocument>() {
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
                BsonBuffer buffer = new BsonBuffer();
                try {
                    buffer.LoadFrom(tcpClient.GetStream());
                } catch (SocketException ex) {
                    HandleSocketException(ex);
                    throw;
                }
                var reply = new MongoReplyMessage<TDocument>();
                reply.ReadFrom(buffer);
                return reply;
            }
        }

        internal BsonDocument SendMessage(
            MongoRequestMessage message,
            SafeMode safeMode
        ) {
            if (closed) { throw new InvalidOperationException("Connection is closed"); }
            lock (connectionLock) {
                message.WriteToBuffer();
                if (safeMode.Enabled) {
                    var command = new BsonDocument {
                        { "getlasterror", 1 }, // use all lowercase for backward compatibility
                        { "w", safeMode.Replications, safeMode.Replications > 1 },
                        { "wtimeout", (int) safeMode.Timeout.TotalMilliseconds, safeMode.Replications > 1 && safeMode.Timeout != TimeSpan.Zero }
                    };
                    using (
                        var getLastErrorMessage = new MongoQueryMessage<BsonDocument>(
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
                    NetworkStream networkStream = tcpClient.GetStream();
                    message.Buffer.WriteTo(networkStream);
                    messageCounter++;
                } catch (SocketException ex) {
                    HandleSocketException(ex);
                    throw;
                }

                BsonDocument lastError = null;
                if (safeMode.Enabled) {
                    var replyMessage = ReceiveMessage<BsonDocument>();
                    lastError = replyMessage.Documents[0];

                    if (!lastError.Contains("ok")) {
                        throw new MongoSafeModeException("ok element is missing");
                    }
                    if (!lastError["ok"].ToBoolean()) {
                        string errmsg = lastError["errmsg"].AsString;
                        string errorMessage = string.Format("Safemode detected an error ({0})", errmsg);
                        throw new MongoSafeModeException(errorMessage);
                    }

                    if (lastError["err", false].ToBoolean()) {
                        var err = lastError["err"].AsString;
                        string errorMessage = string.Format("Safemode detected an error ({0})", err);
                        throw new MongoSafeModeException(errorMessage);
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

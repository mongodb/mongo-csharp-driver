/* Copyright 2010-2011 10gen Inc.
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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using MongoDB.Bson;

namespace MongoDB.Driver.Internal {
    internal class ReplicaSetConnector {
        #region private fields
        private MongoServer server;
        private HashSet<MongoServerAddress> queries = new HashSet<MongoServerAddress>();
        private Dictionary<MongoServerAddress, QueryNodeResponse> responses = new Dictionary<MongoServerAddress, QueryNodeResponse>();
        private MongoConnection primaryConnection;
        private List<MongoConnection> secondaryConnections = new List<MongoConnection>();
        private List<MongoServerAddress> replicaSet;
        private int maxDocumentSize;
        private int maxMessageLength;
        #endregion

        #region constructors
        public ReplicaSetConnector(
            MongoServer server
        ) {
            this.server = server;
        }
        #endregion

        #region public properties
        public int MaxDocumentSize {
            get { return maxDocumentSize; }
        }

        public int MaxMessageLength {
            get { return maxMessageLength; }
        }

        public MongoConnection PrimaryConnection {
            get { return primaryConnection; }
        }

        public List<MongoConnection> SecondaryConnections {
            get { return secondaryConnections; }
        }

        public IEnumerable<MongoServerAddress> ReplicaSet {
            get { return replicaSet; }
        }
        #endregion

        #region public methods
        public void Connect(
            TimeSpan timeout
        ) {
            // query all servers in seed list in parallel (they will report responses back through the responsesQueue)
            var responsesQueue = QueueSeedListQueries();

            // process the responses as they come back and stop as soon as we find the primary (unless SlaveOk is true)
            // stragglers will continue to report responses to the responsesQueue but no one will read them
            // and eventually it will all get garbage collected

            var exceptions = new List<Exception>();
            var timeoutAt = DateTime.UtcNow + timeout;
            while (responses.Count < queries.Count) {
                var timeRemaining = timeoutAt - DateTime.UtcNow;
                var response = responsesQueue.Dequeue(timeRemaining);
                if (response == null) {
                    break; // we timed out
                }
                responses.Add(response.Address, response);

                if (response.Exception != null) {
                    exceptions.Add(response.Exception);
                    continue;
                }

                if (response.IsPrimary) {
                    primaryConnection = response.Connection;
                    replicaSet = GetHostAddresses(response);
                    maxDocumentSize = response.MaxDocumentSize;
                    maxMessageLength = response.MaxMessageLength;
                    if (!server.Settings.SlaveOk) {
                        break; // if we're not going to use the secondaries no need to wait for their replies
                    }
                } else {
                    if (server.Settings.SlaveOk) {
                        secondaryConnections.Add(response.Connection);
                    } else {
                        response.Connection.Close();
                    }
                }

                // look for additional members of the replica set that might not have been in the seed list and query them also
                foreach (var address in GetHostAddresses(response)) {
                    if (!queries.Contains(address)) {
                        var args = new QueryNodeParameters {
                            Address = address,
                            EndPoint = address.ToIPEndPoint(server.Settings.AddressFamily),
                            ResponseQueue = responsesQueue
                        };
                        ThreadPool.QueueUserWorkItem(QueryNodeWorkItem, args);
                        queries.Add(address);
                    }
                }
            }

            if (primaryConnection == null) {
                var innerException = exceptions.FirstOrDefault();
                var exception = new MongoConnectionException("Unable to connect to server", innerException);
                if (exceptions.Count > 1) {
                    exception.Data.Add("InnerExceptions", exceptions);
                }
                throw exception;
            }
        }
        #endregion

        #region private methods
        private List<MongoServerAddress> GetHostAddresses(
            QueryNodeResponse response
        ) {
            if (!response.IsMasterResult.Response.Contains("hosts")) {
                var message = string.Format("Server is not a member of a replica set: {0}", response.Address);
                throw new MongoConnectionException(message);
            }

            var nodes = new List<MongoServerAddress>();
            foreach (BsonString host in response.IsMasterResult.Response["hosts"].AsBsonArray.Values) {
                var address = MongoServerAddress.Parse(host.Value);
                nodes.Add(address);
            }
            return nodes;
        }

        private BlockingQueue<QueryNodeResponse> QueueSeedListQueries() {
            var responseQueue = new BlockingQueue<QueryNodeResponse>();
            var addresses = server.Settings.Servers.ToList();
            var endPoints = server.EndPoints.ToList();
            for (int i = 0; i < addresses.Count; i++) {
                var args = new QueryNodeParameters {
                    Address = addresses[i],
                    EndPoint = endPoints[i],
                    ResponseQueue = responseQueue
                };
                ThreadPool.QueueUserWorkItem(QueryNodeWorkItem, args);
                queries.Add(addresses[i]);
            }
            return responseQueue;
        }

        // note: this method will run on a thread from the ThreadPool
        private void QueryNodeWorkItem(
            object parameters
        ) {
            // this method has to work at a very low level because the connection pool isn't set up yet
            var args = (QueryNodeParameters) parameters;
            var response = new QueryNodeResponse { Address = args.Address, EndPoint = args.EndPoint };

            try {
                var connection = new MongoConnection(null, args.EndPoint); // no connection pool
                try {
                    var isMasterCommand = new CommandDocument("ismaster", 1);
                    var isMasterResult = connection.RunCommand(server, "admin.$cmd", QueryFlags.SlaveOk, isMasterCommand);

                    response.IsMasterResult = isMasterResult;
                    response.Connection = connection; // might become the first connection in the connection pool
                    response.IsPrimary = isMasterResult.Response["ismaster", false].ToBoolean();
                    response.MaxDocumentSize = isMasterResult.Response["maxBsonObjectSize", server.MaxDocumentSize].ToInt32();
                    response.MaxMessageLength = Math.Max(MongoDefaults.MaxMessageLength, response.MaxDocumentSize + 1024); // derived from maxDocumentSize

                    if (server.Settings.ReplicaSetName != null) {
                        var getStatusCommand = new CommandDocument("replSetGetStatus", 1);
                        var getStatusResult = connection.RunCommand(server, "admin.$cmd", QueryFlags.SlaveOk, getStatusCommand);

                        var replicaSetName = getStatusResult.Response["set"].AsString;
                        if (replicaSetName != server.Settings.ReplicaSetName) {
                            var message = string.Format("Host {0} belongs to a different replica set: {1}", args.EndPoint, replicaSetName);
                            throw new MongoConnectionException(message);
                        }
                    }
                } catch {
                    try { connection.Close(); } catch { } // ignore exceptions
                    throw;
                }
            } catch (Exception ex) {
                response.Exception = ex;
            }

            args.ResponseQueue.Enqueue(response);
        }
        #endregion

        #region private nested classes
        // note: OK to use automatic properties on private helper class
        private class QueryNodeParameters {
            public MongoServerAddress Address { get; set; }
            public IPEndPoint EndPoint { get; set; }
            public BlockingQueue<QueryNodeResponse> ResponseQueue { get; set; }
        }

        // note: OK to use automatic properties on private helper class
        private class QueryNodeResponse {
            public MongoServerAddress Address { get; set; }
            public IPEndPoint EndPoint { get; set; }
            public CommandResult IsMasterResult { get; set; }
            public bool IsPrimary { get; set; }
            public int MaxDocumentSize { get; set; }
            public int MaxMessageLength { get; set; }
            public MongoConnection Connection { get; set; }
            public Exception Exception { get; set; }
        }
        #endregion
    }
}

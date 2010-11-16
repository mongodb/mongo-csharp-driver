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
using System.Linq;
using System.Text;
using System.Threading;

using MongoDB.Bson;

namespace MongoDB.Driver.Internal {
    internal class ReplicaSetConnector {
        #region private fields
        private MongoUrl url;
        private HashSet<MongoServerAddress> queries = new HashSet<MongoServerAddress>();
        private Dictionary<MongoServerAddress, QueryNodeResponse> responses = new Dictionary<MongoServerAddress, QueryNodeResponse>();
        private MongoConnection primaryConnection;
        private List<MongoConnection> secondaryConnections = new List<MongoConnection>();
        private List<MongoServerAddress> replicaSet;
        #endregion

        #region constructors
        public ReplicaSetConnector(
            MongoUrl url
        ) {
            this.url = url;
        }
        #endregion

        #region public properties
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
            DateTime deadline = DateTime.UtcNow + timeout;

            // query all servers in seed list in parallel (they will report responses back through the responsesQueue)
            var responsesQueue = QuerySeedListNodes();

            // process the responses as they come back and stop as soon as we find the primary (unless slaveOk is true)
            // stragglers will continue to report responses to the responsesQueue but no one will read them
            // and eventually it will all get garbage collected

            var exceptions = new List<Exception>();
            while (responses.Count < queries.Count) {
                var response = responsesQueue.Dequeue(deadline);
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
                    if (!url.SlaveOk) {
                        break; // if we're not going to use the secondaries no need to wait for their replies
                    }
                } else {
                    if (url.SlaveOk) {
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
            if (!response.CommandResult.Contains("hosts")) {
                var message = string.Format("Server is not a member of a replica set: {0}", response.Address);
                throw new MongoConnectionException(message);
            }
            if (url.ReplicaSetName != null) {
                // TODO: check replica set name
            }

            var nodes = new List<MongoServerAddress>();
            foreach (BsonString host in response.CommandResult["hosts"].AsBsonArray.Values) {
                var address = MongoServerAddress.Parse(host.Value);
                nodes.Add(address);
            }
            return nodes;
        }

        private BlockingQueue<QueryNodeResponse> QuerySeedListNodes() {
            var responseQueue = new BlockingQueue<QueryNodeResponse>();
            foreach (var address in url.Servers) {
                var args = new QueryNodeParameters {
                    Address = address,
                    ResponseQueue = responseQueue
                };
                ThreadPool.QueueUserWorkItem(QueryNodeWorkItem, args);
                queries.Add(address);
            }
            return responseQueue;
        }

        // note: this method will run on a thread from the ThreadPool
        private void QueryNodeWorkItem(
            object parameters
        ) {
            // this method has to work at a very low level because the connection pool isn't set up yet
            var args = (QueryNodeParameters) parameters;
            var response = new QueryNodeResponse { Address = args.Address };

            try {
                var connection = new MongoConnection(null, args.Address); // no connection pool
                try {
                    var command = new BsonDocument("ismaster", 1);
                    using (
                        var message = new MongoQueryMessage<BsonDocument>(
                            "admin.$cmd",
                            QueryFlags.SlaveOk,
                            0, // numberToSkip
                            1, // numberToReturn
                            command,
                            null // fields
                        )
                    ) {
                        connection.SendMessage(message, SafeMode.False);
                    }
                    var reply = connection.ReceiveMessage<BsonDocument>();
                    response.CommandResult = reply.Documents[0];
                    response.Connection = connection; // might become the first connection in the connection pool
                    response.IsPrimary =
                        response.CommandResult["ok", false].ToBoolean() &&
                        response.CommandResult["ismaster", false].ToBoolean();
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
            public BlockingQueue<QueryNodeResponse> ResponseQueue { get; set; }
        }

        // note: OK to use automatic properties on private helper class
        private class QueryNodeResponse {
            public MongoServerAddress Address { get; set; }
            public BsonDocument CommandResult { get; set; }
            public bool IsPrimary { get; set; }
            public MongoConnection Connection { get; set; }
            public Exception Exception { get; set; }
        }
        #endregion
    }
}

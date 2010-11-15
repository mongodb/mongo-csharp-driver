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
        private Dictionary<MongoServerAddress, QueryNodeResults> responses = new Dictionary<MongoServerAddress,QueryNodeResults>();
        private MongoServerAddress primary;
        private MongoConnection primaryConnection;
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
        public MongoServerAddress Primary {
            get { return primary; }
        }

        public MongoConnection PrimaryConnection {
            get { return primaryConnection; }
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

            // query all servers in seed list in parallel (they will report results back through the resultsQueue)
            var resultsQueue = QuerySeedListNodes();

            // process the results as they come back and stop as soon as we find the primary
            // stragglers will continue to report results to the resultsQueue but no one will read them
            // and eventually it will all get garbage collected

            QueryNodeResults results = null;
            var exceptions = new List<Exception>();
            while (responses.Count < queries.Count && (results = resultsQueue.Dequeue(deadline)) != null) {
                responses.Add(results.Address, results);

                if (results.Exception != null) {
                    exceptions.Add(results.Exception);
                    continue;
                }

                var commandResult = results.CommandResult;
                if (results.IsPrimary || url.SlaveOk) {
                    primary = results.Address;
                    break;
                } else {
                    results.Connection.Close();
                }

                // look for additional members of the replica set that might not have been in the seed list and query them also
                if (commandResult.Contains("hosts")) {
                    foreach (BsonString host in commandResult["hosts"].AsBsonArray.Values) {
                        var address = MongoServerAddress.Parse(host.Value);
                        if (!queries.Contains(address)) {
                            var args = new QueryNodeParameters {
                                Address = address,
                                ResultsQueue = resultsQueue
                            };
                            ThreadPool.QueueUserWorkItem(QueryNodeWorkItem, args);
                            queries.Add(address);
                        }
                    }
                }
            }

            if (primary == null) {
                var innerException = exceptions.FirstOrDefault();
                var exception = new MongoConnectionException("Unable to connect to server", innerException);
                if (exceptions.Count > 1) {
                    exception.Data.Add("InnerExceptions", exceptions);
                }
                throw exception;
            }

            primaryConnection = results.Connection;
            replicaSet = null;
            if (results.CommandResult.Contains("hosts")) {
                replicaSet = new List<MongoServerAddress>();
                foreach (BsonString host in results.CommandResult["hosts"].AsBsonArray.Values) {
                    // don't let errors parsing the address prevent us from connecting
                    // the replicaSet just won't reflect any replicas with addresses we couldn't parse
                    MongoServerAddress address;
                    if (MongoServerAddress.TryParse(host.Value, out address)) {
                        replicaSet.Add(address);
                    }
                }
            }
        }
        #endregion

        #region private methods
        private BlockingQueue<QueryNodeResults> QuerySeedListNodes() {
            var resultsQueue = new BlockingQueue<QueryNodeResults>();
            foreach (var address in url.Servers) {
                var args = new QueryNodeParameters {
                    Address = address,
                    ResultsQueue = resultsQueue
                };
                ThreadPool.QueueUserWorkItem(QueryNodeWorkItem, args);
                queries.Add(address);
            }
            return resultsQueue;
        }

        // note: this method will run on a thread from the ThreadPool
        private void QueryNodeWorkItem(
            object parameters
        ) {
            // this method has to work at a very low level because the connection pool isn't set up yet
            var args = (QueryNodeParameters) parameters;
            var results = new QueryNodeResults { Address = args.Address };

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
                    results.CommandResult = reply.Documents[0];
                    results.Connection = connection; // might become the first connection in the connection pool
                    results.IsPrimary =
                        results.CommandResult["ok", false].ToBoolean() &&
                        results.CommandResult["ismaster", false].ToBoolean();
                } catch {
                    try { connection.Close(); } catch { } // ignore exceptions
                    throw;
                }
            } catch (Exception ex) {
                results.Exception = ex;
            }

            args.ResultsQueue.Enqueue(results);
        }
        #endregion

        #region private nested classes
        // note: OK to use automatic properties on private helper class
        private class QueryNodeParameters {
            public MongoServerAddress Address { get; set; }
            public BlockingQueue<QueryNodeResults> ResultsQueue { get; set; }
        }

        // note: OK to use automatic properties on private helper class
        private class QueryNodeResults {
            public MongoServerAddress Address { get; set; }
            public BsonDocument CommandResult { get; set; }
            public bool IsPrimary { get; set; }
            public MongoConnection Connection { get; set; }
            public Exception Exception { get; set; }
        }
        #endregion
    }
}

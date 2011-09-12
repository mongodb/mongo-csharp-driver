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
        private int connectionAttempt;
        private DateTime timeoutAt;
        private BlockingQueue<ConnectResponse> responseQueue = new BlockingQueue<ConnectResponse>();
        private List<ConnectArgs> connects = new List<ConnectArgs>();
        private List<ConnectResponse> responses = new List<ConnectResponse>();
        private bool firstResponseHasBeenProcessed;
        #endregion

        #region constructors
        internal ReplicaSetConnector(
            MongoServer server,
            int connectionAttempt
        ) {
            this.server = server;
            this.connectionAttempt = connectionAttempt;
        }
        #endregion

        #region internal methods
        internal void Connect(
            TimeSpan timeout,
            ConnectWaitFor waitFor
        ) {
            timeoutAt = DateTime.UtcNow + timeout;

            // connect to all server instances in parallel (they will report responses back through the responseQueue)
            // the set of Instances initially comes from the seed list, but is adjusted to the official set once connected
            foreach (var serverInstance in server.Instances) {
                QueueConnect(serverInstance);
            }

            // process the responses as they come back and return as soon as we have connected to the primary
            // any remaining responses after the primary will be processed in the background

            while (responses.Count < connects.Count) {
                var timeRemaining = timeoutAt - DateTime.UtcNow;
                var response = responseQueue.Dequeue(timeRemaining);
                if (response == null) {
                    break; // we timed out
                }

                ProcessResponse(response);

                // return as soon as we can (according to the waitFor mode specified)
                bool exitEarly = false;
                switch (waitFor) {
                    case ConnectWaitFor.All:
                        if (server.Instances.All(i => i.State == MongoServerState.Connected)) {
                            exitEarly = true;
                        }
                        break;
                    case ConnectWaitFor.AnySlaveOk:
                        if (server.Instances.Any(i => (i.IsPrimary || i.IsSecondary || i.IsPassive) && i.State == MongoServerState.Connected)) {
                            exitEarly = true;
                        }
                        break;
                   case ConnectWaitFor.Primary:
                        var primary = server.Primary;
                        if (primary != null && primary.State == MongoServerState.Connected) {
                            exitEarly = true;
                        }
                        break;
                    default:
                        throw new ArgumentException("Invalid ConnectWaitFor value.");
                }

                if (exitEarly) {
                    if (responses.Count < connects.Count) {
                        // process any additional responses in the background
                        ThreadPool.QueueUserWorkItem(ProcessAdditionalResponsesWorkItem);
                    }
                    return;
                }
            }

            string waitForString;
            switch (waitFor) {
                case ConnectWaitFor.All: waitForString = "all members"; break;
                case ConnectWaitFor.AnySlaveOk: waitForString = "any slaveOk member"; break;
                case ConnectWaitFor.Primary: waitForString = "the primary member"; break;
                default: throw new ArgumentException("Invalid ConnectWaitFor value.");
            }

            var exceptions = responses.Select(r => r.ServerInstance.ConnectException).Where(e => e != null).ToArray();
            var firstException = exceptions.FirstOrDefault();
            string message;
            if (firstException == null) {
                message = string.Format("Unable to connect to {0} of the replica set.", waitForString);
            } else {
                message = string.Format("Unable to connect to {0} of the replica set: {1}.", waitForString, firstException.Message);
            }
            var connectionException = new MongoConnectionException(message, firstException);
            connectionException.Data.Add("InnerExceptions", exceptions); // useful when there is more than one
            throw connectionException;
        }
        #endregion

        #region private methods
        // note: this method will run on a thread from the ThreadPool
        private void ConnectWorkItem(
            object argsObject
        ) {
            var args = (ConnectArgs) argsObject;
            var serverInstance = args.ServerInstance;

            var response = new ConnectResponse { ServerInstance = serverInstance };
            try {
                serverInstance.Connect(true); // slaveOk
                response.IsMasterResult = serverInstance.IsMasterResult;
            } catch (Exception ex) {
                response.Exception = ex;
            }

            args.ResponseQueue.Enqueue(response);
        }

        private void ProcessAdditionalResponse(
            ConnectResponse response
        ) {
            var replicaSetName = response.IsMasterResult.Response["setName"].AsString;
            if (replicaSetName != server.ReplicaSetName) {
                var message = string.Format(
                    "Server at address '{0}' is a member of replica set '{1}' and not '{2}'.",
                    response.ServerInstance.Address,
                    replicaSetName, 
                    server.ReplicaSetName // additional responses have to be for the same replica set name as the first response
                );
                throw new MongoConnectionException(message);
            }
        }

        private void ProcessAdditionalResponsesWorkItem(
            object args
        ) {
            while (responses.Count < connects.Count) {
                var timeRemaining = timeoutAt - DateTime.UtcNow;
                var response = responseQueue.Dequeue(timeRemaining);
                if (response == null) {
                    break; // we timed out
                }

                ProcessResponse(response);
            }
        }

        private void ProcessFirstResponse(
            ConnectResponse response
        ) {
            var isMasterResponse = response.IsMasterResult.Response;

            var replicaSetName = isMasterResponse["setName"].AsString;
            if (server.Settings.ReplicaSetName != null && replicaSetName != server.Settings.ReplicaSetName) {
                var message = string.Format(
                    "Server at address '{0}' is a member of replica set '{1}' and not '{2}'.",
                    response.ServerInstance.Address,
                    replicaSetName,
                    server.Settings.ReplicaSetName // first response has to match replica set name in settings (if any)
                );
                throw new MongoConnectionException(message);
            }
            server.ReplicaSetName = replicaSetName;

            // find all valid addresses
            var validAddresses = new HashSet<MongoServerAddress>();
            if (isMasterResponse.Contains("hosts")) {
                foreach (string address in isMasterResponse["hosts"].AsBsonArray) {
                    validAddresses.Add(MongoServerAddress.Parse(address));
                }
            }
            if (isMasterResponse.Contains("passives")) {
                foreach (string address in isMasterResponse["passives"].AsBsonArray) {
                    validAddresses.Add(MongoServerAddress.Parse(address));
                }
            }
            if (isMasterResponse.Contains("arbiters")) {
                foreach (string address in isMasterResponse["arbiters"].AsBsonArray) {
                    validAddresses.Add(MongoServerAddress.Parse(address));
                }
            }

            // remove server instances created from the seed list that turn out to be invalid
            var invalidInstances = server.Instances.Where(i => !validAddresses.Contains(i.Address)).ToArray(); // force evaluation
            foreach (var invalidInstance in invalidInstances) {
                server.RemoveInstance(invalidInstance);
            }

            // add any server instances that were missing from the seed list
            foreach (var address in validAddresses) {
                if (!server.Instances.Any(i => i.Address == address)) {
                    var missingInstance = new MongoServerInstance(server, address);
                    server.AddInstance(missingInstance);
                    QueueConnect(missingInstance);
                }
            }
        }

        private void ProcessResponse(
            ConnectResponse response
        ) {
            responses.Add(response);

            // don't process response if it threw an exception
            if (response.Exception != null) {
                return;
            }

            // don't process response if Disconnect was called before the response was received
            if (server.State == MongoServerState.Disconnected || server.State == MongoServerState.Disconnecting) {
                return;
            }

            // don't process response if it was for a previous connection attempt
            if (connectionAttempt != server.ConnectionAttempt) {
                return;
            }

            try {
                if (!response.IsMasterResult.Response.Contains("setName")) {
                    var message = string.Format("Server at address '{0}' does not have a replica set name.", response.ServerInstance.Address);
                    throw new MongoConnectionException(message);
                }
                if (!response.IsMasterResult.Response.Contains("hosts")) {
                    var message = string.Format("Server at address '{0}' does not have a hosts list.", response.ServerInstance.Address);
                    throw new MongoConnectionException(message);
                }

                if (firstResponseHasBeenProcessed) {
                    ProcessAdditionalResponse(response);
                } else {
                    ProcessFirstResponse(response);
                    firstResponseHasBeenProcessed = true; // remains false if ProcessFirstResponse throws an exception
                }
            } catch (Exception ex) {
                response.ServerInstance.ConnectException = ex;
                try { response.ServerInstance.Disconnect(); } catch { } // ignore exceptions
            }
        }

        private void QueueConnect(
            MongoServerInstance serverInstance
        ) {
            var args = new ConnectArgs {
                ServerInstance = serverInstance,
                ResponseQueue = responseQueue
            };
            ThreadPool.QueueUserWorkItem(ConnectWorkItem, args);
            connects.Add(args);
        }
        #endregion

        #region private nested classes
        // note: OK to use automatic properties on private helper class
        private class ConnectArgs {
            public MongoServerInstance ServerInstance { get; set; }
            public BlockingQueue<ConnectResponse> ResponseQueue { get; set; }
        }

        // note: OK to use automatic properties on private helper class
        private class ConnectResponse {
            public MongoServerInstance ServerInstance { get; set; }
            public CommandResult IsMasterResult { get; set; }
            public Exception Exception { get; set; }
        }
        #endregion
    }
}

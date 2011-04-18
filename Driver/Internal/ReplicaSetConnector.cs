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
        private DateTime timeoutAt;
        private BlockingQueue<ConnectResponse> responseQueue;
        private List<ConnectArgs> connects;
        private List<ConnectResponse> responses;
        #endregion

        #region constructors
        internal ReplicaSetConnector(
            MongoServer server
        ) {
            this.server = server;
        }
        #endregion

        #region internal methods
        internal void Connect(
            TimeSpan timeout
        ) {
            timeoutAt = DateTime.UtcNow + timeout;
            responseQueue = new BlockingQueue<ConnectResponse>();
            connects = new List<ConnectArgs>();
            responses = new List<ConnectResponse>();

            // connect to all servers in the seed list in parallel (they will report responses back through the responseQueue)
            server.ClearInstances();
            foreach (var address in server.Settings.Servers) {
                QueueConnect(address);
            }

            // process the responses as they come back and stop as soon as we find the primary (unless SlaveOk is true)
            // stragglers will continue to report responses to the responseQueue but no one will read them
            // and eventually it will all get garbage collected

            var exceptions = new List<Exception>();
            while (responses.Count < connects.Count) {
                var timeRemaining = timeoutAt - DateTime.UtcNow;
                var response = responseQueue.Dequeue(timeRemaining);
                if (response == null) {
                    break; // we timed out
                }

                responses.Add(response);
                if (response.Exception != null) {
                    exceptions.Add(response.Exception);
                    continue;
                }

                if (responses.Count == 1) {
                    ProcessFirstResponse(response);
                } else {
                    ProcessAdditionalResponse(response);
                }

                // return as soon as we've found the primary
                var serverInstance = response.ServerInstance;
                if (serverInstance.IsPrimary) {
                    // process any additional responses in the background
                    ThreadPool.QueueUserWorkItem(ProcessAdditionalResponsesWorkItem);
                    return;
                }
            }

            var innerException = exceptions.FirstOrDefault();
            var exception = new MongoConnectionException("Unable to connect to server", innerException);
            if (exceptions.Count > 1) {
                exception.Data.Add("InnerExceptions", exceptions);
            }
            throw exception;
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
            // is there anything to do here?
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
                responses.Add(response);

                ProcessAdditionalResponse(response);
            }
        }

        private void ProcessFirstResponse(
            ConnectResponse response
        ) {
            var isMasterResponse = response.IsMasterResult.Response;

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
            foreach (var instance in invalidInstances) {
                server.RemoveInstance(instance);
            }

            // add any server instances that were missing from the seed list
            foreach (var address in validAddresses) {
                if (!server.Instances.Any(i => i.Address == address)) {
                    QueueConnect(address);
                }
            }
        }

        private void QueueConnect(
            MongoServerAddress address
        ) {
            var serverInstance = new MongoServerInstance(server, address);
            server.AddInstance(serverInstance);

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

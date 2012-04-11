/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Driver.Internal
{
    internal class ReplicaSetConnector
    {
        // private fields
        private MongoServer _server;
        private int _connectionAttempt;
        private DateTime _timeoutAt;
        private BlockingQueue<ConnectResponse> _responseQueue = new BlockingQueue<ConnectResponse>();
        private List<ConnectArgs> _connects = new List<ConnectArgs>();
        private List<ConnectResponse> _responses = new List<ConnectResponse>();
        private bool _firstResponseHasBeenProcessed;

        // constructors
        internal ReplicaSetConnector(MongoServer server, int connectionAttempt)
        {
            _server = server;
            _connectionAttempt = connectionAttempt;
        }

        // internal methods
        internal void Connect(TimeSpan timeout, ConnectWaitFor waitFor)
        {
            _timeoutAt = DateTime.UtcNow + timeout;

            // connect to all server instances in parallel (they will report responses back through the responseQueue)
            // the set of Instances initially comes from the seed list, but is adjusted to the official set once connected
            foreach (var serverInstance in _server.Instances)
            {
                QueueConnect(serverInstance);
            }

            // process the responses as they come back and return as soon as we have connected to the primary
            // any remaining responses after the primary will be processed in the background

            while (_responses.Count < _connects.Count)
            {
                var timeRemaining = _timeoutAt - DateTime.UtcNow;
                var response = _responseQueue.Dequeue(timeRemaining);
                if (response == null)
                {
                    break; // we timed out
                }

                ProcessResponse(response);

                // return as soon as we can (according to the waitFor mode specified)
                bool exitEarly = false;
                switch (waitFor)
                {
                    case ConnectWaitFor.All:
                        if (_server.Instances.All(i => i.State == MongoServerState.Connected))
                        {
                            exitEarly = true;
                        }
                        break;
                    case ConnectWaitFor.AnySlaveOk:
                        // don't check for IsPassive because IsSecondary is also true for passives (and only true if not in recovery mode)
                        if (_server.Instances.Any(i => (i.IsPrimary || i.IsSecondary) && i.State == MongoServerState.Connected))
                        {
                            exitEarly = true;
                        }
                        break;
                    case ConnectWaitFor.Primary:
                        var primary = _server.Primary;
                        if (primary != null && primary.State == MongoServerState.Connected)
                        {
                            exitEarly = true;
                        }
                        break;
                    default:
                        throw new ArgumentException("Invalid ConnectWaitFor value.");
                }

                if (exitEarly)
                {
                    if (_responses.Count < _connects.Count)
                    {
                        // process any additional responses in the background
                        ThreadPool.QueueUserWorkItem(ProcessAdditionalResponsesWorkItem);
                    }
                    return;
                }
            }

            string waitForString;
            switch (waitFor)
            {
                case ConnectWaitFor.All: waitForString = "all members"; break;
                case ConnectWaitFor.AnySlaveOk: waitForString = "any slaveOk member"; break;
                case ConnectWaitFor.Primary: waitForString = "the primary member"; break;
                default: throw new ArgumentException("Invalid ConnectWaitFor value.");
            }

            var exceptions = _responses.Select(r => r.ServerInstance.ConnectException).Where(e => e != null).ToArray();
            var firstException = exceptions.FirstOrDefault();
            string message;
            if (firstException == null)
            {
                message = string.Format("Unable to connect to {0} of the replica set.", waitForString);
            }
            else
            {
                message = string.Format("Unable to connect to {0} of the replica set: {1}.", waitForString, firstException.Message);
            }
            var connectionException = new MongoConnectionException(message, firstException);
            connectionException.Data.Add("InnerExceptions", exceptions); // useful when there is more than one
            throw connectionException;
        }

        // private methods
        // note: this method will run on a thread from the ThreadPool
        private void ConnectWorkItem(object argsObject)
        {
            var args = (ConnectArgs)argsObject;
            var serverInstance = args.ServerInstance;

            var response = new ConnectResponse { ServerInstance = serverInstance };
            try
            {
                serverInstance.Connect(true); // slaveOk
                response.IsMasterResult = serverInstance.IsMasterResult;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
            }

            args.ResponseQueue.Enqueue(response);
        }

        private void ProcessAdditionalResponse(ConnectResponse response)
        {
            // additional responses have to be for the same replica set name as the first response
            var replicaSetName = response.IsMasterResult.Response["setName"].AsString;
            if (replicaSetName != _server.ReplicaSetName)
            {
                var message = string.Format(
                    "Server at address '{0}' is a member of replica set '{1}' and not '{2}'.",
                    response.ServerInstance.Address, replicaSetName, _server.ReplicaSetName);
                throw new MongoConnectionException(message);
            }
        }

        private void ProcessAdditionalResponsesWorkItem(object args)
        {
            while (_responses.Count < _connects.Count)
            {
                var timeRemaining = _timeoutAt - DateTime.UtcNow;
                var response = _responseQueue.Dequeue(timeRemaining);
                if (response == null)
                {
                    break; // we timed out
                }

                ProcessResponse(response);
            }
        }

        private void ProcessFirstResponse(ConnectResponse response)
        {
            var isMasterResponse = response.IsMasterResult.Response;

            // first response has to match replica set name in settings (if any)
            var replicaSetName = isMasterResponse["setName"].AsString;
            if (_server.Settings.ReplicaSetName != null && replicaSetName != _server.Settings.ReplicaSetName)
            {
                var message = string.Format(
                    "Server at address '{0}' is a member of replica set '{1}' and not '{2}'.",
                    response.ServerInstance.Address, replicaSetName, _server.Settings.ReplicaSetName);
                throw new MongoConnectionException(message);
            }
            _server.ReplicaSetName = replicaSetName;

            // find all valid addresses
            var validAddresses = new HashSet<MongoServerAddress>();
            if (isMasterResponse.Contains("hosts"))
            {
                foreach (string address in isMasterResponse["hosts"].AsBsonArray)
                {
                    validAddresses.Add(MongoServerAddress.Parse(address));
                }
            }
            if (isMasterResponse.Contains("passives"))
            {
                foreach (string address in isMasterResponse["passives"].AsBsonArray)
                {
                    validAddresses.Add(MongoServerAddress.Parse(address));
                }
            }
            if (isMasterResponse.Contains("arbiters"))
            {
                foreach (string address in isMasterResponse["arbiters"].AsBsonArray)
                {
                    validAddresses.Add(MongoServerAddress.Parse(address));
                }
            }

            // remove server instances created from the seed list that turn out to be invalid
            var invalidInstances = _server.Instances.Where(i => !validAddresses.Contains(i.Address)).ToArray(); // force evaluation
            foreach (var invalidInstance in invalidInstances)
            {
                _server.RemoveInstance(invalidInstance);
            }

            // add any server instances that were missing from the seed list
            foreach (var address in validAddresses)
            {
                if (!_server.Instances.Any(i => i.Address == address))
                {
                    var missingInstance = new MongoServerInstance(_server, address);
                    _server.AddInstance(missingInstance);
                    QueueConnect(missingInstance);
                }
            }
        }

        private void ProcessResponse(ConnectResponse response)
        {
            _responses.Add(response);

            // don't process response if it threw an exception
            if (response.Exception != null)
            {
                return;
            }

            // don't process response if Disconnect was called before the response was received
            if (_server.State == MongoServerState.Disconnected || _server.State == MongoServerState.Disconnecting)
            {
                return;
            }

            // don't process response if it was for a previous connection attempt
            if (_connectionAttempt != _server.ConnectionAttempt)
            {
                return;
            }

            try
            {
                if (!response.IsMasterResult.Response.Contains("setName"))
                {
                    var message = string.Format("Server at address '{0}' does not have a replica set name.", response.ServerInstance.Address);
                    throw new MongoConnectionException(message);
                }
                if (!response.IsMasterResult.Response.Contains("hosts"))
                {
                    var message = string.Format("Server at address '{0}' does not have a hosts list.", response.ServerInstance.Address);
                    throw new MongoConnectionException(message);
                }

                if (_firstResponseHasBeenProcessed)
                {
                    ProcessAdditionalResponse(response);
                }
                else
                {
                    ProcessFirstResponse(response);
                    _firstResponseHasBeenProcessed = true; // remains false if ProcessFirstResponse throws an exception
                }
            }
            catch (Exception ex)
            {
                response.ServerInstance.ConnectException = ex;
                try { response.ServerInstance.Disconnect(); }
                catch { } // ignore exceptions
            }
        }

        private void QueueConnect(MongoServerInstance serverInstance)
        {
            var args = new ConnectArgs
            {
                ServerInstance = serverInstance,
                ResponseQueue = _responseQueue
            };
            ThreadPool.QueueUserWorkItem(ConnectWorkItem, args);
            _connects.Add(args);
        }

        // private nested classes
        // note: OK to use automatic properties on private helper class
        private class ConnectArgs
        {
            public MongoServerInstance ServerInstance { get; set; }
            public BlockingQueue<ConnectResponse> ResponseQueue { get; set; }
        }

        // note: OK to use automatic properties on private helper class
        private class ConnectResponse
        {
            public MongoServerInstance ServerInstance { get; set; }
            public CommandResult IsMasterResult { get; set; }
            public Exception Exception { get; set; }
        }
    }
}

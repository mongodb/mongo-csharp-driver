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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Proxy for connecting to a replica set.
    /// </summary>
    internal sealed class ReplicaSetMongoServerProxy : MultipleInstanceMongoServerProxy
    {
        // private fields
        private string _replicaSetName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicaSetMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public ReplicaSetMongoServerProxy(MongoServer server)
            : base(server)
        {
            _replicaSetName = server.Settings.ReplicaSetName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicaSetMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="instances">The instances.</param>
        /// <param name="stateChangeQueue">The state change queue.</param>
        /// <param name="connectionAttempt">The connection attempt.</param>
        public ReplicaSetMongoServerProxy(MongoServer server, IEnumerable<MongoServerInstance> instances, BlockingQueue<MongoServerInstance> stateChangeQueue, int connectionAttempt)
            : base(server, instances, stateChangeQueue, connectionAttempt)
        { }

        // public properties
        /// <summary>
        /// Gets the name of the replica set.
        /// </summary>
        /// <value>
        /// The name of the replica set.
        /// </value>
        public string ReplicaSetName
        {
            get
            {
                // read _replicaSetName in a thread-safe way
                return Interlocked.CompareExchange(ref _replicaSetName, null, null);
            }
        }

        // protected methods
        protected override MongoServerInstance ChooseServerInstance(ConnectedInstanceCollection connectedInstances, ReadPreference readPreference)
        {
            return connectedInstances.ChooseServerInstance(readPreference);
        }

        /// <summary>
        /// Determines the state of the server.
        /// </summary>
        /// <param name="currentState">State of the current.</param>
        /// <param name="instances">The instances.</param>
        /// <returns>The server state.</returns>
        protected override MongoServerState DetermineServerState(MongoServerState currentState, IEnumerable<MongoServerInstance> instances)
        {
            if (!instances.Any())
            {
                return MongoServerState.Disconnected;
            }

            // the order of the tests is significant
            // and resolves ambiguities when more than one state might match
            if (currentState == MongoServerState.Disconnecting)
            {
                if (instances.All(i => i.State == MongoServerState.Disconnected))
                {
                    return MongoServerState.Disconnected;
                }
            }
            else
            {
                if (instances.All(i => i.State == MongoServerState.Disconnected))
                {
                    return MongoServerState.Disconnected;
                }
                else if (instances.All(i => i.State == MongoServerState.Connected))
                {
                    return MongoServerState.Connected;
                }
                else if (instances.Any(i => i.State == MongoServerState.Connecting))
                {
                    return MongoServerState.Connecting;
                }
                else if (instances.Any(i => i.State == MongoServerState.Unknown))
                {
                    return MongoServerState.Unknown;
                }
                else if (instances.Any(i => i.State == MongoServerState.Connected))
                {
                    return MongoServerState.ConnectedToSubset;
                }

                throw new MongoInternalException("Unexpected server instance states.");
            }

            return currentState;
        }

        /// <summary>
        /// Determines whether the instance is a valid.  If not, the instance is removed.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>
        ///   <c>true</c> if the instance is valid; otherwise, <c>false</c>.
        /// </returns>
        protected override bool IsValidInstance(MongoServerInstance instance)
        {
            if (instance.InstanceType != MongoServerInstanceType.ReplicaSetMember)
            {
                return false;
            }

            // read _replicaSetName in a thread-safe way
            var replicaSetName = Interlocked.CompareExchange(ref _replicaSetName, null, null);

            return replicaSetName == null || replicaSetName == instance.ReplicaSetInformation.Name;
        }

        /// <summary>
        /// Processes the connected instance state change.
        /// </summary>
        /// <param name="instance">The instance.</param>
        protected override void ProcessConnectedInstanceStateChange(MongoServerInstance instance)
        {
            if (instance.IsPrimary)
            {
                ProcessConnectedPrimaryStateChange(instance);
            }
            else
            {
                ProcessConnectedSecondaryStateChange(instance);
            }
        }

        // private methods
        private void ProcessConnectedPrimaryStateChange(MongoServerInstance instance)
        {
            Interlocked.CompareExchange(ref _replicaSetName, instance.ReplicaSetInformation.Name, null);

            var members = instance.ReplicaSetInformation.Members;
            if (members.Any())
            {
                // remove instances the primary doesn't know about and add instances we don't know about
                MakeInstancesMatchAddresses(members);
            }
        }

        private void ProcessConnectedSecondaryStateChange(MongoServerInstance instance)
        {
            // make sure the primary exists in the instance list
            EnsureInstanceWithAddress(instance.ReplicaSetInformation.Primary);
        }
    }
}

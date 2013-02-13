/* Copyright 2010-2013 10gen Inc.
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
using System.Threading;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Proxy for connecting to a replica set.
    /// </summary>
    internal sealed class ReplicaSetMongoServerProxy : MultipleInstanceMongoServerProxy
    {
        // private fields
        private readonly Random _random = new Random();
        private readonly object _randomLock = new object();
        private string _replicaSetName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicaSetMongoServerProxy"/> class.
        /// </summary>
        /// <param name="sequentialId">The sequential id.</param>
        /// <param name="settings">The settings.</param>
        public ReplicaSetMongoServerProxy(int sequentialId, MongoServerProxySettings settings)
            : base(sequentialId, settings)
        {
            _replicaSetName = settings.ReplicaSetName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicaSetMongoServerProxy"/> class.
        /// </summary>
        /// <param name="sequentialId">The sequential id.</param>
        /// <param name="serverSettings">The server settings.</param>
        /// <param name="instances">The instances.</param>
        /// <param name="stateChangeQueue">The state change queue.</param>
        /// <param name="connectionAttempt">The connection attempt.</param>
        public ReplicaSetMongoServerProxy(int sequentialId, MongoServerProxySettings serverSettings, IEnumerable<MongoServerInstance> instances, BlockingQueue<MongoServerInstance> stateChangeQueue, int connectionAttempt)
            : base(sequentialId, serverSettings, instances, stateChangeQueue, connectionAttempt)
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
            switch (readPreference.ReadPreferenceMode)
            {
                case ReadPreferenceMode.Primary:
                    return connectedInstances.GetPrimary();

                case ReadPreferenceMode.PrimaryPreferred:
                    var primary = connectedInstances.GetPrimary();
                    if (primary != null)
                    {
                        return primary;
                    }
                    else
                    {
                        return GetMatchingInstance(connectedInstances.GetSecondaries(), readPreference);
                    }

                case ReadPreferenceMode.Secondary:
                    return GetMatchingInstance(connectedInstances.GetSecondaries(), readPreference);

                case ReadPreferenceMode.SecondaryPreferred:
                    var secondary = GetMatchingInstance(connectedInstances.GetSecondaries(), readPreference);
                    if (secondary != null)
                    {
                        return secondary;
                    }
                    else
                    {
                        return connectedInstances.GetPrimary();
                    }

                case ReadPreferenceMode.Nearest:
                    return GetMatchingInstance(connectedInstances.GetPrimaryAndSecondaries(), readPreference);

                default:
                    throw new MongoInternalException("Invalid ReadPreferenceMode.");
            }
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

            return replicaSetName == null || 
                instance.ReplicaSetInformation.Name == null || 
                replicaSetName == instance.ReplicaSetInformation.Name;
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
        /// <summary>
        /// Gets a randomly selected matching instance.
        /// </summary>
        /// <param name="instancesWithPingTime">A list of instances from which to find a matching instance.</param>
        /// <param name="readPreference">The read preference that must be matched.</param>
        /// <returns>A randomly selected matching instance.</returns>
        private MongoServerInstance GetMatchingInstance(List<ConnectedInstanceCollection.InstanceWithPingTime> instancesWithPingTime, ReadPreference readPreference)
        {
            var tagSets = readPreference.TagSets ?? new ReplicaSetTagSet[] { new ReplicaSetTagSet() };
            foreach (var tagSet in tagSets)
            {
                var matchingInstances = new List<MongoServerInstance>();
                var maxPingTime = TimeSpan.MaxValue;

                foreach (var instanceWithPingTime in instancesWithPingTime)
                {
                    if (instanceWithPingTime.CachedAveragePingTime > maxPingTime)
                    {
                        break; // the rest will exceed maxPingTime also
                    }

                    var instance = instanceWithPingTime.Instance;
                    if (tagSet.MatchesInstance(instance))
                    {
                        matchingInstances.Add(instance);
                        if (maxPingTime == TimeSpan.MaxValue)
                        {
                            maxPingTime = instanceWithPingTime.CachedAveragePingTime + readPreference.SecondaryAcceptableLatency;
                        }
                    }
                }

                // stop looking at tagSets if this one yielded any matching instances
                if (matchingInstances.Count == 1)
                {
                    return matchingInstances[0];
                }
                else if (matchingInstances.Count != 0)
                {
                    lock (_randomLock)
                    {
                        var index = _random.Next(matchingInstances.Count);
                        return matchingInstances[index]; // randomly selected matching instance
                    }
                }
            }

            return null;
        }

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
            var address = instance.ReplicaSetInformation.Primary;
            if (address != null)
            {
                // make sure the primary exists in the instance list
                EnsureInstanceWithAddress(address);
            }
        }
    }
}

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

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Connects to a number of mongos' and distributes load based on ping times.
    /// </summary>
    internal sealed class ShardedMongoServerProxy : MultipleInstanceMongoServerProxy
    {
        // private fields
        private readonly Random _random = new Random();
        private readonly object _randomLock = new object();

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ShardedMongoServerProxy"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public ShardedMongoServerProxy(MongoServerSettings settings)
            : base(settings)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShardedMongoServerProxy"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="instances">The instances.</param>
        /// <param name="stateChangedQueue">The state changed queue.</param>
        /// <param name="connectionAttempt">The connection attempt.</param>
        public ShardedMongoServerProxy(MongoServerSettings settings, IEnumerable<MongoServerInstance> instances, BlockingQueue<MongoServerInstance> stateChangedQueue, int connectionAttempt)
            : base(settings, instances, stateChangedQueue, connectionAttempt)
        { }

        // public properties
        /// <summary>
        /// Gets the type of the proxy.
        /// </summary>
        public override MongoServerProxyType ProxyType
        {
            get { return MongoServerProxyType.Sharded; }
        }

        // protected methods
        /// <summary>
        /// Chooses the server instance.
        /// </summary>
        /// <param name="connectedInstances">The connected instances.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <returns>A MongoServerInstance.</returns>
        protected override MongoServerInstance ChooseServerInstance(ConnectedInstanceCollection connectedInstances, ReadPreference readPreference)
        {
            var instancesWithPingTime = connectedInstances.GetAllInstances();
            if (instancesWithPingTime.Count == 0)
            {
                return null;
            }
            else if (instancesWithPingTime.Count == 1)
            {
                return instancesWithPingTime[0].Instance;
            }
            else
            {
                var secondaryAcceptableLatency = Settings.SecondaryAcceptableLatency;
                var minPingTime = instancesWithPingTime[0].CachedAveragePingTime;
                var maxPingTime = minPingTime + secondaryAcceptableLatency;
                var n = instancesWithPingTime.Count(i => i.CachedAveragePingTime <= maxPingTime);
                lock (_randomLock)
                {
                    var index = _random.Next(n);
                    return instancesWithPingTime[index].Instance; // return random instance
                }
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
            if (currentState == MongoServerState.Disconnected)
            {
                return MongoServerState.Disconnected;
            }
            else if (currentState == MongoServerState.Disconnecting)
            {
                if (instances.All(i => i.State == MongoServerState.Disconnected))
                {
                    return MongoServerState.Disconnected;
                }

                return MongoServerState.Disconnecting;
            }
            else
            {
                if (instances.Any(i => i.State == MongoServerState.Connected))
                {
                    return MongoServerState.Connected;
                }

                return MongoServerState.Connecting;
            }
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
            return instance.InstanceType == MongoServerInstanceType.ShardRouter;
        }
    }
}
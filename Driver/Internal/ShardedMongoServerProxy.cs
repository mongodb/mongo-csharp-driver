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
    /// Connects to a number of mongos' and distributes load based on ping times.
    /// </summary>
    internal sealed class ShardedMongoServerProxy : MultipleConnectionMongoServerProxy
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ShardedMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public ShardedMongoServerProxy(MongoServer server)
            : base(server)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShardedMongoServerProxy"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="instances">The instances.</param>
        /// <param name="connectionAttempt">The connection attempt.</param>
        public ShardedMongoServerProxy(MongoServer server, IEnumerable<MongoServerInstance> instances, BlockingQueue<MongoServerInstance> stateChangedQueue, int connectionAttempt)
            : base(server, instances, stateChangedQueue, connectionAttempt)
        { }

        // protected methods
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
                if (instances.Any(i => i.State == MongoServerState.Connected))
                {
                    return MongoServerState.Connected;
                }
                else if (instances.All(i => i.State == MongoServerState.Disconnected))
                {
                    return MongoServerState.Disconnected;
                }
                else if (instances.All(i => i.State == MongoServerState.Connecting))
                {
                    return MongoServerState.Connecting;
                }
                else if (instances.Any(i => i.State == MongoServerState.Unknown))
                {
                    return MongoServerState.Unknown;
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
            return instance.Type == MongoServerInstanceType.ShardRouter;
        }
    }
}
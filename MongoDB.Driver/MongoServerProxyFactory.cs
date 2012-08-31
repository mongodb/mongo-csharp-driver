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
using System.Text;

namespace MongoDB.Driver
{
    /// <summary>
    /// Creates a MongoServerInstanceManager based on the settings.
    /// </summary>
    internal class MongoServerProxyFactory
    {
        // public methods
        /// <summary>
        /// Creates an IMongoServerProxy of some type that depends on the server settings.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>An IMongoServerProxy.</returns>
        public IMongoServerProxy Create(MongoServer server)
        {
            var connectionMode = server.Settings.ConnectionMode;
            if (server.Settings.ConnectionMode == ConnectionMode.Automatic)
            {
                if (server.Settings.ReplicaSetName != null)
                {
                    connectionMode = ConnectionMode.ReplicaSet;
                }
                else if (server.Settings.Servers.Count() == 1)
                {
                    connectionMode = ConnectionMode.Direct;
                }
            }

            switch (connectionMode)
            {
                case ConnectionMode.Direct:
                    return new DirectMongoServerProxy(server);
                case ConnectionMode.ReplicaSet:
                    return new ReplicaSetMongoServerProxy(server);
                case ConnectionMode.ShardRouter:
                    return new ShardedMongoServerProxy(server);
                default:
                    return new DiscoveringMongoServerProxy(server);
            }
        }
    }
}
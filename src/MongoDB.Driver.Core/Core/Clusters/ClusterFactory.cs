/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a factory for clusters.
    /// </summary>
    public class ClusterFactory : IClusterFactory
    {
        // fields
        private readonly IClusterListener _listener;
        private readonly IClusterableServerFactory _serverFactory;
        private readonly ClusterSettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterFactory"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="serverFactory">The server factory.</param>
        /// <param name="listener">The listener.</param>
        public ClusterFactory(ClusterSettings settings, IClusterableServerFactory serverFactory, IClusterListener listener)
        {
            _settings = Ensure.IsNotNull(settings, "settings");
            _serverFactory = Ensure.IsNotNull(serverFactory, "serverFactory");
            _listener = listener;
        }

        // methods
        /// <inheritdoc/>
        public ICluster CreateCluster()
        {
            var connectionMode = _settings.ConnectionMode;

            if (connectionMode == ClusterConnectionMode.Automatic)
            {
                if (_settings.ReplicaSetName != null)
                {
                    connectionMode = ClusterConnectionMode.ReplicaSet;
                }
            }

            var settings = _settings.With(connectionMode: connectionMode);

            switch (connectionMode)
            {
                case ClusterConnectionMode.Automatic:
                    return settings.EndPoints.Count == 1 ? (ICluster)CreateSingleServerCluster(settings) : CreateMultiServerCluster(settings);
                case ClusterConnectionMode.Direct:
                case ClusterConnectionMode.Standalone:
                    return CreateSingleServerCluster(settings);
                case ClusterConnectionMode.ReplicaSet:
                case ClusterConnectionMode.Sharded:
                    return CreateMultiServerCluster(settings);
                default:
                    throw new MongoInternalException(string.Format("Invalid connection mode: {0}.", connectionMode));
            }
        }

        private MultiServerCluster CreateMultiServerCluster(ClusterSettings settings)
        {
            var shardedCluster = new MultiServerCluster(settings, _serverFactory, _listener);
            shardedCluster.Initialize();
            return shardedCluster;
        }

        private SingleServerCluster CreateSingleServerCluster(ClusterSettings settings)
        {
            var standaloneCluster = new SingleServerCluster(settings, _serverFactory, _listener);
            standaloneCluster.Initialize();
            return standaloneCluster;
        }
    }
}

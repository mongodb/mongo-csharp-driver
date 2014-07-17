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
        private readonly IServerFactory _serverFactory;
        private readonly ClusterSettings _settings;

        // constructors
        public ClusterFactory(ClusterSettings settings, IServerFactory serverFactory, IClusterListener listener)
        {
            _settings = Ensure.IsNotNull(settings, "settings");
            _serverFactory = Ensure.IsNotNull(serverFactory, "serverFactory");
            _listener = listener;
        }

        // methods
        public ICluster Create()
        {
            switch (_settings.ClusterType)
            {
                case ClusterType.Direct: return CreateDirectCluster();
                case ClusterType.ReplicaSet: return CreateReplicaSet();
                case ClusterType.Sharded: return CreateShardedCluster();
                case ClusterType.Standalone: return CreateStandaloneCluster();
                default:
                    throw new ArgumentException(string.Format("Invalid cluster type: {0}.", _settings.ClusterType), "settings");
            }
        }

        private DirectCluster CreateDirectCluster()
        {
            var directCluster = new DirectCluster(_settings, _serverFactory, _listener);
            directCluster.Initialize();
            return directCluster;
        }

        private ReplicaSet CreateReplicaSet()
        {
            var replicaSet = new ReplicaSet(_settings, _serverFactory, _listener);
            replicaSet.Initialize();
            return replicaSet;
        }

        private ShardedCluster CreateShardedCluster()
        {
            var shardedCluster = new ShardedCluster(_settings, _serverFactory, _listener);
            shardedCluster.Initialize();
            return shardedCluster;
        }

        private StandaloneCluster CreateStandaloneCluster()
        {
            var standaloneCluster = new StandaloneCluster(_settings, _serverFactory, _listener);
            standaloneCluster.Initialize();
            return standaloneCluster;
        }
    }
}

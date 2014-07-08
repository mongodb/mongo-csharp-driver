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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a static factory for ICluster implementations.
    /// </summary>
    public static class Cluster
    {
        // static methods
        public static ICluster Create(ClusterSettings settings)
        {
            if (settings == null) { throw new ArgumentNullException("settings"); }
            switch (settings.ClusterType)
            {
                case ClusterType.Direct: return CreateDirectCluster(settings);
                case ClusterType.ReplicaSet: return CreateReplicaSet(settings);
                case ClusterType.Sharded: return CreateShardedCluster(settings);
                case ClusterType.Standalone: return CreateStandaloneCluster(settings);
                default:
                    throw new ArgumentException(string.Format("Invalid cluster type: {0}.", settings.ClusterType), "settings");
            }
        }

        public static ICluster Create(string uriString)
        {
            return Create(ClusterSettingsUriParser.Parse(uriString));
        }

        public static ICluster Create(Uri uri)
        {
            return Create(ClusterSettingsUriParser.Parse(uri));
        }

        public static DirectCluster CreateDirectCluster(ClusterSettings settings)
        {
            var directCluster = new DirectCluster(settings);
            directCluster.Initialize();
            return directCluster;
        }

        public static ReplicaSet CreateReplicaSet(ClusterSettings settings)
        {
            var replicaSet = new ReplicaSet(settings);
            replicaSet.Initialize();
            return replicaSet;
        }

        public static ShardedCluster CreateShardedCluster(ClusterSettings settings)
        {
            var shardedCluster = new ShardedCluster(settings);
            shardedCluster.Initialize();
            return shardedCluster;
        }

        public static StandaloneCluster CreateStandaloneCluster(ClusterSettings settings)
        {
            var standaloneCluster = new StandaloneCluster(settings);
            standaloneCluster.Initialize();
            return standaloneCluster;
        }
    }
}

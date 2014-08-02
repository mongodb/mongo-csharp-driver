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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a standalone cluster.
    /// </summary>
    public class SingleServerCluster : Cluster
    {
        // fields
        private IClusterableServer _server;

        // constructor
        internal SingleServerCluster(ClusterSettings settings, IServerFactory serverFactory, IClusterListener listener)
            : base(settings, serverFactory, listener)
        {
            Ensure.IsEqualTo(settings.EndPoints.Count, 1, "settings.EndPoints.Count");
            Ensure.IsNull(settings.ReplicaSetName, "settings.ReplicaSetName");
            if (settings.RequiredClusterType.HasValue)
            {
                switch (settings.RequiredClusterType.Value)
                {
                    case ClusterType.Direct:
                    case ClusterType.Sharded:
                    case ClusterType.Standalone:
                    case ClusterType.Unknown:
                        break;
                    default:
                        var message = string.Format("RequiredClusterType is not compatible with SingleServerCluster: {0}.", settings.RequiredClusterType.Value);
                        throw new ArgumentException(message, "settings.RequiredClusterType");
                }
            }
        }

        // methods
        private ClusterType DetermineClusterType(ServerDescription serverDescription)
        {
            switch (serverDescription.Type)
            {
                case ServerType.ReplicaSetArbiter:
                case ServerType.ReplicaSetOther:
                case ServerType.ReplicaSetPassive:
                case ServerType.ReplicaSetPrimary:
                case ServerType.ReplicaSetSecondary:
                    return ClusterType.ReplicaSet;

                case ServerType.ShardRouter:
                    return ClusterType.Sharded;

                case ServerType.Standalone:
                    return ClusterType.Standalone;

                case ServerType.Unknown:
                    return ClusterType.Unknown;

                default:
                    throw new MongoDBException("Unexpectec ServerTypes.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _server.DescriptionChanged -= ServerDescriptionChanged;
                _server.Dispose();
            }
            base.Dispose(disposing);
        }

        public override IServer GetServer(EndPoint endPoint)
        {
            if (_server.EndPoint.Equals(endPoint))
            {
                return _server;
            }
            else
            {
                return null;
            }
        }

        public void Initialize()
        {
            _server = CreateServer(Settings.EndPoints.Single());
            _server.DescriptionChanged += ServerDescriptionChanged;
            _server.Initialize();
        }

        private void ServerDescriptionChanged(object sender, ServerDescriptionChangedEventArgs args)
        {
            var oldClusterDescription = Description;

            var newServerDescription = args.NewServerDescription;
            var newClusterState = newServerDescription.State == ServerState.Connected ? ClusterState.Connected : ClusterState.Disconnected;

            var clusterType = oldClusterDescription.Type;
            if (clusterType == ClusterType.Unknown)
            {
                clusterType = DetermineClusterType(newServerDescription);
            }

            var newClusterDescription = new ClusterDescription(
                ClusterId,
                clusterType,
                newClusterState,
                new[] { newServerDescription },
                null,
                0);

            UpdateClusterDescription(newClusterDescription);
        }

        protected override bool TryGetServer(EndPoint endPoint, out IClusterableServer server)
        {
            if (_server.EndPoint.Equals(endPoint))
            {
                server = _server;
                return true;
            }
            else
            {
                server = null;
                return false;
            }
        }
    }
}

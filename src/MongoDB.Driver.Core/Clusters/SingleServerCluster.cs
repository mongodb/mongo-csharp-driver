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
using System.Diagnostics;
using System.Net;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a standalone cluster.
    /// </summary>
    internal sealed class SingleServerCluster : Cluster
    {
        // fields
        private IClusterableServer _server;
        private readonly InterlockedInt32 _state;

        // constructor
        internal SingleServerCluster(ClusterSettings settings, IClusterableServerFactory serverFactory, IClusterListener listener)
            : base(settings, serverFactory, listener)
        {
            Ensure.IsEqualTo(settings.EndPoints.Count, 1, "settings.EndPoints.Count");

            _state = new InterlockedInt32(State.Initial);
        }

        // methods
        private ClusterType DetermineClusterType(ServerDescription serverDescription)
        {
            switch (serverDescription.Type)
            {
                case ServerType.ReplicaSetArbiter:
                case ServerType.ReplicaSetGhost:
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
                    throw new MongoDBException("Unexpected ServerTypes.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_state.TryChange(State.Disposed))
            {
                if (disposing)
                {
                    if(Listener != null)
                    {
                        Listener.ClusterBeforeClosing(ClusterId);
                    }
                    var stopwatch = Stopwatch.StartNew();
                    if (_server != null)
                    {
                        _server.DescriptionChanged -= ServerDescriptionChanged;
                        _server.Dispose();
                    }
                    stopwatch.Stop();
                    if(Listener != null)
                    {
                        Listener.ClusterAfterClosing(ClusterId, stopwatch.Elapsed);
                    }
                }
            }
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (_state.TryChange(State.Initial, State.Open))
            {
                if(Listener != null)
                {
                    Listener.ClusterBeforeOpening(ClusterId, Settings);
                    Listener.ClusterBeforeAddingServer(ClusterId, Settings.EndPoints[0]);
                }
                var stopwatch = Stopwatch.StartNew();
                _server = CreateServer(Settings.EndPoints[0]);
                _server.DescriptionChanged += ServerDescriptionChanged;
                _server.Initialize();
                stopwatch.Stop();
                if(Listener != null)
                {
                    Listener.ClusterAfterAddingServer(_server.ServerId, stopwatch.Elapsed);
                    Listener.ClusterAfterOpening(ClusterId, Settings, stopwatch.Elapsed);
                }
            }
        }

        protected override void Invalidate()
        {
            _server.Invalidate();
        }

        private void ServerDescriptionChanged(object sender, ServerDescriptionChangedEventArgs args)
        {
            var oldClusterDescription = Description;
            ClusterDescription newClusterDescription = oldClusterDescription;

            var newServerDescription = args.NewServerDescription;
            if (newServerDescription.State == ServerState.Disconnected)
            {
                newClusterDescription = Description
                    .WithServerDescription(newServerDescription);
            }
            else
            {
                var determinedClusterType = DetermineClusterType(newServerDescription);
                if (oldClusterDescription.Type == ClusterType.Unknown)
                {
                    newClusterDescription = newClusterDescription
                        .WithType(determinedClusterType)
                        .WithServerDescription(newServerDescription);
                }
                else if (determinedClusterType != oldClusterDescription.Type)
                {
                    newClusterDescription = newClusterDescription
                        .WithoutServerDescription(newServerDescription.EndPoint);
                }
                else
                {
                    newClusterDescription = newClusterDescription
                        .WithServerDescription(newServerDescription);
                }
            }

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

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        // nested classes
        private static class State
        {
            public const int Initial = 0;
            public const int Open = 1;
            public const int Disposed = 2;
        }
    }
}

/* Copyright 2013-present MongoDB Inc.
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
using Microsoft.Extensions.Logging;
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
        private readonly string _replicaSetName;

        // constructor
        internal SingleServerCluster(ClusterSettings settings, IClusterableServerFactory serverFactory, IEventSubscriber eventSubscriber, ILoggerFactory loggerFactory)
            : base(settings, serverFactory, eventSubscriber, loggerFactory)
        {
            Ensure.That(settings.SrvMaxHosts == 0, "srvMaxHosts cannot be used with a single server cluster.");
            Ensure.IsEqualTo(settings.EndPoints.Count, 1, "settings.EndPoints.Count");
            _replicaSetName = settings.ReplicaSetName;  // can be null

            _state = new InterlockedInt32(State.Initial);
        }

        // methods
        protected override void Dispose(bool disposing)
        {
            Stopwatch stopwatch = null;
            if (_state.TryChange(State.Disposed))
            {
                if (disposing)
                {
                    _clusterEventLogger.LogAndPublish(new ClusterClosingEvent(ClusterId));

                    stopwatch = Stopwatch.StartNew();

                    if (_server != null)
                    {
                        _clusterEventLogger.LogAndPublish(new ClusterRemovingServerEvent(_server.ServerId, "Removing server."));

                        _server.DescriptionChanged -= ServerDescriptionChanged;
                        _server.Dispose();

                        _clusterEventLogger.LogAndPublish(new ClusterRemovedServerEvent(_server.ServerId, "Server removed.", stopwatch.Elapsed));
                    }
                    stopwatch.Stop();
                }
            }

            base.Dispose(disposing);

            if (stopwatch != null)
            {
                _clusterEventLogger.LogAndPublish(new ClusterClosedEvent(ClusterId, stopwatch.Elapsed));
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            if (_state.TryChange(State.Initial, State.Open))
            {
                _clusterEventLogger.LogAndPublish(new ClusterOpeningEvent(ClusterId, Settings));

                var stopwatch = Stopwatch.StartNew();
                _server = CreateServer(Settings.EndPoints[0]);
                var newClusterDescription = Description
                    .WithType(Settings.GetInitialClusterType())
                    .WithServerDescription(_server.Description);

                _clusterEventLogger.LogAndPublish(new ClusterAddingServerEvent(ClusterId, _server.EndPoint));

                _server.DescriptionChanged += ServerDescriptionChanged;
                stopwatch.Stop();

                _clusterEventLogger.LogAndPublish(new ClusterAddedServerEvent(_server.ServerId, stopwatch.Elapsed));

                UpdateClusterDescription(newClusterDescription);

                _server.Initialize();

                _clusterEventLogger.LogAndPublish(new ClusterOpenedEvent(ClusterId, Settings, stopwatch.Elapsed));
            }
        }

        private bool IsServerValidForCluster(ClusterType clusterType, ClusterSettings clusterSettings, ServerType serverType)
        {
            switch (clusterType)
            {
                case ClusterType.ReplicaSet:
                    return serverType.IsReplicaSetMember();

                case ClusterType.Sharded:
                    return serverType == ServerType.ShardRouter;

                case ClusterType.Standalone:
                    return IsStandaloneServerValidForCluster();

                case ClusterType.Unknown:
                    return IsUnknownServerValidForCluster();

                default:
                    throw new MongoInternalException("Unexpected cluster type.");
            }

            bool IsStandaloneServerValidForCluster()
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (clusterSettings.ConnectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    return clusterSettings.DirectConnection.GetValueOrDefault();
                }
                else
                {
                    return serverType == ServerType.Standalone;
                }
            }

            bool IsUnknownServerValidForCluster()
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (clusterSettings.ConnectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    return clusterSettings.DirectConnection.GetValueOrDefault();
                }
                else
                {
                    var connectionMode = clusterSettings.ConnectionMode;
                    return
                        connectionMode == ClusterConnectionMode.Automatic ||
                        connectionMode == ClusterConnectionMode.Direct;
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        protected override void RequestHeartbeat()
        {
            _server.RequestHeartbeat();
        }

        private void ServerDescriptionChanged(object sender, ServerDescriptionChangedEventArgs args)
        {
            var newServerDescription = args.NewServerDescription;
            var newClusterDescription = Description;

            if (_replicaSetName != null)
            {
                var replicaSetConfig = newServerDescription.ReplicaSetConfig;
                if (replicaSetConfig == null || replicaSetConfig.Name != _replicaSetName)
                {
                    // if the replica set name does not match then the ServerType in the ServerDescription MUST be replaced with Unknown
                    newServerDescription = newServerDescription.With(type: ServerType.Unknown);
                }
            }

            if (newServerDescription.State == ServerState.Disconnected)
            {
                newClusterDescription = newClusterDescription.WithServerDescription(newServerDescription);
            }
            else
            {
                if (IsServerValidForCluster(newClusterDescription.Type, Settings, newServerDescription.Type))
                {
                    if (newClusterDescription.Type == ClusterType.Unknown)
                    {
                        newClusterDescription = newClusterDescription.WithType(newServerDescription.Type.ToClusterType());
                    }

                    newClusterDescription = newClusterDescription.WithServerDescription(newServerDescription);
                }
                else
                {
                    newClusterDescription = newClusterDescription.WithoutServerDescription(newServerDescription.EndPoint);
                }
            }

            var shouldClusterDescriptionChangedEventBePublished = !args.OldServerDescription.SdamEquals(args.NewServerDescription);
            UpdateClusterDescription(newClusterDescription, shouldClusterDescriptionChangedEventBePublished);
        }

        protected override bool TryGetServer(EndPoint endPoint, out IClusterableServer server)
        {
            if (EndPointHelper.Equals(_server.EndPoint, endPoint))
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

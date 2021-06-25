﻿/* Copyright 2021-present MongoDB Inc.
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
using System.Net;
using System.Threading;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Servers
{
    internal class LoadBalancedServer : Server
    {
        private readonly ServerDescription _baseDescription;
        private ServerDescription _currentDescription;
        private readonly object _connectionPoolLock = new object();

        public LoadBalancedServer(
            ClusterId clusterId,
            IClusterClock clusterClock,
            ServerSettings serverSettings,
            EndPoint endPoint,
            IConnectionPoolFactory connectionPoolFactory,
            IEventSubscriber eventSubscriber,
            ServerApi serverApi)
            : base(
                  clusterId,
                  clusterClock,
#pragma warning disable CS0618 // Type or member is obsolete
                  ClusterConnectionMode.Automatic,
                  ConnectionModeSwitch.UseConnectionMode,
#pragma warning restore CS0618 // Type or member is obsolete
                  directConnection: null,
                  serverSettings,
                  endPoint,
                  connectionPoolFactory,
                  eventSubscriber,
                  serverApi)
        {
            _baseDescription = _currentDescription = new ServerDescription(ServerId, endPoint, reasonChanged: "ServerInitialDescription");
        }

        public override ServerDescription Description => Interlocked.CompareExchange(ref _currentDescription, value: null, comparand: null);

        protected override void Dispose(bool disposing)
        {
            // no-opt
        }

        protected override void HandleBeforeHandshakeCompletesException(Exception ex)
        {
            // drivers MUST NOT perform SDAM error handling for any errors that occur before the MongoDB Handshake

            if (ex is MongoAuthenticationException mongoAuthenticationException)
            {
                // when requiring the connection pool to be cleared, MUST only clear connections for the serviceId.
                ConnectionPool.Clear(mongoAuthenticationException.ServiceId.Value); // TODO: serviceId is not implemented yet
            }
        }

        protected override void HandleAfterHandshakeCompletesException(IConnection connection, Exception ex)
        {
            lock (_connectionPoolLock)
            {
                if (ex is MongoConnectionException mongoConnectionException &&
                    mongoConnectionException.Generation != null &&
                    mongoConnectionException.Generation != ConnectionPool.Generation)
                {
                    return; // stale generation number
                }

                if (ShouldClearConnectionPoolForChannelException(ex, connection.Description.ServerVersion))
                {
                    // when requiring the connection pool to be cleared, MUST only clear connections for the serviceId.
                    ConnectionPool.Clear(connection.Description.ServiceId.Value); // TODO: serviceId is not implemented yet
                }
            }
        }

        public override void Initializing()
        {
            // generate initial server description
            var newDescription = _baseDescription
                .With(
                    type: ServerType.LoadBalanced,
                    reasonChanged: "Initialized",
                    state: ServerState.Connected);
            var oldDescription = Interlocked.CompareExchange(ref _currentDescription, value: newDescription, comparand: _currentDescription);
            var eventArgs = new ServerDescriptionChangedEventArgs(oldDescription, newDescription);

            // propagate event to upper levels, this will be called only once
            TriggerServerDescriptionChanged(this, eventArgs);
        }

        public override void Invalidate(string reasonInvalidated, bool clearConnectionPool, TopologyVersion topologyVersion)
        {
            // no-opt
        }

        public override void RequestHeartbeat()
        {
            // no-opt
        }
    }
}

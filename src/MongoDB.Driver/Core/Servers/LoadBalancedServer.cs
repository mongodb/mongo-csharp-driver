/* Copyright 2021-present MongoDB Inc.
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
using MongoDB.Driver.Core.Logging;

namespace MongoDB.Driver.Core.Servers
{
    internal sealed class LoadBalancedServer : Server
    {
        private readonly ServerDescription _baseDescription;
        private ServerDescription _currentDescription;
        private readonly object _connectionPoolLock = new();

        public LoadBalancedServer(
            ClusterId clusterId,
            IClusterClock clusterClock,
            ServerSettings serverSettings,
            EndPoint endPoint,
            IConnectionPoolFactory connectionPoolFactory,
            ServerApi serverApi,
            EventLogger<LogCategories.SDAM> eventLogger)
            : base(
                  clusterId,
                  clusterClock,
                  directConnection: false,
                  serverSettings,
                  endPoint,
                  connectionPoolFactory,
                  serverApi,
                  eventLogger)
        {
            _baseDescription = _currentDescription = new ServerDescription(ServerId, endPoint, reasonChanged: "ServerInitialDescription");
        }

        public override ServerDescription Description => Interlocked.CompareExchange(ref _currentDescription, value: null, comparand: null);

        protected override void Dispose(bool disposing)
        {
            // no-op
        }

        protected override void HandleBeforeHandshakeCompletesException(Exception ex)
        {
            // drivers MUST NOT perform SDAM error handling for any errors that occur before the MongoDB Handshake

            if (ex is MongoAuthenticationException mongoAuthenticationException &&
                mongoAuthenticationException.ServiceId.HasValue) // this value will be always filled for MongoAuthenticationException, adding this condition just in case
            {
                // when requiring the connection pool to be cleared, MUST only clear connections for the serviceId.
                ConnectionPool.Clear(mongoAuthenticationException.ServiceId.Value);
            }
        }

        protected override void HandleAfterHandshakeCompletesException(IConnection connection, Exception ex)
        {
            lock (_connectionPoolLock)
            {
                if (ex is MongoConnectionException mongoConnectionException &&
                    mongoConnectionException.Generation.HasValue &&
                    mongoConnectionException.Generation.Value != ConnectionPool.GetGeneration(connection.Description?.ServiceId))
                {
                    return; // stale generation number
                }

                if (ShouldClearConnectionPoolForChannelException(ex, connection.Description.MaxWireVersion) &&
                    connection.Description.ServiceId.HasValue) // this value will be always filled in this place, adding this here just in case
                {
                    // when requiring the connection pool to be cleared, MUST only clear connections for the serviceId.
                    ConnectionPool.Clear(connection.Description.ServiceId.Value);
                }
            }
        }

        protected override void InitializeSubClass()
        {
            // generate initial server description
            var newDescription = _baseDescription
                .With(
                    type: ServerType.LoadBalanced,
                    reasonChanged: "Initialized",
                    state: ServerState.Connected);
            var oldDescription = Interlocked.CompareExchange(ref _currentDescription, value: newDescription, comparand: _currentDescription);
            var eventArgs = new ServerDescriptionChangedEventArgs(oldDescription, newDescription);

            // mark pool as ready, start the connection creation thread.
            // note that the pool can not be paused after it was marked as ready in LB mode.
            ConnectionPool.SetReady();

            // propagate event to upper levels, this will be called only once
            TriggerServerDescriptionChanged(this, eventArgs);
        }

        protected override void Invalidate(string reasonInvalidated, bool clearConnectionPool, TopologyVersion topologyVersion)
        {
            // no-op
        }

        public override void RequestHeartbeat()
        {
            // no-op
        }
    }
}

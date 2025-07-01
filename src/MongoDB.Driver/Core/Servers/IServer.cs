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
using System.Net;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Servers
{
    internal interface IServer
    {
        event EventHandler<ServerDescriptionChangedEventArgs> DescriptionChanged;

        IClusterClock ClusterClock { get; }
        ServerDescription Description { get; }
        EndPoint EndPoint { get; }
        ServerId ServerId { get; }
        ServerApi ServerApi { get; }

        IConnectionHandle GetConnection(OperationContext operationContext); // questionable to switch from channel to connection
        Task<IConnectionHandle> GetConnectionAsync(OperationContext operationContext);
        void ReturnConnection(IConnectionHandle connection);
        void HandleChannelException(IConnection connection, Exception exception);
    }

    internal interface IClusterableServer : IServer, IDisposable
    {
        bool IsInitialized { get; }
        int OutstandingOperationsCount { get; }

        void Initialize();
        void Invalidate(string reasonInvalidated, TopologyVersion responseTopologyVersion);
        void RequestHeartbeat();
    }

    internal interface ISelectedServer : IServer
    {
        ServerDescription DescriptionWhenSelected { get; }
    }
}

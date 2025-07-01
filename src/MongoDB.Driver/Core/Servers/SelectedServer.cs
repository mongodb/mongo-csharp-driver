/* Copyright 2010-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Servers;

internal class SelectedServer : ISelectedServer
{
    private readonly ServerDescription _descriptionWhenSelected;
    private readonly IServer _server;

    public SelectedServer(IServer server, ServerDescription descriptionWhenSelected)
    {
        _server = server;
        _descriptionWhenSelected = descriptionWhenSelected;
    }

    public event EventHandler<ServerDescriptionChangedEventArgs> DescriptionChanged
    {
        add { _server.DescriptionChanged += value; }
        remove => _server.DescriptionChanged -= value;
    }

    public IClusterClock ClusterClock => _server.ClusterClock;
    public ServerDescription Description => _server.Description;
    public EndPoint EndPoint => _server.EndPoint;
    public ServerId ServerId => _server.ServerId;
    public ServerApi ServerApi => _server.ServerApi;
    public ServerDescription DescriptionWhenSelected => _descriptionWhenSelected;

    public IConnectionHandle GetConnection(OperationContext operationContext) => _server.GetConnection(operationContext);

    public Task<IConnectionHandle> GetConnectionAsync(OperationContext operationContext) => _server.GetConnectionAsync(operationContext);

    public void ReturnConnection(IConnectionHandle connection) => _server.ReturnConnection(connection);

    public void HandleChannelException(IConnection connection, Exception exception) => _server.HandleChannelException(connection, exception);
}

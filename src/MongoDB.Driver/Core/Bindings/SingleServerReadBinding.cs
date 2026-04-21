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
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    internal sealed class SingleServerReadBinding : IReadBinding
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IClusterInternal _cluster;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private bool _disposed;
        private readonly ReadPreference _readPreference;
        private readonly IServerSelector _serverSelector;
        private readonly ICoreSessionHandle _session;

        public SingleServerReadBinding(IClusterInternal cluster, IServer server, ReadPreference readPreference, ICoreSessionHandle session)
        {
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            Ensure.IsNotNull(server, nameof(server));
            _serverSelector = new EndPointServerSelector(server.EndPoint);
            _readPreference = Ensure.IsNotNull(readPreference, nameof(readPreference));
            _session = Ensure.IsNotNull(session, nameof(session));
        }

        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
        }

        public ICoreSessionHandle Session
        {
            get { return _session; }
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext)
        {
            ThrowIfDisposed();
            var server = _cluster.SelectServer(operationContext, _serverSelector);
            return new ChannelSourceHandle(new ServerChannelSource(server, _session.Fork()));
        }

        public async Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext)
        {
            ThrowIfDisposed();
            var server = await _cluster.SelectServerAsync(operationContext, _serverSelector).ConfigureAwait(false);
            return new ChannelSourceHandle(new ServerChannelSource(server, _session.Fork()));
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers) =>
            GetReadChannelSource(operationContext);

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers) =>
            GetReadChannelSourceAsync(operationContext);

        public void Dispose()
        {
            if (!_disposed)
            {
                _session.Dispose();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}

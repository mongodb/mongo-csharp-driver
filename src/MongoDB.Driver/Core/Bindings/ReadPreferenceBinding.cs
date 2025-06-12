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
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    internal sealed class ReadPreferenceBinding : IReadBinding
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IClusterInternal _cluster;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private bool _disposed;
        private readonly ReadPreference _readPreference;
        private readonly IServerSelector _serverSelector;
        private readonly ICoreSessionHandle _session;

        public ReadPreferenceBinding(IClusterInternal cluster, ReadPreference readPreference, ICoreSessionHandle session)
        {
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            _readPreference = Ensure.IsNotNull(readPreference, nameof(readPreference));
            _session = Ensure.IsNotNull(session, nameof(session));
            _serverSelector = new ReadPreferenceServerSelector(readPreference);
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
            return GetReadChannelSource(operationContext, null);
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext)
        {
            return GetReadChannelSourceAsync(operationContext, null);
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            ThrowIfDisposed();
            var server = _cluster.SelectServerAndPinIfNeeded(operationContext, _session, _serverSelector, deprioritizedServers);
            return GetChannelSourceHelper(server);
        }

        public async Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            ThrowIfDisposed();
            var server = await _cluster.SelectServerAndPinIfNeededAsync(operationContext, _session, _serverSelector, deprioritizedServers).ConfigureAwait(false);
            return GetChannelSourceHelper(server);
        }

        private IChannelSourceHandle GetChannelSourceHelper(IServer server)
        {
            return new ChannelSourceHandle(new ServerChannelSource(server, _session.Fork()));
        }

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
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}

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
    internal sealed class WritableServerBinding : IReadWriteBinding
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IClusterInternal _cluster;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private bool _disposed;
        private readonly ICoreSessionHandle _session;

        public WritableServerBinding(IClusterInternal cluster, ICoreSessionHandle session)
        {
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            _session = Ensure.IsNotNull(session, nameof(session));
        }

        public ReadPreference ReadPreference
        {
            get { return ReadPreference.Primary; }
        }

        public ICoreSessionHandle Session
        {
            get { return _session; }
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext)
        {
            return GetReadChannelSource(null, operationContext);
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext)
        {
            return GetReadChannelSourceAsync(null, operationContext);
        }

        public IChannelSourceHandle GetReadChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationContext operationContext)
        {
            ThrowIfDisposed();
            var server = _cluster.SelectServerAndPinIfNeeded(_session, WritableServerSelector.Instance, deprioritizedServers, operationContext);
            return CreateServerChannelSource(server);
        }

        public async Task<IChannelSourceHandle> GetReadChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationContext operationContext)
        {
            ThrowIfDisposed();
            var server = await _cluster.SelectServerAndPinIfNeededAsync(_session, WritableServerSelector.Instance, deprioritizedServers, operationContext).ConfigureAwait(false);
            return CreateServerChannelSource(server);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext)
        {
            return GetWriteChannelSource(deprioritizedServers: null, operationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationContext operationContext)
        {
            ThrowIfDisposed();
            var server = _cluster.SelectServerAndPinIfNeeded(_session, WritableServerSelector.Instance, deprioritizedServers, operationContext);
            return CreateServerChannelSource(server);
        }

        public IChannelSourceHandle GetWriteChannelSource(IMayUseSecondaryCriteria mayUseSecondary, OperationContext operationContext)
        {
            return GetWriteChannelSource(null, mayUseSecondary, operationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary, OperationContext operationContext)
        {
            if (IsSessionPinnedToServer())
            {
                throw new InvalidOperationException($"This overload of {nameof(GetWriteChannelSource)} cannot be called when pinned to a server.");
            }

            var writableServerSelector = new WritableServerSelector(mayUseSecondary);

            var selector = deprioritizedServers != null
                ? (IServerSelector)new CompositeServerSelector(new IServerSelector[] { new PriorityServerSelector(deprioritizedServers), writableServerSelector })
                : writableServerSelector;

            var server = _cluster.SelectServer(selector, operationContext);
            return CreateServerChannelSource(server);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext)
        {
            return GetWriteChannelSourceAsync(deprioritizedServers: null, operationContext);
        }

        public async Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationContext operationContext)
        {
            ThrowIfDisposed();
            var server = await _cluster.SelectServerAndPinIfNeededAsync(_session, WritableServerSelector.Instance, deprioritizedServers, operationContext).ConfigureAwait(false);
            return CreateServerChannelSource(server);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IMayUseSecondaryCriteria mayUseSecondary, OperationContext operationContext)
        {
            return GetWriteChannelSourceAsync(null, mayUseSecondary, operationContext);
        }

        public async Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary, OperationContext operationContext)
        {
            if (IsSessionPinnedToServer())
            {
                throw new InvalidOperationException($"This overload of {nameof(GetWriteChannelSource)} cannot be called when pinned to a server.");
            }

            var writableServerSelector = new WritableServerSelector(mayUseSecondary);

            IServerSelector selector = deprioritizedServers != null
                ? new CompositeServerSelector(new IServerSelector[] { new PriorityServerSelector(deprioritizedServers), writableServerSelector })
                : writableServerSelector;

            var server = await _cluster.SelectServerAsync(selector, operationContext).ConfigureAwait(false);
            return CreateServerChannelSource(server);
        }

        private IChannelSourceHandle CreateServerChannelSource(IServer server)
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

        private bool IsSessionPinnedToServer()
        {
            return _session.IsInTransaction && _session.CurrentTransaction.PinnedServer != null;
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

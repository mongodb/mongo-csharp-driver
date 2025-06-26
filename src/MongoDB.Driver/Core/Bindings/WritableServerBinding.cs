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

        public ReadPreference ReadPreference => ReadPreference.Primary;

        public ICoreSessionHandle Session => _session;

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext)
            => GetReadChannelSource(operationContext, null);

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext)
            => GetReadChannelSourceAsync(operationContext, null);

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            ThrowIfDisposed();
            var server = _cluster.SelectServerAndPinIfNeeded(operationContext, _session, WritableServerSelector.Instance, deprioritizedServers);
            return CreateServerChannelSource(server);
        }

        public async Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            ThrowIfDisposed();
            var server = await _cluster.SelectServerAndPinIfNeededAsync(operationContext, _session, WritableServerSelector.Instance, deprioritizedServers).ConfigureAwait(false);
            return CreateServerChannelSource(server);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext)
        {
            return GetWriteChannelSource(operationContext, deprioritizedServers: null);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            ThrowIfDisposed();
            var server = _cluster.SelectServerAndPinIfNeeded(operationContext, _session, WritableServerSelector.Instance, deprioritizedServers);
            return CreateServerChannelSource(server);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IMayUseSecondaryCriteria mayUseSecondary)
        {
            return GetWriteChannelSource(operationContext, null, mayUseSecondary);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary)
        {
            if (IsSessionPinnedToServer())
            {
                throw new InvalidOperationException($"This overload of {nameof(GetWriteChannelSource)} cannot be called when pinned to a server.");
            }

            var writableServerSelector = new WritableServerSelector(mayUseSecondary);

            var selector = deprioritizedServers != null
                ? (IServerSelector)new CompositeServerSelector(new IServerSelector[] { new PriorityServerSelector(deprioritizedServers), writableServerSelector })
                : writableServerSelector;

            var server = _cluster.SelectServer(operationContext, selector);
            return CreateServerChannelSource(server);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext)
        {
            return GetWriteChannelSourceAsync(operationContext, deprioritizedServers: null);
        }

        public async Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            ThrowIfDisposed();
            var server = await _cluster.SelectServerAndPinIfNeededAsync(operationContext, _session, WritableServerSelector.Instance, deprioritizedServers).ConfigureAwait(false);
            return CreateServerChannelSource(server);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IMayUseSecondaryCriteria mayUseSecondary)
        {
            return GetWriteChannelSourceAsync(operationContext, null, mayUseSecondary);
        }

        public async Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary)
        {
            if (IsSessionPinnedToServer())
            {
                throw new InvalidOperationException($"This overload of {nameof(GetWriteChannelSource)} cannot be called when pinned to a server.");
            }

            var writableServerSelector = new WritableServerSelector(mayUseSecondary);

            IServerSelector selector = deprioritizedServers != null
                ? new CompositeServerSelector(new IServerSelector[] { new PriorityServerSelector(deprioritizedServers), writableServerSelector })
                : writableServerSelector;

            var server = await _cluster.SelectServerAsync(operationContext, selector).ConfigureAwait(false);
            return CreateServerChannelSource(server);
        }

        private IChannelSourceHandle CreateServerChannelSource((IServer Server, TimeSpan RoundTripTime) server)
        {
            return new ChannelSourceHandle(new ServerChannelSource(server.Server, server.RoundTripTime, _session.Fork()));
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

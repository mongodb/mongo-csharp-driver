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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    // LB-only end-transaction binding; rebuilt by EndTransactionOperation.OnRetry between attempts
    // so the next SelectServer reflects the session's current pin state.
    internal sealed class EndTransactionReadWriteBinding : IReadWriteBinding
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IClusterInternal _cluster;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private bool _disposed;
        private IReadWriteBindingHandle _innerBinding;
        private readonly ICoreSessionHandle _session;

        public EndTransactionReadWriteBinding(IClusterInternal cluster, ICoreSessionHandle session)
        {
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            _session = Ensure.IsNotNull(session, nameof(session));
            _innerBinding = ChannelPinningHelper.CreateReadWriteBinding(_cluster, _session.Fork());
        }

        public ReadPreference ReadPreference => ReadPreference.Primary;

        public ICoreSessionHandle Session => _session;

        // Called by EndTransactionOperation.OnRetry between attempts.
        public void RebuildInnerBinding()
        {
            ThrowIfDisposed();
            _innerBinding.Dispose();
            _innerBinding = ChannelPinningHelper.CreateReadWriteBinding(_cluster, _session.Fork());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _innerBinding.Dispose();
                _session.Dispose();
                _disposed = true;
            }
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext)
            => _innerBinding.GetReadChannelSource(operationContext);

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext)
            => _innerBinding.GetReadChannelSourceAsync(operationContext);

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
            => _innerBinding.GetReadChannelSource(operationContext, deprioritizedServers);

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
            => _innerBinding.GetReadChannelSourceAsync(operationContext, deprioritizedServers);

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext)
            => _innerBinding.GetWriteChannelSource(operationContext);

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
            => _innerBinding.GetWriteChannelSource(operationContext, deprioritizedServers);

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IMayUseSecondaryCriteria mayUseSecondary)
            => _innerBinding.GetWriteChannelSource(operationContext, mayUseSecondary);

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary)
            => _innerBinding.GetWriteChannelSource(operationContext, deprioritizedServers, mayUseSecondary);

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext)
            => _innerBinding.GetWriteChannelSourceAsync(operationContext);

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
            => _innerBinding.GetWriteChannelSourceAsync(operationContext, deprioritizedServers);

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IMayUseSecondaryCriteria mayUseSecondary)
            => _innerBinding.GetWriteChannelSourceAsync(operationContext, mayUseSecondary);

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary)
            => _innerBinding.GetWriteChannelSourceAsync(operationContext, deprioritizedServers, mayUseSecondary);

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}

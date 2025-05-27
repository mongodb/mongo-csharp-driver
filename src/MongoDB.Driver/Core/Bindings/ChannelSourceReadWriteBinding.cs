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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    internal sealed class ChannelSourceReadWriteBinding : IReadWriteBinding
    {
        private readonly IChannelSourceHandle _channelSource;
        private bool _disposed;
        private readonly ReadPreference _readPreference;
        private readonly ICoreSessionHandle _session;

        public ChannelSourceReadWriteBinding(IChannelSourceHandle channelSource, ReadPreference readPreference, ICoreSessionHandle session)
        {
            _channelSource = Ensure.IsNotNull(channelSource, nameof(channelSource));
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

        public IChannelSourceHandle GetReadChannelSource(OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return GetChannelSourceHelper();
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return Task.FromResult(GetChannelSourceHelper());
        }

        public IChannelSourceHandle GetReadChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationCancellationContext cancellationContext)
        {
            return GetReadChannelSource(cancellationContext);
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationCancellationContext cancellationContext)
        {
            return GetReadChannelSourceAsync(cancellationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return GetChannelSourceHelper();
        }

        public IChannelSourceHandle GetWriteChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationCancellationContext cancellationContext)
        {
            return GetWriteChannelSource(cancellationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(IMayUseSecondaryCriteria mayUseSecondary, OperationCancellationContext cancellationContext)
        {
            return GetWriteChannelSource(cancellationContext); // ignore mayUseSecondary
        }

        public IChannelSourceHandle GetWriteChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary, OperationCancellationContext cancellationContext)
        {
            return GetWriteChannelSource(mayUseSecondary, cancellationContext);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return Task.FromResult(GetChannelSourceHelper());
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationCancellationContext cancellationContext)
        {
            return GetWriteChannelSourceAsync(cancellationContext);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IMayUseSecondaryCriteria mayUseSecondary, OperationCancellationContext cancellationContext)
        {
            return GetWriteChannelSourceAsync(cancellationContext); // ignore mayUseSecondary
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary, OperationCancellationContext cancellationContext)
        {
            return GetWriteChannelSourceAsync(mayUseSecondary, cancellationContext);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _channelSource.Dispose();
                _session.Dispose();
                _disposed = true;
            }
        }

        private IChannelSourceHandle GetChannelSourceHelper()
        {
            return _channelSource.Fork();
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

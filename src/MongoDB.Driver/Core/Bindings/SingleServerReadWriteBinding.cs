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
    internal sealed class SingleServerReadWriteBinding : IReadWriteBinding
    {
        private bool _disposed;
        private readonly IServer _server;
        private readonly TimeSpan _serverRoundTripTime;
        private readonly ICoreSessionHandle _session;

        public SingleServerReadWriteBinding(IServer server, TimeSpan roundTripTime, ICoreSessionHandle session)
        {
            _server = Ensure.IsNotNull(server, nameof(server));
            _serverRoundTripTime = Ensure.IsGreaterThanZero(roundTripTime, nameof(roundTripTime));
            _session = Ensure.IsNotNull(session, nameof(session));
        }

        public ReadPreference ReadPreference => ReadPreference.Primary;

        public ICoreSessionHandle Session => _session;

        public void Dispose()
        {
            if (!_disposed)
            {
                _session.Dispose();
                _disposed = true;
            }
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return GetChannelSourceHelper();
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return Task.FromResult(GetChannelSourceHelper());
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            return GetReadChannelSource(operationContext);
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            return GetReadChannelSourceAsync(operationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return GetChannelSourceHelper();
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            return GetWriteChannelSource(operationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IMayUseSecondaryCriteria mayUseSecondary)
        {
            return GetWriteChannelSource(operationContext); // ignore mayUseSecondary
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary)
        {
            return GetWriteChannelSource(operationContext, mayUseSecondary);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return Task.FromResult(GetChannelSourceHelper());
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            return GetWriteChannelSourceAsync(operationContext);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IMayUseSecondaryCriteria mayUseSecondary)
        {
            return GetWriteChannelSourceAsync(operationContext); // ignore mayUseSecondary
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary)
        {
            return GetWriteChannelSourceAsync(operationContext, mayUseSecondary);
        }

        private IChannelSourceHandle GetChannelSourceHelper()
        {
            return new ChannelSourceHandle(new ServerChannelSource(_server, _serverRoundTripTime, _session.Fork()));
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

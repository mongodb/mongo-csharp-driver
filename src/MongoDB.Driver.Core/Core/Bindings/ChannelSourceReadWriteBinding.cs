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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    /// <summary>
    /// Represents a read-write binding to a channel source.
    /// </summary>
    public sealed class ChannelSourceReadWriteBinding : IReadWriteBinding
    {
        // fields
        private readonly IChannelSourceHandle _channelSource;
        private bool _disposed;
        private readonly ReadPreference _readPreference;
        private readonly ICoreSessionHandle _session;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelSourceReadWriteBinding" /> class.
        /// </summary>
        /// <param name="channelSource">The channel source.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="session">The session.</param>
        public ChannelSourceReadWriteBinding(IChannelSourceHandle channelSource, ReadPreference readPreference, ICoreSessionHandle session)
        {
            _channelSource = Ensure.IsNotNull(channelSource, nameof(channelSource));
            _readPreference = Ensure.IsNotNull(readPreference, nameof(readPreference));
            _session = Ensure.IsNotNull(session, nameof(session));
        }

        // properties
        /// <inheritdoc/>
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
        }

        /// <inheritdoc/>
        public ICoreSessionHandle Session
        {
            get { return _session; }
        }

        // methods
        /// <inheritdoc/>
        public IChannelSourceHandle GetReadChannelSource(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return GetChannelSourceHelper();
        }

        /// <inheritdoc/>
        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return Task.FromResult(GetChannelSourceHelper());
        }

        /// <inheritdoc />
        public IChannelSourceHandle GetReadChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, CancellationToken cancellationToken)
        {
            return GetReadChannelSource(cancellationToken);
        }

        /// <inheritdoc />
        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, CancellationToken cancellationToken)
        {
            return GetReadChannelSourceAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public IChannelSourceHandle GetWriteChannelSource(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return GetChannelSourceHelper();
        }

        /// <inheritdoc />
        public IChannelSourceHandle GetWriteChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, CancellationToken cancellationToken)
        {
            return GetWriteChannelSource(cancellationToken);
        }

        /// <inheritdoc/>
        public IChannelSourceHandle GetWriteChannelSource(IMayUseSecondaryCriteria mayUseSecondary, CancellationToken cancellationToken)
        {
            return GetWriteChannelSource(cancellationToken); // ignore mayUseSecondary
        }

        /// <inheritdoc />
        public IChannelSourceHandle GetWriteChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary, CancellationToken cancellationToken)
        {
            return GetWriteChannelSource(mayUseSecondary, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return Task.FromResult(GetChannelSourceHelper());
        }

        /// <inheritdoc />
        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, CancellationToken cancellationToken)
        {
            return GetWriteChannelSourceAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IMayUseSecondaryCriteria mayUseSecondary, CancellationToken cancellationToken)
        {
            return GetWriteChannelSourceAsync(cancellationToken); // ignore mayUseSecondary
        }

        /// <inheritdoc />
        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary, CancellationToken cancellationToken)
        {
            return GetWriteChannelSourceAsync(mayUseSecondary, cancellationToken);
        }

        /// <inheritdoc/>
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

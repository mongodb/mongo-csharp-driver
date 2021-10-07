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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    /// <summary>
    /// Represents a write binding to a writable server.
    /// </summary>
    public sealed class WritableServerBinding : IReadWriteBinding
    {
        #region static
        internal static IReadWriteBinding CreateCustomWritableServerBinding(ICluster cluster, ICoreSessionHandle session, IServerSelector serverSelector, ReadPreference readPreference)
        {
            // this is a really special case for operations that consider own rules to determine whether the server is writable or no
            return new WritableServerBinding(cluster, session, serverSelector, readPreference);
        }
        #endregion

        // fields
        private readonly ICluster _cluster;
        private bool _disposed;
        private ReadPreference _readPreference;
        private readonly ICoreSessionHandle _session;
        private IServerSelector _serverSelector;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="WritableServerBinding" /> class.
        /// </summary>
        /// <param name="cluster">The cluster.</param>
        /// <param name="session">The session.</param>
        public WritableServerBinding(ICluster cluster, ICoreSessionHandle session)
            : this(
                cluster,
                session,
                // most write operations must be called for Primary
                WritableServerSelector.Instance,
                ReadPreference.Primary)
        {
        }

        private WritableServerBinding(ICluster cluster, ICoreSessionHandle session, IServerSelector serverSelector, ReadPreference readPreference)
        {
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            _session = Ensure.IsNotNull(session, nameof(session));
            _serverSelector = Ensure.IsNotNull(serverSelector, nameof(serverSelector));
            _readPreference = Ensure.IsNotNull(readPreference, nameof(readPreference));
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
            var server = _cluster.SelectServerAndPinIfNeeded(_session, _serverSelector, cancellationToken);

            return GetChannelSourceHelper(server);
        }

        /// <inheritdoc/>
        public async Task<IChannelSourceHandle> GetReadChannelSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            var server = await _cluster.SelectServerAndPinIfNeededAsync(_session, _serverSelector, cancellationToken).ConfigureAwait(false);
            return GetChannelSourceHelper(server);
        }

        /// <inheritdoc/>
        public IChannelSourceHandle GetWriteChannelSource(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            var server = _cluster.SelectServerAndPinIfNeeded(_session, _serverSelector, cancellationToken);
            return GetChannelSourceHelper(server);
        }

        /// <inheritdoc/>
        public async Task<IChannelSourceHandle> GetWriteChannelSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            var server = await _cluster.SelectServerAndPinIfNeededAsync(_session, _serverSelector, cancellationToken).ConfigureAwait(false);
            return GetChannelSourceHelper(server);
        }

        private IChannelSourceHandle GetChannelSourceHelper(IServer server)
        {
            return new ChannelSourceHandle(new ServerChannelSource(server, _session.Fork()));
        }

        /// <inheritdoc/>
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

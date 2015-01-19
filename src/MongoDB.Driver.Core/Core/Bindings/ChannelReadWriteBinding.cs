/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    /// <summary>
    /// Represents a read-write binding that is bound to a channel.
    /// </summary>
    public sealed class ChannelReadWriteBinding : IReadWriteBinding
    {
        // fields
        private readonly IChannelHandle _channel;
        private bool _disposed;
        private readonly IServer _server;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelReadWriteBinding"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="channel">The channel.</param>
        public ChannelReadWriteBinding(IServer server, IChannelHandle channel)
        {
            _server = Ensure.IsNotNull(server, "server");
            _channel = Ensure.IsNotNull(channel, "channel");
        }

        // properties
        /// <inheritdoc/>
        public ReadPreference ReadPreference
        {
            get { return ReadPreference.Primary; }
        }

        // methods
        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _channel.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        private Task<IChannelSourceHandle> GetChannelSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return Task.FromResult<IChannelSourceHandle>(new ChannelSourceHandle(new ChannelChannelSource(_server, _channel.Fork())));
        }

        /// <inheritdoc/>
        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(CancellationToken cancellationToken)
        {
            return GetChannelSourceAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(CancellationToken cancellationToken)
        {
            return GetChannelSourceAsync(cancellationToken);
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

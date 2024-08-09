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
    internal sealed class ChannelReadBinding : IReadBinding
    {
        private readonly IChannelHandle _channel;
        private bool _disposed;
        private readonly ReadPreference _readPreference;
        private readonly IServer _server;
        private readonly ICoreSessionHandle _session;

        public ChannelReadBinding(IServer server, IChannelHandle channel, ReadPreference readPreference, ICoreSessionHandle session)
        {
            _server = Ensure.IsNotNull(server, nameof(server));
            _channel = Ensure.IsNotNull(channel, nameof(channel));
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _channel.Dispose();
                _session.Dispose();
                _disposed = true;
            }
        }

        public IChannelSourceHandle GetReadChannelSource(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return GetReadChannelSourceHelper();
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return Task.FromResult<IChannelSourceHandle>(GetReadChannelSourceHelper());
        }

        public IChannelSourceHandle GetReadChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, CancellationToken cancellationToken)
        {
            return GetReadChannelSource(cancellationToken);
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, CancellationToken cancellationToken)
        {
            return GetReadChannelSourceAsync(cancellationToken);
        }

        private IChannelSourceHandle GetReadChannelSourceHelper()
        {
            return new ChannelSourceHandle(new ChannelChannelSource(_server, _channel.Fork(), _session.Fork()));
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

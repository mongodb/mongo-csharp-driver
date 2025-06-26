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
    internal sealed class ChannelReadBinding : IReadBinding
    {
        private readonly IChannelHandle _channel;
        private bool _disposed;
        private readonly ReadPreference _readPreference;
        private readonly IServer _server;
        private readonly TimeSpan _roundTripTime;
        private readonly ICoreSessionHandle _session;

        public ChannelReadBinding(IServer server, TimeSpan roundTripTime, IChannelHandle channel, ReadPreference readPreference, ICoreSessionHandle session)
        {
            _server = Ensure.IsNotNull(server, nameof(server));
            _roundTripTime = Ensure.IsGreaterThanZero(roundTripTime, nameof(roundTripTime));
            _channel = Ensure.IsNotNull(channel, nameof(channel));
            _readPreference = Ensure.IsNotNull(readPreference, nameof(readPreference));
            _session = Ensure.IsNotNull(session, nameof(session));
        }

        public ReadPreference ReadPreference => _readPreference;

        public ICoreSessionHandle Session => _session;

        public void Dispose()
        {
            if (!_disposed)
            {
                _channel.Dispose();
                _session.Dispose();
                _disposed = true;
            }
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return GetReadChannelSourceHelper();
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return Task.FromResult<IChannelSourceHandle>(GetReadChannelSourceHelper());
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            return GetReadChannelSource(operationContext);
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            return GetReadChannelSourceAsync(operationContext);
        }

        private IChannelSourceHandle GetReadChannelSourceHelper()
        {
            return new ChannelSourceHandle(new ChannelChannelSource(_server, _roundTripTime, _channel.Fork(), _session.Fork()));
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

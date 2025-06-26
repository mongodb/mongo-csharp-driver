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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    internal sealed class ChannelChannelSource : IChannelSource
    {
        // fields
        private readonly IChannelHandle _channel;
        private bool _disposed;
        private readonly IServer _server;
        private readonly TimeSpan _roundTripTime;
        private readonly ICoreSessionHandle _session;

        // constructors
        public ChannelChannelSource(IServer server, TimeSpan roundTripTime, IChannelHandle channel, ICoreSessionHandle session)
        {
            _server = Ensure.IsNotNull(server, nameof(server));
            _roundTripTime = Ensure.IsGreaterThanZero(roundTripTime, nameof(roundTripTime));
            _channel = Ensure.IsNotNull(channel, nameof(channel));
            _session = Ensure.IsNotNull(session, nameof(session));
        }

        // properties
        public IServer Server => _server;

        public ServerDescription ServerDescription => _server.Description;

        public TimeSpan RoundTripTime => _roundTripTime;

        public ICoreSessionHandle Session => _session;

        // methods
        public void Dispose()
        {
            if (!_disposed)
            {
                _channel.Dispose();
                _session.Dispose();
                _disposed = true;
            }
        }

        public IChannelHandle GetChannel(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return GetChannelHelper();
        }

        public Task<IChannelHandle> GetChannelAsync(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return Task.FromResult(GetChannelHelper());
        }

        private IChannelHandle GetChannelHelper()
        {
            return _channel.Fork();
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

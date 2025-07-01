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
    internal sealed class ServerChannelSource : IChannelSource
    {
        // fields
        private bool _disposed;
        private readonly IServer _server;
        private readonly ICoreSessionHandle _session;

        // constructors
        public ServerChannelSource(IServer server, ICoreSessionHandle session)
        {
            _server = Ensure.IsNotNull(server, nameof(server));
            _session = Ensure.IsNotNull(session, nameof(session));
        }

        // properties
        public IServer Server => _server;

        public ServerDescription ServerDescription => _server.Description;

        public ICoreSessionHandle Session => _session;

        // methods
        public void Dispose()
        {
            if (!_disposed)
            {
                _session.Dispose();
                _disposed = true;
            }
        }

        public IChannelHandle GetChannel(OperationContext operationContext)
        {
            ThrowIfDisposed();
            var connection = _server.GetConnection(operationContext);
            return new ServerChannel(_server, connection);
        }

        public async Task<IChannelHandle> GetChannelAsync(OperationContext operationContext)
        {
            ThrowIfDisposed();
            var connection = await _server.GetConnectionAsync(operationContext).ConfigureAwait(false);
            return new ServerChannel(_server, connection);
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

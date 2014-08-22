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
    public sealed class ConnectionReadBinding : IReadBinding
    {
        // fields
        private readonly IConnectionHandle _connection;
        private bool _disposed;
        private readonly ReadPreference _readPreference;
        private readonly IServer _server;

        // constructors
        public ConnectionReadBinding(IServer server, IConnectionHandle connection, ReadPreference readPreference)
        {
            _server = Ensure.IsNotNull(server, "server");
            _connection = Ensure.IsNotNull(connection, "connection");
            _readPreference = Ensure.IsNotNull(readPreference, "readPreference");
        }

        // properties
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
        }

        // methods
        public void Dispose()
        {
            if (!_disposed)
            {
                _connection.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public Task<IConnectionSourceHandle> GetReadConnectionSourceAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return Task.FromResult<IConnectionSourceHandle>(new ConnectionSourceHandle(new ConnectionConnectionSource(_server, _connection.Fork())));
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

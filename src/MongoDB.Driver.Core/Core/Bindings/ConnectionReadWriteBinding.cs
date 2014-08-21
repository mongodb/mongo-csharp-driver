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
    public sealed class ConnectionReadWriteBinding : IReadWriteBinding
    {
        // fields
        private readonly IConnectionHandle _connection;
        private bool _disposed;
        private readonly IServer _server;

        // constructors
        public ConnectionReadWriteBinding(IServer server, IConnectionHandle connection)
        {
            _server = Ensure.IsNotNull(server, "server");
            _connection = Ensure.IsNotNull(connection, "connection");
        }

        // properties
        public ReadPreference ReadPreference
        {
            get { return ReadPreference.Primary; }
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

        private Task<IConnectionSourceHandle> GetConnectionSourceAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return Task.FromResult<IConnectionSourceHandle>(new ConnectionSourceHandle(new ConnectionConnectionSource(_server, _connection.Fork())));
        }

        public Task<IConnectionSourceHandle> GetReadConnectionSourceAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return GetConnectionSourceAsync(timeout, cancellationToken);
        }

        public Task<IConnectionSourceHandle> GetWriteConnectionSourceAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return GetConnectionSourceAsync(timeout, cancellationToken);
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

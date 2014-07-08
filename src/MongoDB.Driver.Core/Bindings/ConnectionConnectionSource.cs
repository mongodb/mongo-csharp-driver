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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    public class ConnectionConnectionSource : ConnectionSourceHandle
    {
        // constructors
        public ConnectionConnectionSource(IServer server, IConnection connection)
            : this(new ReferenceCountedConnectionSource(new Implementation(server, connection)))
        {
        }

        private ConnectionConnectionSource(ReferenceCountedConnectionSource wrapped)
            : base(wrapped)
        {
        }

        // methods
        protected override ConnectionSourceHandle CreateNewHandle(ReferenceCountedConnectionSource wrapped)
        {
            return new ConnectionConnectionSource(wrapped);
        }

        // nested types
        private class Implementation : IConnectionSource
        {
            // fields
            private readonly IConnection _connection;
            private bool _disposed;
            private readonly IServer _server;

            // constructors
            public Implementation(
                IServer server,
                IConnection connection)
            {
                _server = Ensure.IsNotNull(server, "server");
                _connection = Ensure.IsNotNull(connection, "connection");
            }

            // properties
            public ServerDescription ServerDescription
            {
                get { return _server.Description; }
            }

            // methods
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _connection.Dispose();
                }
                _disposed = true;
            }

            public IConnectionSource Fork()
            {
                throw new NotSupportedException(); // implemented by the handle
            }

            public Task<IConnection> GetConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return Task.FromResult(_connection.Fork());
            }

            protected void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
        }
    }
}

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
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.ConnectionPools
{
    /// <summary>
    /// Represents a connection pool.
    /// </summary>
    internal class SharedConnectionPool : IConnectionPool
    {
        // fields
        private readonly IConnectionFactory _connectionFactory;
        private readonly List<PooledConnection> _connections = new List<PooledConnection>();
        private bool _disposed;
        private readonly EndPoint _endPoint;
        private readonly object _lock = new object();
        private readonly ServerId _serverId;
        private readonly ConnectionPoolSettings _settings;

        // constructors
        public SharedConnectionPool(
            ServerId serverId,
            EndPoint endPoint,
            ConnectionPoolSettings settings,
            IConnectionFactory connectionFactory)
        {
            _serverId = Ensure.IsNotNull(serverId, "serverId");
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _settings = Ensure.IsNotNull(settings, "settings");
            _connectionFactory = Ensure.IsNotNull(connectionFactory, "connectionFactory");
        }

        // properties
        public ServerId ServerId
        {
            get { return _serverId; }
        }

        // methods
        public async Task<IConnectionHandle> AcquireConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            PooledConnection connection;

            lock (_lock)
            {
                connection = ChooseAvailableConnection();
                if (connection == null)
                {
                    connection = CreateConnection();
                }
            }

            await connection.OpenAsync(timeout, cancellationToken);
            var acquiredConnection = new AcquiredConnection(connection);
            var referenceCountedConnection = new ReferenceCountedConnection(acquiredConnection);
            return new ConnectionHandle(referenceCountedConnection);
        }

        public void Initialize()
        {
            // do nothing
        }

        private PooledConnection ChooseAvailableConnection()
        {
            // if the pool is not full return null (so a new connection will be created)
            if (_connections.Count < _settings.MaxConnections)
            {
                return null;
            }

            // distribute load randomly among the connections
            var index = ThreadStaticRandom.Next(_connections.Count);
            return _connections[index];
        }

        private PooledConnection CreateConnection()
        {
            var connection = _connectionFactory.CreateConnection(_serverId, _endPoint); // will be initialized by caller outside of the lock
            var pooledConnection = new PooledConnection(connection);
            _connections.Add(pooledConnection);
            return pooledConnection;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var connection in _connections)
                {
                    connection.Dispose();
                }
            }
            _disposed = true;
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private class PooledConnection : ConnectionWrapper
        {
            // fields
            private int _referenceCount;

            // constructors
            public PooledConnection(IConnection connection)
                : base(connection)
            {
            }

            // properties
            public int ReferenceCount
            {
                get
                {
                    return Interlocked.CompareExchange(ref _referenceCount, 0, 0);
                }
            }

            // methods
            public void DecrementReferenceCount()
            {
                Interlocked.Decrement(ref _referenceCount);
            }

            public void IncrementReferenceCount()
            {
                Interlocked.Increment(ref _referenceCount);
            }
        }

        private class AcquiredConnection : ConnectionWrapper
        {
            // fields
            private readonly PooledConnection _pooledConnection;

            // constructors
            public AcquiredConnection(PooledConnection wrapped)
                : base(wrapped)
            {
                _pooledConnection = Ensure.IsNotNull(wrapped, "wrapped");
                _pooledConnection.IncrementReferenceCount();
            }

            // methods
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (!Disposed)
                    {
                        _pooledConnection.DecrementReferenceCount();
                    }
                }
                base.Dispose(disposing);
            }
        }
    }
}

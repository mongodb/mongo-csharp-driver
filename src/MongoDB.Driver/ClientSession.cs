/* Copyright 2017 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// A client session.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.IClientSession" />
    internal sealed class ClientSession : IClientSession
    {
        // private fields
        private readonly IMongoClient _client;
        private readonly IClusterClock _clusterClock = new ClusterClock();
        private bool _disposed;
        private bool _isImplicit;
        private readonly IOperationClock _operationClock = new OperationClock();
        private readonly ClientSessionOptions _options;
        private IServerSession _serverSession;

        // constructors
        internal ClientSession(
            IMongoClient client,
            ClientSessionOptions options,
            IServerSession serverSession,
            bool isImplicit)
        {
            _client = Ensure.IsNotNull(client, nameof(client));
            _options = Ensure.IsNotNull(options, nameof(options));
            _serverSession = Ensure.IsNotNull(serverSession, nameof(serverSession));
            _isImplicit = isImplicit;
        }

        // public properties
        /// <inheritdoc />
        public IMongoClient Client
        {
            get
            {
                ThrowIfDisposed();
                return _client;
            }
        }

        /// <inheritdoc />
        public BsonDocument ClusterTime
        {
            get
            {
                ThrowIfDisposed();
                return _clusterClock.ClusterTime;
            }
        }

        /// <inheritdoc />
        public bool IsImplicit
        {
            get
            {
                ThrowIfDisposed();
                return _isImplicit;
            }
        }

        /// <inheritdoc />
        public BsonTimestamp OperationTime
        {
            get
            {
                ThrowIfDisposed();
                return _operationClock.OperationTime; ;
            }
        }

        /// <inheritdoc />
        public ClientSessionOptions Options
        {
            get
            {
                ThrowIfDisposed();
                return _options;
            }
        }

        /// <inheritdoc />
        public IServerSession ServerSession
        {
            get
            {
                ThrowIfDisposed();
                return _serverSession;
            }
        }

        // public methods
        /// <inheritdoc />
        public void AdvanceClusterTime(BsonDocument newClusterTime)
        {
            Ensure.IsNotNull(newClusterTime, nameof(newClusterTime));
            ThrowIfDisposed();
            _clusterClock.AdvanceClusterTime(newClusterTime);
        }

        /// <inheritdoc />
        public void AdvanceOperationTime(BsonTimestamp newOperationTime)
        {
            Ensure.IsNotNull(newOperationTime, nameof(newOperationTime));
            ThrowIfDisposed();
            _operationClock.AdvanceOperationTime(newOperationTime);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _serverSession.Dispose();
            }
        }

        /// <summary>
        /// Throws if disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}

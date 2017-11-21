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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal sealed class ClientSessionWrappingCoreSession : ICoreSession
    {
        // private fields
        private readonly IClientSession _clientSession;
        private bool _disposed;

        // constructors
        public ClientSessionWrappingCoreSession(IClientSession clientSession)
        {
            _clientSession = Ensure.IsNotNull(clientSession, nameof(clientSession));
        }

        // public properties
        public BsonDocument ClusterTime
        {
            get
            {
                ThrowIfDisposed();
                return _clientSession.ClusterTime;
            }
        }

        public BsonDocument Id
        {
            get
            {
                ThrowIfDisposed();
                return _clientSession.ServerSession.Id;
            }
        }

        public bool IsCausallyConsistent
        {
            get
            {
                ThrowIfDisposed();
                return _clientSession.Options.CausalConsistency.GetValueOrDefault(true);
            }
        }

        public bool IsImplicit
        {
            get
            {
                ThrowIfDisposed();
                return _clientSession.IsImplicit;
            }
        }

        public BsonTimestamp OperationTime
        {
            get
            {
                ThrowIfDisposed();
                return _clientSession.OperationTime;
            }
        }

        public void AdvanceClusterTime(BsonDocument newClusterTime)
        {
            ThrowIfDisposed();
            _clientSession.AdvanceClusterTime(newClusterTime);
        }

        public void AdvanceOperationTime(BsonTimestamp newOperationTime)
        {
            ThrowIfDisposed();
            _clientSession.AdvanceOperationTime(newOperationTime);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _clientSession.Dispose();
            }
            _disposed = true;
        }

        public void WasUsed()
        {
            ThrowIfDisposed();
            _clientSession.ServerSession.WasUsed();
        }

        // private methods
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}

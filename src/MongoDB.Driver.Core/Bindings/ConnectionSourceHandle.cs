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
    internal sealed class ConnectionSourceHandle : IConnectionSourceHandle
    {
        // fields
        private bool _disposed;
        private readonly ReferenceCounted<IConnectionSource> _reference;

        // constructors
        public ConnectionSourceHandle(IConnectionSource connectionSource)
            : this(new ReferenceCounted<IConnectionSource>(connectionSource))
        {
        }

        private ConnectionSourceHandle(ReferenceCounted<IConnectionSource> reference)
        {
            _reference = reference;
        }

        // properties
        public ServerDescription ServerDescription
        {
            get { return _reference.Instance.ServerDescription; }
        }

        // methods
        public Task<IConnectionHandle> GetConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetConnectionAsync(timeout, cancellationToken);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _reference.DecrementReferenceCount();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public IConnectionSourceHandle Fork()
        {
            ThrowIfDisposed();
            _reference.IncrementReferenceCount();
            return new ConnectionSourceHandle(_reference);
        }

        private void ThrowIfDisposed()
        {
            if(_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}

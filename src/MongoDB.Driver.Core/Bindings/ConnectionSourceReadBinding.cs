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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Bindings
{
    public class ConnectionSourceReadBinding : ReadBindingHandle
    {
        // constructors
        public ConnectionSourceReadBinding(IConnectionSource connectionSource, ReadPreference readPreference)
            : base(new ReferenceCountedReadBinding(new Implementation(connectionSource, readPreference)))
        {
        }

        private ConnectionSourceReadBinding(ReferenceCountedReadBinding wrapped)
            : base(wrapped)
        {
        }

        // methods
        protected override ReadBindingHandle CreateNewHandle(ReferenceCountedReadBinding wrapped)
        {
            return new ConnectionSourceReadBinding(wrapped);
        }

        // nested types
        internal class Implementation : IReadBinding
        {
            // fields
            private readonly IConnectionSource _connectionSource;
            private bool _disposed;
            private readonly ReadPreference _readPreference;

            // constructors
            public Implementation(IConnectionSource connectionSource, ReadPreference readPreference)
            {
                _connectionSource = Ensure.IsNotNull(connectionSource, "connectionSource");
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
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _connectionSource.Dispose();
                }
                _disposed = true;
            }

            public IReadBinding Fork()
            {
                throw new NotSupportedException(); // implemented by the handle
            }

            protected Task<IConnectionSource> GetConnectionSourceAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return Task.FromResult<IConnectionSource>(_connectionSource.Fork());
            }

            public Task<IConnectionSource> GetReadConnectionSourceAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                return GetConnectionSourceAsync(timeout, cancellationToken);
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

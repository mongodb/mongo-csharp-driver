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
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    public class ConsistentPrimaryBinding : ReadWriteBindingHandle
    {
        // constructors
        public ConsistentPrimaryBinding(ICluster cluster)
            : this(new ReferenceCountedReadWriteBinding(new Implementation(cluster)))
        {
        }

        private ConsistentPrimaryBinding(ReferenceCountedReadWriteBinding wrapped)
            : base(wrapped)
        {
        }

        // methods
        protected override ReadBindingHandle CreateNewHandle(ReferenceCountedReadBinding wrapped)
        {
            return new ConsistentPrimaryBinding((ReferenceCountedReadWriteBinding)wrapped);
        }

        // nested types
        private class Implementation : IReadWriteBinding
        {
            // fields
            private readonly ICluster _cluster;
            private bool _disposed;
            private readonly object _lock = new object();
            private IServer _primary;

            // constructors
            public Implementation(ICluster cluster)
            {
                _cluster = Ensure.IsNotNull(cluster, "cluster");
            }

            // properties
            public ReadPreference ReadPreference
            {
                get { return ReadPreference.Primary; }
            }

            // methods
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                _disposed = true;
            }

            IReadBinding IReadBinding.Fork()
            {
                throw new NotSupportedException(); // implemented by the handle
            }

            IWriteBinding IWriteBinding.Fork()
            {
                throw new NotSupportedException(); // implemented by the handle
            }

            public IReadWriteBinding Fork()
            {
                throw new NotSupportedException(); // implemented by the handle
            }

            private async Task<IConnectionSource> GetConnectionSourceAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                var primary = await GetConsistentPrimaryAsync(timeout, cancellationToken);
                return new ServerConnectionSource(primary);
            }

            private async Task<IServer> GetConsistentPrimaryAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                IServer primary;
                lock (_lock)
                {
                    primary = _primary;
                }

                if (primary == null)
                {
                    primary = await _cluster.SelectServerAsync(ReadPreferenceServerSelector.Primary, timeout, cancellationToken);

                    lock (_lock)
                    {
                        if (_primary == null)
                        {
                            _primary = primary;
                        }
                        else
                        {
                            primary = _primary;
                        }
                    }
                }

                return primary;
            }

            public Task<IConnectionSource> GetReadConnectionSourceAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                return GetConnectionSourceAsync(timeout, cancellationToken);
            }

            public Task<IConnectionSource> GetWriteConnectionSourceAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
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
}

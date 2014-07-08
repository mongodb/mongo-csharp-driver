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
    public class MonotonicBinding : ReadWriteBindingHandle
    {
        // constructors
        public MonotonicBinding(IReadBinding readBinding, IReadWriteBinding readWriteBinding)
            : this(new ReferenceCountedReadWriteBinding(new Implementation(readBinding, readWriteBinding)))
        {
        }

        private MonotonicBinding(ReferenceCountedReadWriteBinding wrapped)
            : base(wrapped)
        {
        }

        // methods
        protected override ReadBindingHandle CreateNewHandle(ReferenceCountedReadBinding wrapped)
        {
            return new MonotonicBinding((ReferenceCountedReadWriteBinding)wrapped);
        }

        // nested types
        private class Implementation : IReadWriteBinding
        {
            // fields 
            private bool _disposed;
            private readonly object _lock = new object();
            private readonly IReadBinding _readBinding;
            private readonly IReadWriteBinding _readWriteBinding;
            private bool _useReadWriteBinding;

            // constructors
            public Implementation(IReadBinding readBinding, IReadWriteBinding readWriteBinding)
            {
                _readBinding = Ensure.IsNotNull(readBinding, "readBinding");
                _readWriteBinding = Ensure.IsNotNull(readWriteBinding, "readWriteBinding");
            }

            public Implementation(
                ICluster cluster,
                ReadPreference readPreference)
                : this(
                    new ConsistentSecondaryBinding(cluster, readPreference),
                    new ConsistentPrimaryBinding(cluster))
            {
            }

            // properties
            public ReadPreference ReadPreference
            {
                get
                {
                    bool useReadWriteBinding;
                    lock (_lock)
                    {
                        useReadWriteBinding = _useReadWriteBinding;
                    }

                    if (useReadWriteBinding)
                    {
                        return _readWriteBinding.ReadPreference;
                    }
                    else
                    {
                        return _readBinding.ReadPreference;
                    }
                }
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
                    _readBinding.Dispose();
                    _readWriteBinding.Dispose();
                }
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

            public Task<IConnectionSource> GetReadConnectionSourceAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                ThrowIfDisposed();

                bool useReadWriteBinding;
                lock (_lock)
                {
                    useReadWriteBinding = _useReadWriteBinding;
                }

                if (useReadWriteBinding)
                {
                    return _readWriteBinding.GetReadConnectionSourceAsync(timeout, cancellationToken);
                }
                else
                {
                    return _readBinding.GetReadConnectionSourceAsync(timeout, cancellationToken);
                }
            }

            public Task<IConnectionSource> GetWriteConnectionSourceAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                ThrowIfDisposed();

                lock (_lock)
                {
                    _useReadWriteBinding = true;
                }

                return _readWriteBinding.GetWriteConnectionSourceAsync(timeout, cancellationToken);
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

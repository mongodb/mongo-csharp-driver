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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Bindings
{
    public sealed class SplitReadWriteBinding : IReadWriteBinding
    {
        // fields
        private bool _disposed;
        private readonly IReadBinding _readBinding;
        private readonly IWriteBinding _writeBinding;

        // constructors
        public SplitReadWriteBinding(IReadBinding readBinding, IWriteBinding writeBinding)
        {
            _readBinding = Ensure.IsNotNull(readBinding, "readBinding");
            _writeBinding = Ensure.IsNotNull(writeBinding, "writeBinding");
        }

        public SplitReadWriteBinding(ICluster cluster, ReadPreference readPreference)
            : this(new ReadPreferenceBinding(cluster, readPreference), new WritableServerBinding(cluster))
        {
        }

        // properties
        public ReadPreference ReadPreference
        {
            get { return _readBinding.ReadPreference; }
        }

        // methods
        public Task<IConnectionSourceHandle> GetReadConnectionSourceAsync(TimeSpan timeout, System.Threading.CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _readBinding.GetReadConnectionSourceAsync(timeout, cancellationToken);
        }

        public Task<IConnectionSourceHandle> GetWriteConnectionSourceAsync(TimeSpan timeout, System.Threading.CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _writeBinding.GetWriteConnectionSourceAsync(timeout, cancellationToken);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _readBinding.Dispose();
                _writeBinding.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
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

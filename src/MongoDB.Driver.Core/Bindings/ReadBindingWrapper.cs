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
    public abstract class ReadBindingWrapper : IReadBinding
    {
        // fields
        private bool _disposed;
        private readonly bool _ownsWrapped;
        private readonly IReadBinding _wrapped;

        // constructors
        protected ReadBindingWrapper(IReadBinding wrapped, bool ownsWrapped = true)
        {
            _wrapped = Ensure.IsNotNull(wrapped, "wrapped");
            _ownsWrapped = ownsWrapped;
        }

        // properties
        protected bool Disposed
        {
            get { return _disposed; }
        }

        public virtual ReadPreference ReadPreference
        {
            get { return _wrapped.ReadPreference; }
        }

        // methods
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_ownsWrapped)
                    {
                        _wrapped.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        public IReadBinding Fork()
        {
            return ForkImplementation();
        }

        protected virtual IReadBinding ForkImplementation()
        {
            throw new NotSupportedException(); // implemented by the handle
        }

        public virtual Task<IConnectionSource> GetReadConnectionSourceAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return _wrapped.GetReadConnectionSourceAsync(timeout, cancellationToken);
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

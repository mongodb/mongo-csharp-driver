/* Copyright 2015-present MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    internal sealed class ReadBindingHandle : IReadBindingHandle
    {
        private bool _disposed;
        private readonly ReferenceCounted<IReadBinding> _reference;

        public ReadBindingHandle(IReadBinding readBinding)
            : this(new ReferenceCounted<IReadBinding>(readBinding))
        {
        }

        private ReadBindingHandle(ReferenceCounted<IReadBinding> reference)
        {
            _reference = reference;
        }

        public ReadPreference ReadPreference
        {
            get { return _reference.Instance.ReadPreference; }
        }

        public ICoreSessionHandle Session
        {
            get { return _reference.Instance.Session; }
        }

        public TokenBucket TokenBucket
        {
            get { return _reference.Instance.TokenBucket; }
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetReadChannelSource(operationContext);
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetReadChannelSourceAsync(operationContext);
        }

        public IChannelSourceHandle GetReadChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetReadChannelSource(operationContext, deprioritizedServers);
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetReadChannelSourceAsync(operationContext, deprioritizedServers);
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

        public IReadBindingHandle Fork()
        {
            ThrowIfDisposed();
            _reference.IncrementReferenceCount();
            return new ReadBindingHandle(_reference);
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

/* Copyright 2010-present MongoDB Inc.
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
    internal sealed class ReadWriteBindingHandle : IReadWriteBindingHandle
    {
        private bool _disposed;
        private readonly ReferenceCounted<IReadWriteBinding> _reference;

        public ReadWriteBindingHandle(IReadWriteBinding writeBinding)
            : this(new ReferenceCounted<IReadWriteBinding>(writeBinding))
        {
        }

        private ReadWriteBindingHandle(ReferenceCounted<IReadWriteBinding> reference)
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

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSource(operationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSource(operationContext, deprioritizedServers);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IMayUseSecondaryCriteria mayUseSecondary)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSource(operationContext, mayUseSecondary);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSource(operationContext, deprioritizedServers, mayUseSecondary);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSourceAsync(operationContext);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSourceAsync(operationContext, deprioritizedServers);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IMayUseSecondaryCriteria mayUseSecondary)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSourceAsync(operationContext, mayUseSecondary);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSourceAsync(operationContext, deprioritizedServers, mayUseSecondary);
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

        public IReadWriteBindingHandle Fork()
        {
            ThrowIfDisposed();
            _reference.IncrementReferenceCount();
            return new ReadWriteBindingHandle(_reference);
        }

        IReadBindingHandle IReadBindingHandle.Fork()
        {
            return Fork();
        }

        IWriteBindingHandle IWriteBindingHandle.Fork()
        {
            return Fork();
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

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

        public IChannelSourceHandle GetReadChannelSource(OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetReadChannelSource(cancellationContext);
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetReadChannelSourceAsync(cancellationContext);
        }

        public IChannelSourceHandle GetReadChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetReadChannelSource(deprioritizedServers, cancellationContext);
        }

        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetReadChannelSourceAsync(deprioritizedServers, cancellationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSource(cancellationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSource(deprioritizedServers, cancellationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(IMayUseSecondaryCriteria mayUseSecondary, OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSource(mayUseSecondary, cancellationContext);
        }

        public IChannelSourceHandle GetWriteChannelSource(IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary, OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSource(deprioritizedServers, mayUseSecondary, cancellationContext);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSourceAsync(cancellationContext);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSourceAsync(deprioritizedServers, cancellationContext);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IMayUseSecondaryCriteria mayUseSecondary, OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSourceAsync(mayUseSecondary, cancellationContext);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(IReadOnlyCollection<ServerDescription> deprioritizedServers, IMayUseSecondaryCriteria mayUseSecondary, OperationCancellationContext cancellationContext)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSourceAsync(deprioritizedServers, mayUseSecondary, cancellationContext);
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

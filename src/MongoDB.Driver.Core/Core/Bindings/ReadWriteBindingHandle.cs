using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Bindings
{
    public sealed class ReadWriteBindingHandle : IReadWriteBindingHandle
    {
        // fields
        private bool _disposed;
        private readonly ReferenceCounted<IReadWriteBinding> _reference;

        // constructors
        public ReadWriteBindingHandle(IReadWriteBinding writeBinding)
            : this(new ReferenceCounted<IReadWriteBinding>(writeBinding))
        {
        }

        private ReadWriteBindingHandle(ReferenceCounted<IReadWriteBinding> reference)
        {
            _reference = reference;
        }

        // properties
        public ReadPreference ReadPreference
        {
            get { return _reference.Instance.ReadPreference; }
        }

        // methods
        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetReadChannelSourceAsync(cancellationToken);
        }

        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSourceAsync(cancellationToken);
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

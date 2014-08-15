using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Bindings
{
    public sealed class WriteBindingHandle : IWriteBindingHandle
    {
        // fields
        private bool _disposed;
        private readonly ReferenceCounted<IWriteBinding> _reference;

        // constructors
        public WriteBindingHandle(IWriteBinding writeBinding)
            : this(new ReferenceCounted<IWriteBinding>(writeBinding))
        {
        }

        private WriteBindingHandle(ReferenceCounted<IWriteBinding> reference)
        {
            _reference = reference;
            _reference.IncrementReferenceCount();
        }

        public Task<IConnectionSourceHandle> GetWriteConnectionSourceAsync(TimeSpan timeout, System.Threading.CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteConnectionSourceAsync(timeout, cancellationToken);
        }

        public void Dispose()
        {
            if(!_disposed)
            {
                _reference.DecrementReferenceCount();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public IWriteBindingHandle Fork()
        {
            ThrowIfDisposed();
            return new WriteBindingHandle(_reference);
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

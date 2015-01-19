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
    /// <summary>
    /// Represents a handle to a read-write binding.
    /// </summary>
    public sealed class ReadWriteBindingHandle : IReadWriteBindingHandle
    {
        // fields
        private bool _disposed;
        private readonly ReferenceCounted<IReadWriteBinding> _reference;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadWriteBindingHandle"/> class.
        /// </summary>
        /// <param name="writeBinding">The write binding.</param>
        public ReadWriteBindingHandle(IReadWriteBinding writeBinding)
            : this(new ReferenceCounted<IReadWriteBinding>(writeBinding))
        {
        }

        private ReadWriteBindingHandle(ReferenceCounted<IReadWriteBinding> reference)
        {
            _reference = reference;
        }

        // properties
        /// <inheritdoc/>
        public ReadPreference ReadPreference
        {
            get { return _reference.Instance.ReadPreference; }
        }

        // methods
        /// <inheritdoc/>
        public Task<IChannelSourceHandle> GetReadChannelSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetReadChannelSourceAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IChannelSourceHandle> GetWriteChannelSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _reference.Instance.GetWriteChannelSourceAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _reference.DecrementReferenceCount();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <inheritdoc/>
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

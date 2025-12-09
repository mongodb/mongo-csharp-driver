/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Bindings
{
    /// <summary>
    /// An abstract base class for a core session that wraps another core session.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.Core.Bindings.ICoreSession" />
    public abstract class WrappingCoreSession : ICoreSession, ICoreSessionInternal
    {
        // private fields
        private bool _disposed;
        private readonly bool _ownsWrapped;
        private readonly ICoreSession _wrapped;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="WrappingCoreSession" /> class.
        /// </summary>
        /// <param name="wrapped">The wrapped.</param>
        /// <param name="ownsWrapped">if set to <c>true</c> [owns wrapped].</param>
        public WrappingCoreSession(ICoreSession wrapped, bool ownsWrapped)
        {
            _wrapped = Ensure.IsNotNull(wrapped, nameof(wrapped));
            _ownsWrapped = ownsWrapped;
        }

        // public properties
        /// <inheritdoc />
        public virtual BsonDocument ClusterTime
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.ClusterTime;
            }
        }

        /// <inheritdoc />
        public virtual CoreTransaction CurrentTransaction
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.CurrentTransaction;
            }
        }

        /// <inheritdoc />
        public virtual BsonDocument Id
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.Id;
            }
        }

        /// <inheritdoc />
        public virtual bool IsDirty
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.IsDirty;
            }
        }

        /// <inheritdoc />
        public virtual bool IsCausallyConsistent
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.IsCausallyConsistent;
            }
        }

        /// <inheritdoc />
        public virtual bool IsImplicit
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.IsImplicit;
            }
        }

        /// <inheritdoc />
        public virtual bool IsInTransaction
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.IsInTransaction;
            }
        }

        /// <inheritdoc />
        public bool IsSnapshot
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.IsSnapshot;
            }
        }

        /// <inheritdoc />
        public virtual BsonTimestamp OperationTime
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.OperationTime;
            }
        }

        /// <inheritdoc />
        public virtual CoreSessionOptions Options
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.Options;
            }
        }

        /// <inheritdoc />
        public virtual ICoreServerSession ServerSession
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.ServerSession;
            }
        }

        /// <inheritdoc />
        public BsonTimestamp SnapshotTime
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.SnapshotTime;
            }
        }

        /// <summary>
        /// Gets the wrapped session.
        /// </summary>
        /// <value>
        /// The wrapped session.
        /// </value>
        public ICoreSession Wrapped
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped;
            }
        }

        // public methods
        /// <inheritdoc />
        public virtual void AbortTransaction(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            _wrapped.AbortTransaction(cancellationToken);
        }

        // TODO: CSOT: Make it public when CSOT will be ready for GA and add default value to cancellationToken parameter.
        void ICoreSessionInternal.AbortTransaction(AbortTransactionOptions options, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _wrapped.AbortTransaction(options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task AbortTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _wrapped.AbortTransactionAsync(cancellationToken);
        }

        // TODO: CSOT: Make it public when CSOT will be ready for GA and add default value to cancellationToken parameter.
        Task ICoreSessionInternal.AbortTransactionAsync(AbortTransactionOptions options, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _wrapped.AbortTransactionAsync(options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual void AboutToSendCommand()
        {
            ThrowIfDisposed();
            _wrapped.AboutToSendCommand();
        }

        /// <inheritdoc />
        public virtual void AdvanceClusterTime(BsonDocument newClusterTime)
        {
            ThrowIfDisposed();
            _wrapped.AdvanceClusterTime(newClusterTime);
        }

        /// <inheritdoc />
        public virtual void AdvanceOperationTime(BsonTimestamp newOperationTime)
        {
            ThrowIfDisposed();
            _wrapped.AdvanceOperationTime(newOperationTime);
        }

        /// <inheritdoc />
        public long AdvanceTransactionNumber()
        {
            return _wrapped.AdvanceTransactionNumber();
        }

        /// <inheritdoc />
        public virtual void CommitTransaction(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            _wrapped.CommitTransaction(cancellationToken);
        }


        // TODO: CSOT: Make it public when CSOT will be ready for GA and add default value to cancellationToken parameter.
        void ICoreSessionInternal.CommitTransaction(CommitTransactionOptions options, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            _wrapped.CommitTransaction(options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _wrapped.CommitTransactionAsync(cancellationToken);
        }

        // TODO: CSOT: Make it public when CSOT will be ready for GA and add default value to cancellationToken parameter.
        Task ICoreSessionInternal.CommitTransactionAsync(CommitTransactionOptions options, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _wrapped.CommitTransactionAsync(options, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public virtual void MarkDirty()
        {
            ThrowIfDisposed();
            _wrapped.MarkDirty();
        }

        /// <inheritdoc />
        public virtual void StartTransaction(TransactionOptions transactionOptions = null)
        {
            ThrowIfDisposed();
            _wrapped.StartTransaction(transactionOptions);
        }

        void ICoreSessionInternal.StartTransaction(TransactionOptions transactionOptions, bool isTracingEnabled)
        {
            ThrowIfDisposed();
            ((ICoreSessionInternal)_wrapped).StartTransaction(transactionOptions, isTracingEnabled);
        }

        /// <inheritdoc />
        public void SetSnapshotTimeIfNeeded(BsonTimestamp snapshotTime)
        {
            ThrowIfDisposed();
            _wrapped.SetSnapshotTimeIfNeeded(snapshotTime);
        }

        /// <inheritdoc />
        public virtual void WasUsed()
        {
            ThrowIfDisposed();
            _wrapped.WasUsed();
        }

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
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

        /// <summary>
        /// Determines whether this instance is disposed.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </returns>
        protected bool IsDisposed() => _disposed;

        /// <summary>
        /// Throws if disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}

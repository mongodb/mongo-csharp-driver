/* Copyright 2017 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Bindings
{
    /// <summary>
    /// An abstract base class for a core session that wraps another core session.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.Core.Bindings.ICoreSession" />
    public abstract class WrappingCoreSession : ICoreSession
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
        public virtual BsonDocument Id
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.Id;
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
        public virtual BsonTimestamp OperationTime
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.OperationTime;
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public virtual void WasUsed()
        {
            ThrowIfDisposed();
            _wrapped.WasUsed();
        }

        // protected methods
        /// <inheritdoc />
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

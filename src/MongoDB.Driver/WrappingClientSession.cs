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

namespace MongoDB.Driver
{
    /// <summary>
    /// A base class for classes that wrap a client session.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.IClientSession" />
    internal abstract class WrappingClientSession : IClientSession
    {
        // private fields
        private bool _disposed;
        private readonly bool _ownsWrapped;
        private readonly IClientSession _wrapped;

        // constructors
        public WrappingClientSession(IClientSession wrapped, bool ownsWrapped)
        {
            _wrapped = Ensure.IsNotNull(wrapped, nameof(wrapped));
            _ownsWrapped = ownsWrapped;
        }

        // public properties
        /// <inheritdoc />
        public IMongoClient Client
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.Client;
            }
        }

        /// <inheritdoc />
        public BsonDocument ClusterTime
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.ClusterTime;
            }
        }

        /// <inheritdoc />
        public bool IsImplicit
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.IsImplicit;
            }
        }

        /// <inheritdoc />
        public BsonTimestamp OperationTime
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.OperationTime;
            }
        }

        /// <inheritdoc />
        public ClientSessionOptions Options
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.Options;
            }
        }

        /// <inheritdoc />
        public IServerSession ServerSession
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.ServerSession;
            }
        }

        /// <summary>
        /// Gets the wrapped session.
        /// </summary>
        /// <value>
        /// The wrapped session.
        /// </value>
        public IClientSession Wrapped
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped;
            }
        }

        // public methods
        /// <inheritdoc />
        public void AdvanceClusterTime(BsonDocument newClusterTime)
        {
            ThrowIfDisposed();
            _wrapped.AdvanceClusterTime(newClusterTime);
        }

        /// <inheritdoc />
        public void AdvanceOperationTime(BsonTimestamp newOperationTime)
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
        protected virtual void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}

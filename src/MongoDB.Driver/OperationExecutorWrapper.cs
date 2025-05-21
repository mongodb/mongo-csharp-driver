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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// Wraps provided IOperationExecutor to preserve it from disposal.
    /// Used by MongoClient when IOperationExecutor is injected from outside.
    /// </summary>
    internal sealed class OperationExecutorWrapper : IOperationExecutor
    {
        private readonly IOperationExecutor _wrappedExecutor;
        private bool _isDisposed;

        public OperationExecutorWrapper(IOperationExecutor executor)
        {
            _wrappedExecutor = executor;
        }

        public void Dispose()
        {
            _isDisposed = true;
        }

        public TResult ExecuteReadOperation<TResult>(
            IReadOperation<TResult> operation,
            ReadOperationOptions options,
            IClientSessionHandle session = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _wrappedExecutor.ExecuteReadOperation(operation, options, session, cancellationToken);
        }

        public Task<TResult> ExecuteReadOperationAsync<TResult>(
            IReadOperation<TResult> operation,
            ReadOperationOptions options,
            IClientSessionHandle session = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _wrappedExecutor.ExecuteReadOperationAsync(operation, options, session, cancellationToken);
        }

        public TResult ExecuteWriteOperation<TResult>(
            IWriteOperation<TResult> operation,
            WriteOperationOptions options,
            IClientSessionHandle session = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _wrappedExecutor.ExecuteWriteOperation(operation, options, session, cancellationToken);
        }

        public Task<TResult> ExecuteWriteOperationAsync<TResult>(
            IWriteOperation<TResult> operation,
            WriteOperationOptions options,
            IClientSessionHandle session = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _wrappedExecutor.ExecuteWriteOperationAsync(operation, options, session, cancellationToken);
        }

        public IClientSessionHandle StartImplicitSession(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _wrappedExecutor.StartImplicitSession(cancellationToken);
        }

        public Task<IClientSessionHandle> StartImplicitSessionAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _wrappedExecutor.StartImplicitSessionAsync(cancellationToken);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(OperationExecutorWrapper));
            }
        }
    }
}

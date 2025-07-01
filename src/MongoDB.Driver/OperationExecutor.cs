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
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    internal sealed class OperationExecutor : IOperationExecutor
    {
        private readonly IMongoClient _client;
        private bool _isDisposed;

        public OperationExecutor(IMongoClient client)
        {
            _client = client;
        }

        public void Dispose()
        {
            _isDisposed = true;
        }

        public TResult ExecuteReadOperation<TResult>(
            IClientSessionHandle session,
            IReadOperation<TResult> operation,
            ReadOperationOptions options,
            bool allowChannelPinning,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(operation, nameof(operation));
            Ensure.IsNotNull(options, nameof(options));
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();

            using var operationContext = options.ToOperationContext(cancellationToken);
            var readPreference = options.GetEffectiveReadPreference(session);
            using var binding = CreateReadBinding(session, readPreference, allowChannelPinning);
            return operation.Execute(operationContext, binding);
        }

        public async Task<TResult> ExecuteReadOperationAsync<TResult>(
            IClientSessionHandle session,
            IReadOperation<TResult> operation,
            ReadOperationOptions options,
            bool allowChannelPinning,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(operation, nameof(operation));
            Ensure.IsNotNull(options, nameof(options));
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();

            using var operationContext = options.ToOperationContext(cancellationToken);
            var readPreference = options.GetEffectiveReadPreference(session);
            using var binding = CreateReadBinding(session, readPreference, allowChannelPinning);
            return await operation.ExecuteAsync(operationContext, binding).ConfigureAwait(false);
        }

        public TResult ExecuteWriteOperation<TResult>(
            IClientSessionHandle session,
            IWriteOperation<TResult> operation,
            WriteOperationOptions options,
            bool allowChannelPinning,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(operation, nameof(operation));
            Ensure.IsNotNull(options, nameof(options));
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();

            using var operationContext = options.ToOperationContext(cancellationToken);
            using var binding = CreateReadWriteBinding(session, allowChannelPinning);
            return operation.Execute(operationContext, binding);
        }

        public async Task<TResult> ExecuteWriteOperationAsync<TResult>(
            IClientSessionHandle session,
            IWriteOperation<TResult> operation,
            WriteOperationOptions options,
            bool allowChannelPinning,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(operation, nameof(operation));
            Ensure.IsNotNull(options, nameof(options));
            Ensure.IsNotNull(session, nameof(session));
            ThrowIfDisposed();

            using var operationContext = options.ToOperationContext(cancellationToken);
            using var binding = CreateReadWriteBinding(session, allowChannelPinning);
            return await operation.ExecuteAsync(operationContext, binding).ConfigureAwait(false);
        }

        public IClientSessionHandle StartImplicitSession()
        {
            ThrowIfDisposed();
            var options = new ClientSessionOptions { CausalConsistency = false, Snapshot = false };
            var coreSession = _client.GetClusterInternal().StartSession(options.ToCore(isImplicit: true));
            return new ClientSessionHandle(_client, options, coreSession);
        }

        private IReadBindingHandle CreateReadBinding(IClientSessionHandle session, ReadPreference readPreference, bool allowChannelPinning)
        {
            if (session.IsInTransaction && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary)
            {
                throw new InvalidOperationException("Read preference in a transaction must be primary.");
            }

            if (allowChannelPinning)
            {
                return ChannelPinningHelper.CreateReadBinding(_client.GetClusterInternal(), session.WrappedCoreSession.Fork(), readPreference);
            }

            var binding = new ReadPreferenceBinding(_client.GetClusterInternal(), readPreference, session.WrappedCoreSession.Fork());
            return new ReadBindingHandle(binding);
        }

        private IReadWriteBindingHandle CreateReadWriteBinding(IClientSessionHandle session, bool allowChannelPinning)
        {
            if (allowChannelPinning)
            {
                return ChannelPinningHelper.CreateReadWriteBinding(_client.GetClusterInternal(), session.WrappedCoreSession.Fork());
            }

            var binding = new WritableServerBinding(_client.GetClusterInternal(), session.WrappedCoreSession.Fork());
            return new ReadWriteBindingHandle(binding);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(OperationExecutor));
            }
        }
    }
}

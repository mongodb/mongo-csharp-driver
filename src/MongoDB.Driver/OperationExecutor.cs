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
using System.Diagnostics;
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
            OperationContext operationContext,
            IReadOperation<TResult> operation,
            ReadPreference readPreference,
            bool allowChannelPinning)
        {
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            Ensure.IsNotNull(operation, nameof(operation));
            Ensure.IsNotNull(readPreference, nameof(readPreference));
            ThrowIfDisposed();

            using var transactionActivityScope = TransactionActivityScope.CreateIfNeeded(operationContext.Session.CurrentTransaction);
            using var activity = MongoTelemetry.StartOperationActivity(operationContext);

            try
            {
                using var binding = CreateReadBinding(operationContext.Session, readPreference, allowChannelPinning);
                var result = operation.Execute(operationContext, binding);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                MongoTelemetry.RecordException(activity, ex, isOperationLevel: true);
                throw;
            }
        }

        public async Task<TResult> ExecuteReadOperationAsync<TResult>(
            OperationContext operationContext,
            IReadOperation<TResult> operation,
            ReadPreference readPreference,
            bool allowChannelPinning)
        {
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            Ensure.IsNotNull(operation, nameof(operation));
            Ensure.IsNotNull(readPreference, nameof(readPreference));
            ThrowIfDisposed();

            using var transactionActivityScope = TransactionActivityScope.CreateIfNeeded(operationContext.Session.CurrentTransaction);
            using var activity = MongoTelemetry.StartOperationActivity(operationContext);

            try
            {
                using var binding = CreateReadBinding(operationContext.Session, readPreference, allowChannelPinning);
                var result = await operation.ExecuteAsync(operationContext, binding).ConfigureAwait(false);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                MongoTelemetry.RecordException(activity, ex, isOperationLevel: true);
                throw;
            }
        }

        public TResult ExecuteWriteOperation<TResult>(
            OperationContext operationContext,
            IWriteOperation<TResult> operation,
            bool allowChannelPinning)
        {
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            Ensure.IsNotNull(operation, nameof(operation));
            ThrowIfDisposed();

            using var transactionActivityScope = TransactionActivityScope.CreateIfNeeded(operationContext.Session.CurrentTransaction);
            using var activity = MongoTelemetry.StartOperationActivity(operationContext);

            try
            {
                using var binding = CreateReadWriteBinding(operationContext.Session, allowChannelPinning);
                var result = operation.Execute(operationContext, binding);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                MongoTelemetry.RecordException(activity, ex, isOperationLevel: true);
                throw;
            }
        }

        public async Task<TResult> ExecuteWriteOperationAsync<TResult>(
            OperationContext operationContext,
            IWriteOperation<TResult> operation,
            bool allowChannelPinning)
        {
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            Ensure.IsNotNull(operation, nameof(operation));
            ThrowIfDisposed();

            using var transactionActivityScope = TransactionActivityScope.CreateIfNeeded(operationContext.Session.CurrentTransaction);
            using var activity = MongoTelemetry.StartOperationActivity(operationContext);

            try
            {
                using var binding = CreateReadWriteBinding(operationContext.Session, allowChannelPinning);
                var result = await operation.ExecuteAsync(operationContext, binding).ConfigureAwait(false);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                MongoTelemetry.RecordException(activity, ex, isOperationLevel: true);
                throw;
            }
        }

        public IClientSessionHandle StartImplicitSession()
        {
            ThrowIfDisposed();
            var options = new ClientSessionOptions { CausalConsistency = false, Snapshot = false };
            var coreSession = _client.GetClusterInternal().StartSession(options.ToCore(
                isImplicit: true,
                maxAdaptiveRetries: _client.Settings.MaxAdaptiveRetries,
                enableOverloadRetargeting: _client.Settings.EnableOverloadRetargeting));
            return new ClientSessionHandle(_client, options, coreSession);
        }

        private IReadBindingHandle CreateReadBinding(ICoreSessionHandle session, ReadPreference readPreference, bool allowChannelPinning)
        {
            if (session.IsInTransaction && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary)
            {
                throw new InvalidOperationException("Read preference in a transaction must be primary.");
            }

            if (allowChannelPinning)
            {
                return ChannelPinningHelper.CreateReadBinding(_client.GetClusterInternal(), session, readPreference);
            }

            var binding = new ReadPreferenceBinding(_client.GetClusterInternal(), readPreference);
            return new ReadBindingHandle(binding);
        }

        private IReadWriteBindingHandle CreateReadWriteBinding(ICoreSessionHandle session, bool allowChannelPinning)
        {
            if (allowChannelPinning)
            {
                return ChannelPinningHelper.CreateReadWriteBinding(_client.GetClusterInternal(), session);
            }

            var binding = new WritableServerBinding(_client.GetClusterInternal());
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

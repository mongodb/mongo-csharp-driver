/* Copyright 2013-present MongoDB Inc.
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

using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class ReadCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>, IReadOperation<TCommandResult>, IRetryableReadOperation<TCommandResult>
    {
        private bool _retryRequested;

        public ReadCommandOperation(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
            : base(databaseNamespace, command, resultSerializer, messageEncoderSettings)
        {
        }

        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        public TCommandResult Execute(IReadBinding binding, OperationCancellationContext cancellationContext)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = RetryableReadContext.Create(binding, _retryRequested, cancellationContext))
            {
                return Execute(context, cancellationContext);
            }
        }

        public TCommandResult Execute(RetryableReadContext context, OperationCancellationContext cancellationContext)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginOperation())
            {
                return RetryableReadOperationExecutor.Execute(this, context, cancellationContext);
            }
        }

        public async Task<TCommandResult> ExecuteAsync(IReadBinding binding, OperationCancellationContext cancellationContext)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = await RetryableReadContext.CreateAsync(binding, _retryRequested, cancellationContext).ConfigureAwait(false))
            {
                return await ExecuteAsync(context, cancellationContext).ConfigureAwait(false);
            }
        }

        public async Task<TCommandResult> ExecuteAsync(RetryableReadContext context, OperationCancellationContext cancellationContext)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginOperation())
            {
                return await RetryableReadOperationExecutor.ExecuteAsync(this, context, cancellationContext).ConfigureAwait(false);
            }
        }

        public TCommandResult ExecuteAttempt(RetryableReadContext context, int attempt, long? transactionNumber, OperationCancellationContext cancellationContext)
        {
            return ExecuteProtocol(context.Channel, context.Binding.Session, context.Binding.ReadPreference, cancellationContext.CancellationToken);
        }

        public Task<TCommandResult> ExecuteAttemptAsync(RetryableReadContext context, int attempt, long? transactionNumber, OperationCancellationContext cancellationContext)
        {
            return ExecuteProtocolAsync(context.Channel, context.Binding.Session, context.Binding.ReadPreference, cancellationContext.CancellationToken);
        }
    }
}

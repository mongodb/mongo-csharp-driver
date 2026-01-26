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

using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class WriteCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>, IWriteOperation<TCommandResult>, IRetryableWriteOperation<TCommandResult>
    {
        private ReadPreference _readPreference = ReadPreference.Primary;
        private bool _retryRequested;
        private WriteConcern _writeConcern;

        public WriteCommandOperation(DatabaseNamespace databaseNamespace, BsonDocument command, IBsonSerializer<TCommandResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : base(databaseNamespace, command, resultSerializer, messageEncoderSettings)
        {
        }

        public ReadPreference ReadPreference
        {
            get => _readPreference;
            set => _readPreference = Ensure.IsNotNull(value, nameof(value));
        }

        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        public WriteConcern WriteConcern
        {
            get => _writeConcern;
            set => _writeConcern = value;
        }

        public TCommandResult Execute(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = RetryableWriteContext.Create(operationContext, binding, _retryRequested))
            {
                return Execute(operationContext, context);
            }
        }

        public TCommandResult Execute(OperationContext operationContext, RetryableWriteContext context)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginOperation())
            {
                return RetryableWriteOperationExecutor.Execute(operationContext, this, context);
            }
        }

        public async Task<TCommandResult> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = await RetryableWriteContext.CreateAsync(operationContext, binding, _retryRequested).ConfigureAwait(false))
            {
                return await ExecuteAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        public async Task<TCommandResult> ExecuteAsync(OperationContext operationContext, RetryableWriteContext context)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginOperation())
            {
                return await RetryableWriteOperationExecutor.ExecuteAsync(operationContext, this, context).ConfigureAwait(false);
            }
        }

        public TCommandResult ExecuteAttempt(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            AddTransactionNumberToCommandIfNecessary(transactionNumber);
            return ExecuteProtocol(operationContext, context.ChannelSource, context.Binding.Session, _readPreference);
        }

        public Task<TCommandResult> ExecuteAttemptAsync(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            AddTransactionNumberToCommandIfNecessary(transactionNumber);
            return ExecuteProtocolAsync(operationContext, context.ChannelSource, context.Binding.Session, _readPreference);
        }

        //TODO Not the cleanest, but the easiest for now. With more time, we need to find a way to merge WriteCommandOperation and RetryableWriteCommandOperationBase
        //Maybe the first could be a single command, while the second can have the retryable logic. In this case only the second will be used by the other operations
        private void AddTransactionNumberToCommandIfNecessary(long? transactionNumber)
        {
            if (transactionNumber.HasValue)
            {
                Command["txnNumber"] = transactionNumber.Value;
            }
        }
    }
}

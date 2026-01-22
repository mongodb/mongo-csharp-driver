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
        private readonly string _operationName;
        private bool _retryRequested;

        public ReadCommandOperation(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings,
            string operationName = null)
            : base(databaseNamespace, command, resultSerializer, messageEncoderSettings)
        {
            _operationName = operationName;
        }

        public string OperationName => _operationName;

        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        public TCommandResult Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = RetryableReadContext.Create(operationContext, binding, _retryRequested))
            {
                return Execute(operationContext, context);
            }
        }

        public TCommandResult Execute(OperationContext operationContext, RetryableReadContext context)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginOperation())
            {
                return RetryableReadOperationExecutor.Execute(operationContext, this, context);
            }
        }

        public async Task<TCommandResult> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = await RetryableReadContext.CreateAsync(operationContext, binding, _retryRequested).ConfigureAwait(false))
            {
                return await ExecuteAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        public async Task<TCommandResult> ExecuteAsync(OperationContext operationContext, RetryableReadContext context)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginOperation())
            {
                return await RetryableReadOperationExecutor.ExecuteAsync(operationContext, this, context).ConfigureAwait(false);
            }
        }

        public TCommandResult ExecuteAttempt(OperationContext operationContext, RetryableReadContext context, int attempt, long? transactionNumber)
        {
            return ExecuteProtocol(operationContext, context.Channel, context.Binding.Session, context.Binding.ReadPreference);
        }

        public Task<TCommandResult> ExecuteAttemptAsync(OperationContext operationContext, RetryableReadContext context, int attempt, long? transactionNumber)
        {
            return ExecuteProtocolAsync(operationContext, context.Channel, context.Binding.Session, context.Binding.ReadPreference);
        }
    }
}

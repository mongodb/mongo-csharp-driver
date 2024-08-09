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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a read command operation.
    /// </summary>
    /// <typeparam name="TCommandResult">The type of the command result.</typeparam>
    public class ReadCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>, IReadOperation<TCommandResult>, IRetryableReadOperation<TCommandResult>
    {
        // private fields
        private bool _retryRequested;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCommandOperation{TCommandResult}"/> class.
        /// </summary>
        /// <param name="databaseNamespace">The database namespace.</param>
        /// <param name="command">The command.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public ReadCommandOperation(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
            : base(databaseNamespace, command, resultSerializer, messageEncoderSettings)
        {
        }

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether to retry.
        /// </summary>
        /// <value>Whether to retry.</value>
        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        // public methods
        /// <inheritdoc/>
        public TCommandResult Execute(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = RetryableReadContext.Create(binding, _retryRequested, cancellationToken))
            {
                return Execute(context, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public TCommandResult Execute(RetryableReadContext context, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginOperation())
            {
                return RetryableReadOperationExecutor.Execute(this, context, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<TCommandResult> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = await RetryableReadContext.CreateAsync(binding, _retryRequested, cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<TCommandResult> ExecuteAsync(RetryableReadContext context, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginOperation())
            {
                return await RetryableReadOperationExecutor.ExecuteAsync(this, context, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public TCommandResult ExecuteAttempt(RetryableReadContext context, int attempt, long? transactionNumber, CancellationToken cancellationToken)
        {
            return ExecuteProtocol(context.Channel, context.Binding.Session, context.Binding.ReadPreference, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<TCommandResult> ExecuteAttemptAsync(RetryableReadContext context, int attempt, long? transactionNumber, CancellationToken cancellationToken)
        {
            return ExecuteProtocolAsync(context.Channel, context.Binding.Session, context.Binding.ReadPreference, cancellationToken);
        }
    }
}

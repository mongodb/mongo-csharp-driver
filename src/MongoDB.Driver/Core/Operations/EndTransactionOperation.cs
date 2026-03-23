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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal abstract class EndTransactionOperation : IRetryableWriteOperation<BsonDocument>
    {
        private MessageEncoderSettings _messageEncoderSettings;
        private readonly BsonDocument _recoveryToken;
        private readonly WriteConcern _writeConcern;

        protected EndTransactionOperation(BsonDocument recoveryToken, WriteConcern writeConcern)
        {
            _recoveryToken = recoveryToken;
            _writeConcern = Ensure.IsNotNull(writeConcern, nameof(writeConcern));
        }

        protected EndTransactionOperation(WriteConcern writeConcern)
            : this(recoveryToken: null, writeConcern)
        {
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public WriteConcern WriteConcern => _writeConcern;

        public string OperationName => CommandName;

        // abort/commitTransaction can be retried regardless of the value of retryWrites
        public bool CanBeRetried => true;

        protected abstract string CommandName { get; }

        public BsonDocument Execute(OperationContext operationContext, RetryableWriteContext context)
        {
            return RetryableWriteOperationExecutor.Execute(operationContext, this, context);
        }

        public Task<BsonDocument> ExecuteAsync(OperationContext operationContext, RetryableWriteContext context)
        {
            return RetryableWriteOperationExecutor.ExecuteAsync(operationContext, this, context);
        }

        public virtual BsonDocument ExecuteAttempt(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            var channelSource = context.ChannelSource;
            var channel = context.Channel;

            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, context.Binding.Session.Fork()))
            {
                var operation = CreateWriteOperation(operationContext, GetEffectiveWriteConcern(operationContext, attempt));
                return operation.Execute(operationContext, channelBinding);
            }
        }

        public virtual async Task<BsonDocument> ExecuteAttemptAsync(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            var channelSource = context.ChannelSource;
            var channel = context.Channel;

            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, context.Binding.Session.Fork()))
            {
                var operation = CreateWriteOperation(operationContext, GetEffectiveWriteConcern(operationContext, attempt));
                return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
            }
        }

        internal virtual void OnRetry(RetryableWriteContext context, Exception exception)
        {
        }

        protected virtual WriteConcern GetEffectiveWriteConcern(OperationContext operationContext, int attempt)
        {
            return _writeConcern;
        }

        protected virtual BsonDocument CreateCommand(OperationContext operationContext, WriteConcern writeConcern)
        {
            var effectiveWriteConcern = writeConcern;
            if (operationContext.IsRootContextTimeoutConfigured())
            {
                effectiveWriteConcern = effectiveWriteConcern.With(wTimeout: null);
            }

            return new BsonDocument
            {
                { CommandName, 1 },
                { "writeConcern", () => effectiveWriteConcern.ToBsonDocument(), !effectiveWriteConcern.IsServerDefault },
                { "recoveryToken", _recoveryToken, _recoveryToken != null }
            };
        }

        private IWriteOperation<BsonDocument> CreateWriteOperation(OperationContext operationContext, WriteConcern writeConcern)
        {
            var command = CreateCommand(operationContext, writeConcern);
            return new WriteCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, _messageEncoderSettings, OperationName);
        }
    }

    internal sealed class AbortTransactionOperation : EndTransactionOperation
    {
        public AbortTransactionOperation(BsonDocument recoveryToken, WriteConcern writeConcern)
            : base(recoveryToken, writeConcern)
        {
        }

        public AbortTransactionOperation(WriteConcern writeConcern)
            : base(writeConcern)
        {
        }

        protected override string CommandName => "abortTransaction";

        internal override void OnRetry(RetryableWriteContext context, Exception exception)
        {
            context.Binding.Session.CurrentTransaction?.UnpinAll();
        }
    }

    internal sealed class CommitTransactionOperation : EndTransactionOperation
    {
        private TimeSpan? _maxCommitTime;

        public CommitTransactionOperation(WriteConcern writeConcern)
            : base(writeConcern)
        {
        }

        public CommitTransactionOperation(BsonDocument recoveryToken, WriteConcern writeConcern)
            : base(recoveryToken, writeConcern)
        {
        }

        public TimeSpan? MaxCommitTime
        {
            get => _maxCommitTime;
            set => _maxCommitTime = Ensure.IsNullOrGreaterThanZero(value, nameof(value));
        }

        /// <summary>
        /// When true, the write concern is upgraded to w:majority to prevent the transaction
        /// from being committed twice. Set when a non-overload error occurs during a retry
        /// attempt, or when the user explicitly re-calls commitTransaction.
        /// </summary>
        public bool RequiresMajorityWriteConcern { get; set; }

        protected override string CommandName => "commitTransaction";

        internal override void OnRetry(RetryableWriteContext context, Exception exception)
        {
            TransactionHelper.UnpinServerIfNeededOnRetryableCommitException(context.Binding.Session.CurrentTransaction, exception);

            if (!RetryabilityHelper.IsSystemOverloadedException(exception))
            {
                RequiresMajorityWriteConcern = true;
            }
        }

        public override BsonDocument ExecuteAttempt(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            try
            {
                return base.ExecuteAttempt(operationContext, context, attempt, transactionNumber);
            }
            catch (MongoException exception) when (ShouldReplaceTransientTransactionErrorWithUnknownTransactionCommitResult(exception))
            {
                ReplaceTransientTransactionErrorWithUnknownTransactionCommitResult(exception);
                throw;
            }
        }

        public override async Task<BsonDocument> ExecuteAttemptAsync(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            try
            {
                return await base.ExecuteAttemptAsync(operationContext, context, attempt, transactionNumber).ConfigureAwait(false);
            }
            catch (MongoException exception) when (ShouldReplaceTransientTransactionErrorWithUnknownTransactionCommitResult(exception))
            {
                ReplaceTransientTransactionErrorWithUnknownTransactionCommitResult(exception);
                throw;
            }
        }

        protected override WriteConcern GetEffectiveWriteConcern(OperationContext operationContext, int attempt)
        {
            var writeConcern = base.GetEffectiveWriteConcern(operationContext, attempt);

            if (RequiresMajorityWriteConcern)
            {
                writeConcern = writeConcern.With(mode: "majority");
                if (writeConcern.WTimeout == null && !operationContext.IsRootContextTimeoutConfigured())
                {
                    writeConcern = writeConcern.With(wTimeout: TimeSpan.FromMilliseconds(10000));
                }
            }

            return writeConcern;
        }

        protected override BsonDocument CreateCommand(OperationContext operationContext, WriteConcern writeConcern)
        {
            var command = base.CreateCommand(operationContext, writeConcern);
            if (_maxCommitTime.HasValue && !operationContext.IsRootContextTimeoutConfigured())
            {
                command.Add("maxTimeMS", (long)_maxCommitTime.Value.TotalMilliseconds);
            }
            return command;
        }

        private void ReplaceTransientTransactionErrorWithUnknownTransactionCommitResult(MongoException exception)
        {
            exception.RemoveErrorLabel("TransientTransactionError");
            exception.AddErrorLabel("UnknownTransactionCommitResult");
        }

        private bool ShouldReplaceTransientTransactionErrorWithUnknownTransactionCommitResult(MongoException exception)
        {
            if (exception is MongoConnectionException)
            {
                return true;
            }

            if (exception is MongoNotPrimaryException ||
                exception is MongoNodeIsRecoveringException ||
                exception is MongoExecutionTimeoutException) // MaxTimeMSExpired
            {
                return true;
            }

            var writeConcernException = exception as MongoWriteConcernException;
            if (writeConcernException != null)
            {
                var writeConcernError = writeConcernException.WriteConcernResult.Response?.GetValue("writeConcernError", null)?.AsBsonDocument;
                if (writeConcernError != null)
                {
                    var code = (ServerErrorCode)writeConcernError.GetValue("code", -1).ToInt32();
                    switch (code)
                    {
                        case ServerErrorCode.UnsatisfiableWriteConcern:
                        case ServerErrorCode.UnknownReplWriteConcern:
                            return false;

                        default:
                            return true;
                    }
                }
            }

            return false;
        }
    }
}

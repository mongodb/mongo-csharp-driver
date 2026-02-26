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
    internal abstract class EndTransactionOperation : IReadOperation<BsonDocument>
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

        protected abstract string CommandName { get; }

        public virtual BsonDocument Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var channelSource = binding.GetReadChannelSource(operationContext))
            using (var channel = channelSource.GetChannel(operationContext))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext);
                return operation.Execute(operationContext, channelBinding);
            }
        }

        public virtual async Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var channelSource = await binding.GetReadChannelSourceAsync(operationContext).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext);
                return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
            }
        }

        protected virtual BsonDocument CreateCommand(OperationContext operationContext)
        {
            var writeConcern = _writeConcern;
            if (operationContext.IsRootContextTimeoutConfigured())
            {
                writeConcern = writeConcern.With(wTimeout: null);
            }

            return new BsonDocument
            {
                { CommandName, 1 },
                { "writeConcern", () => _writeConcern.ToBsonDocument(), !writeConcern.IsServerDefault },
                { "recoveryToken", _recoveryToken, _recoveryToken != null }
            };
        }

        private IReadOperation<BsonDocument> CreateOperation(OperationContext operationContext)
        {
            var command = CreateCommand(operationContext);
            return new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, _messageEncoderSettings, OperationName)
            {
                RetryRequested = false
            };
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

        protected override string CommandName => "commitTransaction";

        public override BsonDocument Execute(OperationContext operationContext, IReadBinding binding)
        {
            try
            {
                return base.Execute(operationContext, binding);
            }
            catch (MongoException exception) when (ShouldReplaceTransientTransactionErrorWithUnknownTransactionCommitResult(exception))
            {
                ReplaceTransientTransactionErrorWithUnknownTransactionCommitResult(exception);
                throw;
            }
        }

        public override async Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            try
            {
                return await base.ExecuteAsync(operationContext, binding).ConfigureAwait(false);
            }
            catch (MongoException exception) when (ShouldReplaceTransientTransactionErrorWithUnknownTransactionCommitResult(exception))
            {
                ReplaceTransientTransactionErrorWithUnknownTransactionCommitResult(exception);
                throw;
            }
        }

        protected override BsonDocument CreateCommand(OperationContext operationContext)
        {
            var command = base.CreateCommand(operationContext);
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

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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal abstract class RetryableWriteCommandOperationBase : IWriteOperation<BsonDocument>, IRetryableWriteOperation<BsonDocument>
    {
        private BsonValue _comment;
        private readonly DatabaseNamespace _databaseNamespace;
        private bool _isOrdered = true;
        private int? _maxBatchCount;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private bool _retryRequested;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        public RetryableWriteCommandOperationBase(
            DatabaseNamespace databaseNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public bool IsOrdered
        {
            get { return _isOrdered; }
            set { _isOrdered = value; }
        }

        public int? MaxBatchCount
        {
            get { return _maxBatchCount; }
            set { _maxBatchCount = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public string OperationName => null;

        public bool RetryRequested
        {
            get { return _retryRequested; }
            set { _retryRequested = value; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        public virtual BsonDocument Execute(OperationContext operationContext, IWriteBinding binding)
        {
            using (var context = RetryableWriteContext.Create(operationContext, binding, _retryRequested))
            {
                return Execute(operationContext, context);
            }
        }

        public virtual BsonDocument Execute(OperationContext operationContext, RetryableWriteContext context)
        {
            return RetryableWriteOperationExecutor.Execute(operationContext, this, context);
        }

        public virtual async Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            using (var context = await RetryableWriteContext.CreateAsync(operationContext, binding, _retryRequested).ConfigureAwait(false))
            {
                return await ExecuteAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        public virtual Task<BsonDocument> ExecuteAsync(OperationContext operationContext, RetryableWriteContext context)
        {
            return RetryableWriteOperationExecutor.ExecuteAsync(operationContext, this, context);
        }

        public BsonDocument ExecuteAttempt(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            var args = GetCommandArgs(operationContext, context, attempt, transactionNumber);
            return context.Channel.Command<BsonDocument>(
                operationContext,
                context.ChannelSource.Session,
                ReadPreference.Primary,
                _databaseNamespace,
                args.Command,
                args.CommandPayloads,
                NoOpElementNameValidator.Instance,
                null, // additionalOptions,
                args.PostWriteAction,
                args.ResponseHandling,
                BsonDocumentSerializer.Instance,
                args.MessageEncoderSettings);
        }

        public Task<BsonDocument> ExecuteAttemptAsync(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            var args = GetCommandArgs(operationContext, context, attempt, transactionNumber);
            return context.Channel.CommandAsync<BsonDocument>(
                operationContext,
                context.ChannelSource.Session,
                ReadPreference.Primary,
                _databaseNamespace,
                args.Command,
                args.CommandPayloads,
                NoOpElementNameValidator.Instance,
                null, // additionalOptions,
                args.PostWriteAction,
                args.ResponseHandling,
                BsonDocumentSerializer.Instance,
                args.MessageEncoderSettings);
        }

        protected abstract BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session, int attempt, long? transactionNumber);

        protected abstract IEnumerable<BatchableCommandMessageSection> CreateCommandPayloads(IChannelHandle channel, int attempt);

        private MessageEncoderSettings CreateMessageEncoderSettings(IChannelHandle channel)
        {
            var clone = _messageEncoderSettings.Clone();
            clone.Add(MessageEncoderSettingsName.MaxDocumentSize, channel.ConnectionDescription.MaxDocumentSize);
            clone.Add(MessageEncoderSettingsName.MaxMessageSize, channel.ConnectionDescription.MaxMessageSize);
            clone.Add(MessageEncoderSettingsName.MaxWireDocumentSize, channel.ConnectionDescription.MaxWireDocumentSize);
            return clone;
        }

        private CommandArgs GetCommandArgs(OperationContext operationContext, RetryableWriteContext context, int attempt, long? transactionNumber)
        {
            var args = new CommandArgs();
            args.Command = CreateCommand(operationContext, context.Binding.Session, attempt, transactionNumber);
            args.CommandPayloads = CreateCommandPayloads(context.Channel, attempt).ToList();
            args.PostWriteAction = GetPostWriteAction(args.CommandPayloads);
            args.ResponseHandling = GetResponseHandling();
            args.MessageEncoderSettings = CreateMessageEncoderSettings(context.Channel);
            return args;
        }

        private Action<IMessageEncoderPostProcessor> GetPostWriteAction(List<BatchableCommandMessageSection> commandPayloads)
        {
            if (!_writeConcern.IsAcknowledged && _isOrdered)
            {
                return encoder =>
                {
                    var requestsPayload = commandPayloads.Single();
                    if (!requestsPayload.Documents.AllItemsWereProcessed)
                    {
                        encoder.ChangeWriteConcernFromW0ToW1();
                    }
                };
            }
            else
            {
                return null;
            }
        }

        private CommandResponseHandling GetResponseHandling()
        {
            return _writeConcern.IsAcknowledged ? CommandResponseHandling.Return : CommandResponseHandling.NoResponseExpected;
        }

        // nested types
        private class CommandArgs
        {
            public BsonDocument Command { get; set; }
            public List<BatchableCommandMessageSection> CommandPayloads { get; set; }
            public Action<IMessageEncoderPostProcessor> PostWriteAction { get; set; }
            public CommandResponseHandling ResponseHandling { get; set; }
            public MessageEncoderSettings MessageEncoderSettings { get; set; }
        }
    }
}

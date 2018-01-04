/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a base class for a delete, insert or update command operation.
    /// </summary>
    public abstract class RetryableWriteCommandOperationBase : IWriteOperation<BsonDocument>, IRetryableWriteOperation<BsonDocument>
    {
        // private fields
        private readonly DatabaseNamespace _databaseNamespace;
        private int? _maxBatchCount;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private bool _retryRequested;
        private Func<WriteConcern> _writeConcernFunc = () => WriteConcern.Acknowledged;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryableWriteCommandOperationBase" /> class.
        /// </summary>
        /// <param name="databaseNamespace">The database namespace.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public RetryableWriteCommandOperationBase(
            DatabaseNamespace databaseNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        // public properties
        /// <summary>
        /// Gets the database namespace.
        /// </summary>
        /// <value>
        /// The database namespace.
        /// </value>
        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        /// <summary>
        /// Gets or sets the maximum batch count.
        /// </summary>
        /// <value>
        /// The maximum batch count.
        /// </value>
        public int? MaxBatchCount
        {
            get { return _maxBatchCount; }
            set { _maxBatchCount = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets the message encoder settings.
        /// </summary>
        /// <value>
        /// The message encoder settings.
        /// </value>
        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether retry is enabled for the operation.
        /// </summary>
        /// <value>A value indicating whether retry is enabled.</value>
        public bool RetryRequested
        {
            get { return _retryRequested; }
            set { _retryRequested = value; }
        }

        /// <summary>
        /// Gets or sets the write concern.
        /// </summary>
        /// <value>
        /// The write concern.
        /// </value>
        public Func<WriteConcern> WriteConcernFunc
        {
            get { return _writeConcernFunc; }
            set { _writeConcernFunc = value; }
        }

        // public methods
        /// <inheritdoc />
        public virtual BsonDocument Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            using (var context = RetryableWriteContext.Create(binding, _retryRequested, cancellationToken))
            {
                return Execute(context, cancellationToken);
            }
        }

        /// <inheritdoc />
        public virtual BsonDocument Execute(RetryableWriteContext context, CancellationToken cancellationToken)
        {
            return RetryableWriteOperationExecutor.Execute(this, context, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            using (var context = await RetryableWriteContext.CreateAsync(binding, _retryRequested, cancellationToken).ConfigureAwait(false))
            {
                return Execute(context, cancellationToken);
            }
        }

        /// <inheritdoc />
        public virtual Task<BsonDocument> ExecuteAsync(RetryableWriteContext context, CancellationToken cancellationToken)
        {
            return RetryableWriteOperationExecutor.ExecuteAsync(this, context, cancellationToken);
        }

        /// <inheritdoc />
        public BsonDocument ExecuteAttempt(RetryableWriteContext context, int attempt, long? transactionNumber, CancellationToken cancellationToken)
        {
            var command = CreateCommand(context.Channel.ConnectionDescription, attempt, transactionNumber);
            return context.Channel.Command<BsonDocument>(
                context.ChannelSource.Session,
                ReadPreference.Primary,
                _databaseNamespace,
                command,
                NoOpElementNameValidator.Instance,
                null, // additionalOptions,
                () => CommandResponseHandling.Return,
                false, // slaveOk
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings,
                cancellationToken);
        }

        /// <inheritdoc />
        public Task<BsonDocument> ExecuteAttemptAsync(RetryableWriteContext context, int attempt, long? transactionNumber, CancellationToken cancellationToken)
        {
            var command = CreateCommand(context.Channel.ConnectionDescription, attempt, transactionNumber);
            return context.Channel.CommandAsync<BsonDocument>(
                context.ChannelSource.Session,
                ReadPreference.Primary,
                _databaseNamespace,
                command,
                NoOpElementNameValidator.Instance,
                null, // additionalOptions,
                () => CommandResponseHandling.Return,
                false, // slaveOk
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings,
                cancellationToken);
        }

        // protected methods
        /// <summary>
        /// Creates the command.
        /// </summary>
        /// <param name="connectionDescription">The connection description.</param>
        /// <param name="attempt">The attempt.</param>
        /// <param name="transactionNumber">The transaction number.</param>
        /// <returns>A command.</returns>
        protected abstract BsonDocument CreateCommand(ConnectionDescription connectionDescription, int attempt, long? transactionNumber);
    }
}

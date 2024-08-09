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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents an explainable operation.
    /// </summary>
    public interface IExplainableOperation
    {
        /// <summary>
        /// Creates the command to be explained.
        /// </summary>
        /// <param name="connectionDescription">The connection description.</param>
        /// <param name="session">The session.</param>
        /// <returns>The command.</returns>
        BsonDocument CreateCommand(ConnectionDescription connectionDescription, ICoreSession session);
    }

    /// <summary>
    /// Represents an explain operation.
    /// </summary>
    public class ExplainOperation : IReadOperation<BsonDocument>, IWriteOperation<BsonDocument>
    {
        // fields
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly IExplainableOperation _explainableOperation;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private ExplainVerbosity _verbosity;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ExplainOperation"/> class.
        /// </summary>
        /// <param name="databaseNamespace">The database namespace.</param>
        /// <param name="explainableOperation">The explainable operation.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public ExplainOperation(DatabaseNamespace databaseNamespace, IExplainableOperation explainableOperation, MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _explainableOperation = Ensure.IsNotNull(explainableOperation, nameof(explainableOperation));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
            _verbosity = ExplainVerbosity.QueryPlanner;
        }

        // properties
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
        /// Gets the operation to be explained.
        /// </summary>
        /// <value>
        /// The operation to be explained.
        /// </value>
        public IExplainableOperation ExplainableOperation
        {
            get { return _explainableOperation; }
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
        /// Gets or sets the verbosity.
        /// </summary>
        /// <value>
        /// The verbosity.
        /// </value>
        public ExplainVerbosity Verbosity
        {
            get { return _verbosity; }
            set { _verbosity = value; }
        }

        // public methods
        /// <inheritdoc/>
        public BsonDocument Execute(IReadBinding binding, CancellationToken cancellationToken)
        {
            using (var channelSource = binding.GetReadChannelSource(cancellationToken))
            using (var channel = channelSource.GetChannel(cancellationToken))
            using (var channelBinding = new ChannelReadBinding(channelSource.Server, channel, binding.ReadPreference, binding.Session))
            {
                var operation = CreateReadOperation(channel.ConnectionDescription, binding.Session);
                return operation.Execute(channelBinding, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public BsonDocument Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            using (var channelSource = binding.GetWriteChannelSource(cancellationToken))
            using (var channel = channelSource.GetChannel(cancellationToken))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session))
            {
                var operation = CreateWriteOperation(channel.ConnectionDescription, binding.Session);
                return operation.Execute(channelBinding, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            using (var channelSource = await binding.GetReadChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadBinding(channelSource.Server, channel, binding.ReadPreference, binding.Session))
            {
                var operation = CreateReadOperation(channel.ConnectionDescription, binding.Session);
                return await operation.ExecuteAsync(channelBinding, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session))
            {
                var operation = CreateWriteOperation(channel.ConnectionDescription, binding.Session);
                return await operation.ExecuteAsync(channelBinding, cancellationToken).ConfigureAwait(false);
            }
        }

        // private methods
        private static string ConvertVerbosityToString(ExplainVerbosity verbosity)
        {
            switch (verbosity)
            {
                case ExplainVerbosity.AllPlansExecution:
                    return "allPlansExecution";
                case ExplainVerbosity.ExecutionStats:
                    return "executionStats";
                case ExplainVerbosity.QueryPlanner:
                    return "queryPlanner";
                default:
                    var message = string.Format("Unsupported explain verbosity: {0}.", verbosity.ToString());
                    throw new InvalidOperationException(message);
            }
        }

        internal BsonDocument CreateExplainCommand(ConnectionDescription connectionDescription, ICoreSession session)
        {
            var explainableCommand = _explainableOperation.CreateCommand(connectionDescription, session);
            return new BsonDocument
            {
                { "explain", explainableCommand },
                { "verbosity", ConvertVerbosityToString(_verbosity) }
            };
        }

        private ReadCommandOperation<BsonDocument> CreateReadOperation(ConnectionDescription connectionDescription, ICoreSession session)
        {
            var explainCommand = CreateExplainCommand(connectionDescription, session);
            return new ReadCommandOperation<BsonDocument>(
                _databaseNamespace,
                explainCommand,
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings)
            {
                RetryRequested = false
            };
        }

        private WriteCommandOperation<BsonDocument> CreateWriteOperation(ConnectionDescription connectionDescription, ICoreSession session)
        {
            var explainCommand = CreateExplainCommand(connectionDescription, session);
            return new WriteCommandOperation<BsonDocument>(
                _databaseNamespace,
                explainCommand,
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings);
        }
    }
}

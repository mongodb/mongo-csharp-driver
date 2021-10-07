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
    /// Represents a write command operation.
    /// </summary>
    /// <typeparam name="TCommandResult">The type of the command result.</typeparam>
    public class WriteCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>, IWriteOperation<TCommandResult>
    {
        #region static
        internal static WriteCommandOperation<TCommandResult> CreateWriteCommandOperationWithReadPreference(DatabaseNamespace databaseNamespace, BsonDocument command, IBsonSerializer<TCommandResult> resultSerializer, ReadPreference readPreference, MessageEncoderSettings messageEncoderSettings)
        {
            // this is a really special case for operations that consider own rules to determine whether the server is writable or no
            return new WriteCommandOperation<TCommandResult>(databaseNamespace, command, resultSerializer, readPreference, messageEncoderSettings);
        }
        #endregion

        private readonly ReadPreference _readPreference;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteCommandOperation{TCommandResult}"/> class.
        /// </summary>
        /// <param name="databaseNamespace">The database namespace.</param>
        /// <param name="command">The command.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public WriteCommandOperation(DatabaseNamespace databaseNamespace, BsonDocument command, IBsonSerializer<TCommandResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : this(
                  databaseNamespace,
                  command,
                  resultSerializer,
                  // most of write operations mist be called on Primary
                  ReadPreference.Primary,
                  messageEncoderSettings)
        {
        }

        private WriteCommandOperation(DatabaseNamespace databaseNamespace, BsonDocument command, IBsonSerializer<TCommandResult> resultSerializer, ReadPreference readPreference, MessageEncoderSettings messageEncoderSettings)
            : base(databaseNamespace, command, resultSerializer, messageEncoderSettings)
        {
            _readPreference = Ensure.IsNotNull(readPreference, nameof(readPreference));
        }

        // methods
        /// <inheritdoc/>
        public TCommandResult Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (EventContext.BeginOperation())
            using (var channelSource = binding.GetWriteChannelSource(cancellationToken))
            {
                return ExecuteProtocol(channelSource, binding.Session, _readPreference, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<TCommandResult> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (EventContext.BeginOperation())
            using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteProtocolAsync(channelSource, binding.Session, _readPreference, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

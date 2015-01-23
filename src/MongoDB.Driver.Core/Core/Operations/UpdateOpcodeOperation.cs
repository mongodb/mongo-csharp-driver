/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents an update operation using the update opcode.
    /// </summary>
    public class UpdateOpcodeOperation : IWriteOperation<WriteConcernResult>
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private int? _maxDocumentSize;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly UpdateRequest _request;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateOpcodeOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="request">The request.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public UpdateOpcodeOperation(
            CollectionNamespace collectionNamespace,
            UpdateRequest request,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _request = Ensure.IsNotNull(request, "request");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
        }

        // properties
        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        /// <value>
        /// The collection namespace.
        /// </value>
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        /// <summary>
        /// Gets or sets the maximum size of a document.
        /// </summary>
        /// <value>
        /// The maximum size of a document.
        /// </value>
        public int? MaxDocumentSize
        {
            get { return _maxDocumentSize; }
            set { _maxDocumentSize = Ensure.IsNullOrGreaterThanZero(value, "value"); }
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
        /// Gets the request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        public UpdateRequest Request
        {
            get { return _request; }
        }

        /// <summary>
        /// Gets or sets the write concern.
        /// </summary>
        /// <value>
        /// The write concern.
        /// </value>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        private Task<WriteConcernResult> ExecuteProtocolAsync(IChannelHandle channel, CancellationToken cancellationToken)
        {
            return channel.UpdateAsync(
                _collectionNamespace,
                _messageEncoderSettings,
                _writeConcern,
                _request.Filter,
                _request.Update,
                ElementNameValidatorFactory.ForUpdateType(_request.UpdateType),
                _request.IsMulti,
                _request.IsUpsert,
                cancellationToken);
        }

        private async Task<WriteConcernResult> ExecuteAsync(IChannelHandle channel, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(channel, "channel");

            if (channel.ConnectionDescription.BuildInfoResult.ServerVersion >= new SemanticVersion(2, 6, 0) && _writeConcern.IsAcknowledged)
            {
                var emulator = new UpdateOpcodeOperationEmulator(_collectionNamespace, _request, _messageEncoderSettings)
                {
                    MaxDocumentSize = _maxDocumentSize,
                    WriteConcern = _writeConcern
                };
                return await emulator.ExecuteAsync(channel, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await ExecuteProtocolAsync(channel, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<WriteConcernResult> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");

            using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteAsync(channel, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

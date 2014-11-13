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
    public class UpdateOpcodeOperation : IWriteOperation<WriteConcernResult>
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private int? _maxDocumentSize;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly UpdateRequest _request;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
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
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public int? MaxDocumentSize
        {
            get { return _maxDocumentSize; }
            set { _maxDocumentSize = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public UpdateRequest Request
        {
            get { return _request; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        private UpdateWireProtocol CreateProtocol()
        {
            return new UpdateWireProtocol(
                _collectionNamespace,
                _messageEncoderSettings,
                _writeConcern,
                _request.Criteria,
                _request.Update,
                ElementNameValidatorFactory.ForUpdateType(_request.UpdateType),
                _request.IsMulti,
                _request.IsUpsert);
        }

        public async Task<WriteConcernResult> ExecuteAsync(IConnectionHandle connection, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");

            if (connection.Description.BuildInfoResult.ServerVersion >= new SemanticVersion(2, 6, 0) && _writeConcern.IsAcknowledged)
            {
                var emulator = new UpdateOpcodeOperationEmulator(_collectionNamespace, _request, _messageEncoderSettings)
                {
                    MaxDocumentSize = _maxDocumentSize,
                    WriteConcern = _writeConcern
                };
                return await emulator.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var protocol = CreateProtocol();
                return await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<WriteConcernResult> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var connection = await connectionSource.GetConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

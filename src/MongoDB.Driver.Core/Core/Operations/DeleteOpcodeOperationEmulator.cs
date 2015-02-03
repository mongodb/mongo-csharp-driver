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
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class DeleteOpcodeOperationEmulator
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly DeleteRequest _request;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
        public DeleteOpcodeOperationEmulator(
            CollectionNamespace collectionNamespace,
            DeleteRequest request,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _request = Ensure.IsNotNull(request, "request");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public DeleteRequest Request
        {
            get { return _request; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        public async Task<WriteConcernResult> ExecuteAsync(IChannelHandle channel, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(channel, "channel");

            var requests = new[] { _request };

            var operation = new BulkDeleteOperation(_collectionNamespace, requests, _messageEncoderSettings)
            {
                WriteConcern = _writeConcern
            };

            BulkWriteOperationResult bulkWriteResult;
            MongoBulkWriteOperationException bulkWriteException = null;
            try
            {
                bulkWriteResult = await operation.ExecuteAsync(channel, cancellationToken).ConfigureAwait(false);
            }
            catch (MongoBulkWriteOperationException ex)
            {
                bulkWriteResult = ex.Result;
                bulkWriteException = ex;
            }

            var converter = new BulkWriteOperationResultConverter();
            if (bulkWriteException != null)
            {
                throw converter.ToWriteConcernException(channel.ConnectionDescription.ConnectionId, bulkWriteException);
            }
            else
            {
                if (_writeConcern.IsAcknowledged)
                {
                    return converter.ToWriteConcernResult(bulkWriteResult);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}

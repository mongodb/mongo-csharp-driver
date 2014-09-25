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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    public abstract class WriteWireProtocolBase : IWireProtocol<WriteConcernResult>
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly Func<bool> _shouldSendGetLastError;
        private readonly WriteConcern _writeConcern;

        // constructors
        protected WriteWireProtocolBase(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings,
            WriteConcern writeConcern,
            Func<bool> shouldSendGetLastError = null)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _messageEncoderSettings = messageEncoderSettings;
            _writeConcern = Ensure.IsNotNull(writeConcern, "writeConcern");
            _shouldSendGetLastError = shouldSendGetLastError;
        }

        // properties
        protected CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        protected WriteConcern WriteConcern
        {
            get { return _writeConcern; }
        }

        // methods
        private QueryMessage CreateGetLastErrorMessage()
        {
            if (!_writeConcern.IsAcknowledged)
            {
                return null;
            }

            var command = new BsonDocument 
            {
                { "getLastError", 1 },
                { "w", () => _writeConcern.W.ToBsonValue(), _writeConcern.W != null },
                { "wtimeout", () => _writeConcern.WTimeout.Value.TotalMilliseconds, _writeConcern.WTimeout.HasValue },
                { "fsync", () => _writeConcern.FSync.Value, _writeConcern.FSync.HasValue },
                { "j", () => _writeConcern.Journal.Value, _writeConcern.Journal.HasValue }
            };

            return new QueryMessage(
               RequestMessage.GetNextRequestId(),
               _collectionNamespace.DatabaseNamespace.CommandCollection,
               command,
               null,
               NoOpElementNameValidator.Instance,
               0,
               -1,
               true,
               false,
               false,
               false,
               false,
               _shouldSendGetLastError);
        }

        protected abstract RequestMessage CreateWriteMessage(IConnection connection);

        public async Task<WriteConcernResult> ExecuteAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);

            var writeMessage = CreateWriteMessage(connection);
            var getLastErrorMessage = CreateGetLastErrorMessage();

            var messages = new List<RequestMessage>();
            messages.Add(writeMessage);
            if (getLastErrorMessage != null)
            {
                messages.Add(getLastErrorMessage);
            }

            await connection.SendMessagesAsync(messages, _messageEncoderSettings, slidingTimeout, cancellationToken).ConfigureAwait(false);
            if (getLastErrorMessage != null && getLastErrorMessage.WasSent)
            {
                var reply = await connection.ReceiveMessageAsync<BsonDocument>(getLastErrorMessage.RequestId, BsonDocumentSerializer.Instance, _messageEncoderSettings, slidingTimeout, cancellationToken).ConfigureAwait(false);
                return ProcessReply(reply);
            }
            else
            {
                return null;
            }
        }

        private WriteConcernResult ProcessReply(ReplyMessage<BsonDocument> reply)
        {
            if (reply.NumberReturned == 0)
            {
                throw new WriteProtocolException("GetLastError reply had no documents.");
            }
            if (reply.NumberReturned > 1)
            {
                throw new WriteProtocolException("GetLastError reply had more than one document.");
            }
            if (reply.QueryFailure)
            {
                throw new WriteProtocolException("GetLastError reply had QueryFailure flag set.", reply.QueryFailureDocument);
            }

            var response = reply.Documents.Single();
            var writeConcernResult = new WriteConcernResult(response);

            var mappedException = ExceptionMapper.Map(writeConcernResult);
            if (mappedException != null)
            {
                throw mappedException;
            }

            return writeConcernResult;
        }
    }
}

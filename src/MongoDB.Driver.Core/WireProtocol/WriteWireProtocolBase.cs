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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Exceptions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.WireProtocol
{
    public abstract class WriteWireProtocolBase : IWireProtocol<BsonDocument>
    {
        // fields
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly WriteConcern _writeConcern;

        // constructors
        protected WriteWireProtocolBase(
            string databaseName,
            string collectionName,
            WriteConcern writeConcern)
        {
            _databaseName = Ensure.IsNotNull(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNull(collectionName, "collectionName");
            _writeConcern = Ensure.IsNotNull(writeConcern, "writeConcern");
        }

        // properties
        protected string CollectionName
        {
            get { return _collectionName; }
        }

        protected string DatabaseName
        {
            get { return _databaseName; }
        }

        protected WriteConcern WriteConcern
        {
            get { return _writeConcern; }
        }

        // methods
        private QueryMessage CreateGetLastErrorMessage()
        {
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
               _databaseName,
               "$cmd",
               command,
               null,
               0,
               -1,
               true,
               false,
               false,
               false,
               false);
        }

        protected abstract RequestMessage CreateWriteMessage(IConnection connection);

        public async Task<BsonDocument> ExecuteAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);

            var writeMessage = CreateWriteMessage(connection);
            if (_writeConcern.Equals(WriteConcern.Unacknowledged))
            {
                await connection.SendMessageAsync(writeMessage, slidingTimeout, cancellationToken);
                return null;
            }
            else
            {
                var getLastErrorMessage = CreateGetLastErrorMessage();
                await connection.SendMessagesAsync(new RequestMessage[] { writeMessage, getLastErrorMessage }, slidingTimeout, cancellationToken);
                var reply = await connection.ReceiveMessageAsync<BsonDocument>(getLastErrorMessage.RequestId, BsonDocumentSerializer.Instance, slidingTimeout, cancellationToken);
                return ProcessReply(reply);
            }
        }

        private BsonDocument ProcessReply(ReplyMessage<BsonDocument> reply)
        {
            if (reply.NumberReturned == 0)
            {
                throw new WriteException("GetLastError reply had no documents.");
            }
            if (reply.NumberReturned > 1)
            {
                throw new WriteException("GetLastError reply had more than one document.");
            }
            if (reply.QueryFailure)
            {
                throw new WriteException("GetLastError reply had QueryFailure flag set.", reply.QueryFailureDocument);
            }

            var result = reply.Documents.Single();
            if (!result.Contains("ok"))
            {
                throw new WriteException("GetLastError result has no ok field.", result);
            }
            if (!result["ok"].ToBoolean())
            {
                throw new WriteException("GetLastError result had ok 0.", result);
            }
            if (!result["err"].Equals(BsonNull.Value))
            {
                var message = string.Format("GetLastError detected an error: {0}.", result);
                throw new WriteException(message, result);
            }

            return result;
        }
    }
}

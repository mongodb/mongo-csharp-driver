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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Bindings
{
    public interface IChannel : IDisposable
    {
        ConnectionDescription ConnectionDescription { get;  }

        Task<TResult> CommandAsync<TResult>(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IElementNameValidator commandValidator,
            bool slaveOk,
            IBsonSerializer<TResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken);

        Task<WriteConcernResult> DeleteAsync(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            bool isMulti,
            MessageEncoderSettings messageEncoderSettings,
            WriteConcern writeConcern,
            CancellationToken cancellationToken);

        Task<CursorBatch<TDocument>> GetMoreAsync<TDocument>(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            long cursorId,
            int batchSize,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken);

        Task<WriteConcernResult> InsertAsync<TDocument>(
            CollectionNamespace collectionNamespace,
            WriteConcern writeConcern,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings,
            BatchableSource<TDocument> documentSource,
            int? maxBatchCount,
            int? maxMessageSize,
            bool continueOnError,
            Func<bool> shouldSendGetLastError,
            CancellationToken cancellationToken);

        Task KillCursorsAsync(
            IEnumerable<long> cursorIds,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken);

        Task<CursorBatch<TDocument>> QueryAsync<TDocument>(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            BsonDocument fields,
            IElementNameValidator queryValidator,
            int skip,
            int batchSize,
            bool slaveOk,
            bool partialOk,
            bool noCursorTimeout,
            bool tailableCursor,
            bool awaitData,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken);

        Task<WriteConcernResult> UpdateAsync(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings,
            WriteConcern writeConcern,
            BsonDocument query,
            BsonDocument update,
            IElementNameValidator updateValidator,
            bool isMulti,
            bool isUpsert,
            CancellationToken cancellationToken);
    }

    public interface IChannelHandle : IChannel
    {
        IChannelHandle Fork();
    }
}

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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Bindings
{
    internal interface IChannel : IDisposable
    {
        IConnectionHandle Connection { get; }
        ConnectionDescription ConnectionDescription { get; }

        TResult Command<TResult>(
            ICoreSession session,
            ReadPreference readPreference,
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IEnumerable<BatchableCommandMessageSection> commandPayloads,
            IElementNameValidator commandValidator,
            BsonDocument additionalOptions,
            Action<IMessageEncoderPostProcessor> postWriteAction,
            CommandResponseHandling responseHandling,
            IBsonSerializer<TResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken);

        Task<TResult> CommandAsync<TResult>(
            ICoreSession session,
            ReadPreference readPreference,
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IEnumerable<BatchableCommandMessageSection> commandPayloads,
            IElementNameValidator commandValidator,
            BsonDocument additionalOptions,
            Action<IMessageEncoderPostProcessor> postWriteAction,
            CommandResponseHandling responseHandling,
            IBsonSerializer<TResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken);

        CursorBatch<TDocument> Query<TDocument>(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            BsonDocument fields,
            IElementNameValidator queryValidator,
            int skip,
            int batchSize,
            bool secondaryOk,
            bool partialOk,
            bool noCursorTimeout,
            bool tailableCursor,
            bool awaitData,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken);

        [Obsolete("Use an overload that does not have an oplogReplay parameter instead.")]
        CursorBatch<TDocument> Query<TDocument>(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            BsonDocument fields,
            IElementNameValidator queryValidator,
            int skip,
            int batchSize,
            bool secondaryOk,
            bool partialOk,
            bool noCursorTimeout,
            bool oplogReplay, // obsolete: OplogReplay is ignored by server versions 4.4.0 and newer
            bool tailableCursor,
            bool awaitData,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken);

        Task<CursorBatch<TDocument>> QueryAsync<TDocument>(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            BsonDocument fields,
            IElementNameValidator queryValidator,
            int skip,
            int batchSize,
            bool secondaryOk,
            bool partialOk,
            bool noCursorTimeout,
            bool tailableCursor,
            bool awaitData,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken);

        [Obsolete("Use an overload that does not have an oplogReplay parameter instead.")]
        Task<CursorBatch<TDocument>> QueryAsync<TDocument>(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            BsonDocument fields,
            IElementNameValidator queryValidator,
            int skip,
            int batchSize,
            bool secondaryOk,
            bool partialOk,
            bool noCursorTimeout,
            bool oplogReplay, // obsolete: OplogReplay is ignored by server versions 4.4.0 and newer
            bool tailableCursor,
            bool awaitData,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings,
            CancellationToken cancellationToken);
    }

    internal interface IChannelHandle : IChannel
    {
        IChannelHandle Fork();
    }
}

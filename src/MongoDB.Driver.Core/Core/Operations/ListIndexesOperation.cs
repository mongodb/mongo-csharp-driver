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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a list indexes operation.
    /// </summary>
    public class ListIndexesOperation : IReadOperation<IAsyncCursor<BsonDocument>>
    {
        #region static
        // static fields
        private static readonly SemanticVersion __serverVersionSupportingListIndexesCommand = new SemanticVersion(2, 7, 6);
        #endregion

        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ListIndexesOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public ListIndexesOperation(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _messageEncoderSettings = messageEncoderSettings;
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
        /// Gets the message encoder settings.
        /// </summary>
        /// <value>
        /// The message encoder settings.
        /// </value>
        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        // methods
        /// <inheritdoc/>
        public async Task<IAsyncCursor<BsonDocument>> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");

            using (var channelSource = await binding.GetReadChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            {
                if (channelSource.ServerDescription.Version >= __serverVersionSupportingListIndexesCommand)
                {
                    return await ExecuteUsingCommandAsync(channelSource, binding.ReadPreference, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await ExecuteUsingQueryAsync(channelSource, binding.ReadPreference, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<IAsyncCursor<BsonDocument>> ExecuteUsingCommandAsync(IChannelSourceHandle channelSource, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            var databaseNamespace = _collectionNamespace.DatabaseNamespace;
            var command = new BsonDocument("listIndexes", _collectionNamespace.CollectionName);
            var operation = new ReadCommandOperation<BsonDocument>(databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            try
            {
                var response = await operation.ExecuteAsync(channelSource, readPreference, cancellationToken).ConfigureAwait(false);
                var cursorDocument = response["cursor"].AsBsonDocument;
                var cursor = new AsyncCursor<BsonDocument>(
                    channelSource.Fork(),
                    CollectionNamespace.FromFullName(cursorDocument["ns"].AsString),
                    command,
                    cursorDocument["firstBatch"].AsBsonArray.OfType<BsonDocument>().ToList(),
                    cursorDocument["id"].ToInt64(),
                    0,
                    0,
                    BsonDocumentSerializer.Instance,
                    _messageEncoderSettings);

                return cursor;
            }
            catch (MongoCommandException ex)
            {
                if (ex.Code == 26)
                {
                    return new SingleBatchAsyncCursor<BsonDocument>(new List<BsonDocument>());
                }
                throw;
            }
        }

        private async Task<IAsyncCursor<BsonDocument>> ExecuteUsingQueryAsync(IChannelSourceHandle channelSource, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            var systemIndexesCollection = _collectionNamespace.DatabaseNamespace.SystemIndexesCollection;
            var filter = new BsonDocument("ns", _collectionNamespace.FullName);
            var operation = new FindOperation<BsonDocument>(systemIndexesCollection, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Filter = filter
            };

            return await operation.ExecuteAsync(channelSource, readPreference, cancellationToken).ConfigureAwait(false);
        }
    }
}

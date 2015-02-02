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
using System.Text;
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
    /// Represents a create indexes operation.
    /// </summary>
    public class CreateIndexesOperation : IWriteOperation<BsonDocument>
    {
        #region static
        // static fields
        private static readonly SemanticVersion __serverVersionSupportingCreateIndexesCommand = new SemanticVersion(2, 7, 6);
        #endregion

        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IEnumerable<CreateIndexRequest> _requests;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateIndexesOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="requests">The requests.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public CreateIndexesOperation(
            CollectionNamespace collectionNamespace,
            IEnumerable<CreateIndexRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _requests = Ensure.IsNotNull(requests, "requests").ToList();
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
        /// Gets the create index requests.
        /// </summary>
        /// <value>
        /// The create index requests.
        /// </value>
        public IEnumerable<CreateIndexRequest> Requests
        {
            get { return _requests; }
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
        internal BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "createIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray(_requests.Select(request => request.CreateIndexDocument())) }
            };
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            {
                if (channelSource.ServerDescription.Version >= __serverVersionSupportingCreateIndexesCommand)
                {
                    return await ExecuteUsingCommandAsync(channelSource, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await ExecuteUsingInsertAsync(channelSource, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private Task<BsonDocument> ExecuteUsingCommandAsync(IChannelSourceHandle channelSource, CancellationToken cancellationToken)
        {
            var databaseNamespace = _collectionNamespace.DatabaseNamespace;
            var command = CreateCommand();
            var resultSerializer = BsonDocumentSerializer.Instance;
            var operation = new WriteCommandOperation<BsonDocument>(databaseNamespace, command, resultSerializer, _messageEncoderSettings);
            return operation.ExecuteAsync(channelSource, cancellationToken);
        }

        private async Task<BsonDocument> ExecuteUsingInsertAsync(IChannelSourceHandle channelSource, CancellationToken cancellationToken)
        {
            var systemIndexesCollection = _collectionNamespace.DatabaseNamespace.SystemIndexesCollection;

            foreach (var createIndexRequest in _requests)
            {
                var document = createIndexRequest.CreateIndexDocument();
                document.InsertAt(0, new BsonElement("ns", _collectionNamespace.FullName));
                var documentSource = new BatchableSource<BsonDocument>(new[] { document });
                var operation = new InsertOpcodeOperation<BsonDocument>(
                    systemIndexesCollection,
                    documentSource,
                    BsonDocumentSerializer.Instance,
                    _messageEncoderSettings);
                await operation.ExecuteAsync(channelSource, cancellationToken).ConfigureAwait(false);
            }

            return new BsonDocument("ok", 1);
        }
    }
}

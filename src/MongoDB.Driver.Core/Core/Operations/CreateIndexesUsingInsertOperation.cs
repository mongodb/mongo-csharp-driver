/* Copyright 2013-2015 MongoDB Inc.
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
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a create indexes operation that inserts into the system.indexes collection (used with older server versions).
    /// </summary>
    public class CreateIndexesUsingInsertOperation : IWriteOperation<BsonDocument>
    {
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
        public CreateIndexesUsingInsertOperation(
            CollectionNamespace collectionNamespace,
            IEnumerable<CreateIndexRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _requests = Ensure.IsNotNull(requests, nameof(requests)).ToList();
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
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
            set { _writeConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        // public methods
        /// <inheritdoc/>
        public BsonDocument Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            using (EventContext.BeginOperation())
            {
                foreach (var createIndexRequest in _requests)
                {
                    var operation = CreateOperation(createIndexRequest);
                    operation.Execute(binding, cancellationToken);
                }

                return new BsonDocument("ok", 1);
            }
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            using (EventContext.BeginOperation())
            {
                foreach (var createIndexRequest in _requests)
                {
                    var operation = CreateOperation(createIndexRequest);
                    await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
                }

                return new BsonDocument("ok", 1);
            }
        }

        // private methods
        private InsertOpcodeOperation<BsonDocument> CreateOperation(CreateIndexRequest createIndexRequest)
        {
            var systemIndexesCollection = _collectionNamespace.DatabaseNamespace.SystemIndexesCollection;
            var document = createIndexRequest.CreateIndexDocument();
            document.InsertAt(0, new BsonElement("ns", _collectionNamespace.FullName));
            var documentSource = new BatchableSource<BsonDocument>(new[] { document });
            return new InsertOpcodeOperation<BsonDocument>(
                systemIndexesCollection,
                documentSource,
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings);
        }
    }
}

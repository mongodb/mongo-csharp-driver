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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a create collection operation.
    /// </summary>
    public class CreateCollectionOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private bool? _autoIndexId;
        private bool? _capped;
        private readonly CollectionNamespace _collectionNamespace;
        private long? _maxDocuments;
        private long? _maxSize;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private BsonDocument _storageEngine;
        private bool? _usePowerOf2Sizes;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateCollectionOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public CreateCollectionOperation(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether an index on _id should be created automatically.
        /// </summary>
        /// <value>
        /// A value indicating whether an index on _id should be created automatically.
        /// </value>
        public bool? AutoIndexId
        {
            get { return _autoIndexId; }
            set { _autoIndexId = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the collection is a capped collection.
        /// </summary>
        /// <value>
        /// A value indicating whether the collection is a capped collection.
        /// </value>
        public bool? Capped
        {
            get { return _capped; }
            set { _capped = value; }
        }

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
        /// Gets or sets the maximum number of documents in a capped collection.
        /// </summary>
        /// <value>
        /// The maximum number of documents in a capped collection.
        /// </value>
        public long? MaxDocuments
        {
            get { return _maxDocuments; }
            set { _maxDocuments = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        /// <summary>
        /// Gets or sets the maximum size of a capped collection.
        /// </summary>
        /// <value>
        /// The maximum size of a capped collection.
        /// </value>
        public long? MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = Ensure.IsNullOrGreaterThanZero(value, "value"); }
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
        /// Gets or sets the storage engine options.
        /// </summary>
        /// <value>
        /// The storage engine options.
        /// </value>
        public BsonDocument StorageEngine
        {
            get { return _storageEngine; }
            set { _storageEngine = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the collection should use power of 2 sizes.
        /// </summary>
        /// <value>
        /// A value indicating whether the collection should use power of 2 sizes..
        /// </value>
        public bool? UsePowerOf2Sizes
        {
            get { return _usePowerOf2Sizes; }
            set { _usePowerOf2Sizes = value; }
        }

        // methods
        internal BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "capped", () => _capped.Value, _capped.HasValue },
                { "autoIndexId", () => _autoIndexId.Value, _autoIndexId.HasValue },
                { "size", () => _maxSize.Value, _maxSize.HasValue },
                { "max", () => _maxDocuments.Value, _maxDocuments.HasValue },
                { "flags", () => _usePowerOf2Sizes.Value ? 1 : 0, _usePowerOf2Sizes.HasValue},
                { "storageEngine", () => _storageEngine, _storageEngine != null }
            };
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
        }
    }
}

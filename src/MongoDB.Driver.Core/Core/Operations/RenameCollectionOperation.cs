﻿/* Copyright 2013-2014 MongoDB Inc.
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
    /// Represents a rename collection operation.
    /// </summary>
    public class RenameCollectionOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private bool? _dropTarget;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly CollectionNamespace _newCollectionNamespace;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RenameCollectionOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="newCollectionNamespace">The new collection namespace.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public RenameCollectionOperation(
            CollectionNamespace collectionNamespace,
            CollectionNamespace newCollectionNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _newCollectionNamespace = Ensure.IsNotNull(newCollectionNamespace, "newCollectionNamespace");
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
        /// Gets or sets a value indicating whether to drop the target collection first if it already exists.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the target collection should be dropped first if it already exists.; otherwise, <c>false</c>.
        /// </value>
        public bool? DropTarget
        {
            get { return _dropTarget; }
            set { _dropTarget = value; }
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
        /// Gets the new collection namespace.
        /// </summary>
        /// <value>
        /// The new collection namespace.
        /// </value>
        public CollectionNamespace NewCollectionNamespace
        {
            get { return _newCollectionNamespace; }
        }

        // methods
        internal BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "renameCollection", _collectionNamespace.FullName },
                { "to", _newCollectionNamespace.FullName },
                { "dropTarget", () => _dropTarget.Value, _dropTarget.HasValue }
            };
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
        }
    }
}

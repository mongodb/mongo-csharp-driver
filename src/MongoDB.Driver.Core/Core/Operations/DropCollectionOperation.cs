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

using System.Collections.Generic;
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
    /// Represents a drop collection operation.
    /// </summary>
    public class DropCollectionOperation : IWriteOperation<BsonDocument>
    {
        #region static
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateCollectionOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="encryptedFields">The encrypted feilds.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public static DropCollectionOperation CreateDropCollectionOperation(
            CollectionNamespace collectionNamespace,
            BsonDocument encryptedFields,
            MessageEncoderSettings messageEncoderSettings)
        {
            DropCollectionOperation[] postOperations = null;
            if (encryptedFields != null)
            {
                postOperations = new[]
                {
                    CreateInnerDropOperation(encryptedFields.TryGetValue("escCollection", out var escCollection) ? escCollection.ToString() : $"enxcol_.{collectionNamespace.CollectionName}.esc"),
                    CreateInnerDropOperation(encryptedFields.TryGetValue("eccCollection", out var eccCollection) ? eccCollection.ToString() : $"enxcol_.{collectionNamespace.CollectionName}.ecc"),
                    CreateInnerDropOperation(encryptedFields.TryGetValue("ecocCollection", out var ecocCollection) ? ecocCollection.ToString() : $"enxcol_.{collectionNamespace.CollectionName}.ecoc"),
                };
            }

            var dropCollectionOperation = new DropCollectionOperation(
                collectionNamespace,
                messageEncoderSettings,
                postOperations: postOperations)
            {
                EncryptedFields = encryptedFields
            };

            return dropCollectionOperation;

            DropCollectionOperation CreateInnerDropOperation(string collectionName)
                => new DropCollectionOperation(new CollectionNamespace(collectionNamespace.DatabaseNamespace.DatabaseName, collectionName), messageEncoderSettings);
        }
        #endregion

        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private BsonDocument _encryptedFields;
        private readonly IEnumerable<DropCollectionOperation> _postOperations;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private WriteConcern _writeConcern;

        // constructors
        internal DropCollectionOperation(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings,
            IEnumerable<DropCollectionOperation> postOperations = null)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _messageEncoderSettings = messageEncoderSettings;
            _postOperations = postOperations;
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

        ///<summary>
        /// Gets the encrypted fields.
        /// </summary>
        /// <value>
        /// The encrypted fields.
        /// </value>
        public BsonDocument EncryptedFields
        {
            get { return _encryptedFields; }
            private set { _encryptedFields = value; }
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
        /// Gets or sets the write concern.
        /// </summary>
        /// <value>
        /// The write concern.
        /// </value>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        // public methods
        /// <inheritdoc/>
        public BsonDocument Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            BsonDocument result = null;
            foreach (var operation in CreateOperations(binding.Session))
            {
                using (var channelSource = binding.GetWriteChannelSource(cancellationToken))
                using (var channel = channelSource.GetChannel(cancellationToken))
                using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
                {
                    try
                    {
                        var itemResult = operation.Operation.Execute(channelBinding, cancellationToken);
                        if (operation.IsMainOperation)
                        {
                            result = itemResult;
                        }
                    }
                    catch (MongoCommandException ex)
                    {
                        if (!ShouldIgnoreException(ex))
                        {
                            throw;
                        }
                        if (operation.IsMainOperation)
                        {
                            result = ex.Result;
                        }
                    }
                }
            }
            return result;
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            BsonDocument result = null;
            foreach (var operation in CreateOperations(binding.Session))
            {
                using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
                using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
                using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
                {
                    try
                    {
                        var itemResult = await operation.Operation.ExecuteAsync(channelBinding, cancellationToken).ConfigureAwait(false);
                        if (operation.IsMainOperation)
                        {
                            result = itemResult;
                        }
                    }
                    catch (MongoCommandException ex)
                    {
                        if (!ShouldIgnoreException(ex))
                        {
                            throw;
                        }
                        if (operation.IsMainOperation)
                        {
                            result = ex.Result;
                        }
                    }
                }
            }
            return result;
        }

        // private methods
        internal BsonDocument CreateCommand(ICoreSessionHandle session)
        {
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(session, _writeConcern);
            return new BsonDocument
            {
                { "drop", _collectionNamespace.CollectionName },
                { "writeConcern", writeConcern, writeConcern != null }
            };
        }

        private IEnumerable<(IWriteOperation<BsonDocument> Operation, bool IsMainOperation)> CreateOperations(ICoreSessionHandle session)
        {
            var command = CreateCommand(session);
            yield return (new WriteCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings), IsMainOperation: true);

            if (_postOperations != null)
            {
                foreach (var postOperation in _postOperations)
                {
                    yield return (postOperation, IsMainOperation: false);
                }
            }
        }

        private bool ShouldIgnoreException(MongoCommandException ex)
        {
            return
                ex.Code == (int)ServerErrorCode.NamespaceNotFound ||
                ex.ErrorMessage == "ns not found";
        }
    }
}

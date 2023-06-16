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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Encryption;
using static MongoDB.Driver.Encryption.EncryptedCollectionHelper;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a create collection operation.
    /// </summary>
    public class CreateCollectionOperation : IWriteOperation<BsonDocument>
    {
        #region static
        internal static IWriteOperation<BsonDocument> CreateEncryptedCreateCollectionOperationIfConfigured(
            CollectionNamespace collectionNamespace,
            BsonDocument encryptedFields,
            MessageEncoderSettings messageEncoderSettings,
            Action<CreateCollectionOperation> createCollectionOperationConfigurator)
        {
            var mainOperation = new CreateCollectionOperation(
                collectionNamespace,
                messageEncoderSettings,
                encryptedFields != null ? Feature.Csfle2QEv2 : null)
            {
                EncryptedFields = encryptedFields
            };

            createCollectionOperationConfigurator?.Invoke(mainOperation);

            if (encryptedFields != null)
            {
                return new CompositeWriteOperation<BsonDocument>(
                    (CreateInnerCollectionOperation(EncryptedCollectionHelper.GetAdditionalCollectionName(encryptedFields, collectionNamespace, HelperCollectionForEncryption.Esc)), IsMainOperation: false),
                    (CreateInnerCollectionOperation(EncryptedCollectionHelper.GetAdditionalCollectionName(encryptedFields, collectionNamespace, HelperCollectionForEncryption.Ecos)), IsMainOperation: false),
                    (mainOperation, IsMainOperation: true),
                    (new CreateIndexesOperation(collectionNamespace, new[] { new CreateIndexRequest(EncryptedCollectionHelper.AdditionalCreateIndexDocument) }, messageEncoderSettings), IsMainOperation: false));
            }
            else
            {
                return mainOperation;
            }

            CreateCollectionOperation CreateInnerCollectionOperation(string collectionName)
                => new(new CollectionNamespace(collectionNamespace.DatabaseNamespace.DatabaseName, collectionName), messageEncoderSettings, Feature.Csfle2QEv2)
                   {
                      ClusteredIndex = new BsonDocument { { "key", new BsonDocument("_id", 1) }, { "unique", true } }
                   };
        }
        #endregion

        // fields
        private bool? _autoIndexId;
        private bool? _capped;
        private BsonDocument _changeStreamPreAndPostImages;
        private BsonDocument _clusteredIndex;
        private Collation _collation;
        private readonly CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private BsonDocument _encryptedFields;
        private TimeSpan? _expireAfter;
        private BsonDocument _indexOptionDefaults;
        private long? _maxDocuments;
        private long? _maxSize;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private bool? _noPadding;
        private BsonDocument _storageEngine;
        private TimeSeriesOptions _timeSeriesOptions;
        private bool? _usePowerOf2Sizes;
        private DocumentValidationAction? _validationAction;
        private DocumentValidationLevel? _validationLevel;
        private BsonDocument _validator;
        private WriteConcern _writeConcern;

        private readonly Feature _supportedFeature;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateCollectionOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public CreateCollectionOperation(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings)
            : this(collectionNamespace, messageEncoderSettings, supportedFeature: null)
        {
        }

        private CreateCollectionOperation(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings,
            Feature supportedFeature)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _messageEncoderSettings = messageEncoderSettings;
            _supportedFeature = supportedFeature;
        }

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether an index on _id should be created automatically.
        /// </summary>
        /// <value>
        /// A value indicating whether an index on _id should be created automatically.
        /// </value>
        [Obsolete("AutoIndexId has been deprecated since server version 3.2.")]
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
        /// Gets or sets a change streams pre and post images options.
        /// </summary>
        /// <value>
        /// change streams pre and post images options.
        /// </value>
        public BsonDocument ChangeStreamPreAndPostImages
        {
            get { return _changeStreamPreAndPostImages; }
            set { _changeStreamPreAndPostImages = value; }
        }

        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        /// <value>
        /// The collation.
        /// </value>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        /// <value>
        /// The comment.
        /// </value>
        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
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

        internal BsonDocument EncryptedFields
        {
            get { return _encryptedFields; }
            private set { _encryptedFields = value; }
        }

        /// <summary>
        /// Gets or sets the expiration timespan for time series collections. Used to automatically delete documents in time series collections.
        /// See https://www.mongodb.com/docs/manual/reference/command/create/ for supported options and https://www.mongodb.com/docs/manual/core/timeseries-collections/
        /// for more information on time series collections.
        /// </summary>
        /// <value>
        /// The timespan after which to expire documents.
        /// </value>
        public TimeSpan? ExpireAfter
        {
            get { return _expireAfter; }
            set { _expireAfter = value; }
        }

        /// <summary>
        /// Gets or sets the index option defaults.
        /// </summary>
        /// <value>
        /// The index option defaults.
        /// </value>
        public BsonDocument IndexOptionDefaults
        {
            get { return _indexOptionDefaults; }
            set { _indexOptionDefaults = value; }
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
            set { _maxDocuments = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
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
            set { _maxSize = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
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
        /// Gets or sets whether padding should not be used.
        /// </summary>
        public bool? NoPadding
        {
            get { return _noPadding; }
            set { _noPadding = value; }
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
        /// Gets or sets the <see cref="TimeSeriesOptions"/>. Represents an object containing options for creating time series collections.
        /// See https://www.mongodb.com/docs/manual/reference/command/create/ for supported options and https://www.mongodb.com/docs/manual/core/timeseries-collections/
        /// for more information on time series collections.
        /// </summary>
        /// <value>
        /// The time series options.
        /// </value>
        public TimeSeriesOptions TimeSeriesOptions
        {
            get { return _timeSeriesOptions; }
            set { _timeSeriesOptions = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the collection should use power of 2 sizes.
        /// </summary>
        /// <value>
        /// A value indicating whether the collection should use power of 2 sizes.
        /// </value>
        public bool? UsePowerOf2Sizes
        {
            get { return _usePowerOf2Sizes; }
            set { _usePowerOf2Sizes = value; }
        }

        /// <summary>
        /// Gets or sets the validation action.
        /// </summary>
        /// <value>
        /// The validation action.
        /// </value>
        public DocumentValidationAction? ValidationAction
        {
            get { return _validationAction; }
            set { _validationAction = value; }
        }

        /// <summary>
        /// Gets or sets the validation level.
        /// </summary>
        /// <value>
        /// The validation level.
        /// </value>
        public DocumentValidationLevel? ValidationLevel
        {
            get { return _validationLevel; }
            set { _validationLevel = value; }
        }

        /// <summary>
        /// Gets or sets the validator.
        /// </summary>
        /// <value>
        /// The validator.
        /// </value>
        public BsonDocument Validator
        {
            get { return _validator; }
            set { _validator = value; }
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

        /// <summary>
        /// Gets or sets the clustered index definition.
        /// </summary>
        /// <value>
        /// The clustered index definition.
        /// </value>
        public BsonDocument ClusteredIndex
        {
            get => _clusteredIndex;
            set => _clusteredIndex = value;
        }

        // methods
        internal BsonDocument CreateCommand(ICoreSessionHandle session)
        {
            var flags = GetFlags();
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(session, _writeConcern);
            return new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "clusteredIndex", _clusteredIndex, _clusteredIndex != null },
                { "capped", () => _capped.Value, _capped.HasValue },
                { "autoIndexId", () => _autoIndexId.Value, _autoIndexId.HasValue },
                { "size", () => _maxSize.Value, _maxSize.HasValue },
                { "max", () => _maxDocuments.Value, _maxDocuments.HasValue },
                { "flags", () => (int)flags.Value, flags.HasValue },
                { "storageEngine", _storageEngine, _storageEngine != null },
                { "indexOptionDefaults", _indexOptionDefaults, _indexOptionDefaults != null },
                { "validator", _validator, _validator != null },
                { "validationAction", () => _validationAction.Value.ToString().ToLowerInvariant(), _validationAction.HasValue },
                { "validationLevel", () => _validationLevel.Value.ToString().ToLowerInvariant(), _validationLevel.HasValue },
                { "collation", () => _collation.ToBsonDocument(), _collation != null },
                { "comment",  _comment, _comment != null },
                { "writeConcern", writeConcern, writeConcern != null },
                { "expireAfterSeconds", () => _expireAfter.Value.TotalSeconds, _expireAfter.HasValue },
                { "timeseries", () => _timeSeriesOptions.ToBsonDocument(), _timeSeriesOptions != null },
                { "encryptedFields", _encryptedFields, _encryptedFields != null },
                { "changeStreamPreAndPostImages", _changeStreamPreAndPostImages, _changeStreamPreAndPostImages != null }
            };
        }

        private CreateCollectionFlags? GetFlags()
        {
            if (_usePowerOf2Sizes.HasValue || _noPadding.HasValue)
            {
                var flags = CreateCollectionFlags.None;
                if (_usePowerOf2Sizes.HasValue && _usePowerOf2Sizes.Value)
                {
                    flags |= CreateCollectionFlags.UsePowerOf2Sizes;
                }
                if (_noPadding.HasValue && _noPadding.Value)
                {
                    flags |= CreateCollectionFlags.NoPadding;
                }
                return flags;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public BsonDocument Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var channelSource = binding.GetWriteChannelSource(cancellationToken))
            using (var channel = channelSource.GetChannel(cancellationToken))
            {
                EnsureServerIsValid(channel.ConnectionDescription.MaxWireVersion);
                using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
                {
                    var operation = CreateOperation(channelBinding.Session);
                    return operation.Execute(channelBinding, cancellationToken);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
            {
                EnsureServerIsValid(channel.ConnectionDescription.MaxWireVersion);
                using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
                {
                    var operation = CreateOperation(channelBinding.Session);
                    return await operation.ExecuteAsync(channelBinding, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // private methods
        private IDisposable BeginOperation() => EventContext.BeginOperation("create");

        private WriteCommandOperation<BsonDocument> CreateOperation(ICoreSessionHandle session)
        {
            var command = CreateCommand(session);
            return new WriteCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
        }

        private void EnsureServerIsValid(int maxWireVersion)
        {
            _supportedFeature?.ThrowIfNotSupported(maxWireVersion);
        }

        [Flags]
        private enum CreateCollectionFlags
        {
            None = 0,
            UsePowerOf2Sizes = 1,
            NoPadding = 2
        }
    }
}

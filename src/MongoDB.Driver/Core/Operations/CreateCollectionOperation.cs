/* Copyright 2010-present MongoDB Inc.
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
    internal sealed class CreateCollectionOperation : IWriteOperation<BsonDocument>
    {
        #region static

        public static IWriteOperation<BsonDocument> CreateEncryptedCreateCollectionOperationIfConfigured(
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
        private BsonDocument _storageEngine;
        private TimeSeriesOptions _timeSeriesOptions;
        private DocumentValidationAction? _validationAction;
        private DocumentValidationLevel? _validationLevel;
        private BsonDocument _validator;
        private WriteConcern _writeConcern;

        private readonly Feature _supportedFeature;

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

        public bool? Capped
        {
            get { return _capped; }
            set { _capped = value; }
        }

        public BsonDocument ChangeStreamPreAndPostImages
        {
            get { return _changeStreamPreAndPostImages; }
            set { _changeStreamPreAndPostImages = value; }
        }

        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        internal BsonDocument EncryptedFields
        {
            get { return _encryptedFields; }
            private set { _encryptedFields = value; }
        }

        public TimeSpan? ExpireAfter
        {
            get { return _expireAfter; }
            set { _expireAfter = value; }
        }

        public BsonDocument IndexOptionDefaults
        {
            get { return _indexOptionDefaults; }
            set { _indexOptionDefaults = value; }
        }

        public long? MaxDocuments
        {
            get { return _maxDocuments; }
            set { _maxDocuments = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        public long? MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public string OperationName => "createCollection";

        public BsonDocument StorageEngine
        {
            get { return _storageEngine; }
            set { _storageEngine = value; }
        }

        public TimeSeriesOptions TimeSeriesOptions
        {
            get { return _timeSeriesOptions; }
            set { _timeSeriesOptions = value; }
        }

        public DocumentValidationAction? ValidationAction
        {
            get { return _validationAction; }
            set { _validationAction = value; }
        }

        public DocumentValidationLevel? ValidationLevel
        {
            get { return _validationLevel; }
            set { _validationLevel = value; }
        }

        public BsonDocument Validator
        {
            get { return _validator; }
            set { _validator = value; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        public BsonDocument ClusteredIndex
        {
            get => _clusteredIndex;
            set => _clusteredIndex = value;
        }

        internal BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session)
        {
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(operationContext, session, _writeConcern);
            return new BsonDocument
            {
                { "create", _collectionNamespace.CollectionName },
                { "clusteredIndex", _clusteredIndex, _clusteredIndex != null },
                { "capped", () => _capped.Value, _capped.HasValue },
                { "size", () => _maxSize.Value, _maxSize.HasValue },
                { "max", () => _maxDocuments.Value, _maxDocuments.HasValue },
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

        public BsonDocument Execute(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var channelSource = binding.GetWriteChannelSource(operationContext))
            using (var channel = channelSource.GetChannel(operationContext))
            {
                EnsureServerIsValid(channel.ConnectionDescription.MaxWireVersion);
                using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
                {
                    var operation = CreateOperation(operationContext, channelBinding.Session);
                    return operation.Execute(operationContext, channelBinding);
                }
            }
        }

        public async Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var channelSource = await binding.GetWriteChannelSourceAsync(operationContext).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false))
            {
                EnsureServerIsValid(channel.ConnectionDescription.MaxWireVersion);
                using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
                {
                    var operation = CreateOperation(operationContext, channelBinding.Session);
                    return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
                }
            }
        }

        private EventContext.OperationNameDisposer BeginOperation() => EventContext.BeginOperation("create");

        private WriteCommandOperation<BsonDocument> CreateOperation(OperationContext operationContext, ICoreSessionHandle session)
        {
            var command = CreateCommand(operationContext, session);
            return new WriteCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
        }

        private void EnsureServerIsValid(int maxWireVersion)
        {
            _supportedFeature?.ThrowIfNotSupported(maxWireVersion);
        }
    }
}

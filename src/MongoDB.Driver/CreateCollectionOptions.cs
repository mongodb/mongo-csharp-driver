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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for creating a collection.
    /// </summary>
    public class CreateCollectionOptions
    {
        // fields
        private bool? _capped;
        private ChangeStreamPreAndPostImagesOptions _changeStreamPreAndPostImagesOptions;
        private Collation _collation;
        private BsonDocument _encryptedFields;
        private TimeSpan? _expireAfter;
        private IndexOptionDefaults _indexOptionDefaults;
        private long? _maxDocuments;
        private long? _maxSize;
        private bool? _noPadding;
        private BsonDocument _storageEngine;
        private TimeSeriesOptions _timeSeriesOptions;
        private bool? _usePowerOf2Sizes;
        private IBsonSerializerRegistry _serializerRegistry;
        private DocumentValidationAction? _validationAction;
        private DocumentValidationLevel? _validationLevel;

        // properties
        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the collection is capped.
        /// </summary>
        public bool? Capped
        {
            get { return _capped; }
            set { _capped = value; }
        }

        /// <summary>
        /// Gets or sets  Gets or sets a change streams pre and post images options.
        /// </summary>
        public ChangeStreamPreAndPostImagesOptions ChangeStreamPreAndPostImagesOptions
        {
            get { return _changeStreamPreAndPostImagesOptions; }
            set { _changeStreamPreAndPostImagesOptions = value; }
        }

        /// <summary>
        /// Gets or sets encrypted fields.
        /// </summary>
        public BsonDocument EncryptedFields
        {
            get { return _encryptedFields; }
            set { _encryptedFields = value; }
        }

        /// <summary>
        /// Gets or sets a timespan indicating how long documents in a time series collection should be retained.
        /// </summary>
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
        public IndexOptionDefaults IndexOptionDefaults
        {
            get { return _indexOptionDefaults; }
            set { _indexOptionDefaults = value; }
        }

        /// <summary>
        /// Gets or sets the maximum number of documents (used with capped collections).
        /// </summary>
        public long? MaxDocuments
        {
            get { return _maxDocuments; }
            set { _maxDocuments = value; }
        }

        /// <summary>
        /// Gets or sets the maximum size of the collection (used with capped collections).
        /// </summary>
        public long? MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = value; }
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
        /// Gets or sets the serializer registry.
        /// </summary>
        public IBsonSerializerRegistry SerializerRegistry
        {
            get { return _serializerRegistry; }
            set { _serializerRegistry = value; }
        }

        /// <summary>
        /// Gets or sets the storage engine options.
        /// </summary>
        public BsonDocument StorageEngine
        {
            get { return _storageEngine; }
            set { _storageEngine = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="TimeSeriesOptions"/> to use when creating a time series collection.
        /// </summary>
        public TimeSeriesOptions TimeSeriesOptions
        {
            get { return _timeSeriesOptions; }
            set { _timeSeriesOptions = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use power of 2 sizes.
        /// </summary>
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
        /// Clones the CreateCollectionOptions.
        /// </summary>
        /// <returns>The cloned CreateCollectionOptions.</returns>
        public virtual CreateCollectionOptions Clone() =>
            new CreateCollectionOptions
            {
                _capped = _capped,
                _changeStreamPreAndPostImagesOptions = _changeStreamPreAndPostImagesOptions,
                _collation = _collation,
                _encryptedFields = _encryptedFields,
                _expireAfter = _expireAfter,
                _indexOptionDefaults = _indexOptionDefaults,
                _maxDocuments = _maxDocuments,
                _maxSize = _maxSize,
                _noPadding = _noPadding,
                _serializerRegistry = _serializerRegistry,
                _storageEngine = _storageEngine,
                _timeSeriesOptions = _timeSeriesOptions,
                _usePowerOf2Sizes = _usePowerOf2Sizes,
                _validationAction = _validationAction,
                _validationLevel = _validationLevel
            };
    }

    /// <summary>
    /// Options for creating a collection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class CreateCollectionOptions<TDocument> : CreateCollectionOptions
    {
        #region static
        // internal static methods
        /// <summary>
        /// Coerces a generic CreateCollectionOptions{TDocument} from a non-generic CreateCollectionOptions.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The generic options.</returns>
        internal static CreateCollectionOptions<TDocument> CoercedFrom(CreateCollectionOptions options)
        {
            if (options == null)
            {
                return null;
            }

            if (options.GetType() == typeof(CreateCollectionOptions))
            {
                return new CreateCollectionOptions<TDocument>
                {
                    Capped = options.Capped,
                    Collation = options.Collation,
                    ChangeStreamPreAndPostImagesOptions = options.ChangeStreamPreAndPostImagesOptions,
                    EncryptedFields = options.EncryptedFields,
                    ExpireAfter = options.ExpireAfter,
                    IndexOptionDefaults = options.IndexOptionDefaults,
                    MaxDocuments = options.MaxDocuments,
                    MaxSize = options.MaxSize,
                    NoPadding = options.NoPadding,
                    SerializerRegistry = options.SerializerRegistry,
                    StorageEngine = options.StorageEngine,
                    TimeSeriesOptions = options.TimeSeriesOptions,
                    UsePowerOf2Sizes = options.UsePowerOf2Sizes,
                    ValidationAction = options.ValidationAction,
                    ValidationLevel = options.ValidationLevel
                };
            }

            return (CreateCollectionOptions<TDocument>)options;
        }
        #endregion

        // private fields
        private ClusteredIndexOptions<TDocument> _clusteredIndex;
        private IBsonSerializer<TDocument> _documentSerializer;
        private FilterDefinition<TDocument> _validator;

        // public properties
        /// <summary>
        /// Gets or sets the <see cref="ClusteredIndexOptions{TDocument}"/>.
        /// </summary>
        public ClusteredIndexOptions<TDocument> ClusteredIndex
        {
            get { return _clusteredIndex; }
            set { _clusteredIndex = value; }
        }

        /// <summary>
        /// Gets or sets the document serializer.
        /// </summary>
        public IBsonSerializer<TDocument> DocumentSerializer
        {
            get { return _documentSerializer; }
            set { _documentSerializer = value; }
        }

        /// <summary>
        /// Gets or sets the validator.
        /// </summary>
        /// <value>
        /// The validator.
        /// </value>
        public FilterDefinition<TDocument> Validator
        {
            get { return _validator; }
            set { _validator = value; }
        }

        /// <summary>
        /// Clones the CreateCollectionOptions.
        /// </summary>
        /// <returns>The cloned CreateCollectionOptions.</returns>
        public override CreateCollectionOptions Clone() =>
            new CreateCollectionOptions<TDocument>
            {
                Capped = base.Capped,
                ChangeStreamPreAndPostImagesOptions = base.ChangeStreamPreAndPostImagesOptions,
                Collation = base.Collation,
                EncryptedFields = base.EncryptedFields,
                ExpireAfter = base.ExpireAfter,
                IndexOptionDefaults = base.IndexOptionDefaults,
                MaxDocuments = base.MaxDocuments,
                MaxSize = base.MaxSize,
                NoPadding = base.NoPadding,
                SerializerRegistry = base.SerializerRegistry,
                StorageEngine = base.StorageEngine,
                TimeSeriesOptions = base.TimeSeriesOptions,
                UsePowerOf2Sizes = base.UsePowerOf2Sizes,
                ValidationAction = base.ValidationAction,
                ValidationLevel = base.ValidationLevel,

                _clusteredIndex = _clusteredIndex,
                _documentSerializer = _documentSerializer,
                _validator = _validator
            };
    }
}

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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents replace one operation in the scope of BulkWrite operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class BulkWriteReplaceOneModel<TDocument> : BulkWriteModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteReplaceOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="replacement">Update definition.</param>
        /// <param name="collation">Specifies a collation.</param>
        /// <param name="hint">The index to use.</param>
        /// <param name="isUpsert">A value indicating whether to insert the document if it doesn't already exist.</param>
        public BulkWriteReplaceOneModel(
            string collectionNamespace,
            FilterDefinition<TDocument> filter,
            TDocument replacement,
            Collation collation = null,
            BsonValue hint = null,
            bool isUpsert = false)
            : this(CollectionNamespace.FromFullName(collectionNamespace), filter, replacement, collation, hint, isUpsert, sort: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteReplaceOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="replacement">Update definition.</param>
        /// <param name="collation">Specifies a collation.</param>
        /// <param name="hint">The index to use.</param>
        /// <param name="isUpsert">Indicating whether to insert the document if it doesn't already exist.</param>
        public BulkWriteReplaceOneModel(
            CollectionNamespace collectionNamespace,
            FilterDefinition<TDocument> filter,
            TDocument replacement,
            Collation collation = null,
            BsonValue hint = null,
            bool isUpsert = false)
            : this(collectionNamespace, filter, replacement, collation, hint, isUpsert, sort: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteReplaceOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="replacement">Update definition.</param>
        /// <param name="sort">The sort definition to use.</param>
        /// <param name="collation">Specifies a collation.</param>
        /// <param name="hint">The index to use.</param>
        /// <param name="isUpsert">A value indicating whether to insert the document if it doesn't already exist.</param>
        public BulkWriteReplaceOneModel(
            string collectionNamespace,
            FilterDefinition<TDocument> filter,
            TDocument replacement,
            SortDefinition<TDocument> sort,
            Collation collation = null,
            BsonValue hint = null,
            bool isUpsert = false)
            : this(CollectionNamespace.FromFullName(collectionNamespace), filter, replacement, collation, hint, isUpsert, sort)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteReplaceOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="replacement">Update definition.</param>
        /// <param name="sort">The sort definition to use.</param>
        /// <param name="collation">Specifies a collation.</param>
        /// <param name="hint">The index to use.</param>
        /// <param name="isUpsert">Indicating whether to insert the document if it doesn't already exist.</param>
        public BulkWriteReplaceOneModel(
            CollectionNamespace collectionNamespace,
            FilterDefinition<TDocument> filter,
            TDocument replacement,
            SortDefinition<TDocument> sort,
            Collation collation = null,
            BsonValue hint = null,
            bool isUpsert = false)
            : this(collectionNamespace, filter, replacement, collation, hint, isUpsert, sort)
        {
        }

        private BulkWriteReplaceOneModel(
            CollectionNamespace collectionNamespace,
            FilterDefinition<TDocument> filter,
            TDocument replacement,
            Collation collation = null,
            BsonValue hint = null,
            bool isUpsert = false,
            SortDefinition<TDocument> sort = null)
            : base(collectionNamespace)
        {
            Filter = Ensure.IsNotNull(filter, nameof(filter));
            Replacement = replacement;
            Collation = collation;
            Hint = hint;
            IsUpsert = isUpsert;
            Sort = sort;
        }

        /// <summary>
        /// Specifies a collation.
        /// </summary>
        public Collation Collation { get; init; }

        /// <summary>
        /// The filter to apply.
        /// </summary>
        public FilterDefinition<TDocument> Filter { get; }

        /// <summary>
        /// The index to use.
        /// </summary>
        public BsonValue Hint { get; init; }

        /// <summary>
        /// Indicating whether to insert the document if it doesn't already exist.
        /// </summary>
        public bool IsUpsert { get; init; }

        /// <summary>
        /// Update definition.
        /// </summary>
        public TDocument Replacement { get; }

        /// <summary>
        /// The sort definition to use.
        /// </summary>
        public SortDefinition<TDocument> Sort { get; init; }

        internal override bool IsMulti => false;

        internal override void Render(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, IBulkWriteModelRenderer renderer)
            => renderer.RenderReplaceOne(renderArgs, serializationContext, this);
    }
}

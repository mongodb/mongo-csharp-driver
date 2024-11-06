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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents update one operation in the scope of BulkWrite operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class BulkWriteUpdateOneModel<TDocument> : BulkWriteModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteUpdateOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="update">Update definition.</param>
        /// <param name="collation">Specifies a collation.</param>
        /// <param name="hint">The index to use.</param>
        /// <param name="isUpsert">Indicating whether to insert the document if it doesn't already exist.</param>
        /// <param name="arrayFilters">A set of filters specifying to which array elements an update should apply.</param>
        public BulkWriteUpdateOneModel(
            string collectionNamespace,
            FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update,
            Collation collation = null,
            BsonValue hint = null,
            bool isUpsert = false,
            IEnumerable<ArrayFilterDefinition> arrayFilters = null)
            : this(CollectionNamespace.FromFullName(collectionNamespace), filter, update, collation, hint, isUpsert, arrayFilters, sort: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteUpdateOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="update">Update definition.</param>
        /// <param name="collation">Specifies a collation.</param>
        /// <param name="hint">The index to use.</param>
        /// <param name="isUpsert">Indicating whether to insert the document if it doesn't already exist.</param>
        /// <param name="arrayFilters">A set of filters specifying to which array elements an update should apply.</param>
        public BulkWriteUpdateOneModel(
            CollectionNamespace collectionNamespace,
            FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update,
            Collation collation = null,
            BsonValue hint = null,
            bool isUpsert = false,
            IEnumerable<ArrayFilterDefinition> arrayFilters = null)
            : this(collectionNamespace, filter, update, collation, hint, isUpsert, arrayFilters, sort: null)
        {
        }

                /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteUpdateOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="update">Update definition.</param>
        /// <param name="sort">The sort definition to use.</param>
        /// <param name="collation">Specifies a collation.</param>
        /// <param name="hint">The index to use.</param>
        /// <param name="isUpsert">Indicating whether to insert the document if it doesn't already exist.</param>
        /// <param name="arrayFilters">A set of filters specifying to which array elements an update should apply.</param>
        public BulkWriteUpdateOneModel(
            string collectionNamespace,
            FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update,
            SortDefinition<TDocument> sort,
            Collation collation = null,
            BsonValue hint = null,
            bool isUpsert = false,
            IEnumerable<ArrayFilterDefinition> arrayFilters = null)
            : this(CollectionNamespace.FromFullName(collectionNamespace), filter, update, collation, hint, isUpsert, arrayFilters, sort)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteUpdateOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="update">Update definition.</param>
        /// <param name="sort">The sort definition to use.</param>
        /// <param name="collation">Specifies a collation.</param>
        /// <param name="hint">The index to use.</param>
        /// <param name="isUpsert">Indicating whether to insert the document if it doesn't already exist.</param>
        /// <param name="arrayFilters">A set of filters specifying to which array elements an update should apply.</param>
        public BulkWriteUpdateOneModel(
            CollectionNamespace collectionNamespace,
            FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update,
            SortDefinition<TDocument> sort,
            Collation collation = null,
            BsonValue hint = null,
            bool isUpsert = false,
            IEnumerable<ArrayFilterDefinition> arrayFilters = null)
            : this(collectionNamespace, filter, update, collation, hint, isUpsert, arrayFilters, sort)
        {
        }

        private BulkWriteUpdateOneModel(
            CollectionNamespace collectionNamespace,
            FilterDefinition<TDocument> filter,
            UpdateDefinition<TDocument> update,
            Collation collation = null,
            BsonValue hint = null,
            bool isUpsert = false,
            IEnumerable<ArrayFilterDefinition> arrayFilters = null,
            SortDefinition<TDocument> sort = null)
            : base(collectionNamespace)
        {
            Filter = Ensure.IsNotNull(filter, nameof(filter));
            Update = Ensure.IsNotNull(update, nameof(update));
            Collation = collation;
            Hint = hint;
            IsUpsert = isUpsert;
            ArrayFilters = arrayFilters;
            Sort = sort;
        }

        /// <summary>
        /// A set of filters specifying to which array elements an update should apply.
        /// </summary>
        public IEnumerable<ArrayFilterDefinition> ArrayFilters { get; init; }

        /// <summary>
        /// Specifies a collation.
        /// </summary>
        public Collation Collation { get; init; }

        /// <summary>
        /// The filter to apply.
        /// </summary>
        public FilterDefinition<TDocument> Filter { get; init; }

        /// <summary>
        /// The index to use.
        /// </summary>
        public BsonValue Hint { get; init; }

        /// <summary>
        /// Indicating whether to insert the document if it doesn't already exist.
        /// </summary>
        public bool IsUpsert { get; init; }

        /// <summary>
        /// The sort definition to use.
        /// </summary>
        public SortDefinition<TDocument> Sort { get; init; }

        /// <summary>
        /// Update definition.
        /// </summary>
        public UpdateDefinition<TDocument> Update { get; init; }

        internal override bool IsMulti => false;

        internal override void Render(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, IBulkWriteModelRenderer renderer)
            => renderer.RenderUpdateOne(renderArgs, serializationContext, this);
    }
}

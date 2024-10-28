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
    /// Represents delete one operation in the scope of BulkWrite operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class BulkWriteDeleteOneModel<TDocument> : BulkWriteModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteDeleteOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="collation">Specifies a collation.</param>
        /// <param name="hint">The index to use.</param>
        public BulkWriteDeleteOneModel(
            string collectionNamespace,
            FilterDefinition<TDocument> filter,
            Collation collation = null,
            BsonValue hint = null)
            : this(CollectionNamespace.FromFullName(collectionNamespace), filter, collation, hint)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteDeleteOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="collation">Specifies a collation.</param>
        /// <param name="hint">The index to use.</param>
        public BulkWriteDeleteOneModel(
            CollectionNamespace collectionNamespace,
            FilterDefinition<TDocument> filter,
            Collation collation = null,
            BsonValue hint = null)
            : base(collectionNamespace)
        {
            Filter = Ensure.IsNotNull(filter, nameof(filter));
            Collation = collation;
            Hint = hint;
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

        internal override bool IsMulti => false;

        internal override void Render(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, IBulkWriteModelRenderer renderer)
            => renderer.RenderDeleteOne(renderArgs, serializationContext, this);
    }
}

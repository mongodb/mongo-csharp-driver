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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents insert one operation in the scope of BulkWrite operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class BulkWriteInsertOneModel<TDocument> : BulkWriteModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteInsertOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="document">The document.</param>
        public BulkWriteInsertOneModel(
            string collectionNamespace,
            TDocument document)
            : this(CollectionNamespace.FromFullName(collectionNamespace), document)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteInsertOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="document">The document.</param>
        public BulkWriteInsertOneModel(
            CollectionNamespace collectionNamespace,
            TDocument document)
        : base(collectionNamespace)
        {
            Document = document;
        }

        /// <summary>
        /// The document to insert
        /// </summary>
        public TDocument Document { get; }

        internal override bool IsMulti => false;

        internal override void Render(RenderArgs<BsonDocument> renderArgs, BsonSerializationContext serializationContext, IBulkWriteModelRenderer renderer)
            => renderer.RenderInsertOne(renderArgs, serializationContext, this);
    }
}

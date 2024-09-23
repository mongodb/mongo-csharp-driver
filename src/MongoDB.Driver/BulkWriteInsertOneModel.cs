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

using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents insert one operation in scope of BulkWrite operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class BulkWriteInsertOneModel<TDocument> : BulkWriteModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteInsertOneModel{TDocument}"/> class.
        /// </summary>
        public BulkWriteInsertOneModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteInsertOneModel{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">Collection on which the operation should be performed.</param>
        /// <param name="document">The document.</param>
        public BulkWriteInsertOneModel(CollectionNamespace collectionNamespace, TDocument document)
        {
            Namespace = collectionNamespace;
            Document = document;
        }

        /// <summary>
        /// The document to insert
        /// </summary>
        public TDocument Document { get; init; }

        internal override int WriteTo(BulkWriteModelSerializationContext context)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("insert");
            writer.WriteInt32(context.NsInfo.IndexOf(Namespace.FullName));
            writer.WriteName("document");
            BsonDocumentSerializer.Instance.Serialize(context, Document);
            writer.WriteEndDocument();
        }
    }
}

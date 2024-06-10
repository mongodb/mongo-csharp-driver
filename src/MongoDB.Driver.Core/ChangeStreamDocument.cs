/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver
{
    /// <summary>
    /// An output document from a $changeStream pipeline stage.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    [BsonSerializer(typeof(ChangeStreamDocumentSerializer<>))]
    public sealed class ChangeStreamDocument<TDocument> : BsonDocumentBackedClass
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeStreamDocument{TDocument}"/> class.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        /// <param name="documentSerializer">The document serializer.</param>
        public ChangeStreamDocument(
            BsonDocument backingDocument,
            IBsonSerializer<TDocument> documentSerializer)
            : base(backingDocument, new ChangeStreamDocumentSerializer<TDocument>(documentSerializer))
        {
        }

        // public properties
        /// <summary>
        /// Gets the backing document.
        /// </summary>
        new public BsonDocument BackingDocument => base.BackingDocument;

        /// <summary>
        /// Gets the cluster time.
        /// </summary>
        /// <value>
        /// The cluster time.
        /// </value>
        public BsonTimestamp ClusterTime => GetValue<BsonTimestamp>(nameof(ClusterTime), null);

        /// <summary>
        /// Gets the namespace of the collection.
        /// </summary>
        /// <value>
        /// The namespace of the collection.
        /// </value>
        public CollectionNamespace CollectionNamespace => GetValue<CollectionNamespace>(nameof(CollectionNamespace), null);

        /// <summary>
        /// Gets ui field from the oplog entry corresponding to the change event.
        /// Only present when the showExpandedEvents change stream option is enabled and for the following event types (MongoDB 6.0 and later):
        /// <list type="bullet">
        ///     <item><description><see cref="ChangeStreamOperationType.Create"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.CreateIndexes"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.Delete"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.Drop"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.DropIndexes"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.Insert"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.Modify"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.RefineCollectionShardKey"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.ReshardCollection"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.ShardCollection"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.Update"/></description></item>
        /// </list>
        /// </summary>
        /// <value>
        /// The UUID of the collection.
        /// </value>
        public Guid? CollectionUuid => GetValue<Guid?>(nameof(CollectionUuid), null);

        /// <summary>
        /// Gets the database namespace.
        /// </summary>
        /// <value>
        /// The database namespace.
        /// </value>
        public DatabaseNamespace DatabaseNamespace => GetValue<DatabaseNamespace>(nameof(DatabaseNamespace), null);

        /// <summary>
        /// Gets the disambiguated paths if present.
        /// </summary>
        /// <value>
        /// The disambiguated paths.
        /// </value>
        /// <remarks>
        /// <para>
        /// A document containing a map that associates an update path to an array containing the path components used in the update document. This data
        /// can be used in combination with the other fields in an <see cref="ChangeStreamDocument{TDocument}.UpdateDescription"/> to determine the
        /// actual path in the document that was updated. This is necessary in cases where a key contains dot-separated strings (i.e. <c>{ "a.b": "c" }</c>) or
        /// a document contains a numeric literal string key (i.e. <c>{ "a": { "0": "a" } }</c>). Note that in this scenario, the numeric key can't be the top
        /// level key because <c>{ "0": "a" }</c> is not ambiguous - update paths would simply be <c>'0'</c> which is unambiguous because BSON documents cannot have
        /// arrays at the top level. Each entry in the document maps an update path to an array which contains the actual path used when the document
        /// was updated. For example, given a document with the following shape <c>{ "a": { "0": 0 } }</c> and an update of <c>{ $inc: { "a.0": 1 } }</c>,
        /// <see cref="ChangeStreamDocument{TDocument}.DisambiguatedPaths"/> would look like the following:
        /// </para>
        /// <code>
        ///   {
        ///      "a.0": ["a", "0"]
        ///   }
        /// </code>
        /// <para>
        /// In each array, all elements will be returned as strings with the exception of array indices, which will be returned as 32-bit integers.
        /// </para>
        /// <para>
        /// Added in MongoDB version 6.1.0.
        /// </para>
        /// </remarks>
        public BsonDocument DisambiguatedPaths => GetValue<BsonDocument>(nameof(DisambiguatedPaths), null);

        /// <summary>
        /// Gets the document key.
        /// </summary>
        /// <value>
        /// The document key.
        /// </value>
        public BsonDocument DocumentKey => GetValue<BsonDocument>(nameof(DocumentKey), null);

        /// <summary>
        /// Gets the full document.
        /// </summary>
        /// <value>
        /// The full document.
        /// </value>
        public TDocument FullDocument
        {
            get
            {
                // if TDocument is BsonDocument avoid deserializing it again to prevent possible duplicate element name errors
                if (typeof(TDocument) == typeof(BsonDocument) && BackingDocument.TryGetValue("fullDocument", out var fullDocument))
                {
                    if (fullDocument.IsBsonNull)
                    {
                        return default;
                    }

                    return (TDocument)(object)fullDocument.AsBsonDocument;
                }
                else
                {
                    return GetValue<TDocument>(nameof(FullDocument), default(TDocument));
                }
            }
        }

        /// <summary>
        /// Gets the full document before change.
        /// </summary>
        /// <value>
        /// The full document before change.
        /// </value>
        public TDocument FullDocumentBeforeChange
        {
            get
            {
                // if TDocument is BsonDocument avoid deserializing it again to prevent possible duplicate element name errors
                if (typeof(TDocument) == typeof(BsonDocument) && BackingDocument.TryGetValue("fullDocumentBeforeChange", out var fullDocumentBeforeChange))
                {
                    if (fullDocumentBeforeChange.IsBsonNull)
                    {
                        return default;
                    }

                    return (TDocument)(object)fullDocumentBeforeChange.AsBsonDocument;
                }
                else
                {
                    return GetValue<TDocument>(nameof(FullDocumentBeforeChange), default(TDocument));
                }
            }
        }

        /// <summary>
        /// Gets the description for the operation.
        /// Only present when the showExpandedEvents change stream option is enabled and for the following event types (MongoDB 6.0 and later):
        /// <list type="bullet">
        ///     <item><description><see cref="ChangeStreamOperationType.Create"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.CreateIndexes"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.DropIndexes"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.Modify"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.RefineCollectionShardKey"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.Rename"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.ReshardCollection"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.ShardCollection"/></description></item>
        /// </list>
        /// </summary>
        /// <value>
        /// The description of the operation.
        /// </value>
        public BsonDocument OperationDescription => GetValue<BsonDocument>(nameof(OperationDescription), null);

        /// <summary>
        /// Gets the type of the operation.
        /// </summary>
        /// <value>
        /// The type of the operation.
        /// </value>
        public ChangeStreamOperationType OperationType => GetValue<ChangeStreamOperationType>(nameof(OperationType), (ChangeStreamOperationType)(-1));

        /// <summary>
        /// Gets the new namespace for the ns collection. This field is omitted for all operation types except "rename".
        /// </summary>
        /// <value>
        /// The new namespace of the ns collection.
        /// </value>
        public CollectionNamespace RenameTo => GetValue<CollectionNamespace>(nameof(RenameTo), null);

        /// <summary>
        /// Gets the resume token.
        /// </summary>
        /// <value>
        /// The resume token.
        /// </value>
        public BsonDocument ResumeToken => GetValue<BsonDocument>(nameof(ResumeToken), null);

        /// <summary>
        /// Gets the split event.
        /// </summary>
        public ChangeStreamSplitEvent SplitEvent => GetValue<ChangeStreamSplitEvent>(nameof(SplitEvent), null);

        /// <summary>
        /// Gets the update description.
        /// </summary>
        /// <value>
        /// The update description.
        /// </value>
        public ChangeStreamUpdateDescription UpdateDescription => GetValue<ChangeStreamUpdateDescription>(nameof(UpdateDescription), null);

        /// <summary>
        /// Gets the wall time of the change stream event.
        /// </summary>
        /// <value>
        /// The wall time.
        /// </value>
        public DateTime? WallTime => GetValue<DateTime?>(nameof(WallTime), null);
    }
}

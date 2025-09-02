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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Defines an Atlas vector search index model using strongly-typed C# APIs.
    /// </summary>
    public class CreateAtlasVectorIndexModel<TDocument> : CreateSearchIndexModel
    {
        private readonly RenderArgs<TDocument> _renderArgs
            = new(BsonSerializer.LookupSerializer<TDocument>(), BsonSerializer.SerializerRegistry);

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSearchIndexModel"/> class, passing the required
        /// options for <see cref="VectorSimilarity"/> and number of vector dimensions to the constructor.
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="field">The field containing the vectors to index.</param>
        /// <param name="similarity">The <see cref="VectorSimilarity"/> to use to search for top K-nearest neighbors.</param>
        /// <param name="dimensions">Number of vector dimensions that Atlas Vector Search enforces at index-time and query-time.</param>
        /// <param name="filterFields">Fields that may be used as filters in the vector query.</param>
        public CreateAtlasVectorIndexModel(
            FieldDefinition<TDocument> field,
            string name,
            VectorSimilarity similarity,
            int dimensions,
            params FieldDefinition<TDocument>[] filterFields)
            : base(name, SearchIndexType.VectorSearch)
        {
            Field = field;
            Similarity = similarity;
            Dimensions = dimensions;
            FilterFields = filterFields?.ToList() ?? [];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSearchIndexModel"/> class, passing the required
        /// options for <see cref="VectorSimilarity"/> and number of vector dimensions to the constructor.
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="field">An expression pointing to the field containing the vectors to index.</param>
        /// <param name="similarity">The <see cref="VectorSimilarity"/> to use to search for top K-nearest neighbors.</param>
        /// <param name="dimensions">Number of vector dimensions that Atlas Vector Search enforces at index-time and query-time.</param>
        /// <param name="filterFields">Expressions pointing to fields that may be used as filters in the vector query.</param>
        public CreateAtlasVectorIndexModel(
            Expression<Func<TDocument, object>> field,
            string name,
            VectorSimilarity similarity,
            int dimensions,
            params Expression<Func<TDocument, object>>[] filterFields)
            : this(
                new ExpressionFieldDefinition<TDocument>(field),
                name,
                similarity,
                dimensions,
                filterFields?.Select(f => (FieldDefinition<TDocument>)new ExpressionFieldDefinition<TDocument>(f)).ToArray())
        {
            Similarity = similarity;
            Dimensions = dimensions;
        }

        /// <summary>
        /// The field containing the vectors to index.
        /// </summary>
        public FieldDefinition<TDocument> Field { get; }

        /// <summary>
        /// The <see cref="VectorSimilarity"/> to use to search for top K-nearest neighbors.
        /// </summary>
        public VectorSimilarity Similarity { get; }

        /// <summary>
        /// Number of vector dimensions that Atlas Vector Search enforces at index-time and query-time.
        /// </summary>
        public int Dimensions { get; }

        /// <summary>
        /// Fields that may be used as filters in the vector query.
        /// </summary>
        public IReadOnlyList<FieldDefinition<TDocument>> FilterFields { get; }

        /// <summary>
        /// Type of automatic vector quantization for your vectors.
        /// </summary>
        public VectorQuantization? Quantization { get; init; }

        /// <summary>
        /// Maximum number of edges (or connections) that a node can have in the Hierarchical Navigable Small Worlds graph.
        /// </summary>
        public int? HnswMaxEdges { get; init; }

        /// <summary>
        /// Analogous to numCandidates at query-time, this parameter controls the maximum number of nodes to evaluate to find the closest neighbors to connect to a new node.
        /// </summary>
        public int? HnswNumEdgeCandidates { get; init; }

        // /// <summary>Paths to properties that may be used as filters on the entity type or its nested types.</summary>
        // public IReadOnlyList<string> FilterPaths { get; init; }

        /// <inheritdoc/>
        public override SearchIndexType? Type
            => SearchIndexType.VectorSearch;

        /// <inheritdoc/>
        public override BsonDocument Definition
        {
            get
            {
                if (base.Definition != null)
                {
                    return base.Definition;
                }

                var similarityValue = Similarity == VectorSimilarity.DotProduct
                    ? "dotProduct" // Because neither "DotProduct" or "dotproduct" are allowed.
                    : Similarity.ToString().ToLowerInvariant();

                var vectorField = new BsonDocument
                {
                    { "type", BsonString.Create("vector") },
                    { "path", Field.Render(_renderArgs).FieldName },
                    { "numDimensions", BsonInt32.Create(Dimensions) },
                    { "similarity", BsonString.Create(similarityValue) },
                };

                if (Quantization.HasValue)
                {
                    vectorField.Add("quantization", BsonString.Create(Quantization.ToString()?.ToLower()));
                }

                if (HnswMaxEdges != null || HnswNumEdgeCandidates != null)
                {
                    var hnswDocument = new BsonDocument
                    {
                        { "maxEdges", BsonInt32.Create(HnswMaxEdges ?? 16) },
                        { "numEdgeCandidates", BsonInt32.Create(HnswNumEdgeCandidates ?? 100) }
                    };
                    vectorField.Add("hnswOptions", hnswDocument);
                }

                var fieldDocuments = new List<BsonDocument> { vectorField };

                if (FilterFields != null)
                {
                    foreach (var filterPath in FilterFields)
                    {
                        var fieldDocument = new BsonDocument
                        {
                            { "type", BsonString.Create("filter") },
                            { "path", BsonString.Create(filterPath.Render(_renderArgs).FieldName) }
                        };

                        fieldDocuments.Add(fieldDocument);
                    }
                }

                base.Definition = new BsonDocument { { "fields", BsonArray.Create(fieldDocuments) } };

                return base.Definition;
            }
        }
    }

    /// <summary>
    /// Defines an Atlas vector search index model using strongly-typed C# APIs.
    /// </summary>
    public class CreateAtlasVectorIndexModel : CreateAtlasVectorIndexModel<BsonDocument>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSearchIndexModel"/> class, passing the required
        /// options for <see cref="VectorSimilarity"/> and number of vector dimensions to the constructor.
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="field">The field containing the vectors to index.</param>
        /// <param name="similarity">The <see cref="VectorSimilarity"/> to use to search for top K-nearest neighbors.</param>
        /// <param name="dimensions">Number of vector dimensions that Atlas Vector Search enforces at index-time and query-time.</param>
        /// <param name="filterFields">Fields that may be used as filters in the vector query.</param>
        public CreateAtlasVectorIndexModel(
            FieldDefinition<BsonDocument> field,
            string name,
            VectorSimilarity similarity,
            int dimensions,
            params FieldDefinition<BsonDocument>[] filterFields)
            : base(field, name, similarity, dimensions, filterFields)
        {
        }
    }


    /// <summary>
    /// TODO
    /// </summary>
    public class CreateAtlasSearchIndexModel
    {
    }
}

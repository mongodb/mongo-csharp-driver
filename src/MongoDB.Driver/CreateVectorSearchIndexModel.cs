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

namespace MongoDB.Driver;

/// <summary>
/// Defines a vector index model using strongly-typed C# APIs.
/// </summary>
public sealed class CreateVectorSearchIndexModel<TDocument> : CreateSearchIndexModel
{
    /// <summary>
    /// The field containing the vectors to index.
    /// </summary>
    public FieldDefinition<TDocument> Field { get; }

    /// <summary>
    /// The <see cref="VectorSimilarity"/> to use to search for top K-nearest neighbors. Not used for auto-embedding
    /// vector indexes.
    /// </summary>
    public VectorSimilarity Similarity { get; }

    /// <summary>
    /// Number of vector dimensions that vector search enforces at index-time and query-time. For auto-embedding
    /// indexes, this is only used when specifying explicit field compression using <see cref="Quantization"/>.
    /// </summary>
    public int Dimensions { get; init; }

    /// <summary>
    /// The name of the embedding model to use, such as "voyage-4", voyage-4-large", etc. Only used for auto-embedding
    /// vector indexes.
    /// </summary>
    public string AutoEmbeddingModelName { get; }

    /// <summary>
    /// Indicates the type of data that will be embedded for an auto-embedding index. Only used for auto-embedding
    /// vector indexes. Defaults to <see cref="VectorEmbeddingModality.Text"/>.
    /// </summary>
    public VectorEmbeddingModality Modality { get; init; } = VectorEmbeddingModality.Text;

    /// <summary>
    /// Fields that may be used as filters in the vector query.
    /// </summary>
    public IReadOnlyList<FieldDefinition<TDocument>> FilterFields { get; }

    /// <summary>
    /// Type of automatic vector quantization for your vectors. At most one of <see cref="Quantization"/> and
    /// <see cref="CompressionProfileName"/> can be used on any given index.
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

    /// <summary>
    /// Specifies the compression profile to use for auto-embedding indexes. For example, "storage_optimized",
    /// "balanced", or "accuracy_optimized". Only used by auto-embedding indexes. At most one
    /// of <see cref="Quantization"/> and <see cref="CompressionProfileName"/> can be used on any given index.
    /// </summary>
    public string CompressionProfileName { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateVectorSearchIndexModel{TDocument}"/> class for a vector
    /// index where the vector embeddings are created manually. The required options for <see cref="VectorSimilarity"/>
    /// and the number of vector dimensions are passed to the constructor.
    /// </summary>
    /// <param name="name">The index name.</param>
    /// <param name="field">The field containing the vectors to index.</param>
    /// <param name="similarity">The <see cref="VectorSimilarity"/> to use to search for top K-nearest neighbors.</param>
    /// <param name="dimensions">Number of vector dimensions that vector search enforces at index-time and query-time.</param>
    /// <param name="filterFields">Fields that may be used as filters in the vector query.</param>
    public CreateVectorSearchIndexModel(
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
        AutoEmbeddingModelName = null;
        FilterFields = filterFields?.ToList() ?? [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateVectorSearchIndexModel{TDocument}"/> class for a vector
    /// index where the vector embeddings are created manually. The required options for <see cref="VectorSimilarity"/>
    /// and the number of vector dimensions are passed to the constructor.
    /// </summary>
    /// <param name="name">The index name.</param>
    /// <param name="field">An expression pointing to the field containing the vectors to index.</param>
    /// <param name="similarity">The <see cref="VectorSimilarity"/> to use to search for top K-nearest neighbors.</param>
    /// <param name="dimensions">Number of vector dimensions that vector search enforces at index-time and query-time.</param>
    /// <param name="filterFields">Expressions pointing to fields that may be used as filters in the vector query.</param>
    public CreateVectorSearchIndexModel(
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
            filterFields?
                .Select(f => (FieldDefinition<TDocument>)new ExpressionFieldDefinition<TDocument>(f))
                .ToArray())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateVectorSearchIndexModel{TDocument}"/> for a vector index
    /// that will automatically create embeddings from a given field in the document. The embedding model to use must
    /// be passed to this constructor.
    /// </summary>
    /// <param name="name">The index name.</param>
    /// <param name="field">The field containing the vectors to index.</param>
    /// <param name="embeddingModelName">The name of the embedding model to use, such as "voyage-4", voyage-4-large", etc.</param>
    /// <param name="filterFields">Fields that may be used as filters in the vector query.</param>
    public CreateVectorSearchIndexModel(
        FieldDefinition<TDocument> field,
        string name,
        string embeddingModelName,
        params FieldDefinition<TDocument>[] filterFields)
        : base(name, SearchIndexType.VectorSearch)
    {
        Field = field;
        Similarity = default;
        Dimensions = -1;
        AutoEmbeddingModelName = embeddingModelName;
        FilterFields = filterFields?.ToList() ?? [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateVectorSearchIndexModel{TDocument}"/> for a vector index
    /// that will automatically create embeddings from a given field in the document. The embedding model to use must
    /// be passed to this constructor.
    /// </summary>
    /// <param name="name">The index name.</param>
    /// <param name="field">An expression pointing to the field containing the vectors to index.</param>
    /// <param name="embeddingModelName">The name of the embedding model to use, such as "voyage-4", voyage-4-large", etc.</param>
    /// <param name="filterFields">Expressions pointing to fields that may be used as filters in the vector query.</param>
    public CreateVectorSearchIndexModel(
        Expression<Func<TDocument, object>> field,
        string name,
        string embeddingModelName,
        params Expression<Func<TDocument, object>>[] filterFields)
        : this(
            new ExpressionFieldDefinition<TDocument>(field),
            name,
            embeddingModelName,
            filterFields?
                .Select(f => (FieldDefinition<TDocument>)new ExpressionFieldDefinition<TDocument>(f))
                .ToArray())
    {
    }

    /// <summary>
    /// Renders the index model to a <see cref="BsonDocument"/>.
    /// </summary>
    /// <param name="renderArgs">The render arguments.</param>
    /// <returns>A <see cref="BsonDocument" />.</returns>
    public BsonDocument Render(RenderArgs<TDocument> renderArgs)
    {
        var vectorField = new BsonDocument { { "path", Field.Render(renderArgs).FieldName }, };

        if (AutoEmbeddingModelName == null)
        {
            vectorField.Add("type", "vector");

            var similarityValue = Similarity == VectorSimilarity.DotProduct
                ? "dotProduct" // Because neither "DotProduct" or "dotproduct" are allowed.
                : Similarity.ToString().ToLowerInvariant();

            vectorField.Add("numDimensions", Dimensions);
            vectorField.Add("similarity", similarityValue);

            if (Quantization.HasValue)
            {
                vectorField.Add("quantization", Quantization.ToString()?.ToLowerInvariant());
            }
        }
        else
        {
            vectorField.Add("type", "autoEmbed");
            vectorField.Add("modality", Modality.ToString().ToLowerInvariant());
            vectorField.Add("model", AutoEmbeddingModelName);

            if (CompressionProfileName != null)
            {
                if (Quantization != null)
                {
                    throw new NotSupportedException(
                        $"Both compression profile and explicit compression options have been set for this index. Either set '{nameof(CompressionProfileName)}' or set '{nameof(Quantization)}' and '{nameof(Dimensions)}', but not both.");
                }

                // TODO: CSHARP-5763
                // Currently throws "Command createSearchIndexes failed: "userCommand.indexes[0].fields[0]" unrecognized field "compression"."
                // vectorField.Add("compression", CompressionProfileName);
            }
            else if (Quantization != null)
            {
                // TODO: CSHARP-5763
                // Currently throws "Command createSearchIndexes failed: "userCommand.indexes[0].fields[0]" unrecognized field "compression"."
                // vectorField.Add("compression", new BsonDocument
                // {
                //     { "quantization", Quantization.ToString()?.ToLowerInvariant() },
                //     { "dimensions", Dimensions }
                // });
            }
        }

        if (HnswMaxEdges != null || HnswNumEdgeCandidates != null)
        {
            // TODO: CSHARP-5763
            // Currently throws "Command createSearchIndexes failed: "userCommand.indexes[0].fields[0]" unrecognized field "hnswOptions"."
            if (AutoEmbeddingModelName == null)
            {
                vectorField.Add("hnswOptions",
                    new BsonDocument
                    {
                        { "maxEdges", HnswMaxEdges ?? 16 }, { "numEdgeCandidates", HnswNumEdgeCandidates ?? 100 }
                    });
            }
        }

        var fieldDocuments = new List<BsonDocument> { vectorField };

        if (FilterFields != null)
        {
            foreach (var filterPath in FilterFields)
            {
                var fieldDocument = new BsonDocument
                {
                    { "type", "filter" },
                    { "path", filterPath.Render(renderArgs).FieldName }
                };

                fieldDocuments.Add(fieldDocument);
            }
        }

        return new BsonDocument { { "fields", new BsonArray(fieldDocuments) } };
    }
}

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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver;

/// <summary>
/// Defines common parts of a vector index model using strongly-typed C# APIs.
/// </summary>
public abstract class CreateVectorSearchIndexModelBase<TDocument> : CreateSearchIndexModel
{
    /// <summary>
    /// The field containing the vectors to index.
    /// </summary>
    public FieldDefinition<TDocument> Field { get; }

    /// <summary>
    /// Fields that may be used as filters in the vector query.
    /// </summary>
    public IReadOnlyList<FieldDefinition<TDocument>> FilterFields { get; }

    /// <summary>
    /// The fields that must be stored in the index. Use
    /// <see cref="CreateVectorSearchIndexModel{TDocument}.WithIncludedStoredFields(FieldDefinition{TDocument}[])"/>
    /// or <see cref="CreateAutoEmbeddingVectorSearchIndexModel{TDocument}.WithIncludedStoredFields(FieldDefinition{TDocument}[])"/>
    /// to configure this.
    /// </summary>
    public IReadOnlyList<FieldDefinition<TDocument>> IncludedStoredFields { get; protected init; }

    /// <summary>
    /// The fields that must NOT be stored in the index. Use
    /// <see cref="CreateVectorSearchIndexModel{TDocument}.WithExcludedStoredFields(FieldDefinition{TDocument}[])"/>
    /// or <see cref="CreateAutoEmbeddingVectorSearchIndexModel{TDocument}.WithExcludedStoredFields(FieldDefinition{TDocument}[])"/>
    /// to configure this.
    /// </summary>
    public IReadOnlyList<FieldDefinition<TDocument>> ExcludedStoredFields { get; protected init; }

    /// <summary>
    /// Number of vector dimensions that vector search enforces at index-time and query-time, or uses to build
    /// the embeddings for auto-embedding indexes.
    /// </summary>
    public int Dimensions { get; init; }

    /// <summary>
    /// Type of automatic vector quantization for your vectors.
    /// </summary>
    public VectorQuantization? Quantization { get; init; }

    /// <summary>
    /// Maximum number of edges (or connections) that a node can have in the Hierarchical Navigable Small Worlds graph.
    /// </summary>
    public int? HnswMaxEdges { get; init; }

    /// <summary>
    /// Analogous to numCandidates at query-time, this parameter controls the maximum number of nodes to evaluate to
    /// find the closest neighbors to connect to a new node.
    /// </summary>
    public int? HnswNumEdgeCandidates { get; init; }

    /// <summary>
    /// Indexing algorithm used for the vector field. Defaults to <see cref="VectorIndexingMethod.Hnsw"/> if omitted.
    /// <see cref="VectorIndexingMethod.Flat"/> is incompatible with <see cref="HnswMaxEdges"/> and
    /// <see cref="HnswNumEdgeCandidates"/>.
    /// </summary>
    public VectorIndexingMethod? IndexingMethod { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateVectorSearchIndexModelBase{TDocument}"/> class for a vector
    /// index where the vector embeddings are created manually. The required options for <see cref="VectorSimilarity"/>
    /// and the number of vector dimensions are passed to the constructor.
    /// </summary>
    /// <param name="name">The index name.</param>
    /// <param name="field">The field containing the vectors to index.</param>
    /// <param name="filterFields">Fields that may be used as filters in the vector query.</param>
    protected CreateVectorSearchIndexModelBase(
        FieldDefinition<TDocument> field,
        string name,
        params FieldDefinition<TDocument>[] filterFields)
        : base(name, SearchIndexType.VectorSearch)
    {
        Ensure.IsNotNull(field, nameof(field));

        Field = field;
        FilterFields = filterFields?.ToList() ?? [];
    }

    /// <summary>
    /// Renders the index model to a <see cref="BsonDocument"/>.
    /// </summary>
    /// <param name="renderArgs">The render arguments.</param>
    /// <returns>A <see cref="BsonDocument" />.</returns>
    internal abstract BsonDocument Render(RenderArgs<TDocument> renderArgs);

    /// <summary>
    /// Called by subclasses to render the filters for the index fields section.
    /// </summary>
    /// <param name="renderArgs">The render args.</param>
    /// <param name="fields">The list into which fields should be added.</param>
    private protected void RenderFilterFields(RenderArgs<TDocument> renderArgs, List<BsonDocument> fields)
    {
        if (FilterFields != null)
        {
            foreach (var filterPath in FilterFields)
            {
                var fieldDocument = new BsonDocument
                {
                    { "type", "filter" }, { "path", filterPath.Render(renderArgs).FieldName }
                };

                fields.Add(fieldDocument);
            }
        }
    }

    /// <summary>
    /// Called by subclasses to render common top-level elements in the index definition.
    /// </summary>
    /// <param name="renderArgs">The render args.</param>
    /// <param name="indexDocument">The index document into which the elements will go.</param>
    private protected void RenderCommonElements(RenderArgs<TDocument> renderArgs, BsonDocument indexDocument)
    {
        var fieldName = Field.Render(renderArgs).FieldName;
        var dotPos = fieldName.LastIndexOf('.');
        if (dotPos > 0)
        {
            indexDocument.Add("nestedRoot", fieldName.Substring(0, dotPos));
        }

        var exclude = ExcludedStoredFields?.Any() == true;
        if (exclude || IncludedStoredFields?.Any() == true)
        {
            var fields = new BsonArray();
            foreach (var field in exclude ? ExcludedStoredFields : IncludedStoredFields)
            {
                fields.Add(field.Render(renderArgs).FieldName);
            }

            indexDocument.Add("storedSource", new BsonDocument { { exclude ? "exclude" : "include", fields } });
        }
    }

    /// <summary>
    /// Called by subclasses to render common elements in a field of the index definition.
    /// </summary>
    /// <param name="renderArgs">The render args.</param>
    /// <param name="fieldDocument">The field document into which the elements will go.</param>
    private protected void RenderCommonFieldElements(RenderArgs<TDocument> renderArgs, BsonDocument fieldDocument)
    {
        if (IndexingMethod == VectorIndexingMethod.Flat && (HnswMaxEdges != null || HnswNumEdgeCandidates != null))
        {
            throw new InvalidOperationException(
                $"{nameof(HnswMaxEdges)} and {nameof(HnswNumEdgeCandidates)} cannot be set when {nameof(IndexingMethod)} is {nameof(VectorIndexingMethod.Flat)}.");
        }

        if (Quantization != null && Quantization != VectorQuantization.None)
        {
            fieldDocument.Add("quantization", Quantization == VectorQuantization.BinaryNoRescore
                ? "binaryNoRescore"
                : Quantization.ToString().ToLowerInvariant());
        }

        if (HnswMaxEdges != null || HnswNumEdgeCandidates != null)
        {
            fieldDocument.Add("hnswOptions",
                new BsonDocument
                {
                    { "maxEdges", HnswMaxEdges ?? 16 }, { "numEdgeCandidates", HnswNumEdgeCandidates ?? 100 }
                });
        }

        if (IndexingMethod != null)
        {
            fieldDocument.Add("indexingMethod", IndexingMethod.ToString().ToLowerInvariant());
        }
    }
}

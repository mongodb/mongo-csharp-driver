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
/// Defines a vector index model for pre-embedded vector indexes using strongly-typed C# APIs.
/// </summary>
public sealed class CreateVectorSearchIndexModel<TDocument> : CreateVectorSearchIndexModelBase<TDocument>
{
    /// <summary>
    /// The <see cref="VectorSimilarity"/> to use to search for top K-nearest neighbors.
    /// </summary>
    public VectorSimilarity Similarity { get; }

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
        : base(field, name, filterFields)
    {
        Similarity = similarity;
        Dimensions = dimensions;
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
    /// Creates a new <see cref="CreateVectorSearchIndexModel{TDocument}"/> with the given fields configured
    /// to be stored in the index. Note that storing full documents might significantly impact
    /// performance during indexing and querying. Explicitly storing vector fields is not recommended.
    /// </summary>
    /// <param name="includedStoredFields">The fields to store.</param>
    /// <returns>A new model with the fields configured.</returns>
    public CreateVectorSearchIndexModel<TDocument> WithIncludedStoredFields(
        params FieldDefinition<TDocument>[] includedStoredFields)
        => new(Field, Name, Similarity, Dimensions, FilterFields.ToArray())
        {
            IncludedStoredFields = includedStoredFields,
            ExcludedStoredFields = null,
            Quantization = Quantization,
            HnswMaxEdges = HnswMaxEdges,
            HnswNumEdgeCandidates = HnswNumEdgeCandidates,
            IndexingMethod = IndexingMethod
        };

    /// <summary>
    /// Creates a new <see cref="CreateVectorSearchIndexModel{TDocument}"/> with the given fields configured
    /// to be stored in the index. Note that storing full documents might significantly impact
    /// performance during indexing and querying. Explicitly storing vector fields is not recommended.
    /// </summary>
    /// <param name="includedStoredFields">The fields to store.</param>
    /// <returns>A new model with the fields configured.</returns>
    public CreateVectorSearchIndexModel<TDocument> WithIncludedStoredFields(
        params Expression<Func<TDocument, object>>[] includedStoredFields)
        => WithIncludedStoredFields(includedStoredFields
            .Select(f => (FieldDefinition<TDocument>)new ExpressionFieldDefinition<TDocument>(f)).ToArray());

    /// <summary>
    /// Creates a new <see cref="CreateVectorSearchIndexModel{TDocument}"/> with the given fields configured
    /// to be excluded from being stored in the index. This is typically used to exclude vector fields from being
    /// stored when other fields should be stored.
    /// </summary>
    /// <param name="excludedStoredFields">The fields to exclude from being stored.</param>
    /// <returns>A new model with the fields configured.</returns>
    public CreateVectorSearchIndexModel<TDocument> WithExcludedStoredFields(
        params FieldDefinition<TDocument>[] excludedStoredFields)
        => new(Field, Name, Similarity, Dimensions, FilterFields.ToArray())
        {
            ExcludedStoredFields = excludedStoredFields,
            IncludedStoredFields = null,
            Quantization = Quantization,
            HnswMaxEdges = HnswMaxEdges,
            HnswNumEdgeCandidates = HnswNumEdgeCandidates,
            IndexingMethod = IndexingMethod
        };

    /// <summary>
    /// Creates a new <see cref="CreateVectorSearchIndexModel{TDocument}"/> with the given fields configured
    /// to be excluded from being stored in the index. This is typically used to exclude vector fields from being
    /// stored when other fields should be stored.
    /// </summary>
    /// <param name="excludedStoredFields">The fields to exclude from being stored.</param>
    /// <returns>A new model with the fields configured.</returns>
    public CreateVectorSearchIndexModel<TDocument> WithExcludedStoredFields(
        params Expression<Func<TDocument, object>>[] excludedStoredFields)
        => WithExcludedStoredFields(excludedStoredFields
            .Select(f => (FieldDefinition<TDocument>)new ExpressionFieldDefinition<TDocument>(f)).ToArray());

    /// <inheritdoc/>
    internal override BsonDocument Render(RenderArgs<TDocument> renderArgs)
    {
        var similarityValue = Similarity == VectorSimilarity.DotProduct
            ? "dotProduct" // Because neither "DotProduct" or "dotproduct" are allowed.
            : Similarity.ToString().ToLowerInvariant();

        var vectorField = new BsonDocument
        {
            { "type", "vector" },
            { "path", Field.Render(renderArgs).FieldName },
            { "numDimensions", Dimensions },
            { "similarity", similarityValue },
        };

        RenderCommonFieldElements(renderArgs, vectorField);

        var fieldDocuments = new List<BsonDocument> { vectorField };
        RenderFilterFields(renderArgs, fieldDocuments);

        var indexDefinition = new BsonDocument { { "fields", new BsonArray(fieldDocuments) } };
        RenderCommonElements(renderArgs, indexDefinition);

        return indexDefinition;
    }
}

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
/// Defines a vector index model for an auto-embedding vector index using strongly-typed C# APIs.
/// </summary>
public sealed class CreateAutoEmbeddingVectorSearchIndexModel<TDocument> : CreateVectorSearchIndexModelBase<TDocument>
{
    /// <summary>
    /// The name of the embedding model to use, such as "voyage-4", "voyage-4-large", etc.
    /// </summary>
    public string AutoEmbeddingModelName { get; }

    /// <summary>
    /// Indicates the type of data that will be embedded for an auto-embedding index.
    /// </summary>
    public VectorEmbeddingModality Modality { get; init; } = VectorEmbeddingModality.Text;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateVectorSearchIndexModel{TDocument}"/> for a vector index
    /// that will automatically create embeddings from a given field in the document. The embedding model to use must
    /// be passed to this constructor.
    /// </summary>
    /// <param name="name">The index name.</param>
    /// <param name="field">The field containing the vectors to index.</param>
    /// <param name="embeddingModelName">The name of the embedding model to use, such as "voyage-4", "voyage-4-large", etc.</param>
    /// <param name="filterFields">Fields that may be used as filters in the vector query.</param>
    public CreateAutoEmbeddingVectorSearchIndexModel(
        FieldDefinition<TDocument> field,
        string name,
        string embeddingModelName,
        params FieldDefinition<TDocument>[] filterFields)
        : base(field, name, filterFields)
    {
        AutoEmbeddingModelName = embeddingModelName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateVectorSearchIndexModel{TDocument}"/> for a vector index
    /// that will automatically create embeddings from a given field in the document. The embedding model to use must
    /// be passed to this constructor.
    /// </summary>
    /// <param name="name">The index name.</param>
    /// <param name="field">An expression pointing to the field containing the vectors to index.</param>
    /// <param name="embeddingModelName">The name of the embedding model to use, such as "voyage-4", "voyage-4-large", etc.</param>
    /// <param name="filterFields">Expressions pointing to fields that may be used as filters in the vector query.</param>
    public CreateAutoEmbeddingVectorSearchIndexModel(
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

    /// <inheritdoc/>
    public override BsonDocument Render(RenderArgs<TDocument> renderArgs)
    {
        var vectorField = new BsonDocument
        {
            { "type", "autoEmbed" },
            { "path", Field.Render(renderArgs).FieldName },
            { "modality", Modality.ToString().ToLowerInvariant() },
            { "model", AutoEmbeddingModelName },
        };

        var fieldDocuments = new List<BsonDocument> { vectorField };
        RenderFilterFields(renderArgs, fieldDocuments);
        return new BsonDocument { { "fields", new BsonArray(fieldDocuments) } };
    }
}

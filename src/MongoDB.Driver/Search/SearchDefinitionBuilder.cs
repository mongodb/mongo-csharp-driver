// Copyright 2010-present MongoDB Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// A builder for a search definition.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SearchDefinitionBuilder<TDocument>
    {
        /// <summary>
        /// Creates a search definition that performs full-text search using the analyzer specified
        /// in the index configuration.
        /// </summary>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="fuzzy">The options for fuzzy search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A text search definition.</returns>
        public SearchDefinition<TDocument> Text(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            FuzzyOptions fuzzy = null,
            ScoreDefinition<TDocument> score = null) =>
            new TextSearchDefinition<TDocument>(query, path, fuzzy, score);

        /// <summary>
        /// Creates a search definition that performs full-text search using the analyzer specified
        /// in the index configuration.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="path">The indexed field or field to search.</param>
        /// <param name="fuzzy">The options for fuzzy search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A text search definition.</returns>
        public SearchDefinition<TDocument> Text<TField>(
            QueryDefinition query,
            Expression<Func<TDocument, TField>> path,
            FuzzyOptions fuzzy = null,
            ScoreDefinition<TDocument> score = null) =>
            Text(query, new ExpressionFieldDefinition<TDocument>(path), fuzzy, score);
    }

    internal sealed class TextSearchDefinition<TDocument> : SearchDefinition<TDocument>
    {
        private readonly QueryDefinition _query;
        private readonly PathDefinition<TDocument> _path;
        private readonly FuzzyOptions _fuzzy;
        private readonly ScoreDefinition<TDocument> _score;

        public TextSearchDefinition(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            FuzzyOptions fuzzy,
            ScoreDefinition<TDocument> score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _path = Ensure.IsNotNull(path, nameof(path));
            _fuzzy = fuzzy;
            _score = score;
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) => new()
        {
            {
                "text",
                new BsonDocument()
                {
                    { "query", _query.Render() },
                    { "path", _path.Render(documentSerializer, serializerRegistry) },
                    { "fuzzy", () => _fuzzy.Render(), _fuzzy != null },
                    { "score", () => _score.Render(documentSerializer, serializerRegistry), _score != null }
                }
            }
        };
    }
}

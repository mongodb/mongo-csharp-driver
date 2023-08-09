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

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Base class for span clauses.
    /// </summary>
    /// <typeparam name="TDocument"></typeparam>
    public abstract class SearchSpanDefinition<TDocument>
    {
        /// <summary>
        /// Span clause type.
        /// </summary>
        private protected enum ClauseType
        {
            First,
            Near,
            Or,
            Subtract,
            Term
        }

        private readonly ClauseType _clauseType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchSpanDefinition{TDocument}"/> class.
        /// </summary>
        /// <param name="clauseType">The clause type.</param>
        private protected SearchSpanDefinition(ClauseType clauseType) => _clauseType = clauseType;

        /// <summary>
        /// Renders the span clause to a <see cref="BsonDocument"/>.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <returns>A <see cref="BsonDocument"/>.</returns>
        public BsonDocument Render(SearchDefinitionRenderContext<TDocument> renderContext) =>
            new(_clauseType.ToCamelCase(), RenderClause(renderContext));

        private protected virtual BsonDocument RenderClause(SearchDefinitionRenderContext<TDocument> renderContext) => new();
    }
}

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

using MongoDB.Driver.Search;

namespace MongoDB.Driver
{
    /// <summary>
    /// A static helper class containing various builders.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public static class Builders<TDocument>
    {
        /// <summary>Gets a <see cref="FilterDefinitionBuilder{TDocument}"/>.</summary>
        public static FilterDefinitionBuilder<TDocument> Filter { get; } = new FilterDefinitionBuilder<TDocument>();

        /// <summary>Gets an <see cref="IndexKeysDefinitionBuilder{TDocument}"/>.</summary>
        public static IndexKeysDefinitionBuilder<TDocument> IndexKeys { get; } = new IndexKeysDefinitionBuilder<TDocument>();

        /// <summary>Gets a <see cref="ProjectionDefinitionBuilder{TDocument}"/>.</summary>
        public static ProjectionDefinitionBuilder<TDocument> Projection { get; } = new ProjectionDefinitionBuilder<TDocument>();

        /// <summary>Gets a <see cref="SetFieldDefinitionsBuilder{TDocument}"/>.</summary>
        public static SetFieldDefinitionsBuilder<TDocument> SetFields { get; } = new SetFieldDefinitionsBuilder<TDocument>();

        /// <summary>Gets a <see cref="SortDefinitionBuilder{TDocument}"/>.</summary>
        public static SortDefinitionBuilder<TDocument> Sort { get; } = new SortDefinitionBuilder<TDocument>();

        /// <summary>Gets an <see cref="UpdateDefinitionBuilder{TDocument}"/>.</summary>
        public static UpdateDefinitionBuilder<TDocument> Update { get; } = new UpdateDefinitionBuilder<TDocument>();

        // Search builders
        /// <summary>Gets a <see cref="SearchFacetBuilder{TDocument}"/>.</summary>
        public static SearchFacetBuilder<TDocument> SearchFacet { get; } = new SearchFacetBuilder<TDocument>();

        /// <summary>Gets a <see cref="SearchPathDefinition{TDocument}"/>.</summary>
        public static SearchPathDefinitionBuilder<TDocument> SearchPath { get; } = new SearchPathDefinitionBuilder<TDocument>();

        /// <summary>Gets a <see cref="SearchScoreDefinitionBuilder{TDocument}"/>.</summary>
        public static SearchScoreDefinitionBuilder<TDocument> SearchScore { get; } = new SearchScoreDefinitionBuilder<TDocument>();

        /// <summary>Gets a <see cref="SearchScoreFunctionBuilder{TDocument}"/>.</summary>
        public static SearchScoreFunctionBuilder<TDocument> SearchScoreFunction { get; } = new SearchScoreFunctionBuilder<TDocument>();

        /// <summary>Gets a <see cref="SearchDefinitionBuilder{TDocument}"/>.</summary>
        public static SearchDefinitionBuilder<TDocument> Search { get; } = new SearchDefinitionBuilder<TDocument>();

        /// <summary> Gets a <see cref="SearchSpanDefinitionBuilder{TDocument}"/>.</summary>
        public static SearchSpanDefinitionBuilder<TDocument> SearchSpan { get; } = new SearchSpanDefinitionBuilder<TDocument>();
    }
}

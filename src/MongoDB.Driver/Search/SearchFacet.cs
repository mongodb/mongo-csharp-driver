﻿/* Copyright 2010-present MongoDB Inc.
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
    /// Base class for search facets.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class SearchFacet<TDocument>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchFacet{TDocument}"/> class.
        /// </summary>
        /// <param name="name">The name of the facet.</param>
        protected SearchFacet(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the facet.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Renders the search facet to a <see cref="BsonDocument"/>.
        /// </summary>
        /// <param name="renderContext">The render context.</param>
        /// <returns>A <see cref="BsonDocument" />.</returns>
        public abstract BsonDocument Render(SearchDefinitionRenderContext<TDocument> renderContext);
    }
}

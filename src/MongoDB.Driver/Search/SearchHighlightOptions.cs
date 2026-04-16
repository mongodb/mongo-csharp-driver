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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Options for highlighting.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SearchHighlightOptions<TDocument>
    {
        private int? _maxCharsToExamine;
        private int? _maxNumPassages;
        private SearchPathDefinition<TDocument> _path;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchHighlightOptions{TValue}"/> class.
        /// </summary>
        /// <param name="path">The document field to search.</param>
        /// <param name="maxCharsToExamine">maximum number of characters to examine.</param>
        /// <param name="maxNumPassages">The number of high-scoring passages.</param>
        public SearchHighlightOptions(SearchPathDefinition<TDocument> path, int? maxCharsToExamine = null, int? maxNumPassages = null)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
            _maxCharsToExamine = maxCharsToExamine;
            _maxNumPassages = maxNumPassages;
        }

        /// <summary>
        /// Creates highlighting options.
        /// </summary>
        /// <param name="path">The document field to search.</param>
        /// <param name="maxCharsToExamine">
        /// The maximum number of characters to examine on a document when performing highlighting
        /// for a field.
        /// </param>
        /// <param name="maxNumPassages">
        /// The number of high-scoring passages to return per document in the highlighting results
        /// for each field.
        /// </param>
        /// <returns>Highlighting options.</returns>
        public SearchHighlightOptions(
            Expression<Func<TDocument, object>> path,
            int? maxCharsToExamine = null,
            int? maxNumPassages = null)
                : this(new ExpressionFieldDefinition<TDocument>(path), maxCharsToExamine, maxNumPassages)
        {
        }

        /// <summary>
        /// Gets or sets the maximum number of characters to examine on a document when performing
        /// highlighting for a field.
        /// </summary>
        public int? MaxCharsToExamine
        {
            get => _maxCharsToExamine;
            set => _maxCharsToExamine = Ensure.IsNullOrGreaterThanZero(value, nameof(value));
        }

        /// <summary>
        /// Gets or sets the number of high-scoring passages to return per document in the
        /// highlighting results for each field.
        /// </summary>
        public int? MaxNumPassages
        {
            get => _maxNumPassages;
            set => _maxNumPassages = Ensure.IsNullOrGreaterThanZero(value, nameof(value));
        }

        /// <summary>
        /// Gets or sets the document field to search.
        /// </summary>
        public SearchPathDefinition<TDocument> Path
        {
            get => _path;
            set => _path = Ensure.IsNotNull(value, nameof(value));
        }

        /// <summary>
        /// Renders the options to a <see cref="BsonDocument"/>.
        /// </summary>
        /// <param name="args">The render arguments.</param>
        /// <returns>A <see cref="BsonDocument" />.</returns>
        public BsonDocument Render(RenderArgs<TDocument> args)
            => new()
            {
                { "path", _path.Render(args) },
                { "maxCharsToExamine", _maxCharsToExamine, _maxCharsToExamine != null},
                { "maxNumPassages", _maxNumPassages, _maxNumPassages != null }
            };
    }
}

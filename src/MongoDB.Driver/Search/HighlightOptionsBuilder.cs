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

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// A build for highlighting options.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class HighlightOptionsBuilder<TDocument>
    {
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
        public HighlightOptions<TDocument> Options(
            PathDefinition<TDocument> path,
            int? maxCharsToExamine = null,
            int? maxNumPassages = null) => new()
        {
            Path = path,
            MaxCharsToExamine = maxCharsToExamine,
            MaxNumPassages = maxNumPassages
        };

        /// <summary>
        /// Creates highlighting options.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
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
        public HighlightOptions<TDocument> Options<TField>(
            Expression<Func<TDocument, TField>> path,
            int? maxCharsToExamine = null,
            int? maxNumPassages = null) =>
            Options(new ExpressionFieldDefinition<TDocument>(path), maxCharsToExamine, maxNumPassages);
    }
}

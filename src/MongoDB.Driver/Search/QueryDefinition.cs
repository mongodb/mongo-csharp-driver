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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Base class for search queries.
    /// </summary>
    public abstract class QueryDefinition
    {
        /// <summary>
        /// Renders the query to a <see cref="BsonValue"/>.
        /// </summary>
        /// <returns>A <see cref="BsonValue"/>.</returns>
        public abstract BsonValue Render();

        /// <summary>
        /// Performs an implicit conversion from a string to <see cref="QueryDefinition"/>.
        /// </summary>
        /// <param name="query">The string.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator QueryDefinition(string query) =>
            new SingleQueryDefinition(query);

        /// <summary>
        /// Performs an implicit conversion from an array of strings to <see cref="QueryDefinition"/>.
        /// </summary>
        /// <param name="queries">The array of strings.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator QueryDefinition(string[] queries) =>
            new MultiQueryDefinition(queries);

        /// <summary>
        /// Performs an implicit conversion from a list of strings to <see cref="QueryDefinition"/>.
        /// </summary>
        /// <param name="queries">The list of strings.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator QueryDefinition(List<string> queries) =>
            new MultiQueryDefinition(queries);
    }

    /// <summary>
    /// A query definition for a single string.
    /// </summary>
    public sealed class SingleQueryDefinition : QueryDefinition
    {
        private readonly string _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="SinglePathDefinition{TDocument}"/> class.
        /// </summary>
        /// <param name="query">The query string.</param>
        public SingleQueryDefinition(string query)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
        }

        /// <inheritdoc />
        public override BsonValue Render() => new BsonString(_query);
    }

    /// <summary>
    /// A query definition for multiple strings.
    /// </summary>
    public sealed class MultiQueryDefinition : QueryDefinition
    {
        private readonly IEnumerable<string> _queries;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiPathDefinition{TDocument}"/> class.
        /// </summary>
        /// <param name="queries">The query strings.</param>
        public MultiQueryDefinition(IEnumerable<string> queries)
        {
            _queries = Ensure.IsNotNull(queries, nameof(queries));
        }

        /// <inheritdoc/>
        public override BsonValue Render() => new BsonArray(_queries);
    }
}

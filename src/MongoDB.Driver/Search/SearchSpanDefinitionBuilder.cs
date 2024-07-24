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
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// A builder for a span clause.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SearchSpanDefinitionBuilder<TDocument>
    {
        /// <summary>
        /// Creates a span clause that matches near the beginning of the string.
        /// </summary>
        /// <param name="operator">The span operator.</param>
        /// <param name="endPositionLte">The highest position in which to match the query.</param>
        /// <returns>A first span clause.</returns>
        public SearchSpanDefinition<TDocument> First(SearchSpanDefinition<TDocument> @operator, int endPositionLte) =>
            new FirstSearchSpanDefinition<TDocument>(@operator, endPositionLte);

        /// <summary>
        /// Creates a span clause that matches multiple string found near each other.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <param name="slop">The allowable distance between words in the query phrase.</param>
        /// <param name="inOrder">Whether to require that the clauses appear in the specified order.</param>
        /// <returns>A near span clause.</returns>
        public SearchSpanDefinition<TDocument> Near(
            IEnumerable<SearchSpanDefinition<TDocument>> clauses,
            int slop,
            bool inOrder = false) =>
                new NearSearchSpanDefinition<TDocument>(clauses, slop, inOrder);

        /// <summary>
        /// Creates a span clause that matches any of its subclauses.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>An or span clause.</returns>
        public SearchSpanDefinition<TDocument> Or(IEnumerable<SearchSpanDefinition<TDocument>> clauses) =>
            new OrSearchSpanDefinition<TDocument>(clauses);

        /// <summary>
        /// Creates a span clause that matches any of its subclauses.
        /// </summary>
        /// <param name="clauses">The clauses.</param>
        /// <returns>An or span clause.</returns>
        public SearchSpanDefinition<TDocument> Or(params SearchSpanDefinition<TDocument>[] clauses) =>
            Or((IEnumerable<SearchSpanDefinition<TDocument>>)clauses);

        /// <summary>
        /// Creates a span clause that excludes certain strings from the search results.
        /// </summary>
        /// <param name="include">Clause to be included.</param>
        /// <param name="exclude">Clause to be excluded.</param>
        /// <returns>A subtract span clause.</returns>
        public SearchSpanDefinition<TDocument> Subtract(
            SearchSpanDefinition<TDocument> include,
            SearchSpanDefinition<TDocument> exclude) =>
                new SubtractSearchSpanDefinition<TDocument>(include, exclude);

        /// <summary>
        /// Creates a span clause that matches a single term.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <returns>A term span clause.</returns>
        public SearchSpanDefinition<TDocument> Term(SearchPathDefinition<TDocument> path, SearchQueryDefinition query) =>
            new TermSearchSpanDefinition<TDocument>(path, query);

        /// <summary>
        /// Creates a span clause that matches a single term.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="query">The string or string to search for.</param>
        /// <returns>A term span clause.</returns>
        public SearchSpanDefinition<TDocument> Term<TField>(
            Expression<Func<TDocument, TField>> path,
            SearchQueryDefinition query) =>
                Term(new ExpressionFieldDefinition<TDocument>(path), query);
    }

    internal sealed class FirstSearchSpanDefinition<TDocument> : SearchSpanDefinition<TDocument>
    {
        private readonly int _endPositionLte;
        private readonly SearchSpanDefinition<TDocument> _operator;

        public FirstSearchSpanDefinition(SearchSpanDefinition<TDocument> @operator, int endPositionLte)
            : base(ClauseType.First)
        {
            _operator = Ensure.IsNotNull(@operator, nameof(@operator));
            _endPositionLte = endPositionLte;
        }

        private protected override BsonDocument RenderClause(RenderArgs<TDocument> args) =>
          new()
          {
              { "operator", _operator.Render(args) },
              { "endPositionLte", _endPositionLte }
          };
    }

    internal sealed class NearSearchSpanDefinition<TDocument> : SearchSpanDefinition<TDocument>
    {
        private readonly List<SearchSpanDefinition<TDocument>> _clauses;
        private readonly bool _inOrder;
        private readonly int _slop;

        public NearSearchSpanDefinition(IEnumerable<SearchSpanDefinition<TDocument>> clauses, int slop, bool inOrder)
            : base(ClauseType.Near)
        {
            _clauses = Ensure.IsNotNull(clauses, nameof(clauses)).ToList();
            _slop = slop;
            _inOrder = inOrder;
        }

        private protected override BsonDocument RenderClause(RenderArgs<TDocument> args) =>
            new()
            {
                { "clauses", new BsonArray(_clauses.Select(clause => clause.Render(args))) },
                { "slop", _slop },
                { "inOrder", _inOrder },
            };
    }

    internal sealed class OrSearchSpanDefinition<TDocument> : SearchSpanDefinition<TDocument>
    {
        private readonly List<SearchSpanDefinition<TDocument>> _clauses;

        public OrSearchSpanDefinition(IEnumerable<SearchSpanDefinition<TDocument>> clauses)
            : base(ClauseType.Or)
        {
            _clauses = Ensure.IsNotNull(clauses, nameof(clauses)).ToList();
        }

        private protected override BsonDocument RenderClause(RenderArgs<TDocument> args) =>
            new("clauses", new BsonArray(_clauses.Select(clause => clause.Render(args))));
    }

    internal sealed class SubtractSearchSpanDefinition<TDocument> : SearchSpanDefinition<TDocument>
    {
        private readonly SearchSpanDefinition<TDocument> _exclude;
        private readonly SearchSpanDefinition<TDocument> _include;

        public SubtractSearchSpanDefinition(SearchSpanDefinition<TDocument> include, SearchSpanDefinition<TDocument> exclude)
            : base(ClauseType.Subtract)
        {
            _include = Ensure.IsNotNull(include, nameof(include));
            _exclude = Ensure.IsNotNull(exclude, nameof(exclude));
        }

        private protected override BsonDocument RenderClause(RenderArgs<TDocument> args) =>
            new()
            {
                { "include", _include.Render(args) },
                { "exclude", _exclude.Render(args) },
            };
    }

    internal sealed class TermSearchSpanDefinition<TDocument> : SearchSpanDefinition<TDocument>
    {
        private readonly SearchPathDefinition<TDocument> _path;
        private readonly SearchQueryDefinition _query;

        public TermSearchSpanDefinition(SearchPathDefinition<TDocument> path, SearchQueryDefinition query)
            : base(ClauseType.Term)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _path = Ensure.IsNotNull(path, nameof(path));
        }

        private protected override BsonDocument RenderClause(RenderArgs<TDocument> args) =>
            new()
            {
                { "query", _query.Render() },
                { "path", _path.Render(args) },
            };
    }
}

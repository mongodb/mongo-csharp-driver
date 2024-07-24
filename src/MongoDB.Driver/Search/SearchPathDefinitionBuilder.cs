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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// A builder for a search path.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SearchPathDefinitionBuilder<TDocument>
    {
        /// <summary>
        /// Creates a search path that searches using the specified analyzer.
        /// </summary>
        /// <param name="field">The field definition</param>
        /// <param name="analyzerName">The name of the analyzer.</param>
        /// <returns>An analyzer search path.</returns>
        public SearchPathDefinition<TDocument> Analyzer(FieldDefinition<TDocument> field, string analyzerName) =>
            new AnalyzerSearchPathDefinition<TDocument>(field, analyzerName);

        /// <summary>
        /// Creates a search path that searches using the specified analyzer.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="field">The field definition</param>
        /// <param name="analyzerName">The name of the analyzer.</param>
        /// <returns>An analyzer search path.</returns>
        public SearchPathDefinition<TDocument> Analyzer<TField>(Expression<Func<TDocument, TField>> field, string analyzerName) =>
            Analyzer(new ExpressionFieldDefinition<TDocument>(field), analyzerName);

        /// <summary>
        /// Creates a search path for multiple fields.
        /// </summary>
        /// <param name="fields">The collection of field definitions.</param>
        /// <returns>A multi-field search path.</returns>
        public SearchPathDefinition<TDocument> Multi(IEnumerable<FieldDefinition<TDocument>> fields) =>
            new MultiSearchPathDefinition<TDocument>(fields);

        /// <summary>
        /// Creates a search path for multiple fields.
        /// </summary>
        /// <param name="fields">The array of field definitions.</param>
        /// <returns>A multi-field search path.</returns>
        public SearchPathDefinition<TDocument> Multi(params FieldDefinition<TDocument>[] fields) =>
            Multi((IEnumerable<FieldDefinition<TDocument>>)fields);

        /// <summary>
        /// Creates a search path for multiple fields.
        /// </summary>
        /// <typeparam name="TField">The type of the fields.</typeparam>
        /// <param name="fields">The array of field definitions.</param>
        /// <returns>A multi-field search path.</returns>
        public SearchPathDefinition<TDocument> Multi<TField>(params Expression<Func<TDocument, TField>>[] fields) =>
            Multi(fields.Select(x => new ExpressionFieldDefinition<TDocument>(x)));

        /// <summary>
        /// Creates a search path for a single field.
        /// </summary>
        /// <param name="field">The field definition.</param>
        /// <returns>A single-field search path.</returns>
        public SearchPathDefinition<TDocument> Single(FieldDefinition<TDocument> field) =>
            new SingleSearchPathDefinition<TDocument>(field);

        /// <summary>
        /// Creates a search path for a single field.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="field">The field definition.</param>
        /// <returns>A single-field search path.</returns>
        public SearchPathDefinition<TDocument> Single<TField>(Expression<Func<TDocument, TField>> field) =>
            Single(new ExpressionFieldDefinition<TDocument>(field));

        /// <summary>
        /// Creates a search path that uses special characters in the field name
        /// that can match any character.
        /// </summary>
        /// <param name="query">
        /// The wildcard string that the field name must match.
        /// </param>
        /// <returns>A wildcard search path.</returns>
        public SearchPathDefinition<TDocument> Wildcard(string query) =>
            new WildcardSearchPathDefinition<TDocument>(query);
    }

    internal sealed class AnalyzerSearchPathDefinition<TDocument> : SearchPathDefinition<TDocument>
    {
        private readonly string _analyzerName;
        private readonly FieldDefinition<TDocument> _field;

        public AnalyzerSearchPathDefinition(FieldDefinition<TDocument> field, string analyzerName)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _analyzerName = Ensure.IsNotNull(analyzerName, nameof(analyzerName));
        }

        public override BsonValue Render(RenderArgs<TDocument> args) =>
            new BsonDocument()
            {
                {  "value", RenderField(_field, args) },
                {  "multi", _analyzerName }
            };
    }

    internal sealed class MultiSearchPathDefinition<TDocument> : SearchPathDefinition<TDocument>
    {
        private readonly FieldDefinition<TDocument>[] _fields;

        public MultiSearchPathDefinition(IEnumerable<FieldDefinition<TDocument>> fields)
        {
            _fields = Ensure.IsNotNull(fields, nameof(fields)).ToArray();
        }

        public override BsonValue Render(RenderArgs<TDocument> args) =>
            new BsonArray(_fields.Select(field => RenderField(field, args)));
    }

    internal sealed class SingleSearchPathDefinition<TDocument> : SearchPathDefinition<TDocument>
    {
        private readonly FieldDefinition<TDocument> _field;

        public SingleSearchPathDefinition(FieldDefinition<TDocument> field)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
        }

        public override BsonValue Render(RenderArgs<TDocument> args) =>
            RenderField(_field, args);
    }

    internal sealed class WildcardSearchPathDefinition<TDocument> : SearchPathDefinition<TDocument>
    {
        private readonly string _query;

        public WildcardSearchPathDefinition(string query)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
        }

        public override BsonValue Render(RenderArgs<TDocument> args) =>
            new BsonDocument("wildcard", _query);
    }
}

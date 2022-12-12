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
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// A builder for a search definition.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SearchDefinitionBuilder<TDocument>
    {
        /// <summary>
        /// Creates a search definition that performs a search for a word or phrase that contains
        /// a sequence of characters from an incomplete input string.
        /// </summary>
        /// <param name="query">The query definition specifying the string or strings to search for.</param>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="tokenOrder">The order in which to search for tokens.</param>
        /// <param name="fuzzy">The options for fuzzy search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>An autocomplete search definition.</returns>
        public SearchDefinition<TDocument> Autocomplete(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            AutocompleteTokenOrder tokenOrder = AutocompleteTokenOrder.Any,
            FuzzyOptions fuzzy = null,
            ScoreDefinition<TDocument> score = null) =>
            new AutocompleteSearchDefinition<TDocument>(query, path, tokenOrder, fuzzy, score);

        /// <summary>
        /// Creates a search definition that performs a search for a word or phrase that contains
        /// a sequence of characters from an incomplete search string.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="query">The query definition specifying the string or strings to search for.</param>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="tokenOrder">The order in which to search for tokens.</param>
        /// <param name="fuzzy">The options for fuzzy search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>An autocomplete search definition.</returns>
        public SearchDefinition<TDocument> Autocomplete<TField>(
            QueryDefinition query,
            Expression<Func<TDocument, TField>> path,
            AutocompleteTokenOrder tokenOrder = AutocompleteTokenOrder.Any,
            FuzzyOptions fuzzy = null,
            ScoreDefinition<TDocument> score = null)
            => Autocomplete(query, new ExpressionFieldDefinition<TDocument>(path), tokenOrder, fuzzy, score);

        /// <summary>
        /// Creates a search definition that combines two or more operators into a single query.
        /// </summary>
        /// <returns></returns>
        public CompoundFluent<TDocument> Compound() => new CompoundFluentImpl<TDocument>();

        /// <summary>
        /// Creates a search definition that queries for documents where an indexed field is equal
        /// to the specified value.
        /// </summary>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="value">The value to query for.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>An equality search definition.</returns>
        public SearchDefinition<TDocument> Equals(
            FieldDefinition<TDocument, bool> path,
            bool value,
            ScoreDefinition<TDocument> score = null) =>
            new EqualsSearchDefinition<TDocument>(path, value, score);

        /// <summary>
        /// Creates a search definition that queries for documents where an indexed field is equal
        /// to the specified value.
        /// </summary>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="value">The value to query for.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>An equality search definition.</returns>
        public SearchDefinition<TDocument> Equals(
            FieldDefinition<TDocument, ObjectId> path,
            ObjectId value,
            ScoreDefinition<TDocument> score = null) =>
            new EqualsSearchDefinition<TDocument>(path, value, score);

        /// <summary>
        /// Creates a search definition that queries for documents where an indexed field is equal
        /// to the specified value.
        /// </summary>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="value">The value to query for.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>An equality search definition.</returns>
        public SearchDefinition<TDocument> Equals(
            Expression<Func<TDocument, bool>> path,
            bool value,
            ScoreDefinition<TDocument> score = null) =>
            Equals(new ExpressionFieldDefinition<TDocument, bool>(path), value, score);

        /// <summary>
        /// Creates a search definition that queries for documents where an indexed field is equal
        /// to the specified value.
        /// </summary>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="value">The value to query for.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>An equality search definition.</returns>
        public SearchDefinition<TDocument> Equals(
            Expression<Func<TDocument, ObjectId>> path,
            ObjectId value,
            ScoreDefinition<TDocument> score = null) =>
            Equals(new ExpressionFieldDefinition<TDocument, ObjectId>(path), value, score);

        /// <summary>
        /// Creates a search definition that tests if a path to a specified indexed field name
        /// exists in a document.
        /// </summary>
        /// <param name="path">The field to test for.</param>
        /// <returns>An existence search definition.</returns>
        public SearchDefinition<TDocument> Exists(FieldDefinition<TDocument> path) =>
            new ExistsSearchDefinition<TDocument>(path);

        /// <summary>
        /// Creates a search definition that tests if a path to a specified indexed field name
        /// exists in a document.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The field to test for.</param>
        /// <returns>An existence search definition.</returns>
        public SearchDefinition<TDocument> Exists<TField>(Expression<Func<TDocument, TField>> path) =>
            Exists(new ExpressionFieldDefinition<TDocument>(path));

        /// <summary>
        /// Creates a search definition that groups results by values or ranges in the specified
        /// faceted fields and returns the count for each of those groups.
        /// </summary>
        /// <param name="operator">The operator to use to perform the facet over.</param>
        /// <param name="facets">Information for bucketing the data for each facet.</param>
        /// <returns>A facet search definition.</returns>
        public SearchDefinition<TDocument> Facet(
            SearchDefinition<TDocument> @operator,
            IEnumerable<SearchFacet<TDocument>> facets) =>
            new FacetSearchDefinition<TDocument>(@operator, facets);

        /// <summary>
        /// Creates a search definition that groups results by values or ranges in the specified
        /// faceted fields and returns the count for each of those groups.
        /// </summary>
        /// <param name="operator">The operator to use to perform the facet over.</param>
        /// <param name="facets">Information for bucketing the data for each facet.</param>
        /// <returns>A facet search definition.</returns>
        public SearchDefinition<TDocument> Facet(
            SearchDefinition<TDocument> @operator,
            params SearchFacet<TDocument>[] facets) =>
            Facet(@operator, (IEnumerable<SearchFacet<TDocument>>)facets);

        /// <summary>
        /// Creates a search definition that queries for shapes with a given geometry.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="geometry">
        /// GeoJSON object specifying the Polygon, MultiPolygon, or LineString shape or point
        /// to search.
        /// </param>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="relation">
        /// Relation of the query shape geometry to the indexed field geometry.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo shape search definition.</returns>
        public SearchDefinition<TDocument> GeoShape<TCoordinates>(
            GeoJsonGeometry<TCoordinates> geometry,
            PathDefinition<TDocument> path,
            GeoShapeRelation relation,
            ScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
            new GeoShapeSearchDefinition<TDocument, TCoordinates>(geometry, path, relation, score);

        /// <summary>
        /// Creates a search definition that queries for shapes with a given geometry.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="geometry">
        /// GeoJSON object specifying the Polygon, MultiPolygon, or LineString shape or point
        /// to search.
        /// </param>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="relation">
        /// Relation of the query shape geometry to the indexed field geometry.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo shape search definition.</returns>
        public SearchDefinition<TDocument> GeoShape<TCoordinates, TField>(
            GeoJsonGeometry<TCoordinates> geometry,
            Expression<Func<TDocument, TField>> path,
            GeoShapeRelation relation,
            ScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
            GeoShape(
                geometry,
                new ExpressionFieldDefinition<TDocument>(path),
                relation,
                score);

        /// <summary>
        /// Creates a search definition that queries for geographic points within a given
        /// geometry.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="geometry">
        /// GeoJSON object specifying the MultiPolygon or Polygon to search within.
        /// </param>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo within search definition.</returns>
        public SearchDefinition<TDocument> GeoWithin<TCoordinates>(
            GeoJsonGeometry<TCoordinates> geometry,
            PathDefinition<TDocument> path,
            ScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
            GeoWithin(new GeoWithinGeometry<TCoordinates>(geometry), path, score);

        /// <summary>
        /// Creates a search definition that queries for geographic points within a given
        /// geometry.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="geometry">
        /// GeoJSON object specifying the MultiPolygon or Polygon to search within.
        /// </param>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo within search definition.</returns>
        public SearchDefinition<TDocument> GeoWithin<TCoordinates, TField>(
            GeoJsonGeometry<TCoordinates> geometry,
            Expression<Func<TDocument, TField>> path,
            ScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
            GeoWithin(new GeoWithinGeometry<TCoordinates>(geometry), path, score);

        /// <summary>
        /// Creates a search definition that queries for geographic points within a given geo object.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="geoWithinQuery">Object that specifies the geo object to search within./// </param>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo within search definition.</returns>
        public SearchDefinition<TDocument> GeoWithin<TCoordinates, TField>(
            GeoWithin<TCoordinates> geoWithinQuery,
            Expression<Func<TDocument, TField>> path,
            ScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
            GeoWithin(geoWithinQuery, new ExpressionFieldDefinition<TDocument>(path), score);

        /// <summary>
        /// Creates a search definition that queries for geographic points within a given geo object.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="geoWithinQuery">Object that specifies the geo object to search within./// </param>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo within search definition.</returns>
        public SearchDefinition<TDocument> GeoWithin<TCoordinates>(
            GeoWithin<TCoordinates> geoWithinQuery,
            PathDefinition<TDocument> path,
            ScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
            new GeoWithinSearchDefinition<TDocument, TCoordinates>(geoWithinQuery, path, score);

        /// <summary>
        /// Creates a search definition that returns documents similar to the input documents.
        /// </summary>
        /// <param name="like">
        /// One or more documents that Atlas Search uses to extract representative terms for.
        /// </param>
        /// <returns>A more like this search definition.</returns>
        public SearchDefinition<TDocument> MoreLikeThis(IEnumerable<TDocument> like) =>
            new MoreLikeThisSearchDefinition<TDocument>(like);

        /// <summary>
        /// Creates a search definition that returns documents similar to the input documents.
        /// </summary>
        /// <param name="like">
        /// One or more documents that Atlas Search uses to extract representative terms for.
        /// </param>
        /// <returns>A more like this search definition.</returns>
        public SearchDefinition<TDocument> MoreLikeThis(params TDocument[] like) =>
            MoreLikeThis((IEnumerable<TDocument>)like);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to use to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near(
            PathDefinition<TDocument> path,
            double origin,
            double pivot,
            ScoreDefinition<TDocument> score = null) =>
            new NearSearchDefinition<TDocument>(path, origin, pivot, score);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to use to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near<TField>(
            Expression<Func<TDocument, TField>> path,
            double origin,
            double pivot,
            ScoreDefinition<TDocument> score = null) =>
            Near(new ExpressionFieldDefinition<TDocument>(path), origin, pivot, score);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to use to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near(
            PathDefinition<TDocument> path,
            int origin,
            int pivot,
            ScoreDefinition<TDocument> score = null) =>
            new NearSearchDefinition<TDocument>(path, new BsonInt32(origin), new BsonInt32(pivot), score);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to use to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near<TField>(
            Expression<Func<TDocument, TField>> path,
            int origin,
            int pivot,
            ScoreDefinition<TDocument> score = null) =>
            Near(new ExpressionFieldDefinition<TDocument>(path), origin, pivot, score);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to use to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near(
            PathDefinition<TDocument> path,
            long origin,
            long pivot,
            ScoreDefinition<TDocument> score = null) =>
            new NearSearchDefinition<TDocument>(path, new BsonInt64(origin), new BsonInt64(pivot), score);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to use to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near<TField>(
            Expression<Func<TDocument, TField>> path,
            long origin,
            long pivot,
            ScoreDefinition<TDocument> score = null) =>
            Near(new ExpressionFieldDefinition<TDocument>(path), origin, pivot, score);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to use to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near(
            PathDefinition<TDocument> path,
            DateTime origin,
            long pivot,
            ScoreDefinition<TDocument> score = null) =>
            new NearSearchDefinition<TDocument>(path, new BsonDateTime(origin), new BsonInt64(pivot), score);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to use to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near<TField>(
            Expression<Func<TDocument, TField>> path,
            DateTime origin,
            long pivot,
            ScoreDefinition<TDocument> score = null) =>
            Near(new ExpressionFieldDefinition<TDocument>(path), origin, pivot, score);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to use to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near<TCoordinates>(
            PathDefinition<TDocument> path,
            GeoJsonPoint<TCoordinates> origin,
            double pivot,
            ScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
            new NearSearchDefinition<TDocument>(path, origin.ToBsonDocument(), pivot, score);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates</typeparam>
        /// <typeparam name="TField">The type of the fields.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to user to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near<TCoordinates, TField>(
            Expression<Func<TDocument, TField>> path,
            GeoJsonPoint<TCoordinates> origin,
            double pivot,
            ScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
            Near(new ExpressionFieldDefinition<TDocument>(path), origin, pivot, score);

        /// <summary>
        /// Creates a search definition that performs search for documents containing an ordered
        /// sequence of terms.
        /// </summary>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="slop">The allowable distance between words in the query phrase.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A phrase search definition.</returns>
        public SearchDefinition<TDocument> Phrase(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            int? slop = null,
            ScoreDefinition<TDocument> score = null) =>
            new PhraseSearchDefinition<TDocument>(query, path, slop, score);

        /// <summary>
        /// Creates a search definition that performs search for documents containing an ordered
        /// sequence of terms.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="slop">The allowable distance between words in the query phrase.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A phrase search definition.</returns>
        public SearchDefinition<TDocument> Phrase<TField>(
            QueryDefinition query,
            Expression<Func<TDocument, TField>> path,
            int? slop = null,
            ScoreDefinition<TDocument> score = null) =>
            Phrase(query, new ExpressionFieldDefinition<TDocument>(path), slop, score);

        /// <summary>
        /// Creates a search definition that queries a combination of indexed fields and values.
        /// </summary>
        /// <param name="defaultPath">The indexed field to search by default.</param>
        /// <param name="query">One or more indexed fields and values to search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A query string search definition.</returns>
        public SearchDefinition<TDocument> QueryString(
            FieldDefinition<TDocument> defaultPath,
            string query,
            ScoreDefinition<TDocument> score = null) =>
            new QueryStringSearchDefinition<TDocument>(defaultPath, query, score);

        /// <summary>
        /// Creates a search definition that queries a combination of indexed fields and values.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="defaultPath">The indexed field to search by default.</param>
        /// <param name="query">One or more indexed fields and values to search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A query string search definition.</returns>
        public SearchDefinition<TDocument> QueryString<TField>(
            Expression<Func<TDocument, TField>> defaultPath,
            string query,
            ScoreDefinition<TDocument> score = null) =>
            QueryString(new ExpressionFieldDefinition<TDocument>(defaultPath), query, score);

        /// <summary>
        /// Creates a search definition that queries for documents where a floating-point
        /// field is in the specified range.
        /// </summary>
        /// <returns>A fluent range interface.</returns>
        public SearchDefinition<TDocument> Range<TField>(
            SearchRange<TField> range,
            Expression<Func<TDocument, TField>> path,
            ScoreDefinition<TDocument> score = null)
            where TField : struct, IComparable<TField> =>
            Range(range, new ExpressionFieldDefinition<TDocument>(path), score);

        /// <summary>
        /// Creates a search definition that queries for documents where a floating-point
        /// field is in the specified range.
        /// </summary>
        /// <returns>A fluent range interface.</returns>
        public SearchDefinition<TDocument> Range<TField>(
            SearchRange<TField> range,
            PathDefinition<TDocument> path,
            ScoreDefinition<TDocument> score = null)
            where TField : struct, IComparable<TField> =>
            new RangeSearchDefinition<TDocument, TField>(range, path, score);

        /// <summary>
        /// Creates a search definition that interprets the query as a regular expression.
        /// </summary>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="allowAnalyzedField">
        /// Must be set to true if the query is run against an analyzed field.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A regular expression search definition.</returns>
        public SearchDefinition<TDocument> Regex(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            bool allowAnalyzedField = false,
            ScoreDefinition<TDocument> score = null)
        {
            return new RegexSearchDefinition<TDocument>(query, path, allowAnalyzedField, score);
        }

        /// <summary>
        /// Creates a search definition that interprets the query as a regular expression.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="allowAnalyzedField">
        /// Must be set to true if the query is run against an analyzed field.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A regular expression search definition.</returns>
        public SearchDefinition<TDocument> Regex<TField>(
            QueryDefinition query,
            Expression<Func<TDocument, TField>> path,
            bool allowAnalyzedField = false,
            ScoreDefinition<TDocument> score = null) =>
            Regex(query, new ExpressionFieldDefinition<TDocument>(path), allowAnalyzedField, score);

        /// <summary>
        /// Creates a search definition that finds text search matches within regions of a text
        /// field.
        /// </summary>
        /// <param name="clause">The span clause.</param>
        /// <returns>A span search definition.</returns>
        public SearchDefinition<TDocument> Span(SpanDefinition<TDocument> clause) =>
            new SpanSearchDefinition<TDocument>(clause);

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

        /// <summary>
        /// Creates a search definition that uses special characters in the search string that can
        /// match any character.
        /// </summary>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="allowAnalyzedField">
        /// Must be set to true if the query is run against an analyzed field.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A wildcard search definition.</returns>
        public SearchDefinition<TDocument> Wildcard(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            bool allowAnalyzedField = false,
            ScoreDefinition<TDocument> score = null) =>
            new WildcardSearchDefinition<TDocument>(query, path, allowAnalyzedField, score);

        /// <summary>
        /// Creates a search definition that uses special characters in the search string that can
        /// match any character.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="allowAnalyzedField">
        /// Must be set to true if the query is run against an analyzed field.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A wildcard search definition.</returns>
        public SearchDefinition<TDocument> Wildcard<TField>(
            QueryDefinition query,
            Expression<Func<TDocument, TField>> path,
            bool allowAnalyzedField = false,
            ScoreDefinition<TDocument> score = null) =>
            Wildcard(query, new ExpressionFieldDefinition<TDocument>(path), allowAnalyzedField, score);
    }
}

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
        /// <param name="path">The indexed field to search.</param>
        /// <param name="query">The query definition specifying the string or strings to search for.</param>
        /// <param name="tokenOrder">The order in which to search for tokens.</param>
        /// <param name="fuzzy">The options for fuzzy search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>An autocomplete search definition.</returns>
        public SearchDefinition<TDocument> Autocomplete(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            SearchAutocompleteTokenOrder tokenOrder = SearchAutocompleteTokenOrder.Any,
            SearchFuzzyOptions fuzzy = null,
            SearchScoreDefinition<TDocument> score = null) =>
                new AutocompleteSearchDefinition<TDocument>(path, query, tokenOrder, fuzzy, score);

        /// <summary>
        /// Creates a search definition that performs a search for a word or phrase that contains
        /// a sequence of characters from an incomplete search string.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="query">The query definition specifying the string or strings to search for.</param>
        /// <param name="tokenOrder">The order in which to search for tokens.</param>
        /// <param name="fuzzy">The options for fuzzy search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>An autocomplete search definition.</returns>
        public SearchDefinition<TDocument> Autocomplete<TField>(
            Expression<Func<TDocument, TField>> path,
            SearchQueryDefinition query,
            SearchAutocompleteTokenOrder tokenOrder = SearchAutocompleteTokenOrder.Any,
            SearchFuzzyOptions fuzzy = null,
            SearchScoreDefinition<TDocument> score = null) =>
                Autocomplete(new ExpressionFieldDefinition<TDocument>(path), query, tokenOrder, fuzzy, score);

        /// <summary>
        /// Creates a builder for a compound search definition.
        /// </summary>
        /// <param name="score">The score modifier.</param>
        /// <returns>A compound search definition builder.</returns>
        public CompoundSearchDefinitionBuilder<TDocument> Compound(SearchScoreDefinition<TDocument> score = null) =>
            new CompoundSearchDefinitionBuilder<TDocument>(score);

        /// <summary>
        /// Creates a search definition that performs a search for documents where
        /// the specified query <paramref name="operator"/> is satisfied from a single element
        /// of an array of embedded documents specified by <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="operator">The operator.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>
        /// An embeddedDocument search definition.
        /// </returns>
        public SearchDefinition<TDocument> EmbeddedDocument<TField>(
            FieldDefinition<TDocument, IEnumerable<TField>> path,
            SearchDefinition<TField> @operator,
            SearchScoreDefinition<TDocument> score = null) =>
                new EmbeddedDocumentSearchDefinition<TDocument, TField>(path, @operator, score);

        /// <summary>
        /// Creates a search definition that performs a search for documents where
        /// the specified query <paramref name="operator"/> is satisfied from a single element
        /// of an array of embedded documents specified by <paramref name="path"/>.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="operator">The operator.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>
        /// An embeddedDocument search definition.
        /// </returns>
        public SearchDefinition<TDocument> EmbeddedDocument<TField>(
            Expression<Func<TDocument, IEnumerable<TField>>> path,
            SearchDefinition<TField> @operator,
            SearchScoreDefinition<TDocument> score = null) =>
                EmbeddedDocument(new ExpressionFieldDefinition<TDocument, IEnumerable<TField>>(path), @operator, score);

        /// <summary>
        /// Creates a search definition that queries for documents where an indexed field is equal
        /// to the specified value.
        /// Supported value types are boolean, numeric, ObjectId and date.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="value">The value to query for.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>An equality search definition.</returns>
        public SearchDefinition<TDocument> Equals<TField>(
            FieldDefinition<TDocument, TField> path,
            TField value,
            SearchScoreDefinition<TDocument> score = null)
            where TField : struct, IComparable<TField> =>
                new EqualsSearchDefinition<TDocument, TField>(path, value, score);

        /// <summary>
        /// Creates a search definition that queries for documents where an indexed field is equal
        /// to the specified value.
        /// Supported value types are boolean, numeric, ObjectId and date.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field to search.</param>
        /// <param name="value">The value to query for.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>An equality search definition.</returns>
        public SearchDefinition<TDocument> Equals<TField>(
            Expression<Func<TDocument, TField>> path,
            TField value,
            SearchScoreDefinition<TDocument> score = null)
            where TField : struct, IComparable<TField> =>
                Equals(new ExpressionFieldDefinition<TDocument, TField>(path), value, score);

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
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="geometry">
        /// GeoJSON object specifying the Polygon, MultiPolygon, or LineString shape or point
        /// to search.
        /// </param>
        /// <param name="relation">Relation of the query shape geometry to the indexed field geometry.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo shape search definition.</returns>
        public SearchDefinition<TDocument> GeoShape<TCoordinates>(
            SearchPathDefinition<TDocument> path,
            GeoShapeRelation relation,
            GeoJsonGeometry<TCoordinates> geometry,
            SearchScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
                new GeoShapeSearchDefinition<TDocument, TCoordinates>(path, relation, geometry, score);

        /// <summary>
        /// Creates a search definition that queries for shapes with a given geometry.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="geometry">
        /// GeoJSON object specifying the Polygon, MultiPolygon, or LineString shape or point
        /// to search.
        /// </param>
        /// <param name="relation">Relation of the query shape geometry to the indexed field geometry.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo shape search definition.</returns>
        public SearchDefinition<TDocument> GeoShape<TCoordinates, TField>(
            Expression<Func<TDocument, TField>> path,
            GeoShapeRelation relation,
            GeoJsonGeometry<TCoordinates> geometry,
            SearchScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
                GeoShape(
                    new ExpressionFieldDefinition<TDocument>(path),
                    relation,
                    geometry,
                    score);

        /// <summary>
        /// Creates a search definition that queries for geographic points within a given
        /// geometry.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="geometry">
        /// GeoJSON object specifying the MultiPolygon or Polygon to search within.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo within search definition.</returns>
        public SearchDefinition<TDocument> GeoWithin<TCoordinates>(
            SearchPathDefinition<TDocument> path,
            GeoJsonGeometry<TCoordinates> geometry,
            SearchScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
                GeoWithin(path, new GeoWithinGeometry<TCoordinates>(geometry), score);

        /// <summary>
        /// Creates a search definition that queries for geographic points within a given
        /// geometry.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="geometry">
        /// GeoJSON object specifying the MultiPolygon or Polygon to search within.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo within search definition.</returns>
        public SearchDefinition<TDocument> GeoWithin<TCoordinates, TField>(
            Expression<Func<TDocument, TField>> path,
            GeoJsonGeometry<TCoordinates> geometry,
            SearchScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
                GeoWithin(path, new GeoWithinGeometry<TCoordinates>(geometry), score);

        /// <summary>
        /// Creates a search definition that queries for geographic points within a given geo object.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="area">Object that specifies the area to search within.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo within search definition.</returns>
        public SearchDefinition<TDocument> GeoWithin<TCoordinates, TField>(
            Expression<Func<TDocument, TField>> path,
            GeoWithinArea<TCoordinates> area,
            SearchScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
                GeoWithin(new ExpressionFieldDefinition<TDocument>(path), area, score);

        /// <summary>
        /// Creates a search definition that queries for geographic points within a given geo object.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="path">Indexed geo type field or fields to search.</param>
        /// <param name="area">Object that specifies the area to search within.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A geo within search definition.</returns>
        public SearchDefinition<TDocument> GeoWithin<TCoordinates>(
            SearchPathDefinition<TDocument> path,
            GeoWithinArea<TCoordinates> area,
            SearchScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
                new GeoWithinSearchDefinition<TDocument, TCoordinates>(path, area, score);

        /// <summary>
        /// Creates a search definition that returns documents similar to the input documents.
        /// </summary>
        /// <typeparam name="TLike">The type of the like documents.</typeparam>
        /// <param name="like">
        /// One or more documents that Atlas Search uses to extract representative terms for.
        /// </param>
        /// <returns>A more like this search definition.</returns>
        public SearchDefinition<TDocument> MoreLikeThis<TLike>(IEnumerable<TLike> like) =>
            new MoreLikeThisSearchDefinition<TDocument, TLike>(like);

        /// <summary>
        /// Creates a search definition that returns documents similar to the input documents.
        /// </summary>
        /// <typeparam name="TLike">The type of the like documents.</typeparam>
        /// <param name="like">
        /// One or more documents that Atlas Search uses to extract representative terms for.
        /// </param>
        /// <returns>A more like this search definition.</returns>
        public SearchDefinition<TDocument> MoreLikeThis<TLike>(params TLike[] like) =>
            MoreLikeThis((IEnumerable<TLike>)like);

        /// <summary>
        /// Creates a search definition that supports querying and scoring numeric and date values.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="origin">The number, date, or geographic point to search near.</param>
        /// <param name="pivot">The value to use to calculate scores of result documents.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A near search definition.</returns>
        public SearchDefinition<TDocument> Near(
            SearchPathDefinition<TDocument> path,
            double origin,
            double pivot,
            SearchScoreDefinition<TDocument> score = null) =>
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
            SearchScoreDefinition<TDocument> score = null) =>
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
            SearchPathDefinition<TDocument> path,
            int origin,
            int pivot,
            SearchScoreDefinition<TDocument> score = null) =>
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
            SearchScoreDefinition<TDocument> score = null) =>
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
            SearchPathDefinition<TDocument> path,
            long origin,
            long pivot,
            SearchScoreDefinition<TDocument> score = null) =>
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
            SearchScoreDefinition<TDocument> score = null) =>
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
            SearchPathDefinition<TDocument> path,
            DateTime origin,
            long pivot,
            SearchScoreDefinition<TDocument> score = null) =>
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
            SearchScoreDefinition<TDocument> score = null) =>
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
            SearchPathDefinition<TDocument> path,
            GeoJsonPoint<TCoordinates> origin,
            double pivot,
            SearchScoreDefinition<TDocument> score = null)
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
            SearchScoreDefinition<TDocument> score = null)
            where TCoordinates : GeoJsonCoordinates =>
                Near(new ExpressionFieldDefinition<TDocument>(path), origin, pivot, score);

        /// <summary>
        /// Creates a search definition that performs search for documents containing an ordered
        /// sequence of terms.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="slop">The allowable distance between words in the query phrase.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A phrase search definition.</returns>
        public SearchDefinition<TDocument> Phrase(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            int? slop = null,
            SearchScoreDefinition<TDocument> score = null) =>
                new PhraseSearchDefinition<TDocument>(path, query, slop, score);

        /// <summary>
        /// Creates a search definition that performs search for documents containing an ordered
        /// sequence of terms.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="slop">The allowable distance between words in the query phrase.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A phrase search definition.</returns>
        public SearchDefinition<TDocument> Phrase<TField>(
            Expression<Func<TDocument, TField>> path,
            SearchQueryDefinition query,
            int? slop = null,
            SearchScoreDefinition<TDocument> score = null) =>
                Phrase(new ExpressionFieldDefinition<TDocument>(path), query, slop, score);

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
            SearchScoreDefinition<TDocument> score = null) =>
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
            SearchScoreDefinition<TDocument> score = null) =>
                QueryString(new ExpressionFieldDefinition<TDocument>(defaultPath), query, score);

        /// <summary>
        /// Creates a search definition that queries for documents where a field is in the specified range.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="range">The field range.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A a range search definition.</returns>
        public SearchDefinition<TDocument> Range<TField>(
            Expression<Func<TDocument, TField>> path,
            SearchRange<TField> range,
            SearchScoreDefinition<TDocument> score = null)
            where TField : struct, IComparable<TField> =>
                Range(new ExpressionFieldDefinition<TDocument>(path), range, score);

        /// <summary>
        /// Creates a search definition that queries for documents where a field is in the specified range.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="range">The field range.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A a range search definition.</returns>
        public SearchDefinition<TDocument> Range<TField>(
            SearchPathDefinition<TDocument> path,
            SearchRange<TField> range,
            SearchScoreDefinition<TDocument> score = null)
            where TField : struct, IComparable<TField> =>
                new RangeSearchDefinition<TDocument, TField>(path, range, score);

        /// <summary>
        /// Creates a search definition that interprets the query as a regular expression.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="allowAnalyzedField">
        /// Must be set to true if the query is run against an analyzed field.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A regular expression search definition.</returns>
        public SearchDefinition<TDocument> Regex(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            bool allowAnalyzedField = false,
            SearchScoreDefinition<TDocument> score = null) =>
                new RegexSearchDefinition<TDocument>(path, query, allowAnalyzedField, score);

        /// <summary>
        /// Creates a search definition that interprets the query as a regular expression.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="allowAnalyzedField">
        /// Must be set to true if the query is run against an analyzed field.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A regular expression search definition.</returns>
        public SearchDefinition<TDocument> Regex<TField>(
            Expression<Func<TDocument, TField>> path,
            SearchQueryDefinition query,
            bool allowAnalyzedField = false,
            SearchScoreDefinition<TDocument> score = null) =>
                Regex(new ExpressionFieldDefinition<TDocument>(path), query, allowAnalyzedField, score);

        /// <summary>
        /// Creates a search definition that finds text search matches within regions of a text
        /// field.
        /// </summary>
        /// <param name="clause">The span clause.</param>
        /// <returns>A span search definition.</returns>
        public SearchDefinition<TDocument> Span(SearchSpanDefinition<TDocument> clause) =>
            new SpanSearchDefinition<TDocument>(clause);

        /// <summary>
        /// Creates a search definition that performs full-text search using the analyzer specified
        /// in the index configuration.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="fuzzy">The options for fuzzy search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A text search definition.</returns>
        public SearchDefinition<TDocument> Text(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            SearchFuzzyOptions fuzzy = null,
            SearchScoreDefinition<TDocument> score = null) =>
                new TextSearchDefinition<TDocument>(path, query, fuzzy, score, null);

        /// <summary>
        /// Creates a search definition that performs full-text search with synonyms using the analyzer specified
        /// in the index configuration.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="synonyms">The name of the synonym mapping definition in the index definition</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A text search definition.</returns>
        public SearchDefinition<TDocument> Text(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            string synonyms,
            SearchScoreDefinition<TDocument> score = null) =>
                new TextSearchDefinition<TDocument>(path, query, null, score, synonyms);

        /// <summary>
        /// Creates a search definition that performs full-text search using the analyzer specified
        /// in the index configuration.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or field to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="fuzzy">The options for fuzzy search.</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A text search definition.</returns>
        public SearchDefinition<TDocument> Text<TField>(
            Expression<Func<TDocument, TField>> path,
            SearchQueryDefinition query,
            SearchFuzzyOptions fuzzy = null,
            SearchScoreDefinition<TDocument> score = null) =>
                Text(new ExpressionFieldDefinition<TDocument>(path), query, fuzzy, score);

        /// <summary>
        /// Creates a search definition that performs full-text search with synonyms using the analyzer specified
        /// in the index configuration.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or field to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="synonyms">The name of the synonym mapping definition in the index definition</param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A text search definition.</returns>
        public SearchDefinition<TDocument> Text<TField>(
            Expression<Func<TDocument, TField>> path,
            SearchQueryDefinition query,
            string synonyms,
            SearchScoreDefinition<TDocument> score = null) =>
                Text(new ExpressionFieldDefinition<TDocument>(path), query, synonyms, score);

        /// <summary>
        /// Creates a search definition that uses special characters in the search string that can
        /// match any character.
        /// </summary>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="allowAnalyzedField">
        /// Must be set to true if the query is run against an analyzed field.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A wildcard search definition.</returns>
        public SearchDefinition<TDocument> Wildcard(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            bool allowAnalyzedField = false,
            SearchScoreDefinition<TDocument> score = null) =>
                new WildcardSearchDefinition<TDocument>(path, query, allowAnalyzedField, score);

        /// <summary>
        /// Creates a search definition that uses special characters in the search string that can
        /// match any character.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="path">The indexed field or fields to search.</param>
        /// <param name="query">The string or strings to search for.</param>
        /// <param name="allowAnalyzedField">
        /// Must be set to true if the query is run against an analyzed field.
        /// </param>
        /// <param name="score">The score modifier.</param>
        /// <returns>A wildcard search definition.</returns>
        public SearchDefinition<TDocument> Wildcard<TField>(
            Expression<Func<TDocument, TField>> path,
            SearchQueryDefinition query,
            bool allowAnalyzedField = false,
            SearchScoreDefinition<TDocument> score = null) =>
                Wildcard(new ExpressionFieldDefinition<TDocument>(path), query, allowAnalyzedField, score);
    }
}

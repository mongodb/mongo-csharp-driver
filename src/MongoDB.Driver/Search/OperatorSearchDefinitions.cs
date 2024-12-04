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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.GeoJsonObjectModel;

namespace MongoDB.Driver.Search
{
    internal sealed class AutocompleteSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly SearchFuzzyOptions _fuzzy;
        private readonly SearchQueryDefinition _query;
        private readonly SearchAutocompleteTokenOrder _tokenOrder;

        public AutocompleteSearchDefinition(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            SearchAutocompleteTokenOrder tokenOrder,
            SearchFuzzyOptions fuzzy,
            SearchScoreDefinition<TDocument> score)
                : base(OperatorType.Autocomplete, path, score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _tokenOrder = tokenOrder;
            _fuzzy = fuzzy;
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
           new()
           {
                { "query", _query.Render() },
                { "tokenOrder", _tokenOrder.ToCamelCase(), _tokenOrder != SearchAutocompleteTokenOrder.Any },
                { "fuzzy", () => _fuzzy.Render(), _fuzzy != null },
           };
    }

    internal sealed class CompoundSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly List<SearchDefinition<TDocument>> _filter;
        private readonly int _minimumShouldMatch;
        private readonly List<SearchDefinition<TDocument>> _must;
        private readonly List<SearchDefinition<TDocument>> _mustNot;
        private readonly List<SearchDefinition<TDocument>> _should;

        public CompoundSearchDefinition(
            List<SearchDefinition<TDocument>> must,
            List<SearchDefinition<TDocument>> mustNot,
            List<SearchDefinition<TDocument>> should,
            List<SearchDefinition<TDocument>> filter,
            int minimumShouldMatch,
            SearchScoreDefinition<TDocument> score)
                : base(OperatorType.Compound, score)
        {
            // This constructor should always be called from the compound search definition builder that ensures the arguments are valid.
            _must = must;
            _mustNot = mustNot;
            _should = should;
            _filter = filter;
            _minimumShouldMatch = minimumShouldMatch;
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args)
        {
            return new()
            {
                { "must", Render(_must), _must != null },
                { "mustNot", Render(_mustNot), _mustNot != null },
                { "should", Render(_should), _should != null },
                { "filter", Render(_filter), _filter != null },
                { "minimumShouldMatch", _minimumShouldMatch, _minimumShouldMatch > 0 },
            };

            Func<BsonArray> Render(List<SearchDefinition<TDocument>> searchDefinitions) =>
                () => new BsonArray(searchDefinitions.Select(clause => clause.Render(args)));
        }
    }

    internal sealed class EmbeddedDocumentSearchDefinition<TDocument, TField> : OperatorSearchDefinition<TDocument>
    {
        private readonly SearchDefinition<TField> _operator;

        public EmbeddedDocumentSearchDefinition(FieldDefinition<TDocument, IEnumerable<TField>> path, SearchDefinition<TField> @operator, SearchScoreDefinition<TDocument> score)
            : base(OperatorType.EmbeddedDocument,
                  new SingleSearchPathDefinition<TDocument>(path),
                  score)
        {
            _operator = Ensure.IsNotNull(@operator, nameof(@operator));
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args)
        {
            // Add base path to all nested operator paths
            var pathPrefix = _path.Render(args).AsString;
            var newArgs = args
                .WithNewDocumentType(args.SerializerRegistry.GetSerializer<TField>())
                with { PathRenderArgs = new(pathPrefix) };

            return new("operator", _operator.Render(newArgs));
        }
    }

    internal sealed class EqualsSearchDefinition<TDocument, TField> : OperatorSearchDefinition<TDocument>
    {
        private readonly BsonValue _value;

        public EqualsSearchDefinition(FieldDefinition<TDocument> path, TField value, SearchScoreDefinition<TDocument> score)
            : base(OperatorType.Equals, path, score)
        {
            _value = ToBsonValue(value);
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            new("value", _value);

        private static BsonValue ToBsonValue(TField value) =>
            value switch
            {
                bool v => (BsonBoolean)v,
                sbyte v => (BsonInt32)v,
                byte v => (BsonInt32)v,
                short v => (BsonInt32)v,
                ushort v => (BsonInt32)v,
                int v => (BsonInt32)v,
                uint v => (BsonInt64)v,
                long v => (BsonInt64)v,
                float v => (BsonDouble)v,
                double v => (BsonDouble)v,
                DateTime v => (BsonDateTime)v,
                DateTimeOffset v => (BsonDateTime)v.UtcDateTime,
                ObjectId v => (BsonObjectId)v,
                Guid v => new BsonBinaryData(v, GuidRepresentation.Standard),
                string v => (BsonString)v,
                null => BsonNull.Value,
                _ => throw new InvalidCastException()
            };
    }

    internal sealed class ExistsSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        public ExistsSearchDefinition(FieldDefinition<TDocument> path)
            : base(OperatorType.Exists, path, null)
        {
        }
    }

    internal sealed class FacetSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly SearchFacet<TDocument>[] _facets;
        private readonly SearchDefinition<TDocument> _operator;

        public FacetSearchDefinition(SearchDefinition<TDocument> @operator, IEnumerable<SearchFacet<TDocument>> facets)
            : base(OperatorType.Facet)
        {
            _operator = Ensure.IsNotNull(@operator, nameof(@operator));
            _facets = Ensure.IsNotNull(facets, nameof(facets)).ToArray();
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            new()
            {
                { "operator", _operator.Render(args) },
                { "facets", new BsonDocument(_facets.Select(f => new BsonElement(f.Name, f.Render(args)))) }
            };
    }

    internal sealed class GeoShapeSearchDefinition<TDocument, TCoordinates> : OperatorSearchDefinition<TDocument>
        where TCoordinates : GeoJsonCoordinates
    {
        private readonly GeoJsonGeometry<TCoordinates> _geometry;
        private readonly GeoShapeRelation _relation;

        public GeoShapeSearchDefinition(
            SearchPathDefinition<TDocument> path,
            GeoShapeRelation relation,
            GeoJsonGeometry<TCoordinates> geometry,
            SearchScoreDefinition<TDocument> score)
                : base(OperatorType.GeoShape, path, score)
        {
            _geometry = Ensure.IsNotNull(geometry, nameof(geometry));
            _relation = relation;
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            new()
            {
                { "geometry", _geometry.ToBsonDocument() },
                { "relation", _relation.ToCamelCase() }
            };
    }

    internal sealed class GeoWithinSearchDefinition<TDocument, TCoordinates> : OperatorSearchDefinition<TDocument>
        where TCoordinates : GeoJsonCoordinates
    {
        private readonly GeoWithinArea<TCoordinates> _area;

        public GeoWithinSearchDefinition(
            SearchPathDefinition<TDocument> path,
            GeoWithinArea<TCoordinates> area,
            SearchScoreDefinition<TDocument> score)
                : base(OperatorType.GeoWithin, path, score)
        {
            _area = Ensure.IsNotNull(area, nameof(area));
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            new(_area.Render());
    }

    internal sealed class InSearchDefinition<TDocument, TField> : OperatorSearchDefinition<TDocument>
    {
        private readonly BsonArray _values;

        public InSearchDefinition(
           SearchPathDefinition<TDocument> path,
           IEnumerable<TField> values,
           SearchScoreDefinition<TDocument> score)
                : base(OperatorType.In, path, score)
        {
            Ensure.IsNotNullOrEmpty(values, nameof(values));
            var array = new BsonArray(values.Select(ToBsonValue));

            var bsonType = array[0].GetType();
            _values = Ensure.That(array, arr => arr.All(v => v.GetType() == bsonType), nameof(values), "All values must be of the same type.");
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            new("value", _values);

        private static BsonValue ToBsonValue(TField value) =>
            value switch
            {
                bool v => (BsonBoolean)v,
                sbyte v => (BsonInt32)v,
                byte v => (BsonInt32)v,
                short v => (BsonInt32)v,
                ushort v => (BsonInt32)v,
                int v => (BsonInt32)v,
                uint v => (BsonInt64)v,
                long v => (BsonInt64)v,
                float v => (BsonDouble)v,
                double v => (BsonDouble)v,
                decimal v => (BsonDecimal128)v,
                DateTime v => (BsonDateTime)v,
                DateTimeOffset v => (BsonDateTime)v.UtcDateTime,
                string v => (BsonString)v,
                ObjectId v => (BsonObjectId)v,
                Guid v => new BsonBinaryData(v, GuidRepresentation.Standard),
                _ => throw new InvalidCastException()
            };
    }

    internal sealed class MoreLikeThisSearchDefinition<TDocument, TLike> : OperatorSearchDefinition<TDocument>
    {
        private readonly TLike[] _like;

        public MoreLikeThisSearchDefinition(IEnumerable<TLike> like)
            : base(OperatorType.MoreLikeThis)
        {
            _like = Ensure.IsNotNull(like, nameof(like)).ToArray();
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args)
        {
            var likeSerializer = typeof(TLike) switch
            {
                var t when t == typeof(BsonDocument) => null,
                var t when t == typeof(TDocument) => (IBsonSerializer<TLike>)args.DocumentSerializer,
                _ => args.SerializerRegistry.GetSerializer<TLike>()
            };

            return new("like", new BsonArray(_like.Select(document => document.ToBsonDocument(likeSerializer))));
        }
    }

    internal sealed class NearSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly BsonValue _origin;
        private readonly BsonValue _pivot;

        public NearSearchDefinition(
            SearchPathDefinition<TDocument> path,
            BsonValue origin,
            BsonValue pivot,
            SearchScoreDefinition<TDocument> score = null)
                : base(OperatorType.Near, path, score)
        {
            _origin = origin;
            _pivot = pivot;
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
           new()
           {
                { "origin", _origin },
                { "pivot", _pivot }
           };
    }

    internal sealed class PhraseSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly SearchQueryDefinition _query;
        private readonly int? _slop;

        public PhraseSearchDefinition(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            int? slop,
            SearchScoreDefinition<TDocument> score)
                : base(OperatorType.Phrase, path, score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _slop = slop;
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            new()
            {
                { "query", _query.Render() },
                { "slop", _slop, _slop != null }
            };
    }

    internal sealed class QueryStringSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly SingleSearchPathDefinition<TDocument> _defaultPath;
        private readonly string _query;

        public QueryStringSearchDefinition(FieldDefinition<TDocument> defaultPath, string query, SearchScoreDefinition<TDocument> score)
            : base(OperatorType.QueryString, score)
        {
            _defaultPath = new SingleSearchPathDefinition<TDocument>(defaultPath);
            _query = Ensure.IsNotNull(query, nameof(query));
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            new()
            {
                { "defaultPath", _defaultPath.Render(args) },
                { "query", _query }
            };
    }

    internal sealed class RangeSearchDefinition<TDocument, TField> : OperatorSearchDefinition<TDocument>
        where TField : struct, IComparable<TField>
    {
        private readonly SearchRange<TField> _range;
        private readonly BsonValue _min;
        private readonly BsonValue _max;

        public RangeSearchDefinition(
            SearchPathDefinition<TDocument> path,
            SearchRange<TField> range,
            SearchScoreDefinition<TDocument> score)
                : base(OperatorType.Range, path, score)
        {
            _range = range;
            _min = ToBsonValue(_range.Min);
            _max = ToBsonValue(_range.Max);
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            new()
            {
                { _range.IsMinInclusive ? "gte" : "gt", _min, _min != null },
                { _range.IsMaxInclusive ? "lte" : "lt", _max, _max != null },
            };

        private static BsonValue ToBsonValue(TField? value) =>
            value switch
            {
                sbyte v => (BsonInt32)v,
                byte v => (BsonInt32)v,
                short v => (BsonInt32)v,
                ushort v => (BsonInt32)v,
                int v => (BsonInt32)v,
                uint v => (BsonInt64)v,
                long v => (BsonInt64)v,
                float v => (BsonDouble)v,
                double v => (BsonDouble)v,
                DateTime v => (BsonDateTime)v,
                DateTimeOffset v => (BsonDateTime)v.UtcDateTime,
                null => null,
                _ => throw new InvalidCastException()
            };
    }

    internal sealed class RegexSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly bool _allowAnalyzedField;
        private readonly SearchQueryDefinition _query;

        public RegexSearchDefinition(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            bool allowAnalyzedField,
            SearchScoreDefinition<TDocument> score)
                : base(OperatorType.Regex, path, score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _allowAnalyzedField = allowAnalyzedField;
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            new()
            {
                { "query", _query.Render() },
                { "allowAnalyzedField", _allowAnalyzedField, _allowAnalyzedField },
            };
    }

    internal sealed class SpanSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly SearchSpanDefinition<TDocument> _clause;

        public SpanSearchDefinition(SearchSpanDefinition<TDocument> clause)
            : base(OperatorType.Span)
        {
            _clause = Ensure.IsNotNull(clause, nameof(clause));
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            _clause.Render(args);
    }

    internal sealed class TextSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly SearchFuzzyOptions _fuzzy;
        private readonly SearchQueryDefinition _query;
        private readonly string _synonyms;

        public TextSearchDefinition(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            SearchFuzzyOptions fuzzy,
            SearchScoreDefinition<TDocument> score,
            string synonyms)
                : base(OperatorType.Text, path, score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _fuzzy = fuzzy;
            _synonyms = synonyms;
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
            new()
            {
                { "query", _query.Render() },
                { "fuzzy", () => _fuzzy.Render(), _fuzzy != null },
                { "synonyms", _synonyms, _synonyms != null }
            };
    }

    internal sealed class WildcardSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly bool _allowAnalyzedField;
        private readonly SearchQueryDefinition _query;

        public WildcardSearchDefinition(
            SearchPathDefinition<TDocument> path,
            SearchQueryDefinition query,
            bool allowAnalyzedField,
            SearchScoreDefinition<TDocument> score)
                : base(OperatorType.Wildcard, path, score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _allowAnalyzedField = allowAnalyzedField;
        }

        private protected override BsonDocument RenderArguments(RenderArgs<TDocument> args) =>
           new()
           {
                { "query", _query.Render() },
                { "allowAnalyzedField", _allowAnalyzedField, _allowAnalyzedField },
           };
    }
}

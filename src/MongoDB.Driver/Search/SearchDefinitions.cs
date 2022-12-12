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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.GeoJsonObjectModel;

namespace MongoDB.Driver.Search
{
    internal sealed class AutocompleteSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly QueryDefinition _query;
        private readonly AutocompleteTokenOrder _tokenOrder;
        private readonly FuzzyOptions _fuzzy;

        public AutocompleteSearchDefinition(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            AutocompleteTokenOrder tokenOrder,
            FuzzyOptions fuzzy,
            ScoreDefinition<TDocument> score)
            : base(OperatorType.Autocomplete, path, score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _tokenOrder = tokenOrder;
            _fuzzy = fuzzy;
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
           new()
           {
                { "query", _query.Render() },
                { "tokenOrder", _tokenOrder.ToCamelCase(), _tokenOrder == AutocompleteTokenOrder.Sequential },
                { "fuzzy", () => _fuzzy.Render(), _fuzzy != null },
           };
    }

    internal sealed class CompoundSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly List<SearchDefinition<TDocument>> _must;
        private readonly List<SearchDefinition<TDocument>> _mustNot;
        private readonly List<SearchDefinition<TDocument>> _should;
        private readonly List<SearchDefinition<TDocument>> _filter;
        private readonly int _minimumShouldMatch;

        public CompoundSearchDefinition(
            List<SearchDefinition<TDocument>> must,
            List<SearchDefinition<TDocument>> mustNot,
            List<SearchDefinition<TDocument>> should,
            List<SearchDefinition<TDocument>> filter,
            int minimumShouldMatch) : base(OperatorType.Compound)
        {
            // This constructor should always be called from a fluent interface that
            // ensures that the parameters are not null and copies the lists, so there is
            // no need to do any of that here.
            _must = must;
            _mustNot = mustNot;
            _should = should;
            _filter = filter;
            _minimumShouldMatch = minimumShouldMatch;
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
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
               () => new BsonArray(searchDefinitions.Select(clause => clause.Render(documentSerializer, serializerRegistry)));
        }
    }

    internal sealed class EqualsSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly BsonValue _value;

        public EqualsSearchDefinition(FieldDefinition<TDocument> path, BsonValue value, ScoreDefinition<TDocument> score)
            : base(OperatorType.Equals, path, score)
        {
            _value = value;
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            new("value", _value);
    }

    internal sealed class ExistsSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        public ExistsSearchDefinition(FieldDefinition<TDocument> path) : base(OperatorType.Exists, path, null)
        {
        }
    }

    internal sealed class FacetSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly SearchDefinition<TDocument> _operator;
        private readonly IEnumerable<SearchFacet<TDocument>> _facets;

        public FacetSearchDefinition(SearchDefinition<TDocument> @operator, IEnumerable<SearchFacet<TDocument>> facets)
            : base(OperatorType.Facet)
        {
            _operator = Ensure.IsNotNull(@operator, nameof(@operator));
            _facets = Ensure.IsNotNull(facets, nameof(facets));
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            new()
            {
                { "operator", _operator.Render(documentSerializer, serializerRegistry) },
                { "facets", new BsonDocument(_facets.Select(f => new BsonElement(f.Name, f.Render(documentSerializer, serializerRegistry)))) }
            };
    }

    internal sealed class GeoShapeSearchDefinition<TDocument, TCoordinates> : OperatorSearchDefinition<TDocument>
        where TCoordinates : GeoJsonCoordinates
    {
        private readonly GeoJsonGeometry<TCoordinates> _geometry;
        private readonly GeoShapeRelation _relation;

        public GeoShapeSearchDefinition(
            GeoJsonGeometry<TCoordinates> geometry,
            PathDefinition<TDocument> path,
            GeoShapeRelation relation,
            ScoreDefinition<TDocument> score)
            : base(OperatorType.GeoShape, path, score)
        {
            _geometry = Ensure.IsNotNull(geometry, nameof(geometry));
            _relation = relation;
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            new()
            {
                { "geometry", _geometry.ToBsonDocument() },
                { "relation", _relation.ToCamelCase() }
            };
    }

    internal sealed class GeoWithinSearchDefinition<TDocument, TCoordinates> : OperatorSearchDefinition<TDocument>
        where TCoordinates : GeoJsonCoordinates
    {
        private readonly GeoWithin<TCoordinates> _geoWithinQuery;

        public GeoWithinSearchDefinition(
            GeoWithin<TCoordinates> geoWithinQuery,
            PathDefinition<TDocument> path,
            ScoreDefinition<TDocument> score)
            : base(OperatorType.GeoWithin, path, score)
        {
            _geoWithinQuery = Ensure.IsNotNull(geoWithinQuery, nameof(geoWithinQuery));
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            new(_geoWithinQuery.Render());
    }

    internal sealed class MoreLikeThisSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly IEnumerable<TDocument> _like;

        public MoreLikeThisSearchDefinition(IEnumerable<TDocument> like) : base(OperatorType.MoreLikeThis)
        {
            _like = Ensure.IsNotNull(like, nameof(like));
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            new("like", new BsonArray(_like.Select(e => e.ToBsonDocument(documentSerializer))));
    }

    internal sealed class NearSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly BsonValue _origin;
        private readonly BsonValue _pivot;

        public NearSearchDefinition(
            PathDefinition<TDocument> path,
            BsonValue origin,
            BsonValue pivot,
            ScoreDefinition<TDocument> score = null)
            : base(OperatorType.Near, path, score)
        {
            _origin = origin;
            _pivot = pivot;
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
           new()
           {
                { "origin", _origin },
                { "pivot", _pivot }
           };
    }

    internal sealed class PhraseSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly QueryDefinition _query;
        private readonly int? _slop;

        public PhraseSearchDefinition(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            int? slop,
            ScoreDefinition<TDocument> score)
            : base(OperatorType.Phrase, path, score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _slop = slop;
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            new()
            {
                { "query", _query.Render() },
                { "slop", _slop, _slop != null }
            };
    }

    internal sealed class QueryStringSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly FieldDefinition<TDocument> _defaultPath;
        private readonly string _query;

        public QueryStringSearchDefinition(FieldDefinition<TDocument> defaultPath, string query, ScoreDefinition<TDocument> score)
            : base(OperatorType.QueryString, score)
        {
            _defaultPath = Ensure.IsNotNull(defaultPath, nameof(defaultPath));
            _query = Ensure.IsNotNull(query, nameof(query));
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            new()
            {
                { "defaultPath", _defaultPath.Render(documentSerializer, serializerRegistry).FieldName },
                { "query", _query }
            };
    }

    internal sealed class RangeSearchDefinition<TDocument, TField> : OperatorSearchDefinition<TDocument>
        where TField : struct, IComparable<TField>
    {
        private readonly SearchRange<TField> _range;

        public RangeSearchDefinition(
            SearchRange<TField> range,
            PathDefinition<TDocument> path,
            ScoreDefinition<TDocument> score)
             : base(OperatorType.Range, path, score)
        {
            _range = range;
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            new()
            {
                { _range.IsMinInclusive ? "gte" : "gt", () => ToBsonValue(_range.Min.Value), _range.Min != null },
                { _range.IsMaxInclusive ? "lte" : "lt", () => ToBsonValue(_range.Max.Value), _range.Max != null },
            };

        private static BsonValue ToBsonValue(TField value) =>
            value switch
            {
                sbyte int8Value => (BsonValue)int8Value,
                byte uint8Value => (BsonValue)uint8Value,
                short int16Value => (BsonValue)int16Value,
                ushort uint16Value => (BsonValue)uint16Value,
                int int32Value => (BsonValue)int32Value,
                uint uint32Value => (BsonValue)uint32Value,
                long int64Value => (BsonValue)int64Value,
                double doubleValue => (BsonValue)doubleValue,
                DateTime dateTimeValue => (BsonValue)dateTimeValue,
                _ => throw new InvalidCastException()
            };
    }

    internal sealed class RegexSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly QueryDefinition _query;
        private readonly bool _allowAnalyzedField;

        public RegexSearchDefinition(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            bool allowAnalyzedField,
            ScoreDefinition<TDocument> score) : base(OperatorType.Regex, path, score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _allowAnalyzedField = allowAnalyzedField;
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            new()
            {
                { "query", _query.Render() },
                { "allowAnalyzedField", _allowAnalyzedField, _allowAnalyzedField },
            };
    }

    internal sealed class SpanSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly SpanDefinition<TDocument> _clause;

        public SpanSearchDefinition(SpanDefinition<TDocument> clause) : base(OperatorType.Span)
        {
            _clause = Ensure.IsNotNull(clause, nameof(clause));
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            _clause.Render(documentSerializer, serializerRegistry);
    }

    internal sealed class TextSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly QueryDefinition _query;
        private readonly FuzzyOptions _fuzzy;

        public TextSearchDefinition(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            FuzzyOptions fuzzy,
            ScoreDefinition<TDocument> score)
            : base(OperatorType.Text, path, score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _fuzzy = fuzzy;
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
            new()
            {
                { "query", _query.Render() },
                { "fuzzy", () => _fuzzy.Render(), _fuzzy != null },
            };
    }

    internal sealed class WildcardSearchDefinition<TDocument> : OperatorSearchDefinition<TDocument>
    {
        private readonly QueryDefinition _query;
        private readonly bool _allowAnalyzedField;

        public WildcardSearchDefinition(
            QueryDefinition query,
            PathDefinition<TDocument> path,
            bool allowAnalyzedField,
            ScoreDefinition<TDocument> score)
            : base(OperatorType.Wildcard, path, score)
        {
            _query = Ensure.IsNotNull(query, nameof(query));
            _allowAnalyzedField = allowAnalyzedField;
        }

        private protected override BsonDocument RenderOperator(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry) =>
           new()
           {
                { "query", _query.Render() },
                { "allowAnalyzedField", _allowAnalyzedField, _allowAnalyzedField },
           };
    }
}

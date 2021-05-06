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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal sealed class AstGeoNearStage : AstStage
    {
        private readonly string _distanceField;
        private readonly double? _distanceMultiplier;
        private readonly string _includeLocs;
        private readonly string _key;
        private readonly double? _maxDistance;
        private readonly double? _minDistance;
        private readonly BsonValue _near;
        private readonly BsonDocument _query;
        private readonly bool? _spherical;
        private readonly bool? _uniqueDocs;

        public AstGeoNearStage(
            BsonValue near,
            string distanceField,
            bool? spherical,
            double? maxDistance,
            BsonDocument query,
            double? distanceMultiplier,
            string includeLocs,
            bool? uniqueDocs,
            double? minDistance,
            string key)
        {
            _near = Ensure.IsNotNull(near, nameof(near));
            _distanceField = Ensure.IsNotNull(distanceField, nameof(distanceField));
            _spherical = spherical;
            _maxDistance = maxDistance;
            _query = query;
            _distanceMultiplier = distanceMultiplier;
            _includeLocs = includeLocs;
            _uniqueDocs = uniqueDocs;
            _minDistance = minDistance;
            _key = key;
        }

        public string DistanceField => _distanceField;
        public double? DistanceMultiplier => _distanceMultiplier;
        public string IncludeLocs => _includeLocs;
        public string Key => _key;
        public double? MaxDistance => _maxDistance;
        public double? MinDistance => _minDistance;
        public BsonValue Near => _near;
        public override AstNodeType NodeType => AstNodeType.GeoNearStage;
        public BsonDocument Query => _query;
        public bool? Spherical => _spherical;
        public bool? UniqueDocs => _uniqueDocs;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$geoNear", new BsonDocument
                    {
                        { "near", _near },
                        { "distanceField", _distanceField },
                        { "spherical", () => _spherical.Value, _spherical.HasValue },
                        { "maxDistance", () => _maxDistance.Value, _maxDistance.HasValue },
                        { "query", _query, _query != null },
                        { "distanceMultipler", () => _distanceMultiplier.Value, _distanceMultiplier.HasValue },
                        { "includeLocs", _includeLocs, _includeLocs != null },
                        { "uniqueDocs", () => _uniqueDocs.Value, _uniqueDocs.HasValue },
                        { "minDistance", () => _minDistance.Value, _minDistance.HasValue },
                        { "key", _key, _key != null }
                    }
                }
            };
        }
    }
}

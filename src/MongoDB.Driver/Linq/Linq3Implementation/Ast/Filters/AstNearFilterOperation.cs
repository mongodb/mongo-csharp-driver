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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters
{
    internal sealed class AstNearFilterOperation : AstFilterOperation
    {
        private readonly BsonDocument _geometry;
        private readonly BsonValue _maxDistance;
        private readonly BsonValue _minDistance;

        public AstNearFilterOperation(
            BsonDocument geometry,
            BsonValue maxDistance = default,
            BsonValue minDistance = default)
        {
            _geometry = Ensure.IsNotNull(geometry, nameof(geometry));
            _maxDistance = maxDistance; // optional
            _minDistance = minDistance; // optional
        }

        public BsonDocument Geometry => _geometry;
        public override AstNodeType NodeType => AstNodeType.NearFilterOperation;
        public BsonValue MaxDistance => _maxDistance;
        public BsonValue MinDistance => _minDistance;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$near", new BsonDocument
                    {
                        { "$geometry", _geometry },
                        { "$maxDistance", _maxDistance, _maxDistance != default },
                        { "$minDistance", _minDistance, _minDistance != default }
                    }
                }
            };
        }
    }
}

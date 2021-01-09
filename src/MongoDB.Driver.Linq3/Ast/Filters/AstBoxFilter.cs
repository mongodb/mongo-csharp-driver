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

namespace MongoDB.Driver.Linq3.Ast.Filters
{
    public sealed class AstBoxFilter : AstFilter
    {
        private readonly BsonArray _bottomLeftCoordinates;
        private readonly AstFilterField _field;
        private readonly BsonArray _upperRightCoordinates;

        public AstBoxFilter(AstFilterField field, BsonArray bottomLeftCoordinates, BsonArray upperRightCoordinates)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _bottomLeftCoordinates = Ensure.IsNotNull(bottomLeftCoordinates, nameof(bottomLeftCoordinates));
            _upperRightCoordinates = Ensure.IsNotNull(upperRightCoordinates, nameof(upperRightCoordinates));
        }

        public BsonArray BottomLeftCoordinates => _bottomLeftCoordinates;
        public AstFilterField Field => _field;
        public override AstNodeType NodeType => AstNodeType.BoxFilter;
        public BsonArray UpperRightCoordinates => _upperRightCoordinates;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { _field.Path, new BsonDocument
                    {
                        { "$geoWithin", new BsonDocument
                            {
                                { "$box", new BsonArray
                                    {
                                        _bottomLeftCoordinates,
                                        _upperRightCoordinates,
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}

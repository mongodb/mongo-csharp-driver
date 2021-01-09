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
    public sealed class AstModFilter : AstFilter
    {
        private readonly BsonValue _divisor;
        private readonly AstFilterField _field;
        private readonly BsonValue _remainder;

        public AstModFilter(AstFilterField field, BsonValue divisor, BsonValue remainder)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _divisor = Ensure.IsNotNull(divisor, nameof(divisor));
            _remainder = Ensure.IsNotNull(remainder, nameof(remainder));
        }

        public BsonValue Divisor => _divisor;
        public AstFilterField Field => _field;
        public override AstNodeType NodeType => AstNodeType.ModFilter;
        public BsonValue Remainder => _remainder;

        public override BsonValue Render()
        {
            return new BsonDocument(_field.Path, new BsonDocument("$mod", new BsonArray { _divisor, _remainder }));
        }
    }
}

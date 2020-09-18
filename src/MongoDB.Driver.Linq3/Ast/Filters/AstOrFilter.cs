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
    public sealed class AstOrFilter : AstFilter
    {
        private readonly AstFilter _arg1;
        private readonly AstFilter _arg2;

        public AstOrFilter(AstFilter arg1, AstFilter arg2)
        {
            _arg1 = Ensure.IsNotNull(arg1, nameof(arg1));
            _arg2 = Ensure.IsNotNull(arg2, nameof(arg2));
        }

        public AstFilter Arg1 => _arg1;
        public AstFilter Arg2 => _arg2;
        public override AstNodeType NodeType => AstNodeType.OrFilter;

        public override BsonValue Render()
        {
            return new BsonDocument("$or", new BsonArray { _arg1.Render(), _arg2.Render() });
        }
    }
}

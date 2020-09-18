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

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    public sealed class AstFilterExpression : AstExpression
    {
        private readonly string _as;
        private readonly AstExpression _cond;
        private readonly AstExpression _input;

        public AstFilterExpression(
            AstExpression input,
            AstExpression cond,
            string @as = null)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _cond = Ensure.IsNotNull(cond, nameof(cond));
            _as = @as;
        }

        public string As => _as;
        public AstExpression Cond => _cond;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.FilterExpression;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$filter", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "as", _as, _as != null },
                        { "cond", _cond.Render() }
                    }
                }
            };
        }
    }
}

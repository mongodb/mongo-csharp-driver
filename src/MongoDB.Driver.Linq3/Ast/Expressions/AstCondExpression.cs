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
    public sealed class AstCondExpression : AstExpression
    {
        private readonly AstExpression _else;
        private readonly AstExpression _if;
        private readonly AstExpression _then;

        public AstCondExpression(AstExpression @if, AstExpression @then, AstExpression @else)
        {
            _if = Ensure.IsNotNull(@if, nameof(@if));
            _then =Ensure.IsNotNull(@then, nameof(@then));
            _else = Ensure.IsNotNull(@else, nameof(@else));
        }

        public AstExpression Else => _else;
        public AstExpression If => _if;
        public override AstNodeType NodeType => AstNodeType.CondExpression;
        public AstExpression Then => _then;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$cond", new BsonDocument
                    {
                        { "if", _if.Render() },
                        { "then", _then.Render() },
                        { "else", _else.Render() }
                    }
                }
            };
        }
    }
}

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
    internal sealed class AstRTrimExpression : AstExpression
    {
        private readonly AstExpression _chars;
        private readonly AstExpression _input;

        public AstRTrimExpression(
            AstExpression input,
            AstExpression chars = null)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _chars = chars;
        }

        public AstExpression Chars => _chars;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.RTrimExpression;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$rtrim", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "chars", () => _chars.Render(), _chars != null }
                    }
                }
            };
        }
    }
}

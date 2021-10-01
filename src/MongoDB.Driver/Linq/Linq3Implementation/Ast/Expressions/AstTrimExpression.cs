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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstTrimExpression : AstExpression
    {
        private readonly AstExpression _chars;
        private readonly AstExpression _input;

        public AstTrimExpression(
            AstExpression input,
            AstExpression chars = null)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _chars = chars;
        }

        public AstExpression Chars => _chars;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.TrimExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitTrimExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$trim", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "chars", () => _chars.Render(), _chars != null }
                    }
                }
            };
        }

        public AstTrimExpression Update(
            AstExpression input,
            AstExpression chars)
        {
            if (input == _input && chars == _chars)
            {
                return this;
            }

            return new AstTrimExpression(input, chars);
        }
    }
}

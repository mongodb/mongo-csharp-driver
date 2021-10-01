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
    internal sealed class AstReplaceAllExpression : AstExpression
    {
        private readonly AstExpression _find;
        private readonly AstExpression _input;
        private readonly AstExpression _replacement;

        public AstReplaceAllExpression(
            AstExpression input,
            AstExpression find,
            AstExpression replacement)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _find = Ensure.IsNotNull(find, nameof(find));
            _replacement = Ensure.IsNotNull(replacement, nameof(replacement));
        }

        public AstExpression Find => _find;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.ReplaceAllExpression;
        public AstExpression Replacement => _replacement;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitReplaceAllExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$replaceAll", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "find", _find.Render() },
                        { "replacement", _replacement.Render() }
                    }
                }
            };
        }

        public AstReplaceAllExpression Update(
            AstExpression input,
            AstExpression find,
            AstExpression replacement)
        {
            if (input == _input && find == _find && replacement == _replacement)
            {
                return this;
            }

            return new AstReplaceAllExpression(input, find, replacement);
        }
    }
}

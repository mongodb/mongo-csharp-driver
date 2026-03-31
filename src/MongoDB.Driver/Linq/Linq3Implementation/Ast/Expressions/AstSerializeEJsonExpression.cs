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
    internal sealed class AstSerializeEJsonExpression : AstExpression
    {
        private readonly AstExpression _input;
        private readonly AstExpression _onError;
        private readonly AstExpression _relaxed;

        public AstSerializeEJsonExpression(
            AstExpression input,
            AstExpression relaxed = null,
            AstExpression onError = null)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _relaxed = relaxed;
            _onError = onError;
        }

        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.SerializeEJsonExpression;
        public AstExpression OnError => _onError;
        public AstExpression Relaxed => _relaxed;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitSerializeEJsonExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$serializeEJSON", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "relaxed", () => _relaxed.Render(), _relaxed != null },
                        { "onError", () => _onError.Render(), _onError != null }
                    }
                }
            };
        }

        public AstSerializeEJsonExpression Update(
            AstExpression input,
            AstExpression relaxed,
            AstExpression onError)
        {
            if (input == _input && relaxed == _relaxed && onError == _onError)
            {
                return this;
            }

            return new AstSerializeEJsonExpression(input, relaxed, onError);
        }
    }
}

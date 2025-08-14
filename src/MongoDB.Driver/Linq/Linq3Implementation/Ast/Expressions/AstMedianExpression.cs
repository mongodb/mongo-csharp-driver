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
    internal sealed class AstMedianExpression : AstExpression
    {
        private readonly AstExpression _input;

        public AstMedianExpression(AstExpression input)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
        }

        public AstExpression Input => _input;

        public override AstNodeType NodeType => AstNodeType.MedianExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitMedianExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                {
                    "$median", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "method", "approximate" } // server requires this parameter but currently only allows this value
                    }
                }
            };
        }

        public AstMedianExpression Update(AstExpression input)
        {
            if (input == _input)
            {
                return this;
            }
            return new AstMedianExpression(input);
        }
    }
}
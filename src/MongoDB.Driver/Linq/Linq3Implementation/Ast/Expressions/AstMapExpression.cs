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
    internal sealed class AstMapExpression : AstExpression
    {
        private readonly AstVarExpression _as;
        private readonly AstExpression _in;
        private readonly AstExpression _input;

        public AstMapExpression(
            AstExpression input,
            AstVarExpression @as,
            AstExpression @in)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _as = @as;
            _in = Ensure.IsNotNull(@in, nameof(@in));
        }

        public AstVarExpression As => _as;
        public new AstExpression In => _in;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.MapExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitMapExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$map", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "as", _as?.Name, _as != null },
                        { "in", _in.Render() }
                    }
                }
            };
        }

        public AstMapExpression Update(
            AstExpression input,
            AstVarExpression @as,
            AstExpression @in)
        {
            if (input == _input && @as == _as && @in == _in)
            {
                return this;
            }

            return new AstMapExpression(input, @as, @in);
        }
    }
}

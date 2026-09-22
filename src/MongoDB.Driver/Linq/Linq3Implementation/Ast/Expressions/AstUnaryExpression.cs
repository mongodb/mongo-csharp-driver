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
    internal sealed class AstUnaryExpression : AstExpression
    {
        private readonly AstExpression _arg;
        private readonly AstUnaryOperator _operator;

        public AstUnaryExpression(AstUnaryOperator @operator, AstExpression arg)
        {
            _operator = @operator;
            _arg = Ensure.IsNotNull(arg, nameof(arg));
        }

        public AstExpression Arg => _arg;
        public override AstNodeType NodeType => AstNodeType.UnaryExpression;
        public AstUnaryOperator Operator => _operator;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitUnaryExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument(_operator.Render(), RenderArg());
        }

        public AstUnaryExpression Update(AstExpression arg)
        {
            if (arg == _arg)
            {
                return this;
            }

            return new AstUnaryExpression(_operator, arg);
        }

        private BsonValue RenderArg()
        {
            // $literal takes its operand verbatim, so a constant arg renders as its raw value. otherwise
            // AstConstantExpression would quote the value itself and produce a doubly quoted
            // { $literal : { $literal : ... } }, and the array wrapping below would nest an array operand.
            if (_operator == AstUnaryOperator.Literal && _arg is AstConstantExpression constantArg)
            {
                return constantArg.Value;
            }

            var rendered = _arg.Render();
            if (rendered.IsBsonArray)
            {
                rendered = new BsonArray { rendered };
            }
            return rendered;
        }
    }
}
